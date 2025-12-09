using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HostGameManager : IDisposable
{
    /// <summary>
    /// This class is a logic class that handles the following:
    /// 1) Requesting a relay allocation from UGS and joining the relay as a client.
    /// 2) Creating a lobby on UGS and keeping it alive with heartbeats using a coroutine.
    /// 3) Starting a host server using Unity Netcode's NetworkManager on the relay server.
    /// 4) Switching to the game scene after the host is started.
    /// </summary>

    private Allocation allocation;

    private string joinCode;

    private string lobbyId;

    private const int maxConn = 20;

    private const string gameSceneName = "Game";

    public NetworkServer networkServer { get; private set; }

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
        }
        catch (Exception e)
        {
            Debug.Log(e);
            return;
        }

        // Creating a lobby on UGS with the join code as a data object.
        try
        {
            CreateLobbyOptions lobbyOptions = new CreateLobbyOptions();
            lobbyOptions.IsPrivate = false;
            lobbyOptions.Data = new Dictionary<string, DataObject>() 
            {
                {
                    "JoinCode", new DataObject(
                        visibility : DataObject.VisibilityOptions.Member, // this visibility makes sure that this DataObject can be read only if you are a member of the lobby.
                        value : joinCode
                        )
                }
            };
            string hostPlayerName = PlayerPrefs.GetString(
                NameSelector.PLAYERNAMEKEY,
                "???"
                );
            
            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(
                hostPlayerName + "'s Lobby",
                maxConn, 
                lobbyOptions
                );
            
            lobbyId = lobby.Id;
            HostSingelton.Instance.StartCoroutine(HeartBeatLobby(15));
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
            return;
        }

        networkServer = new NetworkServer(NetworkManager.Singleton);
        
        // making a new user data object to then convert to json and send to the server. ( we do this because a host is also a client )f
        UserData userData = new UserData
        {
            userName = PlayerPrefs.GetString(
                NameSelector.PLAYERNAMEKEY,
                "Guest"
                ),
            userAuthId = AuthenticationService.Instance.PlayerId
        };

        // converting the user class to a json object for sirialization.
        string payload = JsonUtility.ToJson(userData);

        // converting the json string to a byte array to be sent as a connection payload.

        byte[] payloadbytes = Encoding.UTF8.GetBytes(payload);

        // setting the connection data to be sent to the server on connect.
        NetworkManager.Singleton.NetworkConfig.ConnectionData = payloadbytes;

        // Getting the NetworkManager's transport component to set the relay server data.
        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        // Unity struct that contains all the connection information the game needs to connect to Unity Relay using the Unity Transport(UTP) networking layer.
        RelayServerData relayServerData = AllocationUtils.ToRelayServerData(allocation, "dtls");

        transport.SetRelayServerData(relayServerData); // notifying the NetworkManager's transport object about the relay server

        // Now starting host on the relay service given by unity instead of a local server.
        NetworkManager.Singleton.StartHost(); 

        NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
    }

    private IEnumerator HeartBeatLobby(float waitTimeSeconds)
    {
        /*<script> Lobbies on UGS must be pingged every 15 seconds to keep the lobby alive or else
         * USG will consider this lobby as inactive and would close it.
         * Stopping the lobby involves stopping this coroutine.
        */
        WaitForSecondsRealtime pingDelay = new WaitForSecondsRealtime(waitTimeSeconds);
        while (true)
        {
            LobbyService.Instance.SendHeartbeatPingAsync(lobbyId);
            yield return pingDelay;
        }
    }

    public async void Dispose()
    {
        // stopping the heartbeat coroutine by name to stop pinging UGS about this lobby.
        HostSingelton.Instance.StopCoroutine(nameof(HeartBeatLobby));

        if (!String.IsNullOrEmpty(lobbyId))
        {
            try
            {
                await LobbyService.Instance.DeleteLobbyAsync(lobbyId);
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }

            lobbyId = string.Empty;
        }

        networkServer?.Dispose();
    }
}
