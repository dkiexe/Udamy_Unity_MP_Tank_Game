using BasicFleetServer.Utils;
using FleetServerUtils;
using System.Data;
using static BasicFleetServer.Utils.AsyncEventSystem;

namespace BasicFleetServer.Operation
{
    public enum MM_QueueType
    {
        SOLO,
        TEAMS
    }

    public class MatchMakingOperator
    {
        private int MinPlayersToStartMatch;
        private MatchMakingData matchMakingData;

        // EVENTS.
        public static event AsyncEventHandler<(string[], string)>? SocketMessageEvent; // (string[] authIDs, string message)
        public static event Action<int, MM_QueueType, HashSet<MM_User>>? CreateServerEvent; // (int MMR_Group, MM_QueueType queueType, HashSet<MM_User> Waiting Room)

        public MatchMakingOperator
            (
                MatchMakingData matchMakingData,
                int MinPlayersToStartMatch
            )
        {
            this.MinPlayersToStartMatch = MinPlayersToStartMatch;
            this.matchMakingData = matchMakingData;
        }

        public async Task MatchMakingUserAssignmentAsync(MM_User connectedUser)
        {
            /// ----------------BACKFILLING PROCESS----------------
            // Select the relevant game server dict based on the users game preference, this is used to backfill players into existing rooms if possible.
            
            Dictionary<int, HashSet<GameServerInstance>> ActiveGameServerDict = GetActiveGameServersByQueueType(connectedUser.gamePreference);

            if (ActiveGameServerDict.TryGetValue(
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
            Dictionary<int, HashSet<MM_User>> WaitingRoomDict = GetWaitingRoomsByQueueType(connectedUser.gamePreference);

            if (!WaitingRoomDict.TryGetValue(connectedUser.MMR, out MMR_room))
            {
                MMR_room = new HashSet<MM_User>();
                WaitingRoomDict[connectedUser.MMR] = MMR_room;
            }

            MMR_room.Add(connectedUser);

            if (MMR_room.Count >= MinPlayersToStartMatch)
            {
                // Matchmaking suscessful, rasing event to spin up a server!
                CreateServerEvent?.Invoke(connectedUser.MMR, connectedUser.gamePreference, MMR_room);
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
        }

        public void RemoveUserFromMatchMaking(MM_User user)
        {
            int UserMMR = user.MMR;
            MM_QueueType userQueuePref = user.gamePreference;

            Dictionary<int, HashSet<MM_User>> WaitingRoomDict = GetWaitingRoomsByQueueType(userQueuePref);

            Dictionary<int, HashSet<GameServerInstance>> ActiveGameServerDict = GetActiveGameServersByQueueType(userQueuePref);

            if (!WaitingRoomDict[UserMMR].Remove(user))
            {
                if (ActiveGameServerDict.TryGetValue(UserMMR, out HashSet<GameServerInstance>? ActiveGameServerList))
                {
                    foreach (GameServerInstance server in ActiveGameServerList)
                    {
                        if (server.Players.Remove(user)) break;
                    }
                }
            }
        }

        public async Task MatchMakingServerAssignmentAsync(GameServerInstance newGameServer)
        {
            // {_(!)_} ISSUE HERE FIX LATER: SERVER MAY TAKE MORE PLAYERS THAN ITS LIMIT HERE, BY TAKING THE WHOLE WAITING ROOM!.
            int MMR = newGameServer.GAME_MMR;
            MM_QueueType gameType = newGameServer.GameType;

            Dictionary<int, HashSet<MM_User>> WaitingRooms = GetWaitingRoomsByQueueType(gameType);

            HashSet<MM_User> MMR_room = WaitingRooms[MMR];

            Dictionary<int, HashSet<GameServerInstance>> ActiveGameServerDict = GetActiveGameServersByQueueType(newGameServer.GameType);

            if (!ActiveGameServerDict.TryAdd(MMR, new HashSet<GameServerInstance> { newGameServer }))
            {
                ActiveGameServerDict[MMR].Add(newGameServer);
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
            newGameServer.Players = MMR_room;
            matchMakingData.InTransit.UnionWith(MMR_room);
            MMR_room.Clear();
        }
        
        private Dictionary<int, HashSet<MM_User>> GetWaitingRoomsByQueueType(MM_QueueType queueType)
        {
            return queueType switch
            {
                MM_QueueType.SOLO => matchMakingData.WaitingRoomsSolo,
                MM_QueueType.TEAMS => matchMakingData.WaitingRoomsTeams,
                _ => throw new ArgumentException($"Invalid game preference: {queueType}")
            };
        }

        private Dictionary<int, HashSet<GameServerInstance>> GetActiveGameServersByQueueType(MM_QueueType queueType)
        {
            return queueType switch
            {
                MM_QueueType.SOLO => matchMakingData.ActiveGameServerRoomsSolos,
                MM_QueueType.TEAMS => matchMakingData.ActiveGameServerRoomsTeams,
                _ => throw new ArgumentException($"Invalid game preference: {queueType}")
            };
        }
    }
}