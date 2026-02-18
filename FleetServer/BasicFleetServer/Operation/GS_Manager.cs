using BasicFleetServer.Utils;
using FleetServerUtils;
using System.Data;
using System.Linq;
using static BasicFleetServer.Operation.FleetServerSocket;
using static BasicFleetServer.Utils.AsyncEventSystem;

namespace BasicFleetServer.Operation
{

    internal class GS_Manager : IAsyncDisposable
    {
        private const int MinPlayersToStartMatch = 2;

        private int ServerCounter = 0;

        private FleetApplicationData fleetAppdata;

        public DataBaseManager dbManager;

        // All connected MatchMaking Users Objects by IP.
        private Dictionary<string, MM_User> ALL_ConnectedUsers = new Dictionary<string, MM_User>();

        // MatchMaking Players waiting for Match by MMR.
        private Dictionary<int, SortedSet<MM_User>> WaitingRooms = new Dictionary<int, SortedSet<MM_User>>();

        // GameServer Instance object Lists by MMR.
        private Dictionary<int, List<GameServerInstance>> ActiveGameServerRooms = new Dictionary<int, List<GameServerInstance>>();

        // GameServerInstances + MMR asignment awaiting spin up by GameServerID
        private Dictionary<int, (int, GameServerInstance)> GameServerSpinUpPool = new Dictionary<int, (int, GameServerInstance)>();
        
        // EVENTS.
        public static event AsyncEventHandler<(string[], string)>? SocketMessageEvent;

        public static event AsyncEventHandler<(string, string)>? bannedClientConnectedEvent;

        private string LocalIP;

        public GS_Manager(FleetApplicationData fleetAppdata)
        {
            newUserConnectEvent += RegisterNewUser;
            newGameServerConnectEvent += RegisterNewServer;
            userDisconnectEvent += UnRegisterUser;
            this.fleetAppdata = fleetAppdata;
            dbManager = new DataBaseManager(fleetAppdata.databasePath);
            LocalIP = UtilsForIP.GetLanIP()!;

            if (LocalIP == null)
            {
                throw new NoNullAllowedException($"Failed To fetch LocalIP address!");
            }
        }

        public void GenerateGameServer(int MMR)
        {
            GameServerInstance serverInstance = new GameServerInstance(
                ServerCounter,
                MMR,
                LocalIP,
                7777 + ServerCounter,
                fleetAppdata.gameServerPath
            );
            serverInstance.StartSelf();
            GameServerSpinUpPool[serverInstance.GameServerID] = (MMR, serverInstance);
        }

        public async Task RegisterNewUser(object _, (string clientIP, string clientName) e)
        {
            MM_User connectedUser = await dbManager.ReadPlayerInfoByIP(e.clientIP, e.clientName);
            ALL_ConnectedUsers[e.clientIP] = connectedUser;

            // Handle Banned Users.
            if (connectedUser.IsBanned)
            {
                await InvokeEventAsync
                (
                    bannedClientConnectedEvent!,
                    this,
                    (
                        connectedUser.User_IP,
                        MSG_Translator.ConstructNetworkMessage("BANNED", ["INF"]) // {_(!)_} Currently banned players are banned permanently.
                    )
                );
            }

            // BackFilling process
            if (ActiveGameServerRooms.TryGetValue(connectedUser.MMR, out List<GameServerInstance>? GameServerOptions))
            {
                foreach (GameServerInstance serverInstance in GameServerOptions)
                {
                    if (!serverInstance.isFull)
                    {
                        await BackFillNewUser(serverInstance, connectedUser);
                        return;
                    }
                }
            }
            SortedSet<MM_User>? MMR_room = null;

            // Matchmaking process
            if (!WaitingRooms.TryGetValue(connectedUser.MMR, out MMR_room)) 
            {
                MMR_room = new SortedSet<MM_User>();
                WaitingRooms[connectedUser.MMR] = MMR_room;
            }
            
            MMR_room.Add(connectedUser);

            if (MMR_room.Count >= MinPlayersToStartMatch)
            {
                if (ServerCounter < fleetAppdata.maxServerCount)
                {
                    // Matchmaking suscessful, spining up server!
                    GenerateGameServer(connectedUser.MMR);
                }
                else
                {
                    Console.WriteLine($"[!@!] Error: Max server count reached, cannot generate new server instance for MMR Group : {connectedUser.MMR}");
                }
            }
        }

        private async Task BackFillNewUser(GameServerInstance serverInstance, MM_User user)
        {
            serverInstance.Players.Add(user);

            await InvokeEventAsync(
                SocketMessageEvent!,
                this,
                (
                    [user.User_IP],
                    MSG_Translator.ConstructNetworkMessage
                    (
                        "CONNECT",
                        [serverInstance.GameIP, serverInstance.GamePort.ToString()]
                    )
                )
            );
            WaitingRooms[user.MMR].Remove(user);
        }

        private async Task RegisterNewServer(object _, int GameServerID)
        {
            try
            {
                // Pop a server from the spin up pool ( its ready ).
                GameServerSpinUpPool.Remove(
                    GameServerID,
                    out (int MMR_Assignment, GameServerInstance GameServerInstance) NewGameServerData
                    );

                SortedSet<MM_User> WaitingRoom = WaitingRooms[NewGameServerData.MMR_Assignment];

                ServerCounter++;

                await MatchMake(NewGameServerData.GameServerInstance, WaitingRoom);
            }
            catch (KeyNotFoundException)
            {
                Console.WriteLine("\n[!@!] Unrecognised server tried to add itself to fleet, ignored.");
            }
        }

        private async Task MatchMake(GameServerInstance newGameServer, SortedSet<MM_User> MMR_room)
        {
            newGameServer.Players = MMR_room;
            int RoomMMR = newGameServer.GAME_MMR;

            ActiveGameServerRooms.Add(RoomMMR, new List<GameServerInstance> { newGameServer });
            
            await InvokeEventAsync(
                SocketMessageEvent!,
                this,
                (
                    MMR_room.Select(x => x.User_IP).ToArray(),
                    MSG_Translator.ConstructNetworkMessage
                    (
                        "CONNECT",
                        [newGameServer.GameIP, newGameServer.GamePort.ToString()]
                    )
                )
            );
            MMR_room.Clear();
        }


        private void UnRegisterUser(string IP)
        {
            if (ALL_ConnectedUsers.TryGetValue(IP, out MM_User? user))
            {
                int UserMMR = user.MMR;
                if (WaitingRooms[UserMMR].TryGetValue(user, out _))
                {
                    WaitingRooms[UserMMR].Remove(user);
                }
                else 
                {
                    if (ActiveGameServerRooms.TryGetValue(UserMMR, out List<GameServerInstance>? ActiveGameServerList))
                    {
                        foreach (GameServerInstance server in ActiveGameServerList)
                        {
                            if (server.Players.Remove(user)) break;
                        }
                    }
                }
                ALL_ConnectedUsers.Remove(IP);
            }
        }

        public async Task StopAllServers()
        {
            foreach (var MMRbracket in ActiveGameServerRooms)
            {
                foreach (var serverInstance in MMRbracket.Value)
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
            ValueTask exitTask1 = dbManager.DisposeAsync();
            Task exitTask2 = StopAllServers();
            await Task.WhenAll([ exitTask1.AsTask(), exitTask2 ]);
        }
    }
}