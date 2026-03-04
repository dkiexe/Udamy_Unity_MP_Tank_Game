using BasicFleetServer.Operation;

namespace FleetServerUtils
{
    public class MM_User // Matchmaking User
    {
        public required string Username;
        public required string authID;
        public required int MMR;
        public required MM_GameType gamePreference;
        public required bool IsBanned;
    }
}