using FleetServerUtils;

namespace BasicFleetServer.Operation
{
    public class MatchMakingData
    {
        /// ----------------GAMESERVER DATA KEEPERS----------------
        // All connected MatchMaking GameServerInstance Objects by ID.
        public Dictionary<int, GameServerInstance> ALL_ConnectedGameServers;

        // GameServerInstances asignment awaiting spin up by GameServerID
        public Dictionary<int, GameServerInstance> GameServerSpinUpPool;

        // GameServer Instance object Lists by MMR.
        public Dictionary<int, HashSet<GameServerInstance>> ActiveGameServerRoomsSolos;

        public Dictionary<int, HashSet<GameServerInstance>> ActiveGameServerRoomsTeams;

        /// ----------------USER DATA KEEPERS----------------
        // All connected MatchMaking Users Objects by authID.
        public Dictionary<string, MM_User> ALL_ConnectedUsers;

        // A set of users that are currently being transfered to a game server, This is used to prevent data drops on clients after losing connection to the
        // MatchMaking Socket while they in transit(intended).
        public HashSet<MM_User> InTransit;

        // MatchMaking Players waiting for Match by MMR Solos.
        public Dictionary<int, HashSet<MM_User>> WaitingRoomsSolo;

        // MatchMaking Players waiting for Match by MMR Teams.
        public Dictionary<int, HashSet<MM_User>> WaitingRoomsTeams;

        public MatchMakingData()
        {
            ALL_ConnectedGameServers = new Dictionary<int, GameServerInstance>();
            GameServerSpinUpPool = new Dictionary<int, GameServerInstance>();
            ActiveGameServerRoomsSolos = new Dictionary<int, HashSet<GameServerInstance>>();
            ActiveGameServerRoomsTeams = new Dictionary<int, HashSet<GameServerInstance>>();

            ALL_ConnectedUsers = new Dictionary<string, MM_User>();
            InTransit = new HashSet<MM_User>();
            WaitingRoomsSolo = new Dictionary<int, HashSet<MM_User>>();
            WaitingRoomsTeams = new Dictionary<int, HashSet<MM_User>>();
        }
    }
}
