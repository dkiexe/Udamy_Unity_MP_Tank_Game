using System;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ServerGameManager : IDisposable
{
    private string serverIP;
    private int serverPort;
    private int serverQPort;
    private NetworkServer networkServer;
    private MultiplayAllocationService multiplayAllocationService;

    private const string gameSceneName = "Game";

    public ServerGameManager(
        string serverIP, 
        int serverPort,  // ServerPort -> Is a port that is used for the game to run on
        int serverQPort, // ServerQPort -> ( Query Port ) Is a port that is used for server analytics like health, status, etc..
        NetworkManager manager
        )
    {
        this.serverIP = serverIP;
        this.serverPort = serverPort;
        this.serverQPort = serverQPort;
        networkServer = new NetworkServer(manager);
        multiplayAllocationService = new MultiplayAllocationService();
    }

    public async Task StartGameServerAsync()
    {
        // This starts the loop that tells UGS and us the status of our server and its health.
        await multiplayAllocationService.BeginServerCheck();

        if (!networkServer.OpenConnection(serverIP, serverPort))
        {
            Debug.LogError("NetworkServer did not start as expected.");
            return;
        }
        // Changing scene.
        NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
    }

    public void Dispose()
    {
        networkServer?.Dispose();
        multiplayAllocationService?.Dispose();
    }
}
