using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ServerGameManager : IDisposable
{
    private string serverIP;
    private int serverPort;
    private NetworkManager networkManager;
    public NetworkServer networkServer { get; private set; }

    private const string gameSceneName = "Game";

    public ServerGameManager(
        string serverIP, 
        int serverPort,  // ServerPort -> Is a port that is used for the game to run on
        NetworkManager networkManager
        )
    {
        this.serverIP = serverIP;
        this.serverPort = serverPort;
        this.networkManager = networkManager;
        networkServer = new NetworkServer(networkManager);
    }

    public async Task StartGameServer()
    {
        if (networkServer.OpenConnection(serverIP, serverPort))
        {
            int serverID = PlayerPrefs.GetInt("id");

            UnityTransport transport = networkManager.GetComponent<UnityTransport>();

            string address = transport.ConnectionData.Address;
            ushort port = transport.ConnectionData.Port;

            TCP_MatchMakingServer tCP_MatchMakingServer = new TCP_MatchMakingServer();

            await tCP_MatchMakingServer.LogInAsync(serverID); // login to match making servers as server.

            _ = tCP_MatchMakingServer.HeartBeat(); // keeps server alive 

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

    public void Dispose()
    {
        networkServer?.Dispose();
    }
}
