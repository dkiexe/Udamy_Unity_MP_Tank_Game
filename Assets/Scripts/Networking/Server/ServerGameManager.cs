using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
using Assets.Scripts.Networking.Server.Services;

public class ServerGameManager : IDisposable
{
    private string serverIP;
    private int serverPort;
    private int serverID;
    private NetworkManager networkManager;
    public NetworkServer networkServer { get; private set; }

    private TCP_MatchMakingServer tcp_MatchMakingServer;

    private const string gameSceneName = "Game";

    private HashSet<UserData> connectedUsers = new HashSet<UserData>();

    private TCP_MatchData matchData;

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
        networkServer.OnClientConnected += HandleClientConnected;
    }

    public async Task StartGameServer()
    {
        if (networkServer.OpenConnection(serverIP, serverPort))
        {
            UnityTransport transport = networkManager.GetComponent<UnityTransport>();

            string address = transport.ConnectionData.Address;
            ushort port = transport.ConnectionData.Port;

            tcp_MatchMakingServer = new TCP_MatchMakingServer();

            matchData = await tcp_MatchMakingServer.LogInAsync(serverID); // login to match MakingServers as GameServer.

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

    private void HandleClientConnected(UserData user)
    {
        connectedUsers.Add(user);
        string authID = user.userAuthId;

        Team team = GetTeamByUserId(authID); // {_(!)_} Possible Issue Here
        Debug.Log($"{team.TeamID}");
    }
    
    public Team GetTeamByUserId(string userId)
    {
        return matchData.Teams.FirstOrDefault(
            team => team.Players.Contains(userId)
            );
    }

    public async void StopGameServer(string reason)
    {
        // send a message to the matchmaking server that the server is shutting down so it can update its records and stop sending players to this server.
        await tcp_MatchMakingServer.LogOutAsync(serverID, reason);
        Application.Quit();
    }

    private async void HandleClientLeave(string authID)
    {
        // remove player from the match team assignment.
        GetTeamByUserId(authID).Players.Remove(authID);

        // remove user from the connected users hash.
        connectedUsers.RemoveWhere(user => user.userAuthId == authID);

        // send a message to the matchmaking server that the user has disconnected so it can update its records and allow the user to rejoin later.
        await tcp_MatchMakingServer.msgUserDisconnect(authID);
    }

    public void Dispose()
    {
        networkServer.OnClientLeft -= HandleClientLeave;
        networkServer.OnClientConnected -= HandleClientConnected;
        networkServer?.Dispose();
    }
}
