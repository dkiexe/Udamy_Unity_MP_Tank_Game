using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkClient : IDisposable
{
    /// <summary>
    /// This logic class handles client specific networking events and logic when a client is already connected to the server.
    /// </summary>
    private NetworkManager networkManager;

    private const string MAIN_SCENE_NAME = "Menu";

    public NetworkClient(NetworkManager networkManager)
    {
        this.networkManager = networkManager;
        networkManager.OnClientDisconnectCallback += OnClientDisconnect;
    }

    private void OnClientDisconnect(ulong clientNetworkID)
    {
        /*The goal with this code is to only run the client disconnect code for the owner client. 
        * I.e. If I am running the game and another client disconnects, nothing happens. 
        * But if I am disconnected, I want to be sent back to the menu scene. 
        * Alternatively, if we are the host, and we exit, this should shut the game down for everyone.
        * The check looks for 2 things. If the clientId is  not "0" (this is the client id of the host) 
        AND if the clientId doesn't match the local id, we exit early. To put this more plainly,
        IF we are NOT the host AND we are NOT the local client, EXIT EARLY.
        */
        if (clientNetworkID != 0 && clientNetworkID != networkManager.LocalClientId) return;

        Disconnect();
        
    }

    public void Disconnect()
    {
        if (SceneManager.GetActiveScene().name != MAIN_SCENE_NAME)
        {
            SceneManager.LoadScene(MAIN_SCENE_NAME);
        }

        // if we are still somehow connected to the server as a client , we should disconnect.
        if (networkManager.IsConnectedClient)
        {
            networkManager.Shutdown();
        }
    }

    public void Dispose()
    {
        if (networkManager != null)
        {
            networkManager.OnClientDisconnectCallback -= OnClientDisconnect;
        }
    }
}