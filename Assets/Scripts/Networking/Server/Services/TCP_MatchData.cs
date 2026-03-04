using System.Collections.Generic;

namespace Assets.Scripts.Networking.Server.Services
{
    public class Team
    {
        public int TeamID { get; set; }
        public List<string> Players { get; set; }
    }

    public class TCP_MatchData
    {
        public int maxTeamSize;
        public int gameType;
        public List<Team> Teams { get; set; }

        public int teamCount => Teams.Count;
    }
}
