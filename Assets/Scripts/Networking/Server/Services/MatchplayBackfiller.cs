using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.Matchmaker;
using Unity.Services.Matchmaker.Models;
using UnityEngine;

// General Role of the code:
// 1) Handles backfilling in unity multiplayer using the unity matchmaker service
//   - Backfilling means filling empty slots in a partially full running multiplayer game
// 2) Creates and manages a backfill ticket with unity matchmaker
// 3) Allows adding/removing players from the backfill state
// 4) Periodically updates the ticket or approves it 
// 5) Stops backfilling once match is full

// Overall flow of class:
// 1) Initialize the local ticket
// 2) Start backfilling -> begin updating the ticket regularly
// 3) Add/Remove players -> Update local ticket info
// 4) Backfill loop: sends updates to marchmaker + approves ticket (check if new players can be matched) + stops once full
// 5) Dispose and clean up

public class MatchplayBackfiller : IDisposable
{
    private CreateBackfillTicketOptions createBackfillOptions; // options needed when creating a backfill ticket (like queue name, conection info)
    private BackfillTicket localBackfillTicket; // Holds the current backfill ticket (the ticket's ID and the current state of the match)
    private bool localDataDirty; // true if local data (ex: player list) has changed and needs uploading to the matchmaker
    private int maxPlayers; // how many players the match should have in total
    private const int TicketCheckMs = 1000; // Delay between polling/updating the ticket in milliseconds

    // Returns the number of players currently in the local ticket
    // This is NOT a lambda expression
    // The => in this case just means that this property returns the value of the expression that follows
    // A ?? B means that if A is not null, return A. If A is null, return B.
    // In this case, if localBackfillTicket is indeed null, then that whole chain will become null and then it returns zero as the matchplayercount
    private int MatchPlayerCount => localBackfillTicket?.Properties.MatchProperties.Players.Count ?? 0;

    // Shortcut to get the matchproperties object -> this stores player and team info
    private MatchProperties MatchProperties => localBackfillTicket.Properties.MatchProperties;

    // Tracks whether backfilling is currently running
    public bool IsBackfilling { get; private set; }

    // this is the class constructor
    // Para: connection = server's connection string (IP, port, etc.), queueName = the matchmaker queue to use, matchmakerPayloadProperties = initial match data (existing players, teams, etc.)
    public MatchplayBackfiller(string connection, string queueName, MatchProperties matchmakerPayloadProperties, int maxPlayers)
    {
        this.maxPlayers = maxPlayers;
        BackfillTicketProperties backfillProperties = new BackfillTicketProperties(matchmakerPayloadProperties); // Creates backfillProperties using current match properties.
        localBackfillTicket = new BackfillTicket // creates the local backfill ticket object
        {
            Id = matchmakerPayloadProperties.BackfillTicketId, // assigns its ID if one was provided
            Properties = backfillProperties
        };

        // Prepares options for creating a ticket later (if there’s no ID yet) -> in the BeginBackfilling method
        createBackfillOptions = new CreateBackfillTicketOptions
        {
            Connection = connection,
            QueueName = queueName,
            Properties = backfillProperties
        };
    }

    public async Task BeginBackfilling()
    {
        if (IsBackfilling)
        {
            Debug.LogWarning("Already backfilling, no need to start another.");
            return;
        }

        Debug.Log($"Starting backfill Server: {MatchPlayerCount}/{maxPlayers}");

        if (string.IsNullOrEmpty(localBackfillTicket.Id)) // If the ticket ID is missing → create a new ticket via Unity Matchmaker and save its ID.
        {
            localBackfillTicket.Id = await MatchmakerService.Instance.CreateBackfillTicketAsync(createBackfillOptions);
        }

        IsBackfilling = true;

        BackfillLoop(); // keeps pinging the server
    }

    // Adds player to the local ticket
    // Note that this code is not needed --> reasons: in the comments down below
    // if u have a matchmade game --> but u also want players to be able to join their friends who are already in a match made game  --> when they join pput them on they friends team
    // The above is what u might need this code for --> but for now we comment out
    // public void AddPlayerToMatch(UserData userData)
    // {
    //     if (!IsBackfilling) // checks if backfilling has started
    //     {
    //         Debug.LogWarning("Can't add users to the backfill ticket before it's been created");
    //         return;
    //     }

