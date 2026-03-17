using BasicFleetServer.Utils;
using FleetServerUtils;
using System.Data;
using static BasicFleetServer.Utils.AsyncEventSystem;
using Newtonsoft.Json;

namespace BasicFleetServer.Operation
{
    public enum MM_GameType
    {
        SOLO,
        TEAMS
    }

    public class MatchMakingOperator
    {
        private int MinPlayersToStartMatch;
        private int MaxTeamCount;
        private MatchMakingData matchMakingData;

        private TeamSplitMatchMaker teamSplit;

        // EVENTS.
        public static event AsyncEventHandler<(string[], string)>? UserSocketMessageEvent; // (string[] authIDs, string message)
        public static event AsyncEventHandler<(int[], string)>? GameServerSocketMessageEvent; // (int[] GameServerIDs, string message)
        public static event Action<int, MM_GameType, HashSet<MM_User>>? CreateServerEvent; // (int MMR_Group, MM_QueueType queueType, HashSet<MM_User> Waiting Room)

        public MatchMakingOperator
            (
                MatchMakingData matchMakingData,
                int MinPlayersToStartMatch,
                int MaxTeamCount
            )
        {
            this.MaxTeamCount = MaxTeamCount;
            this.MinPlayersToStartMatch = MinPlayersToStartMatch;
            this.matchMakingData = matchMakingData;

            teamSplit = new TeamSplitMatchMaker(MaxTeamCount);
        }

        public async Task MatchMakingUserAssignmentAsync(MM_User connectedUser)
        {
            /// ----------------BACKFILLING PROCESS----------------
            // Select the relevant game server dict based on the users game preference, this is used to backfill players into existing rooms if possible.
            
            Dictionary<int, HashSet<GameServerInstance>> ActiveGameServerDict = GetActiveGameServersByGameType(connectedUser.gamePreference);

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
            Dictionary<int, HashSet<MM_User>> WaitingRoomDict = GetWaitingRoomsByGameType(connectedUser.gamePreference);

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

            switch (serverInstance.GameType)
            {
                case MM_GameType.SOLO:
                    teamSplit.MatchDataUpdateSolos
                        (
                            serverInstance.Players.Count, 
                            serverInstance.MatchData!, 
                            user
                        );
                    break;
                case MM_GameType.TEAMS:
                    teamSplit.MatchDataUpdateTeams
                        (
                            serverInstance.Players.Count,
                            serverInstance.MatchData!,
                            user
                        );
                    break;
            }
            Console.WriteLine("Backfill");
            teamSplit.LogTeamStatus(serverInstance.MatchData!); // {_(!)_} FOR TESTING !
            
            await InvokeGameServerMessageEvent // Informing gameServers of a new team assignment.
            (
                [serverInstance.GameServerID],
                "MATCHDATAUPDATE",
                [JsonConvert.SerializeObject(serverInstance.MatchData)]
            );

            await InvokeUserMessageEvent([user.authID], "CONNECT", [serverInstance.GameIP, serverInstance.GamePort.ToString()]);
        }

        public void RemoveUserFromMatchMaking(MM_User user)
        {
            int UserMMR = user.MMR;
            MM_GameType userGamePref = user.gamePreference;

            Dictionary<int, HashSet<MM_User>> WaitingRoomDict = GetWaitingRoomsByGameType(userGamePref);

            Dictionary<int, HashSet<GameServerInstance>> ActiveGameServerDict = GetActiveGameServersByGameType(userGamePref);

            if (!WaitingRoomDict[UserMMR].Remove(user))
            {
                if (ActiveGameServerDict.TryGetValue(UserMMR, out HashSet<GameServerInstance>? ActiveGameServerList))
                {
                    foreach (GameServerInstance server in ActiveGameServerList)
                    {
                        if (server.Players.Remove(user))
                        {
                            foreach(Team team in server.MatchData!.Teams)
                            {
                                if (team.Players.Remove(user.authID)) break;
                            }
                            break;
                        }
                    }
                }
            }
            else
            {
                if (WaitingRoomDict[UserMMR].Count == 0) WaitingRoomDict.Remove(UserMMR);
            }
        }

        public async Task MatchMakingServerAssignmentAsync(GameServerInstance newGameServer)
        {
            // {_(!)_} ISSUE HERE FIX LATER: SERVER MAY TAKE MORE PLAYERS THAN ITS LIMIT HERE, BY TAKING THE WHOLE WAITING ROOM!.
            int MMR = newGameServer.GAME_MMR;
            MM_GameType gameType = newGameServer.GameType;

            Dictionary<int, HashSet<MM_User>> WaitingRooms = GetWaitingRoomsByGameType(gameType);

            HashSet<MM_User> MMR_room = WaitingRooms[MMR];

            Dictionary<int, HashSet<GameServerInstance>> ActiveGameServerDict = GetActiveGameServersByGameType(newGameServer.GameType);

            if (!ActiveGameServerDict.TryAdd(MMR, new HashSet<GameServerInstance> { newGameServer }))
            {
                ActiveGameServerDict[MMR].Add(newGameServer);
            }

            newGameServer.MatchData = teamSplit.AssignTeams(gameType, MMR_room);

            Console.WriteLine("MatchMake");
            teamSplit.LogTeamStatus(newGameServer.MatchData!); // {_(!)_} FOR TESTING !

            await InvokeGameServerMessageEvent // Informing gameServers of teams assignment.
            (
                [newGameServer.GameServerID],
                "MATCHDATA",
                [JsonConvert.SerializeObject(newGameServer.MatchData)]
            );

            await InvokeUserMessageEvent // Informing users of their new server assignment.
            (
                MMR_room.Select(x => x.authID).ToArray(),
                "CONNECT",
                [newGameServer.GameIP, newGameServer.GamePort.ToString()]
            );

            newGameServer.Players = MMR_room.ToHashSet();
            matchMakingData.InTransit.UnionWith(MMR_room);
            MMR_room.Clear();
        }

        public async Task InvokeGameServerMessageEvent(int[] GameServerIDs, string cmd, string[] arguments)
        {
            await InvokeEventAsync
            (
                GameServerSocketMessageEvent!,
                this,
                (
                    GameServerIDs,
                    MSG_Translator.ConstructNetworkMessage
                    (
                        cmd,
                        arguments
                    )
                )
            );
        }

        public async Task InvokeUserMessageEvent(string[] authIDs, string cmd, string[] arguments)
        {
            await InvokeEventAsync
            (
                UserSocketMessageEvent!,
                this,
                (
                    authIDs,
                    MSG_Translator.ConstructNetworkMessage
                    (
                        cmd,
                        arguments
                    )
                )
            );
        }
        
        private Dictionary<int, HashSet<MM_User>> GetWaitingRoomsByGameType(MM_GameType GamePref)
        {
            return GamePref switch
            {
                MM_GameType.SOLO => matchMakingData.WaitingRoomsSolo,
                MM_GameType.TEAMS => matchMakingData.WaitingRoomsTeams,
                _ => throw new ArgumentException($"Invalid game preference: {GamePref}")
            };
        }

        private Dictionary<int, HashSet<GameServerInstance>> GetActiveGameServersByGameType(MM_GameType GamePref)
        {
            return GamePref switch
            {
                MM_GameType.SOLO => matchMakingData.ActiveGameServerRoomsSolos,
                MM_GameType.TEAMS => matchMakingData.ActiveGameServerRoomsTeams,
                _ => throw new ArgumentException($"Invalid game preference: {GamePref}")
            };
        }
    }
}