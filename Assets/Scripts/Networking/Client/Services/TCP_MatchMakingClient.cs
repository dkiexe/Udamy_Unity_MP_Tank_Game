using Assets.Scripts.Networking.Shared.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class TCP_MatchMakingClient : IAsyncDisposable
{
    /// <summary>
    /// A TCP client to connect to the matchmaking server.
    /// Works with Basic Fleet server.
    /// </summary>
    private static string TCP_ServerIP;

    private int TCP_ServerPort;

    private TcpClient client;

    private NetworkStream networkDataStream;

    private TCP_Socket TCP_socket;

    private const int HeartBeatDelay = 5000; // Send heartbeat every 5 seconds

    public TCP_MatchMakingClient()
    {
        TCP_socket = new TCP_Socket();
        ReadJson();
    }

    public void ReadJson()
    {
        string relFilePath = Path.Combine(Application.streamingAssetsPath, "tcp_matchmaking_config.json");
        if (File.Exists(relFilePath))
        {
            string jsonContent = File.ReadAllText(relFilePath);
            TCP_ConfigData configData = JsonUtility.FromJson<TCP_ConfigData>(jsonContent);
            if (configData.serverIP == null && configData.serverPort == default)
            {
                Debug.LogError("TCP Matchmaking Config file invalid!");
                return;
            }
            TCP_ServerIP = configData.serverIP;
            TCP_ServerPort = configData.serverPort;
        }
        else
        {
            Debug.LogError("TCP Matchmaking Config file not found! using Default Values!");
        }
    }

    public async Task<NetworkOperationResult> LogInAsync(
        string authID, 
        string userName,
        int QueueType,
        CancellationToken cancelToken
        )
    {
        client = new TcpClient();
        bool result = false;
        List<string> msg = new List<string>();
        bool ExceptionHappend = false;
        try
        {
            await client.ConnectAsync(TCP_ServerIP, TCP_ServerPort);
            
            string loginMessage = $"REGISTER|{authID}|{userName}|{QueueType}";
            
            networkDataStream = client.GetStream();

            await TCP_socket.SendTCPMessageAsync(networkDataStream, loginMessage, cancelToken);

            result = true;
        }
        catch (SocketException ex)
        {
            msg.Add($"Failed to connect to matchmaking server: Unavailable or Unreachable Server.");
            Debug.LogWarning(ex);
            
        }
        catch (IOException ex)
        {
            msg.Add($"Failed to connect to matchmaking server: connection closed.");
            Debug.LogWarning(ex);
            
        }
        catch (OperationCanceledException)
        {
            msg.Add("MatchMaking Canceled.");
        }
        catch (Exception ex)
        {
            msg.Add($"Failed to connect to matchmaking server: Unexpected error");
            Debug.LogWarning(ex);
            
        }
        
        if (ExceptionHappend) await DisposeAsync();
        
        return new NetworkOperationResult { success = result, message = msg.ToArray() };
    }


    public async Task<NetworkOperationResult> AwaitServerAssignmentAsync(CancellationToken MM_cancelToken)
    {
        bool result = false;
        List<string> msg = new List<string>();
        bool ExceptionHappend = false;

        CancellationTokenSource HeartBeatCancelSource = new CancellationTokenSource();
        CancellationTokenSource CombinedCancelSoruce = CancellationTokenSource.CreateLinkedTokenSource(
            MM_cancelToken, HeartBeatCancelSource.Token
        );

        try
        {
            Task HeartBeatTask = HeartBeat(networkDataStream, CombinedCancelSoruce.Token);

            string RecivedMessage = await TCP_socket.ReceiveTCPMessageAsync(networkDataStream, CombinedCancelSoruce.Token);

            CombinedCancelSoruce.Cancel(); // stop heartbeat task once we received a server message.
            await HeartBeatTask;

            msg = new List<string>(RecivedMessage.Split("|"));

            result = true;
        }
        catch (OperationCanceledException ex)
        {
            if (!MM_cancelToken.IsCancellationRequested)
            {
                msg.Add($"MatchMaking Failed Server Communication Distrupted On Client Side.");
                Debug.LogWarning(ex);
            }
        }
        catch (SocketException ex)
        {
            msg.Add($"MatchMaking Failed Server Communication Closed");
            Debug.LogWarning(ex);
        }
        catch (IOException ex)
        {
            msg.Add($"MatchMaking Failed Server Communication Closed");
            Debug.LogWarning(ex);
        }
        catch (Exception ex)
        {
            msg.Add($"MatchMaking Failed: Unexpected error");
            Debug.LogWarning(ex);

        }

        if (ExceptionHappend) await DisposeAsync();

        HeartBeatCancelSource.Dispose();
        CombinedCancelSoruce.Dispose();
        return new NetworkOperationResult { success = result, message = msg.ToArray() };
    }

    private async Task HeartBeat(NetworkStream networkDtatStream, CancellationToken cancelToken)
    {
        byte[] heartbeatMessage = Encoding.UTF8.GetBytes("HEARTBEAT");
        try
        {
            while (!cancelToken.IsCancellationRequested)
            {
                await TCP_socket.SendTCPMessageAsync(networkDtatStream, "HEARTBEAT", cancelToken);
                await Task.Delay(HeartBeatDelay, cancelToken);
            }
        }
        catch (TaskCanceledException) { }
        catch (IOException)
        {
            Debug.LogWarning("HeartBeat Connection Lost to the server.");
        }
        catch (Exception ex)
        {
            Debug.LogError(ex.ToString());
        }
    }

    public async ValueTask DisposeAsync()
    {
        await networkDataStream.DisposeAsync();
        client.Dispose();
    }
}