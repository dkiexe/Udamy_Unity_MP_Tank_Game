using Assets.Scripts.Networking.Server.Services;
using Assets.Scripts.Networking.Shared.Services;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class TCP_MatchMakingServer : IAsyncDisposable
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

    public TCP_MatchMakingServer()
    {
        TCP_socket = new TCP_Socket();
        TCP_FleetServerIP = GetLocalIPv4();
    }

    public async Task<TCP_MatchData> LogInAsync(int GameServerID)
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

        await TCP_socket.SendTCPMessageAsync(networkDataStream, loginMessage, CancellationToken.None);

        // We then wait for a response from the fleet server with the match properties for this game server.
        while (true)
        {
            string[] FleetServerMessage = (await TCP_socket.ReceiveTCPMessageAsync(
                networkDataStream, 
                CancellationToken.None
                )).Split("|");

            string messageType = FleetServerMessage[0];

            if (messageType != "MATCHDATA")
            {
                Debug.LogWarning($"Received unexpected message type from fleet server: {messageType}, Instead of Match Data.");
                continue;
            }

            TCP_MatchData matchProperties = JsonUtility.FromJson<TCP_MatchData>(FleetServerMessage[1]);
            Debug.Log("Recived MatchData!"); // {_(!)_} TEST REMOVE! 
            return matchProperties;
        }
    }
    
    public async Task LogOutAsync(int GameServerID, string Reason)
    {
        string logoutMessage = $"DEREGISTER|{GameServerID}|{Reason}";
        await TCP_socket.SendTCPMessageAsync(networkDataStream, logoutMessage, CancellationToken.None);
    }

    public async Task msgUserDisconnect(string authID)
    {
        string disconnectMessage = $"USERDISCONNECT|{authID}";
        await TCP_socket.SendTCPMessageAsync(networkDataStream, disconnectMessage, CancellationToken.None);
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

    public static string GetLocalIPv4()
    {
        foreach (IPAddress ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork &&
                !IPAddress.IsLoopback(ip))
            {
                Debug.Log(ip);
                return ip.ToString();
            }
        }

        return "No IPv4 address found";
    }

    public async ValueTask DisposeAsync()
    {
        await networkDataStream.DisposeAsync();
        heartBeatCancelSource.Cancel();
        heartBeatCancelSource?.Dispose();
        tcpClient.Dispose();
    }
}