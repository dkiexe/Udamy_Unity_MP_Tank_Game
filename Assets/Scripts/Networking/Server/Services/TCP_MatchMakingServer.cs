using Assets.Scripts.Networking.Server.Services;
using Assets.Scripts.Networking.Shared.Services;
using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class TCP_MatchMakingServer : IDisposable
{
    /// <summary>
    /// A TCP Server interface to connect to the fleet server.
    /// </summary>
    /// 

    const int TCP_FleetServerPort = 9090;

    private string TCP_FleetServerIP;

    private TcpClient tcpClient;

    private NetworkStream networkDataStream;

    private CancellationTokenSource heartBeatCancelSource;

    private TCP_Socket TCP_socket;

    private const int HeartBeatDelay = 5000; // Send heartbeat every 5 seconds

    // Events ( raising events from the TCP fleet server to the GameServerManager )
    public event Action<TCP_MatchData> OnMatchDataUpdate;

    public TCP_MatchMakingServer()
    {
        TCP_socket = new TCP_Socket();
        TCP_FleetServerIP = GetLocalIPv4();
    }

    public async Task<TCP_MatchData> LogInAsync(int GameServerID, CancellationToken cancelToken)
    {
        tcpClient = new TcpClient();
        try
        {
            await tcpClient.ConnectAsync(TCP_FleetServerIP, TCP_FleetServerPort);
        }
        catch (SocketException ex)
        {
            Debug.LogError($"Failed to connect to matchmaking server: {ex.Message}");
            throw;
        }

        // After connecting to the fleet server, we send a registration message with a GameServerID.
        networkDataStream = tcpClient.GetStream();
        string loginMessage = $"REGISTER|{GameServerID}";

        await TCP_socket.SendTCPMessageAsync(networkDataStream, loginMessage, cancelToken);

        // We then wait for a response from the fleet server with the match properties for this game server.
        string[] FleetServerMessage = (await TCP_socket.ReceiveTCPMessageAsync(
            networkDataStream,
            cancelToken
            )).Split("|");

        string messageType = FleetServerMessage[0];

        if (messageType != "MATCHDATA")
        {
            Debug.LogWarning($"Received unexpected message type from fleet server: {messageType}, Instead of Match Data.");
            throw new OperationCanceledException();
        }

        TCP_MatchData matchProperties = JsonUtility.FromJson<TCP_MatchData>(FleetServerMessage[1]);
        return matchProperties;
    }
    
    public async Task LogOutAsync(int GameServerID, string Reason, CancellationToken cancelToken, string args = default)
    {
        string logoutMessage = $"DEREGISTER|{GameServerID}|{Reason}|{args}";
        await TCP_socket.SendTCPMessageAsync(networkDataStream, logoutMessage, cancelToken);
    }

    public async Task msgUserDisconnect(string authID, CancellationToken cancelToken)
    {
        string disconnectMessage = $"USERDISCONNECT|{authID}";
        await TCP_socket.SendTCPMessageAsync(networkDataStream, disconnectMessage, cancelToken);
    }

    public async Task HeartBeat()
    {
        heartBeatCancelSource = new CancellationTokenSource();
        while (!heartBeatCancelSource.Token.IsCancellationRequested)
        {
            await TCP_socket.SendTCPMessageAsync(networkDataStream, "HEARTBEAT", heartBeatCancelSource.Token);
            await Task.Delay(HeartBeatDelay, heartBeatCancelSource.Token);
        }
    }

    public async Task ListenToCommands(CancellationToken cancelToken)
    {
        while (!cancelToken.IsCancellationRequested)
        {
            string[] FleetServerMessage = 
            (
                await TCP_socket.ReceiveTCPMessageAsync
                (
                networkDataStream,
                cancelToken
                )
            ).Split("|");
        
            string messageType = FleetServerMessage[0];
            string[] args = FleetServerMessage[1..];

            switch (messageType)
            {
                case "MATCHDATAUPDATE":
                {
                    TCP_MatchData matchProperties = JsonUtility.FromJson<TCP_MatchData>(args[0]);
                    OnMatchDataUpdate?.Invoke(matchProperties);
                    break;
                }
            }
        }
    }

    public static string GetLocalIPv4()
    {
        foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            // Must be up and running
            if (ni.OperationalStatus != OperationalStatus.Up)
                continue;

            // Ignore loopback ( PC talking to itself ) & tunnel ( Virtual envs )
            if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
                ni.NetworkInterfaceType == NetworkInterfaceType.Tunnel)
                continue;

            var ipProps = ni.GetIPProperties();

            // IMPORTANT: Must have a default gateway ( connected to a network )
            if (!ipProps.GatewayAddresses.Any(g =>
                g.Address.AddressFamily == AddressFamily.InterNetwork))
                continue;

            foreach (var ip in ipProps.UnicastAddresses)
            {
                if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.Address.ToString();
                }
            }
        }
        return null;
    }

    public void Dispose()
    {
        networkDataStream.Dispose();
        heartBeatCancelSource.Cancel();
        heartBeatCancelSource?.Dispose();
        tcpClient.Dispose();
    }
}