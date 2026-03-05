using System;
using System.Collections.Generic;

namespace Assets.Scripts.Networking.Server.Services
{
    [Serializable]
    public class Team
    {
        public int TeamID;
        public List<string> Players;
    }

    [Serializable]
    public class TCP_MatchData
    {
        public int maxTeamSize;
        public int gameType;
        public List<Team> Teams;
    }
}
