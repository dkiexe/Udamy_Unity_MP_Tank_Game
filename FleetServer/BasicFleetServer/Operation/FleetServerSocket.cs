
using BasicFleetServer.Utils;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using static BasicFleetServer.Utils.AsyncEventSystem;

namespace BasicFleetServer.Operation
{
    public enum SocketType
    {
        ForUsers,
        ForGameServers
    }

    class FleetServerSocket : IAsyncDisposable
    {
        // Events
        public static event AsyncEventHandler<(string, string)>? newUserConnectEvent;
        public static event AsyncEventHandler<int>? newGameServerConnectEvent;

        // IP to TcpClient object mapping for user connections.
        public Dictionary<string, TcpClient>? UserConnectedClients;

        // GameServerID to TcpClient object mapping for GameServer connections.
        public Dictionary<int, TcpClient>? GameServerConnctedClients;

        public Dictionary<TcpClient, CancellationTokenSource> communicationCancelSources = new Dictionary<TcpClient, CancellationTokenSource>();

        private CancellationTokenSource GlobalCancelSource = new CancellationTokenSource();

        public SocketType socketType { get; private set; }

        public int Port { get; private set; }

        private byte[] buffer = new byte[1024];

        private int MessageTimeLimitSeconds = 10;

        private readonly string registerCmd = "REGISTER";

        private readonly string heartbeatCmd = "HEARTBEAT";

        public FleetServerSocket(int Port, SocketType socketType)
        {
            
            this.Port = Port;
            this.socketType = socketType;

            if (socketType == SocketType.ForUsers)
            {
                UserConnectedClients = new Dictionary<string, TcpClient>();
                GS_Manager.SocketMessageEvent += WriteMessageToUsers;
                GS_Manager.bannedClientConnectedEvent += HandleBannedClient;
            }
            else
            {
                GameServerConnctedClients = new Dictionary<int, TcpClient>();
            }
        }

        public async Task StartListening()
        {
            TcpListener listener = new TcpListener(IPAddress.Any, Port);
            listener.Start();

            CancellationToken GlobalCancelToken = GlobalCancelSource.Token;
            Console.WriteLine($"{socketType.ToString()} Socket Uses TCP Protocol and is listening on {Port}");

            while (!GlobalCancelToken.IsCancellationRequested)
            {
                TcpClient client = await listener.AcceptTcpClientAsync(GlobalCancelSource.Token);

                _ = TCP_ClientRegister(client);
            }
        }

        public async Task TCP_ClientRegister(TcpClient client)
        {
            var RegisterRes = await ReadTCP_MessageClient(client);
            
            if (RegisterRes == null) // message timeout or invalid message from client.
            {
                await DisconnectClient(client);
                return;
            }

            (string Cmd, string[] args) = RegisterRes.Value;

            if (Cmd != registerCmd)
            {
                await DisconnectClient(client);
                return;
            }

            NetworkIdentity networkIdentity;

            switch (socketType)
            {
                case SocketType.ForUsers:
                    IPEndPoint remoteEndPoint = (IPEndPoint)client.Client.RemoteEndPoint!;
                    string ipAddress = remoteEndPoint!.Address.ToString();

                    string UserName = args[0];

                    Console.WriteLine($"New User Connected : {ipAddress} UserName : {UserName}"); // {_(!)_} For testing purposes

                    if (!(UserConnectedClients!.ContainsKey(ipAddress)))
                    {
                        UserConnectedClients[ipAddress] = client;

                        communicationCancelSources[client] = new CancellationTokenSource();

                        networkIdentity = new NetworkIdentity
                        {
                            IPAddress = ipAddress
                        };

                        Task UserLoginEvent = InvokeEventAsync
                        (
                            newUserConnectEvent!,
                            this,
                            (networkIdentity.IPAddress, UserName)
                        );

                        Task readerTask = ClientMessageReader(networkIdentity, client);
                    }
                    break;

                case SocketType.ForGameServers:
                    
                    if (int.TryParse(args[0], out int GameServerID))
                    {
                        Console.WriteLine($"New Game Server Connected : {GameServerID}"); // {_(!)_} For testing purposes

                        if (!(GameServerConnctedClients!.ContainsKey(GameServerID)))
                        {
                            GameServerConnctedClients![GameServerID] = client;

                            communicationCancelSources[client] = new CancellationTokenSource();

                            networkIdentity = new NetworkIdentity
                            {
                                GameServerID = GameServerID
                            };

                            Task GameServerLoginEvent = InvokeEventAsync
                            (
                                newGameServerConnectEvent!,
                                this,
                                (GameServerID)
                            );

                            Task readerTask = ClientMessageReader(networkIdentity, client);
                        }
                    }
                    else
                    {
                        client.Dispose();
                        Console.WriteLine("Invalid Game Server ID during registration"); // {_(!)_} For testing purposes
                    }
                    break;
            }
        }


        public async Task ClientMessageReader(NetworkIdentity networkIdentity, TcpClient client)
        {
            CancellationToken GlobalCancelToken = GlobalCancelSource.Token; // Global cancellation token for the entire server
            CancellationToken CommCancelToken = communicationCancelSources[client].Token; // Cancellation token specific a client

            // Combine both cancellation token sources
            using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                [GlobalCancelToken, CommCancelToken]
                );

