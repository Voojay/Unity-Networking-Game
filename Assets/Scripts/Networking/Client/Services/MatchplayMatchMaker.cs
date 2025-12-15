using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Matchmaker;
using Unity.Services.Matchmaker.Models;
using UnityEngine;

// General Role of the Class: Handles matchmaking with unity's matchmaker service
// Creates matchmaking ticket in unity matchmaker
// polls the ticket repeatedly to see if a match has been found
// Cancels ticket if needed
// returns the server IP and port once a match is found (so the client can connect)

// Overall flow:
// Start Matchmake(): creates ticket, polls repeatedly -> Finds a match: returns server info -> cancels if requested -> wraps all results in a consistent MatchmakingResult object


// Defines possible results for matchmaking
public enum MatchmakerPollingResult
{
    Success, // Match found
    TicketCreationError, // error creating ticket
    TicketCancellationError,
    TicketRetrievalError, // Error polling the ticket
    MatchAssignmentError // match assignment failed
}


public class MatchmakingResult
{
    public string ip; // server IP to connect to
    public int port; // server port
    public MatchmakerPollingResult result; // enum describing how matchmaking ended
    public string resultMessage; // error message or info
}

public class MatchplayMatchmaker : IDisposable
{
    private string lastUsedTicket; // ID of the current matchmaking ticket
    private CancellationTokenSource cancelToken; // lets you cancel the polling loop

    private const int TicketCooldown = 1000; // wait time between polling requests

    public bool IsMatchmaking { get; private set; } // true while matchmaking is running

    // Starts matchmaking for a user
    public async Task<MatchmakingResult> Matchmake(UserData data)
    {
        cancelToken = new CancellationTokenSource(); // allows cancelling the loop later

        Debug.Log($"{data.userName}");
        string queueName = data.userGamePreferences.ToMultiplayQueue(); // Decides which queue the player joins based on preferences
        CreateTicketOptions createTicketOptions = new CreateTicketOptions(queueName); // prepares ticket options
        Debug.Log(createTicketOptions.QueueName);

        List<Player> players = new List<Player> // sets up the player data for the ticket
        {
            new Player(data.userAuthId, data.userGamePreferences)
        };

        try
        {
            IsMatchmaking = true;

            // calls unity matchmaker to create a ticket
            CreateTicketResponse createResult = await MatchmakerService.Instance.CreateTicketAsync(players, createTicketOptions);

            // Save ticket ID
            lastUsedTicket = createResult.Id;

            try
            {
                while (!cancelToken.IsCancellationRequested) // Polling Loop: Keeps checking the ticket's status
                {
                    TicketStatusResponse checkTicket = await MatchmakerService.Instance.GetTicketAsync(lastUsedTicket);

                    if (checkTicket.Type == typeof(MultiplayAssignment)) // If the response contains a MultiplayAssignment, we have a match assignment object.
                    {
                        MultiplayAssignment matchAssignment = (MultiplayAssignment)checkTicket.Value;

                        if (matchAssignment.Status == MultiplayAssignment.StatusOptions.Found) // Found a game→ results return success and return the server info.
                        {
                            return ReturnMatchResult(MatchmakerPollingResult.Success, "", matchAssignment);
                        }
                        if (matchAssignment.Status == MultiplayAssignment.StatusOptions.Timeout ||
                            matchAssignment.Status == MultiplayAssignment.StatusOptions.Failed) // If it timed out or failed → return an error result.
                        {
                            return ReturnMatchResult(MatchmakerPollingResult.MatchAssignmentError,
                                $"Ticket: {lastUsedTicket} - {matchAssignment.Status} - {matchAssignment.Message}", null);
                        }
                        Debug.Log($"Polled Ticket: {lastUsedTicket} Status: {matchAssignment.Status} ");
                    }

                    await Task.Delay(TicketCooldown); // If we still waiting for a match: Waits 1 second before polling again.
                }
            }
            catch (MatchmakerServiceException e) // Handles polling errors
            {
                return ReturnMatchResult(MatchmakerPollingResult.TicketRetrievalError, e.ToString(), null);
            }
        }
        catch (MatchmakerServiceException e) // Handles errors creating the ticket.
        {
            return ReturnMatchResult(MatchmakerPollingResult.TicketCreationError, e.ToString(), null);
        }

        return ReturnMatchResult(MatchmakerPollingResult.TicketRetrievalError, "Cancelled Matchmaking", null); // If user cancels → returns cancelled result.
    }

    // Stops matchmaking if running
    public async Task CancelMatchmaking()
    {
        if (!IsMatchmaking) { return; }

        IsMatchmaking = false;

        if (cancelToken.Token.CanBeCanceled)
        {
            cancelToken.Cancel(); // cancels the token so the polling loop exits
        }

        if (string.IsNullOrEmpty(lastUsedTicket)) { return; }

        Debug.Log($"Cancelling {lastUsedTicket}");

        await MatchmakerService.Instance.DeleteTicketAsync(lastUsedTicket); // Deletes the ticket in Matchmaker to clean things up
    }

    // Used to create a MatchmakingResult.
    // Basically gets the IP and Port and returns as matchmakingresult
    private MatchmakingResult ReturnMatchResult(MatchmakerPollingResult resultErrorType, string message, MultiplayAssignment assignment)
    {
        IsMatchmaking = false;

        if (assignment != null)
        {
            // Retrieves the IP and port from the match assignment
            string parsedIp = assignment.Ip;
            int? parsedPort = assignment.Port;

            // Handles the case if the port is missing
            if (parsedPort == null)
            {
                return new MatchmakingResult
                {
                    result = MatchmakerPollingResult.MatchAssignmentError,
                    resultMessage = $"Port missing? - {assignment.Port}\n-{assignment.Message}"
                };
            }

            // parsedPort is not null -> return MatchmakingResult
            return new MatchmakingResult
            {
                result = MatchmakerPollingResult.Success,
                ip = parsedIp,
                port = (int)parsedPort,
                resultMessage = assignment.Message
            };
        }

        // If there’s no assignment → returns an error result.
        return new MatchmakingResult
        {
            result = resultErrorType,
            resultMessage = message
        };
    }

    public void Dispose()
    {
        _ = CancelMatchmaking(); //ignore the returned Task, just like in your earlier code.

        cancelToken?.Dispose();
    }
}
