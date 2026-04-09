using BasicFleetServer.Utils;
using FleetServerUtils;
using System.Data;
using static BasicFleetServer.Operation.MatchMakingOperator;
using static BasicFleetServer.Operation.FleetServerSocket;
using static BasicFleetServer.Utils.AsyncEventSystem;

namespace BasicFleetServer.Operation
{
    internal class Data_Manager : IAsyncDisposable
    {
        private const int MinPlayersToStartMatch = 2;

        private const int MaxTeamCount = 4;

        private FleetApplicationData fleetAppdata;

        public DataBaseManager dbManager;

        // MatchMaking Data.
        private MatchMakingData matchMakingData;

        // MatchMaking Operator.
        private MatchMakingOperator matchMakingOperator;

        // EVENTS
        public static event AsyncEventHandler<string>? DropUserClientEvent;

        public static event AsyncEventHandler<int>? DropServerClientEvent;

        // Internal Data.
        private int ServerCounter => Math.Max(matchMakingData.ALL_ConnectedGameServers.Count, matchMakingData.GameServerSpinUpPool.Count);

        private string LocalIP;

        public Data_Manager(FleetApplicationData fleetAppdata)
        {
            // Event Subscriptions.
            newUserConnectEvent += RegisterNewUser;
            newGameServerConnectEvent += RegisterNewServer;
            userDisconnectEvent += UnRegisterUser;
            gameServerDisconnectEvent += GameServerDisconnectHandle;
            CreateServerEvent += GenerateGameServer;

            LooseMatchMakingTimer.Instance.IntervalElapsedEvent += LoosenMMR;

            // Data Initialization.
            this.fleetAppdata = fleetAppdata;
            LocalIP = UtilsForIP.GetActiveLanIP()!;

            if (LocalIP == null)
            {
                throw new NoNullAllowedException($"Failed To fetch LocalIP address!");
            }

            dbManager = new DataBaseManager(fleetAppdata.databasePath);
            matchMakingData = new MatchMakingData();
            matchMakingOperator = new MatchMakingOperator
                (
                    matchMakingData,
                    MinPlayersToStartMatch,
                    MaxTeamCount
                );
        }
        public async Task RegisterNewUser(object _, string[] eventData)
        {
            string authID = eventData[0];
            string UserName = eventData[1];
            MM_GameType GameTypePref;


            if (!int.TryParse(eventData[2], out int gamePrefInt))
            {
                await InvokeDropUserEvent(authID);
                return;
            }

            try
            {
                GameTypePref = Enum.GetValues<MM_GameType>()[gamePrefInt];
            }
            catch (IndexOutOfRangeException)
            {
                await InvokeDropUserEvent(authID);
                return;
            }

            MM_User connectedUser = await dbManager.ReadPlayerInfoByAuthID(authID, UserName, GameTypePref);
            matchMakingData.ALL_ConnectedUsers[authID] = connectedUser;

            // Handle Banned Users.
            if (connectedUser.IsBanned)
            {
                await matchMakingOperator.InvokeUserMessageEvent
                    (
                        [connectedUser.authID],
                        "BANNED",
                        ["INF"]
                    );
                await InvokeDropUserEvent(authID);
                return;
            }
            
            // Attempting game assignment for the user.
            await matchMakingOperator.MatchMakingUserAssignmentAsync(connectedUser);
        }

        private async Task RegisterNewServer(object _, int GameServerID)
        {
            // Pop a server from the spin up pool ( its ready ).
            if (!matchMakingData.GameServerSpinUpPool.Remove(
                GameServerID,
                out GameServerInstance? gameServerInstance
                ))
            {
                Console.WriteLine("\n[!@!] Unrecognised server tried to add itself to fleet, dropped.");
                await InvokeDropServerEvent(GameServerID);
                return;
            }

            matchMakingData.ALL_ConnectedGameServers[GameServerID] = gameServerInstance;
            await matchMakingOperator.MatchMakingServerAssignmentAsync(gameServerInstance);
        }

        private void UnRegisterUser(string authID)
        {
            if (matchMakingData.ALL_ConnectedUsers.TryGetValue(authID, out MM_User? user))
            {
                if (matchMakingData.InTransit.Remove(user))
                {
                    return; // User was in transit, so we just remove them from the in transit hashSet and exit without removing them from anywhere.
                }
                matchMakingOperator.RemoveUserFromMatchMaking(user);
                matchMakingData.ALL_ConnectedUsers.Remove(authID);
            }
        }

        private async void GameServerDisconnectHandle(int ID, string[] args)
        {
            if (args.Length != 0)
            {
                string reason = args[1];
                switch (reason) 
                {
                    case "WIN":
                        {
                            string winnerAuthID = args[2];

                            if (matchMakingData.ALL_ConnectedUsers.TryGetValue(winnerAuthID, out MM_User? user))
                            {
                                await dbManager.UpdatePlayerMMR(winnerAuthID, MatchMakingOperator.IncreaseMMR_FromWin);
                            }
                            break;
                        }
                    default:
                        {
                            break;
                        }
                }
            }
            UnRegisterServer(ID);
        }

