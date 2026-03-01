using BasicFleetServer.Utils;
using FleetServerUtils;
using System.Data;
using static BasicFleetServer.Utils.AsyncEventSystem;

namespace BasicFleetServer.Operation
{
    public class MatchMakingOperator
    {
        private int MinPlayersToStartMatch;
        private string LocalIP;
        private MatchMakingData matchMakingData;
        private FleetApplicationData fleetAppdata;

        private int ServerCounter => matchMakingData.ALL_ConnectedGameServers.Count;

        // EVENTS.
        public static event AsyncEventHandler<(string[], string)>? SocketMessageEvent; // (string[] authIDs, string message)

        public MatchMakingOperator
            (
                MatchMakingData matchMakingData, 
                FleetApplicationData fleetAppdata, 
                int MinPlayersToStartMatch
            )
        {
            this.MinPlayersToStartMatch = MinPlayersToStartMatch;
            this.matchMakingData = matchMakingData;
            this.fleetAppdata = fleetAppdata;
            
            LocalIP = UtilsForIP.GetActiveLanIP()!;

            if (LocalIP == null)
            {
                throw new NoNullAllowedException($"Failed To fetch LocalIP address!");
            }
        }

        public async Task MatchMakingUserAssignmentAsync(MM_User connectedUser)
        {
            
            /// ----------------BACKFILLING PROCESS----------------
            // Select the relevant game server dict based on the users game preference, this is used to backfill players into existing rooms if possible.
            Dictionary<int, HashSet<GameServerInstance>> ReleventGameServerDict = connectedUser.gamePreference switch
            {
                MM_QueueType.SOLO => matchMakingData.ActiveGameServerRoomsSolos,
                MM_QueueType.TEAMS => matchMakingData.ActiveGameServerRoomsTeams,
                _ => throw new ArgumentException($"Invalid game preference: {connectedUser.gamePreference}")
            };

            if (matchMakingData.ActiveGameServerRoomsSolos.TryGetValue(
                connectedUser.MMR, 
                out HashSet<GameServerInstance>? GameServerOptions
                ))
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

            /// ----------------MATCHMAKING PROCESS----------------
            HashSet<MM_User>? MMR_room = null;
            Dictionary<int, HashSet<MM_User>> ReleventWaitingRoomDict = connectedUser.gamePreference switch
            {
                MM_QueueType.SOLO => matchMakingData.WaitingRoomsSolo,
                MM_QueueType.TEAMS => matchMakingData.WaitingRoomsTeams,
                _ => throw new ArgumentException($"Invalid game preference: {connectedUser.gamePreference}")
            };

            if (!ReleventWaitingRoomDict.TryGetValue(connectedUser.MMR, out MMR_room))
            {
                MMR_room = new HashSet<MM_User>();
                matchMakingData.WaitingRoomsSolo[connectedUser.MMR] = MMR_room;
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
                    [user.authID],
                    MSG_Translator.ConstructNetworkMessage
                    (
                        "CONNECT",
                        [serverInstance.GameIP, serverInstance.GamePort.ToString()]
                    )
                )
            );
            matchMakingData.WaitingRoomsSolo[user.MMR].Remove(user);
        }

        public async Task MatchMakingServerAssignmentAsync(GameServerInstance newGameServer, HashSet<MM_User> MMR_room)
        {
            newGameServer.Players = MMR_room;
            int RoomMMR = newGameServer.GAME_MMR;

            if (!matchMakingData.ActiveGameServerRoomsSolos.TryAdd(RoomMMR, new HashSet<GameServerInstance> { newGameServer }))
            {
                matchMakingData.ActiveGameServerRoomsSolos[RoomMMR].Add(newGameServer);
            }

            await InvokeEventAsync(
                SocketMessageEvent!,
                this,
                (
                    MMR_room.Select(x => x.authID).ToArray(),
                    MSG_Translator.ConstructNetworkMessage
                    (
                        "CONNECT",
                        [newGameServer.GameIP, newGameServer.GamePort.ToString()]
                    )
                )
            );
            matchMakingData.InTransit.UnionWith(MMR_room);
            MMR_room.Clear();
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
            matchMakingData.GameServerSpinUpPool[serverInstance.GameServerID] = serverInstance;
        }
    }
}