    //     if (GetPlayerById(userData.userAuthId) != null) // checks if this user already exists
    //     {
    //         Debug.LogWarningFormat("User: {0} - {1} already in Match. Ignoring add.",
    //             userData.userName,
    //             userData.userAuthId);

    //         return;
    //     }

    //     // Backfilling has started and the user does not exist yet: create a new Player Object
    //     Player matchmakerPlayer = new Player(userData.userAuthId, userData.userGamePreferences);

    //     // Adds this player to the list of players in the match
    //     // This is line is no longer necessary when a player joins thru matchmaking
    //     // The matchmaker already knows that the player has joined the game and what team they are on
    //     // Hence why we dont need this entire method
    //     MatchProperties.Players.Add(matchmakerPlayer);
    //     // Adds this player to the player IDs list for Team 0 --> not ideal at all for team games
    //     MatchProperties.Teams[0].PlayerIds.Add(matchmakerPlayer.Id); 
    //     // The Matchmaker will now know about this change on the next update
    //     localDataDirty = true;
    // }

    // We need this method for matchmaker because it does not know when a player leaves
    public int RemovePlayerFromMatch(string userId)
    {
        Player playerToRemove = GetPlayerById(userId);
        if (playerToRemove == null)
        {
            Debug.LogWarning($"No user by the ID: {userId} in local backfill Data.");
            return MatchPlayerCount;
        }

        // Remove from players list
        // Needed for matchmaker cuz matchmaker doesnt know when a player leaves
        MatchProperties.Players.Remove(playerToRemove);
        // Removes from the team's list of player IDs
        // We must make sure to remove them from the correct team as well
        // We are not doing this anymore: MatchProperties.Teams[0].PlayerIds.Remove(userId);
        // We are doing this instead: 
        // This gets the team of the userId we want to remove -> access the PLayerIds (a list) -> and remove the userId from that list
        GetTeamByUserId(userId).PlayerIds.Remove(userId);
        // The Matchmaker will now know about this change on the next update
        localDataDirty = true;

        // Returns new player count
        return MatchPlayerCount;
    }

    // Chec whether the match still needs players to fill it
    public bool NeedsPlayers()
    {
        return MatchPlayerCount < maxPlayers;
    }

    // Team type is from: Unity.Services.Matchmaker.Models
    // Teams = a list of Team types

    public Team GetTeamByUserId(string userId)
    {
        // returns the first/default team if the given lambda expression returns true: t.PlayerIds.Contains(userId)
        return MatchProperties.Teams.FirstOrDefault(
            t => t.PlayerIds.Contains(userId) // PlayerIds is the LIST of all players in the game and see if it contains a specific userId that we passed in
        );
    }

    // Find the first or default (null) player in the Players list in MatchProperties using the userId
    private Player GetPlayerById(string userId)
    {
        return MatchProperties.Players.FirstOrDefault(
            p => p.Id.Equals(userId));
    }

    // Deletes the backfill ticket in Matchmaker
    public async Task StopBackfill()
    {
        if (!IsBackfilling) // check if we are not backfilling 
        {
            Debug.LogError("Can't stop backfilling before we start.");
            return;
        }

        // Sets flags to indicate backfilling has stopped
        // Flags = variables (often booleans) used to mark a state or condition in your program.
        // Ex: IsBackfilling = true is a flag
        await MatchmakerService.Instance.DeleteBackfillTicketAsync(localBackfillTicket.Id);
        IsBackfilling = false;
        localBackfillTicket.Id = null;
    }

    private async void BackfillLoop()
    {
        while (IsBackfilling)
        {
            if (localDataDirty) // if there was local data changed -> update the ticket in matchmaker (takes in the local ticket's ID and the obj itself)
            {
                await MatchmakerService.Instance.UpdateBackfillTicketAsync(localBackfillTicket.Id, localBackfillTicket);
                localDataDirty = false;
            }
            else // otherwise, approves the ticket to check for new match allocations
            {
                localBackfillTicket = await MatchmakerService.Instance.ApproveBackfillTicketAsync(localBackfillTicket.Id);
            }

            if (!NeedsPlayers()) // dont need no more players
            {
                await StopBackfill();
                break;
            }

            await Task.Delay(TicketCheckMs);
        }
    }

    public void Dispose() // is called when the object is disposed (destroyed)
    {
        _ = StopBackfill(); // I’m calling this method but ignoring its result (this method is async and returns task). (doesnt care about the returned Task) (we dont need to await for this)
    }
}
