using System.Linq;
using BasicFleetServer.Operation;
using FleetServerUtils;


namespace BasicFleetServer.Utils
{
    public class Team
    {
        public required int TeamID;
        public required List<string> Players;
    }

    public class TCP_MatchData
    {
        public int maxTeamSize;
        public int gameType;
        public required List<Team> Teams;
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
                        MatchDataUpdateSolos(i, MatchData, user);
                        break;

                    case MM_GameType.TEAMS:
                        MatchDataUpdateTeams(i, MatchData, user);
                        break;

                    default:
                        break;
                }
            }
            return MatchData;
        }
        public void MatchDataUpdateSolos(int TeamID, TCP_MatchData MatchData, MM_User user)
        {
            Team soloTeam = new Team
            {
                TeamID = TeamID,
                Players = new List<string>() { user.authID },
            };
            MatchData.Teams.Add(soloTeam);
        }

        public void MatchDataUpdateTeams(int TeamID, TCP_MatchData MatchData, MM_User user)
        {
            if (MatchData.Teams.Count < maxTeamCount)
            {
                Team newTeam = new Team
                {
                    TeamID = TeamID,
                    Players = new List<string>() { user.authID },
                };
                MatchData.Teams.Add(newTeam);
            }
            else
            {
                MatchData.Teams[TeamID % MatchData.Teams.Count].Players.Add(user.authID);
            }
        }
    }
}