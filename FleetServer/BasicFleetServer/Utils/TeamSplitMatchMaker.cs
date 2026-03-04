using System.Linq;
using BasicFleetServer.Operation;
using FleetServerUtils;


namespace BasicFleetServer.Utils
{
    public class Team
    {
        public required int TeamID { get; set; }
        public required List<string> Players { get; set; }
    }

    public class TCP_MatchData
    {
        public int maxTeamSize;
        public int gameType;
        public required List<Team> Teams { get; set; }

        public int teamCount => Teams.Count;
    }

    public class TeamSplitMatchMaker
    {
        int maxTeamCount;
        public TeamSplitMatchMaker(int maxTeamCount)
        {
            this.maxTeamCount = maxTeamCount;
        }
        
        public TCP_MatchData AssignTeams(MM_GameType gameType, HashSet<MM_User> userAssignment)
        {
            TCP_MatchData MatchData = new TCP_MatchData
            {
                maxTeamSize = 0,
                gameType = (int)gameType,
                Teams = new List<Team>()
            };

            HashSet<MM_User> userAssignmentCopy = new HashSet<MM_User>(userAssignment);

            for (int i = 0; i < userAssignment.Count; i++) 
            {
                MM_User user = userAssignmentCopy.First();
                userAssignmentCopy.Remove(user);

                switch (gameType)
                {
                    case MM_GameType.SOLO:
                        Team soloTeam = new Team
                        {
                            TeamID = i,
                            Players = new List<string>() { user.authID },
                        };
                        MatchData.Teams.Add(soloTeam);
                        break;

                    case MM_GameType.TEAMS:
                        if (MatchData.teamCount < maxTeamCount)
                        {
                            Team newTeam = new Team
                            {
                                TeamID = i,
                                Players = new List<string>() { user.authID },
                            };
                            MatchData.Teams.Add(newTeam);
                            continue;
                        }

                        MatchData.Teams[i % MatchData.teamCount].Players.Add(user.authID);
                        break;

                    default:
                        break;
                }
            }
            
            return MatchData;
        }
    }
}