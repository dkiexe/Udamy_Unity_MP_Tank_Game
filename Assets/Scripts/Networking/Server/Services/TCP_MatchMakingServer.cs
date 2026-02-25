using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
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

    private const int HeartBeatDelay = 5000; // Send heartbeat every 5 seconds

    public TCP_MatchMakingServer()
    {
        TCP_FleetServerIP = GetLocalIPv4();
    }

    public async Task LogInAsync(int GameServerID)
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

        networkDataStream = tcpClient.GetStream();

        string loginMessage = $"REGISTER|{GameServerID}";
        byte[] data = Encoding.UTF8.GetBytes(loginMessage);
        await networkDataStream.WriteAsync(data, 0, data.Length);
        Debug.Log("\n Written ID to matchmaking server.");
    }
    
    public async Task LogOutAsync(int GameServerID, string Reason)
    {
        string logoutMessage = $"DEREGISTER|{GameServerID}|{Reason}";
        byte[] data = Encoding.UTF8.GetBytes(logoutMessage);
        await networkDataStream.WriteAsync(data, 0, data.Length);
    }

    public async Task msgUserDisconnect(string ip)
    {
        string disconnectMessage = $"USERDISCONNECT|{ip}";
        byte[] data = Encoding.UTF8.GetBytes(disconnectMessage);
        await networkDataStream.WriteAsync(data, 0, data.Length);
    }

    public async Task HeartBeat()
    {
        heartBeatCancelSource = new CancellationTokenSource();
        byte[] heartbeatMessage = Encoding.UTF8.GetBytes("HEARTBEAT");
        while (!heartBeatCancelSource.Token.IsCancellationRequested)
        {
            await networkDataStream.WriteAsync(heartbeatMessage, 0, heartbeatMessage.Length);
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