            while (!linkedCts.Token.IsCancellationRequested)
            {
                var MessageRes = await ReadTCP_MessageClient(client, [linkedCts.Token]);
                
                if (MessageRes == null)
                {
                    break; // Client disconnected or a message error occurred.
                }
                
                (string Cmd, string[] args) = MessageRes.Value;

                if (Cmd == heartbeatCmd) continue;
                
                switch (socketType)
                {
                    case SocketType.ForUsers:
                        await UserMessageReader(networkIdentity, Cmd, args);
                        break;
                    
                    case SocketType.ForGameServers:
                        await GameServerMessageReader(networkIdentity, Cmd, args);
                        break;
                }
            }
            CancellationTokenSource cancelClientSource = communicationCancelSources[client];
            cancelClientSource.Cancel();
            cancelClientSource.Dispose();
            communicationCancelSources.Remove(client);

            switch (socketType)
            {
                case SocketType.ForUsers:
                    UserConnectedClients!.Remove(networkIdentity.IPAddress!);
                    break;

                case SocketType.ForGameServers:
                    GameServerConnctedClients!.Remove(networkIdentity.GameServerID!.Value);
                    break;
            }
            await DisconnectClient(client);
        }
        
        private async Task GameServerMessageReader(NetworkIdentity networkIdentity, string msg, string[] args) { }

        private async Task UserMessageReader(NetworkIdentity networkIdentity, string msg, string[] args) { }

        private async Task HandleBannedClient(object sender, (string IP, string msg) e)
        {
            TcpClient bannedClientSocket = UserConnectedClients![e.IP];
            CancellationTokenSource cancelClientSource = communicationCancelSources[bannedClientSocket];
            
            cancelClientSource.Cancel();
            cancelClientSource.Dispose();

            await WriteMessageToUsers(sender, (new string[] { e.IP }, e.msg));

            UserConnectedClients.Remove(e.IP);
            communicationCancelSources.Remove(bannedClientSocket);
        }

        private async Task WriteMessageToUsers(object _, (string[] IP_Destinations, string msg) e)
        {
            List<Task> WriteTasks = new();
            byte[] messageBytes = Encoding.UTF8.GetBytes(e.msg);

            foreach (string ip in e.IP_Destinations)
            {
                if (UserConnectedClients!.TryGetValue(ip, out TcpClient? client))
                {
                    if (client.Connected)
                    {
                        NetworkStream stream = client.GetStream();
                        WriteTasks.Add(stream.WriteAsync(messageBytes, 0, messageBytes.Length));
                    }
                }
            }
            await Task.WhenAll(WriteTasks);
        }

        public async Task<(string, string[])?> ReadTCP_MessageClient(TcpClient client, List<CancellationToken>? cancelSources = null)
        {
            if (cancelSources == null) cancelSources = new List<CancellationToken>();

            using CancellationTokenSource TimeLimitToken = new CancellationTokenSource(
                TimeSpan.FromSeconds(MessageTimeLimitSeconds)
            );

            cancelSources.Add(GlobalCancelSource.Token);
            cancelSources.Add(TimeLimitToken.Token);

            // Combine cancellation tokens to create a one linked token source for all possible cancellation scenarios (global server shutdown, time limit exceeded, etc.)
            using CancellationTokenSource linkedCancelSources = CancellationTokenSource.CreateLinkedTokenSource(
                cancelSources.ToArray()
            );
            try
            {
                int bytesRead = await client.GetStream().ReadAsync(buffer, 0, buffer.Length, linkedCancelSources.Token);

                if (bytesRead == 0)
                {
                    throw new OperationCanceledException();
                }

                string Msg = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                try
                {
                    (string Cmd, string[] args) = MSG_Translator.ParseNetworkMessage(Msg);

                    return (Cmd, args);
                }
                catch (ArgumentException)
                {
                    // in an event that a user command is invalid, me treat it as a timeout.
                    throw new OperationCanceledException();
                }
            }
            catch (OperationCanceledException)
            {
                return null;
            }
        }

        private async Task DisconnectClient(TcpClient client) // Destroy clients here.
        {
            if (client.Connected)
            {

                try
                {
                    client.Client.Shutdown(SocketShutdown.Both);
                }
                catch (SocketException) { } // Socket may already be closed — ignore


                NetworkStream networkStreamClient = client.GetStream();

                if (networkStreamClient != null)
                {
                    await networkStreamClient.DisposeAsync();
                }

                client.Dispose();
                Console.WriteLine("Client disconnected"); // {_(!)_} For testing purposes
            }
        }

        public async ValueTask DisposeAsync()
        {
            // Unsubscribe from events
            GS_Manager.SocketMessageEvent -= WriteMessageToUsers;
            GS_Manager.bannedClientConnectedEvent -= HandleBannedClient;

            // Cancel all communication tasks
            GlobalCancelSource.Cancel();

            // Cleaning up all cancellation tokens 
            foreach (var cancelSource in communicationCancelSources.Values)
            {
                cancelSource.Dispose();
            }
        }
    }
}