using Microsoft.Data.Sqlite;

namespace FleetServerUtils
{
    public class DataBaseManager : IAsyncDisposable
    {
        public string dbPath { get; private set; }

        private SqliteConnection? dbCon { get; set; }
        
        public DataBaseManager(string dbPath)
        {
            this.dbPath = dbPath;
            SqliteConnect();
            TableCreate();
        }

        private void TableCreate()
        {
            SqliteCommand sqliteCommand = dbCon!.CreateCommand();
            sqliteCommand.CommandText =
            @"
                CREATE TABLE IF NOT EXISTS PlayerInfo(
                    PlayerIP TEXT PRIMARY KEY,
                    PlayerName TEXT NOT NULL,
                    PlayerMMR INTEGER NOT NULL DEFAULT 0,
                    ISBanned INTEGER NOT NULL
                );
            ";
            sqliteCommand.ExecuteNonQuery();
        }

        public async Task<MM_User> ReadPlayerInfoByIP(string playerIP, string playerName)
        {
            SqliteCommand sqliteCommand = dbCon!.CreateCommand();
            sqliteCommand.CommandText =
            @"
                SELECT PlayerIP, PlayerName, PlayerMMR, ISBanned
                FROM PlayerInfo
                WHERE PlayerIP = $playerIP;
            ";
            sqliteCommand.Parameters.AddWithValue("$playerIP", playerIP);
            using SqliteDataReader reader = await sqliteCommand.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new MM_User
                {
                    User_IP = reader.GetString(0),
                    Username = reader.GetString(1),
                    MMR = reader.GetInt32(2),
                    IsBanned = reader.GetInt32(3) != 0
                };
            }
            else
            {
                return await CreatePlayerInfoByIP(playerIP, playerName);
            }
        }

        public async Task<MM_User> CreatePlayerInfoByIP(string IP, string userName)
        {
            SqliteCommand sqliteCommand = dbCon!.CreateCommand();
            sqliteCommand.CommandText =
            @"
                INSERT INTO PlayerInfo (PlayerIP, PlayerName, PlayerMMR, ISBanned)
                VALUES ($playerIP, $playerName, 0, 0);
            ";
            sqliteCommand.Parameters.AddWithValue("$playerIP", IP);
            sqliteCommand.Parameters.AddWithValue("$playerName", userName);
            await sqliteCommand.ExecuteNonQueryAsync();

            return new MM_User
            {
                User_IP = IP,
                Username = userName,
                IsBanned = false,
                MMR = 0
            };
        }

        private void SqliteConnect()
        {
            dbCon = new SqliteConnection($"Data Source={dbPath}");
            dbCon.Open();
        }

        private async Task DB_Disconnect()
        {
            if (dbCon != null)
            {
                await dbCon.CloseAsync();
                await dbCon.DisposeAsync();
                dbCon = null;
            }
        }

        public async ValueTask DisposeAsync()
        {
            await DB_Disconnect();
        }
    }
}