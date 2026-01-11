using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkServer : IDisposable
{
    /// <summary>
    /// This class is a logic class that gets initalized by the HostGameManager and handles additional server side
    /// logic through event subscriptions to the NetworkManager, from this class we can control client approvals, 
    /// client disconnects and even set up logic to to be runned as soon as the server starts.
    /// </summary>
    private NetworkManager networkManager;

    public event Action<string> OnClientLeft;

    // Dictionary UGS server id to a UGS authintication id.
    private Dictionary<ulong, string> clientNetworkID_TO_AuthID = new Dictionary<ulong, string>();

    // Dictionary UGS authintication id to UserData object.
    private Dictionary<string, UserData> authID_TO_UserData = new Dictionary<string, UserData>();

    public NetworkServer(NetworkManager networkManager)
    {
        this.networkManager = networkManager;

        /* Subscribing to a connection approval event.
        * this events gets called when a person tries to connect to the server
        * and would give data about the connection request.
        * This method allows us to control traffic for our server.
        */
        networkManager.ConnectionApprovalCallback += ApprovalCheck;
        // Subscribing to server started event ( gets called after a server is fully initialized. ).
        networkManager.OnServerStarted += OnNetworkReady;
    }

    private void ApprovalCheck(
        NetworkManager.ConnectionApprovalRequest request, 
        NetworkManager.ConnectionApprovalResponse response
        )
    {
        // casing the request from byte[] to string
        string stringPaylaod = System.Text.Encoding.UTF8.GetString(request.Payload);

        // taking the json string and converting it to UserData object
        UserData userData = JsonUtility.FromJson<UserData>(stringPaylaod);

        clientNetworkID_TO_AuthID[request.ClientNetworkId] = userData.userAuthId;
        authID_TO_UserData[userData.userAuthId] = userData;

        response.Approved = true; // Approving all connections for now
        response.Position = SpawnPoint.GetRandomSpawnPos(); // setting a random spawn position for the player
        response.CreatePlayerObject = true; // let the network manager create a player object for the connection
    }
    
    private void OnNetworkReady()
    {
        /* Subscribing to OnServerStarted before OnClientDisconnectCallback gives the following benifits:
         * The server is fully set 
         * Networking callbacks are initialized
         * The server is ready to accept clients and fire disconnect events
         * ** Subscribing before the server is ready could lead to errors if a client disconnects before the server is fully set up. *
         */
        networkManager.OnClientDisconnectCallback += HandleClientDisconnect;
    }

    private void HandleClientDisconnect(ulong clientNetworkID)
    {
        // Dropping data from dictinaries when a client disconnects to prevent memory leaks.
        if (!clientNetworkID_TO_AuthID.TryGetValue(clientNetworkID, out string authID)) return;
        clientNetworkID_TO_AuthID.Remove(clientNetworkID);
        authID_TO_UserData.Remove(authID);
        OnClientLeft?.Invoke(authID);
    }

    public UserData GetUserDataFromClientID(ulong ClientId)
    {
        if (clientNetworkID_TO_AuthID.TryGetValue(ClientId, out string authId))
        {
            if (authID_TO_UserData.TryGetValue(authId, out UserData userData))
            {
                return userData;
            }
        }
        return null;
    }

    public void Dispose()
    {
        if (networkManager == null) return;
        networkManager.ConnectionApprovalCallback -= ApprovalCheck;
        networkManager.OnClientDisconnectCallback -= HandleClientDisconnect;
        networkManager.OnServerStarted -= OnNetworkReady;

        // Incase the network manager is still listening shut it down.
        if (networkManager.IsListening)
        {
            networkManager.Shutdown();
        }
    }
}
