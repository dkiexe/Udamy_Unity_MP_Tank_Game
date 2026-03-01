using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ServerGameManager : IDisposable
{
    private string serverIP;
    private int serverPort;
    private int serverID;
    private NetworkManager networkManager;
    public NetworkServer networkServer { get; private set; }

    private TCP_MatchMakingServer tcp_MatchMakingServer;

    private const string gameSceneName = "Game";

    public ServerGameManager(
        string serverIP, 
        int serverPort,  // ServerPort -> Is a port that is used for the game to run on
        int serverID,    // ServerID -> Is a unique ID that is used to identify the server on the matchmaking server
        NetworkManager networkManager
        )
    {
        this.serverIP = serverIP;
        this.serverPort = serverPort;
        this.serverID = serverID;
        this.networkManager = networkManager;
        networkServer = new NetworkServer(networkManager);

        networkServer.OnClientLeft += HandleClientLeave;
    }

    public async Task StartGameServer()
    {
        if (networkServer.OpenConnection(serverIP, serverPort))
        {
            UnityTransport transport = networkManager.GetComponent<UnityTransport>();

            string address = transport.ConnectionData.Address;
            ushort port = transport.ConnectionData.Port;

            tcp_MatchMakingServer = new TCP_MatchMakingServer();

            await tcp_MatchMakingServer.LogInAsync(serverID); // login to match making servers as server.

            _ = tcp_MatchMakingServer.HeartBeat(); // keeps server alive 

            Debug.Log("\n =============================== ");
            Debug.Log("\n Server started successfull");
            Debug.Log($"\n Server ID: {serverID}");
            Debug.Log($"\n Server running on IP: {address} : Port : {port} Protocol : {transport.Protocol}");
            Debug.Log($"\n Server Listening Status : {networkManager.IsListening}");
            Debug.Log("\n =============================== ");

            networkManager.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
        }
        else
        {
            Debug.LogError("\n {!!!} Server failed to start...");
        }
    }

    public async void StopGameServer(string reason)
    {
        await tcp_MatchMakingServer.LogOutAsync(serverID, reason);
        Application.Quit();
    }

    private async void HandleClientLeave(string authID)
    {
        await tcp_MatchMakingServer.msgUserDisconnect(authID);
    }

    public void Dispose()
    {
        networkServer?.Dispose();
    }
}
