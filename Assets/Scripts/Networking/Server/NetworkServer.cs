using System;
using Unity.Netcode;
using UnityEngine;

public class NetworkServer
{
    private NetworkManager networkManager;
    public NetworkServer(NetworkManager networkManager)
    {
        this.networkManager = networkManager;

        /* Subscribing to a connection approval event.
        * this events gets called when a person tries to connect to the server
        * and would give data about the connection request.
        * This method allows us to control traffic for our server.
        */
        networkManager.ConnectionApprovalCallback += ApprovalCheck;
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

        Debug.Log(userData.userName);

        response.Approved = true; // Approving all connections for now
        response.CreatePlayerObject = true; // let the network manager create a player object for the connection
    }
}
