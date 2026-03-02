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

        private FleetApplicationData fleetAppdata;

        public DataBaseManager dbManager;

        // MatchMaking Data.
        private MatchMakingData matchMakingData;

        // MatchMaking Operator.
        private MatchMakingOperator matchMakingOperator;

        // EVENTS
        public static event AsyncEventHandler<(string, string)>? bannedClientConnectedEvent; // (string authID, string message)

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
            serverDisconnectEvent += UnRegisterServer;
            CreateServerEvent += GenerateGameServer;

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
                    MinPlayersToStartMatch
                );
        }
        public async Task RegisterNewUser(object _, string[] eventData)
        {
            string authID = eventData[0];
            string UserName = eventData[1];
            MM_QueueType QueTypePref;


            if (!int.TryParse(eventData[2], out int gamePrefInt))
            {
                await InvokeEventAsync
                (
                    DropUserClientEvent!,
                    this,
                    authID
                );
                return;
            }

            try
            {
                QueTypePref = Enum.GetValues<MM_QueueType>()[gamePrefInt];
            }
            catch (IndexOutOfRangeException)
            {
                await InvokeEventAsync
                (
                    DropUserClientEvent!,
                    this,
                    authID
                );
                return;
            }

            MM_User connectedUser = await dbManager.ReadPlayerInfoByAuthID(authID, UserName, QueTypePref);
            matchMakingData.ALL_ConnectedUsers[authID] = connectedUser;

            // Handle Banned Users.
            if (connectedUser.IsBanned)
            {
                await InvokeEventAsync
                (
                    bannedClientConnectedEvent!,
                    this,
                    (
                        connectedUser.authID,
                        MSG_Translator.ConstructNetworkMessage("BANNED", ["INF"]) // {_(!)_} Currently banned players are banned permanently.
                    )
                );
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
                await InvokeEventAsync
                (
                    DropServerClientEvent!,
                    this,
                    GameServerID
                );
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

        public void GenerateGameServer(int MMR, MM_QueueType queueType, HashSet<MM_User> waitingRoom)
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
            serverDisconnectEvent -= UnRegisterServer;
            CreateServerEvent -= GenerateGameServer;
            ValueTask exitTask1 = dbManager.DisposeAsync();
            Task exitTask2 = StopAllServers();
            await Task.WhenAll([ exitTask1.AsTask(), exitTask2 ]);
        }
    }
}