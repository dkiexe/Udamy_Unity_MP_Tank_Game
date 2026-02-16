using System.Text;

namespace FleetServerUtils
{
    public class FleetApplicationData
    {
        private Dictionary<string, Action<string>> m_CommandDictionary = new();

        private const string cn_FleetId = "fleetId";
        private const string cn_UCP = "UCP"; // User Communication Port
        private const string cn_MSC = "MSC"; // Max Server Count
        private const string cm_GSP = "GSP"; // Game Server Path
        private const string cn_DBPath = "DBPath"; // Database Path

        public int fleetId { get; private set; } = 0;
        public int UserCommPort { get; private set; } = 6969;

        public const int GameServerCommPort = 9090;

        public int maxServerCount { get; private set; } = 1;
        public string gameServerPath { get; private set; } = "";
        public string databasePath { get; private set; } = "FleetServerDB.sqlite";

        public FleetApplicationData()
        {
            m_CommandDictionary["-" + cn_FleetId] = SetFleetId;
            m_CommandDictionary["-" + cn_UCP] = SetUserCommPort;
            m_CommandDictionary["-" + cn_MSC] = SetMaxServerCount;
            m_CommandDictionary["-" + cm_GSP] = SetGameServerPath;
            m_CommandDictionary["-" + cn_DBPath] = SetDatabasePath;

            // Process command line arguments.
            List<string> argsList = new(Environment.CommandLine.Split(" "));
            ProcessCommandLineArguments(argsList[1..].ToArray<string>());
        }

        private void ProcessCommandLineArguments(string[] args)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Fleet Server Launch Args: ");
            for (var i = 0; i < args.Length; i++)
            {
                var Cmd = args[i];
                var Val = "";
                if (i + 1 < args.Length) // if we are evaluating the last item in the array, it must be a flag
                    Val = args[i + 1];

                if (EvaluatedArgs(Cmd, Val))
                {
                    sb.Append(Cmd);
                    sb.Append(" : ");
                    sb.AppendLine(Val);
                    i++;
                }
            }

            Console.Write(sb);
        }

        private bool EvaluatedArgs(string Cmd, string Val)
        {
            if (!IsCommand(Cmd))
                return false;
            if (IsCommand(Val))
            {
                return false;
            }
            m_CommandDictionary[Cmd].Invoke(Val);
            return true;
        }

        private bool IsCommand(string arg)
        {
            return !string.IsNullOrEmpty(arg) && m_CommandDictionary.ContainsKey(arg) && arg.StartsWith("-");
        }

        private bool ProcessInt(string intAsStr, out int intVal) => int.TryParse(intAsStr, out intVal);

        private bool ProcessPath(string path) => !string.IsNullOrEmpty(path) && File.Exists(path);

        private void SetFleetId(string idString)
        {
            if (ProcessInt(idString, out int idval))
            {
                fleetId = idval;
            }
            else
            {
                Console.WriteLine($"[!] Warning Invalid FleetId detected! , instead using default of: {fleetId}.");
            }
        }

        private void SetUserCommPort(string portString)
        {
            if (portString.Length == 4)
            {
                if (ProcessInt(portString, out int port_Val))
                {
                    UserCommPort = port_Val;
                    return;
                }
            }
            Console.WriteLine($"[!] Warning Invalid User Communication Port(UCP) detected! , instead using default of: {UserCommPort}.");
        }

        private void SetMaxServerCount(string MSC_String)
        {
            if (ProcessInt(MSC_String, out int MSC_Val))
            {
                maxServerCount = MSC_Val;
            }
            else
            {
                Console.WriteLine($"[!] Warning Invalid Max Server Count(MSC) detected! , instead using default of: {maxServerCount}.");
            }
        }

        private void SetGameServerPath(string pathString)
        {
            if (ProcessPath(pathString))
            {
                gameServerPath = pathString;
            }
            else
            {
                Console.WriteLine($"[!] Warning Invalid Server Game Path(GSP) detected! , instead using default of: {gameServerPath}.");
            }
        }

        private void SetDatabasePath(string dbPathString)
        {
            if (ProcessPath(dbPathString))
            {
                databasePath = dbPathString;
            }
            else
            {
                Console.WriteLine($"[!] Warning Invalid Database Path(DBPath) detected! , instead using default of: {databasePath}.");
            }
        }
    }
}