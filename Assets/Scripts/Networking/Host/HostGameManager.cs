using NUnit.Framework;
using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HostGameManager
{
    private Allocation allocation;
    
    private string joinCode;

    private const int maxConn = 20;

    private const string gameSceneName = "Game";

    public async Task StartHostAsync()
    {
        // requesting a relay allocation from UGS with an X amount of max connections
        try
        {
            allocation = await RelayService.Instance.CreateAllocationAsync(maxConn);
        }
        catch (Exception e)
        {
            Debug.Log(e);
            return;
        }
        
        // requesting a join code from UGS from our UGS allocation.
        try
        {
            joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log(joinCode);
        }
        catch (Exception e)
        {
            Debug.Log(e);
            return;
        }

        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        // Unity struct that contains all the connection information the game needs to connect to Unity Relay using the Unity Transport(UTP) networking layer.
        RelayServerData relayServerData = AllocationUtils.ToRelayServerData(allocation, "dtls");

        transport.SetRelayServerData(relayServerData); // notifying the NetworkManager's transport object about the relay server

        // Now starting host on the relay service given by unity instead of a local server.
        NetworkManager.Singleton.StartHost(); 

        NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
    }
}
