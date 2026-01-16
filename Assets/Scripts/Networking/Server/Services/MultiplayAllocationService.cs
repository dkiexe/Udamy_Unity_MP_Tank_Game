using Newtonsoft.Json;
using System;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Matchmaker.Models;
using Unity.Services.Multiplay;
using Unity.Services.Relay.Models;
using Unity.VisualScripting;
using UnityEngine;

public class MultiplayAllocationService : IDisposable
{
    /// <summary>
    /// This Class IS responsible for Requesting and managing dedicated server allocations from the Unity Gaming Services.
    /// </summary>
    private IMultiplayService multiplayService;
    private MultiplayEventCallbacks serverCallbacks;
    private IServerQueryHandler serverCheckManager;
    private IServerEvents serverEvents;
    private CancellationTokenSource serverCheckCancel;
    string allocationId;

    public MultiplayAllocationService()
    {
        try
        {
            multiplayService = MultiplayService.Instance; // Get the singleton instance of the Multiplay service.
            serverCheckCancel = new CancellationTokenSource(); // A cancellation token source to stop the server check loop when disposing.
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Error creating Multiplay allocation service.\n{ex}");
        }
    }

    public async Task<MatchmakingResults> SubscribeAndAwaitMatchmakerAllocation()
    {
        /// This function subscribes to the Multiplay server events and waits for an allocation from UGS for a server.
        if (multiplayService == null) { return null; }

        allocationId = null;
        serverCallbacks = new MultiplayEventCallbacks(); // creates a new set of server event callbacks.
        serverCallbacks.Allocate += OnMultiplayAllocation; // overriding the allocate event.
        serverEvents = await multiplayService.SubscribeToServerEventsAsync(serverCallbacks); // subscribing to server events with the defined callbacks.

        string allocationID = await AwaitAllocationID(); // awaiting an allocation ID from the server events.
        MatchmakingResults matchmakingPayload = await GetMatchmakerAllocationPayloadAsync(); // getting the matchmaking payload associated with the allocation.

        return matchmakingPayload;
    }

    private async Task<string> AwaitAllocationID()
    {
        /// AwaitAllocationID() blocks (asynchronously) until this server instance is officially allocated by Multiplay and returns the allocation ID once it exists.
        /// In simple terms:
        /// It waits until Unity says: “This server is yours, here’s the allocation ID.”
        ServerConfig config = multiplayService.ServerConfig;
        Debug.Log($"Awaiting Allocation. Server Config is:\n" +
            $"-ServerID: {config.ServerId}\n" +
            $"-AllocationID: {config.AllocationId}\n" +
            $"-Port: {config.Port}\n" +
            $"-QPort: {config.QueryPort}\n" +
            $"-logs: {config.ServerLogDirectory}");

        while (string.IsNullOrEmpty(allocationId))
        {
            string configID = config.AllocationId;

            if (!string.IsNullOrEmpty(configID) && string.IsNullOrEmpty(allocationId))
            {
                Debug.Log($"Config had AllocationID: {configID}");
                allocationId = configID;
            }

            await Task.Delay(100);
        }

        return allocationId;
    }

    private async Task<MatchmakingResults> GetMatchmakerAllocationPayloadAsync()
    {
        // This method reads and deserializes the Matchmaker data that Unity attached to your server allocation.
        MatchmakingResults payloadAllocation = await MultiplayService.Instance.GetPayloadAllocationFromJsonAs<MatchmakingResults>(); // getting the network matchmaking payload from the Multiplay service.
        string modelAsJson = JsonConvert.SerializeObject(payloadAllocation, Formatting.Indented); // formatting the payload as JSON for logging.
        Debug.Log(nameof(GetMatchmakerAllocationPayloadAsync) + ":" + Environment.NewLine + modelAsJson); // logging the payload.
        return payloadAllocation;
    }

    public async Task BeginServerCheck()
    {
        ///This function is what starts your dedicated server “heartbeat / status reporting” to Unity Multiplay so that UGS knows your server is alive, queryable, and what state it’s in.
        if (multiplayService == null) { return; }

        // Starting the server query handler with default values to avoids wrong data being published, this also enables server analytics in UGS.
        serverCheckManager = await multiplayService.StartServerQueryHandlerAsync((ushort) 20, "", "", "0", "");

        ServerCheckLoop(serverCheckCancel.Token);
    }

    // The following methods are used to update the server attributes.
    public void SetServerName(string name) // sets the name of the server
    {
        serverCheckManager.ServerName = name;
    }
    public void SetBuildID(string id) // sets the buildID of the server
    {
        serverCheckManager.BuildId = id;
    }

    public void SetMaxPlayers(ushort players) // sets the max player count of the server
    {
        serverCheckManager.MaxPlayers = players;
    }

    public void AddPlayer() // A callback that handles what happens when a player joins the server.
    {
        serverCheckManager.CurrentPlayers++;
    }

    public void RemovePlayer() // A callback that handles what happens when a player joins the server.
    {
        serverCheckManager.CurrentPlayers--;
    }

    public void SetMap(string newMap) // sets the map of the server
    {
        serverCheckManager.Map = newMap;
    }

    public void SetMode(string mode) // sets the game mode count of the server
    {
        serverCheckManager.GameType = mode;
    }
    ///

    private async void ServerCheckLoop(CancellationToken cancellationToken)
    {
        // This function is “heartbeating / status reporting” to Unity Multiplay so that UGS knows your server is alive, queryable, and what state it’s in.
        while (!cancellationToken.IsCancellationRequested)
        {
            serverCheckManager.UpdateServerCheck();
            await Task.Delay(100);
        }
    }

    // Allocation Outcome Callbacks
    private void OnMultiplayAllocation(MultiplayAllocation allocation) // gets called when the server is allocated. 
    {
        Debug.Log($"OnAllocation: {allocation.AllocationId}");

        if (string.IsNullOrEmpty(allocation.AllocationId)) { return; }

        allocationId = allocation.AllocationId;
    }

    private void OnMultiplayDeAllocation(MultiplayDeallocation deallocation) // gets called when the server is deallocated.
    {
        Debug.Log(
                $"Multiplay Deallocated : ID: {deallocation.AllocationId}\nEvent: {deallocation.EventId}\nServer{deallocation.ServerId}");
    }

    private void OnMultiplayError(MultiplayError error) // gets called when there is an error with the multiplay service
    {
        Debug.Log($"MultiplayError : {error.Reason}\n{error.Detail}");
    }

    public void Dispose() // unsbscribes from the multiplay events and cancels the server check loop.
    {
        if (serverCallbacks != null)
        {
            serverCallbacks.Allocate -= OnMultiplayAllocation;
            serverCallbacks.Deallocate -= OnMultiplayDeAllocation;
            serverCallbacks.Error -= OnMultiplayError;
        }

        if (serverCheckCancel != null)
        {
            serverCheckCancel.Cancel();
        }

        serverEvents?.UnsubscribeAsync();
    }
}