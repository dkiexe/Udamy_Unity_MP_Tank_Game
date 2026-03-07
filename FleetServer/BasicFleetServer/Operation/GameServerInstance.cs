using BasicFleetServer.Utils;
using FleetServerUtils;
using System.Diagnostics;

namespace BasicFleetServer.Operation
{
    public class GameServerInstance : IAsyncDisposable
    {
        public Process? gameServerProcess { get; private set; }
        public int GameServerID { get; private set; }
        public string GameIP { get; private set; }
        public int GamePort { get; private set; }
        public int GAME_MMR { get; private set; }
        public MM_GameType GameType { get; private set; }
        
        public HashSet<MM_User> Players = new HashSet<MM_User>();

        public TCP_MatchData? MatchData;

        private string pathToExE;

        private const int MaxPlayers = 10;

        public bool isFull => Players.Count > MaxPlayers;

        public GameServerInstance(int ID, int MMR, string IP, int Port, string pathToExE, MM_GameType queueType)
        {
            GAME_MMR = MMR;
            GameServerID = ID;
            GameIP = IP;
            GamePort = Port;
            GameType = queueType;

            this.pathToExE = pathToExE;
        }

        public void StartSelf()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = pathToExE,
                Arguments = $"-ip {GameIP} -port {GamePort} -id {GameServerID}",
                UseShellExecute = true,
                CreateNoWindow = false,

            };
            try
            {
                Process? process = Process.Start(startInfo);

                if (process == null || process.HasExited)
                {
                    Console.WriteLine($"[!] Err! on {GameServerID} : Failed to start game server process.");
                }
                else
                {
                    gameServerProcess = process;
                }
            }
            catch (SystemException)
            {
                Console.WriteLine($"[!] Err! Failed to start any game server process. Did you forget to specify a path for a Game Server? ");
                Environment.Exit(1);
            }
        }
        public async Task StopSelf()
        {
            if (gameServerProcess != null && !gameServerProcess.HasExited)
            {
                gameServerProcess.Kill();
                await gameServerProcess.WaitForExitAsync();
            }
        }

        public async ValueTask DisposeAsync()
        {
            await StopSelf();
        }
    }
}