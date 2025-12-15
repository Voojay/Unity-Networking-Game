using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Matchmaker.Models;
using UnityEngine;
using UnityEngine.SceneManagement;


public class ServerGameManager : IDisposable
{
    private string serverIP;
    private int serverPort;
    private int queryPort;
    private MatchplayBackfiller backfiller;
    private Dictionary<string, int> teamIdToTeamIndex = new Dictionary<string, int>(); // for converting teamId to the teamindex in UserJoined method for setting the team colors
    public NetworkServer NetworkServer { get; private set; } // handles approving connections, handles when people connect/disconnect
    private MultiplayAllocationService multiplayAllocationService; // One of the four import scripts
    private const string GameSceneName = "Game";


    // Constructor
    public ServerGameManager(string serverIP, int serverPort, int queryPort, NetworkManager manager, NetworkObject playerPrefab)
    {
        this.serverIP = serverIP;
        this.serverPort = serverPort;
        this.queryPort = queryPort;
        NetworkServer = new NetworkServer(manager, playerPrefab);
        multiplayAllocationService = new MultiplayAllocationService();
    }

    public async Task StartGameServerAsync() // Called once we successfully connected to UGS
    {
        await multiplayAllocationService.BeginServerCheck(); // Starts the loop that tells us the status of our server, like the players on it and the health of server, etc.

        // Do an API call to UGS to get match data using thisL multiplayAllocationService.SubscribeAndAwaitMatchmakerAllocation()
        try
        {
            MatchmakingResults matchmakerPayload = await GetMatchmakerPayload();

            // Remember, if it timed out it returns null
            if (matchmakerPayload != null)
            {
                // If we did get the payload, start backfilling here (process of getting more players into your game once it has started)
                // Note that backfilling requires the payLoad (of type matchmakingresults which is part of the namespace: Unity.Services.Matchmaker.Models)

                await StartBackfill(matchmakerPayload); // this is our own method that we created that passes in the payload

                // Now we have to subscribe with the UserJoined and UserLeft methods so they will be called 
                // Such actions will be in the networkserver and the invokes of such will be in the approval check method where we handle a player joining
                NetworkServer.OnUserJoined += UserJoined;
                NetworkServer.OnUserLeft += UserLeft;
            }
            else
            {
                Debug.LogWarning("Matchmaker payload timed out");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning(e);
        }

        // Now that we have done that, we can then open up our server, start running it and have clients connect
        // This method returns a bool whether or not opening the connection was successful or not
        // This if statements says that if opening connection failed...
        if (!NetworkServer.OpenConnection(serverIP, serverPort))
        {
            Debug.LogWarning("NetworkServer did not start as expected");
            return;
        }

        // Btw, no need for the server to change scene now because at this point, the server should be in the correct scene (in app. controller/s coroutine)
        
    }



    // Gets matchmaking data and returns MatchmakingResult (a class in the MatchplayMatchMaker)
    // We also want a time out for this so that it does not hang on this forever if it's not getting anything back
    private async Task<MatchmakingResults> GetMatchmakerPayload()
    {
        // This method is from the imported scripts
        // This method will handle subscribing to events and also return the matchmakingPayload which is of type MatchmakingResults (with an s!!)
        // We are storing this in a var: so, multiplayAllocationService.SubscribeAndAwaitMatchmakerAllocation(); IS NOT RUNNING YET. It's just stored in a task var
        // We are doing this for timing out
        Task<MatchmakingResults> matchmakerPayloadTask = multiplayAllocationService.SubscribeAndAwaitMatchmakerAllocation();

        // waits for the first task to finish out of a group of tasks and returns that completed task.
        // So if timing out means that Task.Delay() is finished first before the matchmakerPayloadTask
        // So in this line, it means that if the Task that finishes first is matchmakerPayloadTask. If no, return null.
        if (await Task.WhenAny(matchmakerPayloadTask, Task.Delay(20000)) == matchmakerPayloadTask)
        {
            return matchmakerPayloadTask.Result;
        }
        return null;
    }

    private async Task StartBackfill(MatchmakingResults payload)
    {
        // Create a new instance of backfiller (one of the imported scripts)
        // The constructor takes in the string connection (server's connection string ("IP:port")), queuename and matchproperties (get from the payload), maxplayers
        // store this obj locally 
        backfiller = new MatchplayBackfiller($"{serverIP}:{serverPort}",
            payload.QueueName,
            payload.MatchProperties,
            20);

        // Check first if  max
        if (backfiller.NeedsPlayers())
        {
            await backfiller.BeginBackfilling(); // a method in the imported script
        }
    }

    // runs whenever a new player joins the game --> for backfilling purposes
    private void UserJoined(UserData user)
    {
        // This is just for Debug.Log
        // team.TeamName = A human-readable name, like "My Team" -> this is what we named it as in our dashboard for matchmaker --> BUT it will be the same for everyone for ALL TEAMS
        // however the team Id would be different for each team! -> use this instead
        // team.TeamId:  A unique identifier, like a GUID or "blue-1234" → Used internally to track the team, especially when names might repeat.
        Team team = backfiller.GetTeamByUserId(user.userAuthId);
        Debug.Log($"{user.userName} has joined the teamId of: {team.TeamId}");

        // If the user joining a team that is already in the dictionary (already in the match)
        // This line means if we cant get the value of the teamindex from the teamid (this teamid not in dict) -> they are the first one to join that team
        if (!teamIdToTeamIndex.TryGetValue(team.TeamId, out int teamIndex))
        {
            // Right now, we didnt get teamIndex cuz it is !
            // So set the teamindex
            teamIndex = teamIdToTeamIndex.Count;
            // Add this teamid and teamindex into the dictionary
            teamIdToTeamIndex.Add(team.TeamId, teamIndex);
        }

        user.teamIndex = teamIndex;

            multiplayAllocationService.AddPlayer(); // increments the player count in the multiplayAllocationService

        // Check if it is full
        if (!backfiller.NeedsPlayers() && backfiller.IsBackfilling)
        {
            _ = backfiller.StopBackfill(); // no need to await -> _ = means that we disregard the task that is returned
        }
    }

    // The same as above but for removing
    private void UserLeft(UserData user)
    {
        int playerCount = backfiller.RemovePlayerFromMatch(user.userAuthId);
        multiplayAllocationService.RemovePlayer();

        if (playerCount <= 0) // empty server
        {
            // Close Server
            CloseServer();
            return;
        }

        // we need players and we are not backfilling 
        if (backfiller.NeedsPlayers() && !backfiller.IsBackfilling)
        {
            _ = backfiller.BeginBackfilling();
        }
    }

    private async void CloseServer() // no one gonna await this method (cuz we dont have to wait for it to finish), hence the void
    {
        await backfiller.StopBackfill();
        Dispose();
        Application.Quit(); // the dedicated server at this point will be shut down entirely
    }



    // When this class gets cleaned up -> we make sure that we clean up anything we instantiate here
    public void Dispose()
    {
        NetworkServer.OnUserJoined -= UserJoined;
        NetworkServer.OnUserLeft -= UserLeft;
        backfiller?.Dispose();
        multiplayAllocationService?.Dispose();
        NetworkServer?.Dispose();
    }
}
