using BasicFleetServer.Utils;
using FleetServerUtils;
using static BasicFleetServer.Operation.FleetServerSocket;
using static BasicFleetServer.Utils.AsyncEventSystem;

namespace BasicFleetServer.Operation
{
    public enum MM_QueueType
    {
        SOLO,
        TEAMS
    }

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

        public Data_Manager(FleetApplicationData fleetAppdata)
        {
            // Event Subscriptions.
            newUserConnectEvent += RegisterNewUser;
            newGameServerConnectEvent += RegisterNewServer;
            userDisconnectEvent += UnRegisterUser;
            serverDisconnectEvent += UnRegisterServer;

            // Data Initialization.
            this.fleetAppdata = fleetAppdata;
            dbManager = new DataBaseManager(fleetAppdata.databasePath);
            matchMakingData = new MatchMakingData();
            matchMakingOperator = new MatchMakingOperator
                (
                    matchMakingData,
                    fleetAppdata,
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
            try
            {
                // Pop a server from the spin up pool ( its ready ).
                matchMakingData.GameServerSpinUpPool.Remove(
                    GameServerID,
                    out GameServerInstance? GameServerInstance
                    );

                // {_(!)_} ISSUE BECAME APPEARENT SERVER MAY TAKE MORE PLAYERS THAN ITS LIMIT HERE, BY TAKING THE WHOLE WAITING ROOM!.
                HashSet<MM_User> WaitingRoom = matchMakingData.WaitingRoomsSolo[GameServerInstance!.GAME_MMR];

                matchMakingData.ALL_ConnectedGameServers[GameServerInstance.GameServerID] = GameServerInstance;

                await matchMakingOperator.MatchMakingServerAssignmentAsync(GameServerInstance, WaitingRoom);
            }
            catch (KeyNotFoundException)
            {
                Console.WriteLine("\n[!@!] Unrecognised server tried to add itself to fleet, dropped.");
                await InvokeEventAsync
                (
                    DropServerClientEvent!,
                    this,
                    GameServerID
                );
            }
        }

        private void UnRegisterUser(string authID)
        {
            if (matchMakingData.ALL_ConnectedUsers.TryGetValue(authID, out MM_User? user))
            {
                if (matchMakingData.InTransit.Remove(user))
                {
                    return; // User was in transit, so we just remove them from the in transit hashSet and exit without removing them from anywhere.
                }
                int UserMMR = user.MMR;
                if (!matchMakingData.WaitingRoomsSolo[UserMMR].Remove(user))
                {
                    if (matchMakingData.ActiveGameServerRoomsSolos.TryGetValue(UserMMR, out HashSet<GameServerInstance>? ActiveGameServerList))
                    {
                        foreach (GameServerInstance server in ActiveGameServerList)
                        {
                            if (server.Players.Remove(user)) break;
                        }
                    }
                }
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

        public async Task StopAllServers()
        {
            foreach (HashSet<GameServerInstance> MMRbracket in matchMakingData.ActiveGameServerRoomsSolos.Values)
            {
                foreach (GameServerInstance serverInstance in MMRbracket)
                {
                    await serverInstance.StopSelf();
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            newUserConnectEvent -= RegisterNewUser;
            newGameServerConnectEvent -= RegisterNewServer;
            userDisconnectEvent -= UnRegisterUser;
            serverDisconnectEvent -= UnRegisterServer;
            ValueTask exitTask1 = dbManager.DisposeAsync();
            Task exitTask2 = StopAllServers();
            await Task.WhenAll([ exitTask1.AsTask(), exitTask2 ]);
        }
    }
}