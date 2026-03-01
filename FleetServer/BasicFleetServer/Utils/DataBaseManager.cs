using BasicFleetServer.Operation;
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
            DB_Connect();
            TableCreate();
        }

        private void TableCreate()
        {
            SqliteCommand sqliteCommand = dbCon!.CreateCommand();
            sqliteCommand.CommandText =
            @"
                CREATE TABLE IF NOT EXISTS PlayerInfo(
                    AuthID TEXT PRIMARY KEY,
                    PlayerName TEXT NOT NULL,
                    PlayerMMR INTEGER NOT NULL DEFAULT 0,
                    ISBanned INTEGER NOT NULL
                );
            ";
            sqliteCommand.ExecuteNonQuery();
        }

        public async Task<MM_User> ReadPlayerInfoByAuthID(string authID, string playerName, MM_QueueType QueTypePref)
        {
            SqliteCommand sqliteCommand = dbCon!.CreateCommand();
            sqliteCommand.CommandText =
            @"
                SELECT AuthID, PlayerName, PlayerMMR, ISBanned
                FROM PlayerInfo
                WHERE AuthID = $authID;
            ";
            sqliteCommand.Parameters.AddWithValue("$authID", authID);
            using SqliteDataReader reader = await sqliteCommand.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new MM_User
                {
                    authID = reader.GetString(0),
                    Username = reader.GetString(1),
                    MMR = reader.GetInt32(2),
                    IsBanned = reader.GetInt32(3) != 0,
                    gamePreference = QueTypePref
                };
            }
            else
            {
                return await CreatePlayerInfoByAuthID(authID, playerName, QueTypePref);
            }
        }

        public async Task<MM_User> CreatePlayerInfoByAuthID(string authID, string userName, MM_QueueType QueTypePref)
        {
            SqliteCommand sqliteCommand = dbCon!.CreateCommand();
            sqliteCommand.CommandText =
            @"
                INSERT INTO PlayerInfo (AuthID, PlayerName, PlayerMMR, ISBanned)
                VALUES ($authID, $playerName, 0, 0);
            ";
            sqliteCommand.Parameters.AddWithValue("$authID", authID);
            sqliteCommand.Parameters.AddWithValue("$playerName", userName);
            await sqliteCommand.ExecuteNonQueryAsync();

            return new MM_User
            {
                authID = authID,
                Username = userName,
                IsBanned = false,
                MMR = 0,
                gamePreference = QueTypePref
            };
        }

        private void DB_Connect()
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