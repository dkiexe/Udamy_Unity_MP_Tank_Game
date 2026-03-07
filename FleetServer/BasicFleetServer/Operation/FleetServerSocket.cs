
using BasicFleetServer.Utils;
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
        public static event AsyncEventHandler<string[]>? newUserConnectEvent; // ( authID, UserName )
        public static event AsyncEventHandler<int>? newGameServerConnectEvent; // ( GameServerID )
        public static event Action<string>? userDisconnectEvent; // (authID)
        public static event Action<int>? gameServerDisconnectEvent; // ( GameServerID )

        // authID to TcpClient object mapping for user connections.
        public Dictionary<string, TcpClient>? UserConnectedClients;

        // GameServerID to TcpClient object mapping for GameServer connections.
        public Dictionary<int, TcpClient>? GameServerConnctedClients;

        public Dictionary<TcpClient, CancellationTokenSource> communicationCancelSources = new Dictionary<TcpClient, CancellationTokenSource>();

        private CancellationTokenSource GlobalCancelSource = new CancellationTokenSource();

        public SocketType socketType { get; private set; }

        public int Port { get; private set; }

        private int MessageTimeLimitSeconds = 10;

        private readonly string registerCmd = "REGISTER";

        private readonly string heartbeatCmd = "HEARTBEAT";

        private readonly int MaxAllowedMessageSize = 1024 * 1024; // 1 MB limit ( 1024 x 1024 bytes )

        public FleetServerSocket(int Port, SocketType socketType)
        {
            
            this.Port = Port;
            this.socketType = socketType;

            // Subscribing to events and Initializing objects based on socket type.
            if (socketType == SocketType.ForUsers)
            {
                UserConnectedClients = new Dictionary<string, TcpClient>();
                MatchMakingOperator.UserSocketMessageEvent += WriteMessageToUsers;
                Data_Manager.DropUserClientEvent += DisconnectUserClient;
            }
            else
            {
                GameServerConnctedClients = new Dictionary<int, TcpClient>();
                MatchMakingOperator.GameServerSocketMessageEvent += WriteMessageToGameServers;
                Data_Manager.DropServerClientEvent += DisconnectServerClient;
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
                await DisposeClient(client);
                return;
            }

            (string Cmd, string[] args) = RegisterRes.Value;

            if (Cmd != registerCmd)
            {
                await DisposeClient(client);
                return;
            }

            NetworkIdentity networkIdentity;

            switch (socketType)
            {
                case SocketType.ForUsers:
                    string authID = args[0];
                    if (!(UserConnectedClients!.ContainsKey(authID)))
                    {
                        UserConnectedClients[authID] = client;

                        communicationCancelSources[client] = new CancellationTokenSource();

                        networkIdentity = new NetworkIdentity
                        {
                            authID = authID
                        };

                        Task UserLoginEvent = InvokeEventAsync
                        (
                            newUserConnectEvent!,
                            this,
                            args
                        );

                        Task readerTask = ClientMessageReader(networkIdentity, client);
                    }
                    break;

                case SocketType.ForGameServers:
                    
                    if (int.TryParse(args[0], out int GameServerID))
                    {
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
                        Console.WriteLine("Invalid Game Server ID during registration.");
                    }
                    break;
            }
        }


        public async Task ClientMessageReader(NetworkIdentity networkIdentity, TcpClient client)
        {
            CancellationToken GlobalCancelToken = GlobalCancelSource.Token; // Global cancellation token for the entire server.
            CancellationToken CommCancelToken = communicationCancelSources[client].Token; // Communication cancellation token specific per client.

            // Combine both cancellation token sources for one.
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
                    UserConnectedClients!.Remove(networkIdentity.authID!);
                    userDisconnectEvent?.Invoke(networkIdentity.authID!);
                    break;

                case SocketType.ForGameServers:
                    GameServerConnctedClients!.Remove(networkIdentity.GameServerID!.Value);
                    gameServerDisconnectEvent?.Invoke(networkIdentity.GameServerID!.Value);
                    break;
            }
            await DisposeClient(client);
        }
        
        private async Task GameServerMessageReader(NetworkIdentity networkIdentity, string msg, string[] args) 
        {
            switch (msg)
            {
                case "DEREGISTER":
                    TcpClient Gameserverclient = GameServerConnctedClients![networkIdentity.GameServerID!.Value];
                    CancellationTokenSource CS_GS_client = communicationCancelSources[Gameserverclient]; //communication cancel source gameserver client 
                    gameServerDisconnectEvent?.Invoke(networkIdentity.GameServerID!.Value);
                    CS_GS_client.Cancel();
                    break;
                
                case "USERDISCONNECT":
                    userDisconnectEvent?.Invoke(args[0]);
                    break;
            }
        }

        private async Task UserMessageReader(NetworkIdentity networkIdentity, string msg, string[] args) { }

        private async Task WriteMessageToUsers(object _, (string[] authIDs, string msg) eventData)
        {
            List<Task> WriteTasks = new();

            foreach (string authID in eventData.authIDs)
            {
                if (UserConnectedClients!.TryGetValue(authID, out TcpClient? client))
                {
                    if (client.Connected)
                    {
                        NetworkStream stream = client.GetStream();
                        WriteTasks.Add(SendTCPMessageAsync(stream, eventData.msg));
                    }
                }
            }
            await Task.WhenAll(WriteTasks);
        }

        private async Task WriteMessageToGameServers(object sender, (int[] GameServerIDs, string msg) eventData)
        {
            List<Task> WriteTasks = new();

            foreach (int Gs_ID in eventData.GameServerIDs)
            {
                if (GameServerConnctedClients!.TryGetValue(Gs_ID, out TcpClient? client))
                {
                    if (client.Connected)
                    {
                        NetworkStream stream = client.GetStream();
                        WriteTasks.Add(SendTCPMessageAsync(stream, eventData.msg));
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
                string Msg = await ReceiveTCPMessageAsync(client.GetStream(), linkedCancelSources.Token);

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

        private async Task DisconnectUserClient(object _, string authID) 
        {
            TcpClient client = UserConnectedClients![authID];
            CancellationTokenSource cancelSource = communicationCancelSources[client];
            cancelSource.Cancel(); // Cancel any ongoing communication tasks for this client.
        }

        private async Task DisconnectServerClient(object _, int ServerID)
        {
            TcpClient client = GameServerConnctedClients![ServerID];
            CancellationTokenSource cancelSource = communicationCancelSources[client];
            cancelSource.Cancel(); // Cancel any ongoing communication tasks for this client.
        }

        private async Task DisposeClient(TcpClient client) // Destroy clients here.
        {
            if (client.Connected) // if still connected, attempt to shutdown the connection gracefully before disposing of the client object.
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
            }
        }

        private async Task SendTCPMessageAsync(NetworkStream stream, string data)
        {
            // converting the message into a byte array.
            byte[] messageBytes = Encoding.UTF8.GetBytes(data);

            // converting the length of the message into a byte array (4 bytes for an int).
            byte[] lengthPrefix = BitConverter.GetBytes(messageBytes.Length);

            // Send length first as an array of 4 bytes
            await stream.WriteAsync(lengthPrefix, 0, lengthPrefix.Length);

            // Send actual message as an array of bytes
            await stream.WriteAsync(messageBytes, 0, messageBytes.Length);
        }

        private async Task<string> ReceiveTCPMessageAsync(NetworkStream stream, CancellationToken CancelToken)
        {
            // Read length (4 bytes)
            byte[] lengthBuffer = await ReadExactAsync(stream, 4, CancelToken);

            if (lengthBuffer == Array.Empty<byte>())
            {
                throw new OperationCanceledException(); // Treat as disconnection
            }

            int messageLength = BitConverter.ToInt32(lengthBuffer, 0);

            // safety check for message length to prevent potential DoS attacks with extremely large messages.
            if (messageLength > MaxAllowedMessageSize) 
            {
                Console.WriteLine("[!] Received a message that exceeds the maximum allowed size. Disconnecting client.");
                throw new OperationCanceledException(); // Treat as disconnection
            }

            // Read actual message
            byte[] messageBuffer = await ReadExactAsync(stream, messageLength, CancelToken);

            if (messageBuffer == Array.Empty<byte>())
            {
                throw new OperationCanceledException(); // Treat as disconnection
            }

            string message = Encoding.UTF8.GetString(messageBuffer);

            return message;
        }

        private async Task<byte[]> ReadExactAsync(NetworkStream stream, int size, CancellationToken CancelToken)
        {
            byte[] buffer = new byte[size]; // creating an empty buffer array of bytes of the expected message size.
            int totalRead = 0; // total count of the bytes read so far.

            while (totalRead < size) // loop until we have read the expected number of bytes.
            {
                // ReadAsync Modifies buffer in place, so we pass the same buffer and adjust the offset and count based on how many bytes we've already read.
                int bytesRead = await stream.ReadAsync(buffer, totalRead, size - totalRead, CancelToken);

                if (bytesRead == 0) return Array.Empty<byte>();

                totalRead += bytesRead; // add the number of bytes read in this iteration to the total count.
            }
            return buffer; // return the modified buffer which now contains the complete message read from the stream.
        }

        public async ValueTask DisposeAsync()
        {
            // Unsubscribe from events
            if (socketType == SocketType.ForUsers)
            {
                MatchMakingOperator.UserSocketMessageEvent -= WriteMessageToUsers;
                Data_Manager.DropUserClientEvent -= DisconnectUserClient;
            }
            else
            {
                MatchMakingOperator.GameServerSocketMessageEvent -= WriteMessageToGameServers;
                Data_Manager.DropServerClientEvent -= DisconnectServerClient;
            }

            // Cancel all communication tasks
            GlobalCancelSource.Cancel();

            // Cleaning up all cancellation tokens 
            foreach (CancellationTokenSource cancelSource in communicationCancelSources.Values)
            {
                if (cancelSource == null) continue;
                cancelSource.Dispose();
            }
        }
    }
}