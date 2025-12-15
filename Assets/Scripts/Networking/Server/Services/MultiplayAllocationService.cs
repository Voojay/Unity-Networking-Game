using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Unity.Services.Matchmaker.Models;
using Unity.Services.Multiplay;
using UnityEngine;

// General Role of this code:
// - This class runs on the dedicated server side of a unity multiplayer game using Unity's Multiplay service
// - Manages the server's life cycle during matchmaking and hosting as follows:
// 1) Waits for unity multiplay to allocate server -> Allocation = Unity decides which physical machine will run the game session
// 2) Gets Matchmaking payload -> Payload = data abt the match (Ex: Game mode, map , players)
// 3) Starts the server query handler:  makes the server visible in tools like server browsers (for status queries = requests made to a server to retrieve information about its current state, performance, or other relevant details).
// 4) Updates Server Info: Name, player count, map, game mode, etc.
// 5) Handles errors, deallocations, and clean-up.


// IDisposable -> clean up resources when shutting down
// Flow Summary: server starts (instantiate multiplayallocationservice) -> wait for allocation either from config or events -> fetches match payload like players, mode,etc -> starts query handler (makes server visible to tools) --> keeps updating multiplay: players, map, etc. -> handles clean up like deallocation or shutdown
public class MultiplayAllocationService : IDisposable
{
    private IMultiplayService multiplayService; // core obj to talk to unity's multiplay SDK 
    private MultiplayEventCallbacks serverCallbacks; // Hooks for allocation, deallocation, and error events from Multiplay
    private IServerQueryHandler serverCheckManager; // Handles reporting server info (players, name, etc.)
    private IServerEvents serverEvents; // manages event subscriptions for Multiplay
    private CancellationTokenSource serverCheckCancel; // Lets you stop the ongoing server status updates
    string allocationId; // allocation id assigned to the server

    // Constructor
    public MultiplayAllocationService()
    {
        try
        {
            multiplayService = MultiplayService.Instance; // instantiates Multiplay service singleton
            serverCheckCancel = new CancellationTokenSource(); // prepares a cancellation token
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Error creating Multiplay allocation service.\n{ex}");
        }
    }

    // Main method for dedicated servers to wait for a game session
    // Sets allocationId to null initially.

    public async Task<MatchmakingResults> SubscribeAndAwaitMatchmakerAllocation()
    {
        if (multiplayService == null) { return null; }

        allocationId = null;
        serverCallbacks = new MultiplayEventCallbacks();
        serverCallbacks.Allocate += OnMultiplayAllocation; // Registers a callback
        serverEvents = await multiplayService.SubscribeToServerEventsAsync(serverCallbacks); // Subscribes to server events from Unity Multiplay

        string allocationID = await AwaitAllocationID(); // Waits until an allocation arrives
        MatchmakingResults matchmakingPayload = await GetMatchmakerAllocationPayloadAsync(); // Fetches match data (players, map, etc.)

        return matchmakingPayload;
    }

    // Waits for Multiplay to assign the server a match.
    private async Task<string> AwaitAllocationID()
    {
        ServerConfig config = multiplayService.ServerConfig; // Pulls initial config: the confid has the serverid, alloc id, etc. as you can see below
        Debug.Log($"Awaiting Allocation. Server Config is:\n" +
            $"-ServerID: {config.ServerId}\n" +
            $"-AllocationID: {config.AllocationId}\n" +
            $"-Port: {config.Port}\n" +
            $"-QPort: {config.QueryPort}\n" +
            $"-logs: {config.ServerLogDirectory}");

        while (string.IsNullOrEmpty(allocationId))
        {
            string configID = config.AllocationId;

            if (!string.IsNullOrEmpty(configID) && string.IsNullOrEmpty(allocationId)) // if configId not null and allocId is null
            {
                Debug.Log($"Config had AllocationID: {configID}");
                allocationId = configID; // since allocid is null in this if statement
            }

            await Task.Delay(100); // Loops every 100ms until allocationId is filled.
        }

        return allocationId;

        // Sometimes the allocation arrives via config, sometimes via an event. This loop covers both cases.
    }

    // Fetches the payload associated with the allocation
    // Payload may contain playerIDs, team info, gamemode, custom metadata
    // Converts it to JSON for easier logging (as in the Debug.Log) -> the important thing in this method is payloadAllocation
    private async Task<MatchmakingResults> GetMatchmakerAllocationPayloadAsync()
    {
        MatchmakingResults payloadAllocation = await MultiplayService.Instance.GetPayloadAllocationFromJsonAs<MatchmakingResults>();
        string modelAsJson = JsonConvert.SerializeObject(payloadAllocation, Formatting.Indented);
        Debug.Log(nameof(GetMatchmakerAllocationPayloadAsync) + ":" + Environment.NewLine + modelAsJson);
        return payloadAllocation;
    }

    // Handles allocation events from multiplay once we do get the allocation 
    // prints the allocation ID and saves it into the class for use
    // This method is subbed to serverCallbacks.Allocate

    private void OnMultiplayAllocation(MultiplayAllocation allocation)
    {
        Debug.Log($"OnAllocation: {allocation.AllocationId}");

        if (string.IsNullOrEmpty(allocation.AllocationId)) { return; }

        allocationId = allocation.AllocationId;
    }

    // starts the server query handler
    // handles with the server analytics and dashboard
    // Makes your server appear in: server browser tools + query tools for player counts
    // The 20 is the default player capacity
    // starts a background loop
    public async Task BeginServerCheck()
    {
        if (multiplayService == null) { return; }

        // The second parameter here is servername and it MUST NOT BE LEFT AS AN EMPTY STRING -> it will cause an error 
        // Thus, name it to be anything u want
        serverCheckManager = await multiplayService.StartServerQueryHandlerAsync((ushort)20, "ServerName", "", "0", "");

        ServerCheckLoop(serverCheckCancel.Token);
    }

    // sets the name that will appear in server browsers
    public void SetServerName(string name)
    {
        serverCheckManager.ServerName = name;
    }
    // Useful for tracking different versions of your game server
    public void SetBuildID(string id)
    {
        serverCheckManager.BuildId = id;
    }

    // Updates the max player capacity for display.
    public void SetMaxPlayers(ushort players)
    {
        serverCheckManager.MaxPlayers = players;
    }

    public void AddPlayer()
    {
        serverCheckManager.CurrentPlayers++;
    }

    public void RemovePlayer()
    {
        serverCheckManager.CurrentPlayers--;
    }

    // Updates the map name shown in server lists.
    public void SetMap(string newMap)
    {
        serverCheckManager.Map = newMap;
    }

    // Updates the game mode (e.g. TDM, CTF).
    public void SetMode(string mode)
    {
        serverCheckManager.GameType = mode;
    }

    // Continuously updates server status to multiplay
    // It tells multiplay: current players, server name, map, gamemode
    // Without this, server browsers wouldn’t stay updated
    private async void ServerCheckLoop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            serverCheckManager.UpdateServerCheck();
            await Task.Delay(100);
        }
    }

    // Logs that the server has been released from hosting a match.
    private void OnMultiplayDeAllocation(MultiplayDeallocation deallocation)
    {
        Debug.Log(
                $"Multiplay Deallocated : ID: {deallocation.AllocationId}\nEvent: {deallocation.EventId}\nServer{deallocation.ServerId}");
    }

    // Handles errors reported by Multiplay.
    private void OnMultiplayError(MultiplayError error)
    {
        Debug.Log($"MultiplayError : {error.Reason}\n{error.Detail}");
    }

    // Removes event subscriptions + cancel background loops + unsubs from multiplay events
    public void Dispose()
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