        private void UnRegisterServer(int ID)
        {
            if (matchMakingData.ALL_ConnectedGameServers.TryGetValue(ID, out GameServerInstance? server))
            {
                int ServerMMR = server.GAME_MMR;

                if (matchMakingData.ActiveGameServerRoomsSolos.TryGetValue(ServerMMR, out HashSet<GameServerInstance>? ActiveGameServerList))
                {
                    if (ActiveGameServerList.Remove(server)) 
                    {
                        foreach (MM_User user in server.Players)
                        {
                            matchMakingData.ALL_ConnectedUsers.Remove(user.authID);
                        }
                    }
                }
                matchMakingData.ALL_ConnectedGameServers.Remove(ID);
            }
        }

        public void GenerateGameServer(int MMR, MM_GameType queueType, HashSet<MM_User> waitingRoom)
        {
            if (ServerCounter < fleetAppdata.maxServerCount)
            {
                int serverID = ServerCounter;

                if (matchMakingData.GameServerSpinUpPool.ContainsKey(serverID) || matchMakingData.ALL_ConnectedGameServers.ContainsKey(serverID))
                {
                    Console.WriteLine($"[!@!] Error: Server with ID {serverID} already exists in the system, cannot generate new server instance for MMR Group : {MMR} with ID {serverID}");
                    return;
                }

                GameServerInstance serverInstance = new GameServerInstance(
                    serverID,
                    MMR,
                    LocalIP,
                    7777 + ServerCounter,
                    fleetAppdata.gameServerPath,
                    queueType
                );
                serverInstance.StartSelf();
                matchMakingData.GameServerSpinUpPool[serverInstance.GameServerID] = serverInstance;
            }
            else
            {
                Console.WriteLine($"[!@!] Error: Max server count reached, cannot generate new server instance for MMR Group : {MMR}");
            }
        }


        private async Task LoosenMMR(object _)
        {
            Dictionary<int, HashSet<MM_User>> loosenWaitingRoomsSolos = LoosenWaitingRooms(matchMakingData.WaitingRoomsSolo);
            Dictionary<int, HashSet<MM_User>> loosenWaitingRoomsTeams = LoosenWaitingRooms(matchMakingData.WaitingRoomsTeams);
            
            // Override the current waiting rooms with the loosened copy,
            // this way we can avoid modifying the waiting rooms while the matchmaking operator is trying to assign players to servers.

            matchMakingData.WaitingRoomsSolo = loosenWaitingRoomsSolos;
            matchMakingData.WaitingRoomsTeams = loosenWaitingRoomsTeams;

            await matchMakingOperator.TryLooseMatchMaking();
        }

        private Dictionary<int, HashSet<MM_User>> LoosenWaitingRooms(Dictionary<int, HashSet<MM_User>> original)
        {
            Dictionary<int, HashSet<MM_User>> newWaitingArrangment = new Dictionary<int, HashSet<MM_User>>();
            
            foreach (int MMR_OF_ROOM in original.Keys)
            {
                int newMMR = MMR_OF_ROOM - MatchMakingOperator.IncreaseMMR_FromWin;

                if (newMMR < 0)
                {
                    if (!newWaitingArrangment.TryAdd(0, original[MMR_OF_ROOM]))
                    {
                        newWaitingArrangment[0].UnionWith(original[MMR_OF_ROOM]);
                    }
                    continue; // ignoring rooms with MMR 0 or below.
                }

                HashSet<MM_User> waitingRoom = original[MMR_OF_ROOM];

                if (waitingRoom.Count <= 0) continue; // ignoring empty rooms.

                foreach (MM_User user in waitingRoom)
                {
                    user.MMR = newMMR;
                }

                if (!newWaitingArrangment.TryAdd(newMMR, waitingRoom))
                {
                    newWaitingArrangment[newMMR].UnionWith(waitingRoom);
                }
            }
            return newWaitingArrangment;
        }

        public async Task InvokeDropUserEvent(string authID)
        {
            await InvokeEventAsync
            (
                DropUserClientEvent!,
                this,
                authID
            );
        }

        public async Task InvokeDropServerEvent(int serverID)
        {
            await InvokeEventAsync
            (
                DropServerClientEvent!,
                this,
                serverID
            );
        }

        public async Task StopAllServers()
        {
            foreach (GameServerInstance serverInstance in matchMakingData.ALL_ConnectedGameServers.Values)
            {
                await serverInstance.StopSelf();
            }
        }

        public async ValueTask DisposeAsync()
        {
            newUserConnectEvent -= RegisterNewUser;
            newGameServerConnectEvent -= RegisterNewServer;
            userDisconnectEvent -= UnRegisterUser;
            gameServerDisconnectEvent -= GameServerDisconnectHandle;
            CreateServerEvent -= GenerateGameServer;
            LooseMatchMakingTimer.Instance.IntervalElapsedEvent -= LoosenMMR;
            ValueTask exitTask1 = dbManager.DisposeAsync();
            Task exitTask2 = StopAllServers();
            await Task.WhenAll([ exitTask1.AsTask(), exitTask2 ]);
        }
    }
}