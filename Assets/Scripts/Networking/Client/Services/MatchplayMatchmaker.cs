using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Matchmaker;
using Unity.Services.Matchmaker.Models;
using UnityEngine;

public enum MatchmakerPollingResult
{
    Success,
    TicketCreationError,
    TicketCancellationError,
    TicketRetrievalError,
    MatchAssignmentError
}

public class MatchmakingResult
{
    public string ip;
    public int port;
    public MatchmakerPollingResult result;
    public string resultMessage;
}

public class MatchplayMatchmaker : IDisposable
{
    private string lastUsedTicket;
    private CancellationTokenSource cancelToken;

    private const int TicketCooldown = 1000; // time in milliseconds between ticket status checks.

    public bool IsMatchmaking { get; private set; }

    public async Task<MatchmakingResult> Matchmake(UserData data)
    {
        /// This method would be called to start the matchmaking process for a player, 
        /// and is done on the client side.
        
        // CancellationTokenSource is a .NET threading utility used to signal that an operation should stop without killing the thread.
        cancelToken = new CancellationTokenSource();

        string queueName = data.userGamePreferences.ToMultiplayQueue();
        CreateTicketOptions createTicketOptions = new CreateTicketOptions(queueName);
        Debug.Log(createTicketOptions.QueueName);

        List<Player> players = new List<Player> // Creating a list of players to be matched ( Party Support Could be done Here ).
        {
            new Player(data.userAuthId, data.userGamePreferences)
        };

        try
        {
            IsMatchmaking = true;
            //What a “ticket” is (important)
            //A matchmaking ticket is:
            //A request to be matched
            //Server Stored - by UGS
            //Continuously evaluated against other player tickets

            //Eventually results in:
            // * a match assignment, or
            // * a timeout / failure
            //Think of it like standing in a queue with metadata attached.
            CreateTicketResponse createResult = await MatchmakerService.Instance.CreateTicketAsync(players, createTicketOptions);

            lastUsedTicket = createResult.Id;

            try
            {
                while (!cancelToken.IsCancellationRequested) // while a async operation has not been cancelled
                {
                    // Checking the status of a ticket.
                    TicketStatusResponse checkTicket = await MatchmakerService.Instance.GetTicketAsync(lastUsedTicket);

                    if (checkTicket.Type == typeof(MultiplayAssignment)) // if the ticket has a match assignment
                    {
                        MultiplayAssignment matchAssignment = (MultiplayAssignment)checkTicket.Value;

                        if (matchAssignment.Status == MultiplayAssignment.StatusOptions.Found)
                        {
                            // Found a match!
                            return ReturnMatchResult(MatchmakerPollingResult.Success, "", matchAssignment);
                        }
                        else if (matchAssignment.Status == MultiplayAssignment.StatusOptions.Timeout ||
                                matchAssignment.Status == MultiplayAssignment.StatusOptions.Failed)
                        {
                            // Matchmaking failed due to timeout or failed.
                            return ReturnMatchResult(MatchmakerPollingResult.MatchAssignmentError,
                                $"Ticket: {lastUsedTicket} - {matchAssignment.Status} - {matchAssignment.Message}", null);
                        }
                        Debug.Log($"Polled Ticket: {lastUsedTicket} Status: {matchAssignment.Status} ");
                    }

                    await Task.Delay(TicketCooldown); // wait before checking the ticket status again to respect UGS rate limits.
                }
            }
            catch (MatchmakerServiceException e) // Catching errors when checking ticket status
            {
                return ReturnMatchResult(MatchmakerPollingResult.TicketRetrievalError, e.ToString(), null);
            }
        }
        catch (MatchmakerServiceException e) // Catching errors when creating the ticket
        {
            return ReturnMatchResult(MatchmakerPollingResult.TicketCreationError, e.ToString(), null);
        }

        // If we exit the loop without finding a match, it means matchmaking was cancelled by player.
        return ReturnMatchResult(MatchmakerPollingResult.TicketRetrievalError, "Cancelled Matchmaking", null);
    }

    public async Task CancelMatchmaking()
    {
        /// This method cancels an ongoing matchmaking operation by deleting the ticket associated with the matchmaking request,
        /// and Reseting the matchmaking fields.
        if (!IsMatchmaking) { return; }

        IsMatchmaking = false;

        if (cancelToken.Token.CanBeCanceled)
        {
            cancelToken.Cancel();
        }

        if (string.IsNullOrEmpty(lastUsedTicket)) { return; }

        Debug.Log($"Cancelling {lastUsedTicket}");

        await MatchmakerService.Instance.DeleteTicketAsync(lastUsedTicket);
    }

    private MatchmakingResult ReturnMatchResult(MatchmakerPollingResult resultErrorType, string message, MultiplayAssignment assignment)
    {
        /// A helper method that takes the matchmaking result data and formats it into a MatchmakingResult object
        /// So we are able to easily use the data and connect to the server.
        IsMatchmaking = false; // Resetting the matchmaking state.

        if (assignment != null) // If we have a valid match assignment we process its data.
        {
            string parsedIp = assignment.Ip;
            int? parsedPort = assignment.Port;
            
            if (parsedPort == null) // If port is missing return error.
            {
                return new MatchmakingResult
                {
                    result = MatchmakerPollingResult.MatchAssignmentError,
                    resultMessage = $"Port missing? - {assignment.Port}\n-{assignment.Message}"
                };
            }

            return new MatchmakingResult // returning success result with IP and port of the match server.
            {
                result = MatchmakerPollingResult.Success,
                ip = parsedIp,
                port = (int)parsedPort,
                resultMessage = assignment.Message
            };
        }

        return new MatchmakingResult // returning error result because of no assignment.
        {
            result = resultErrorType,
            resultMessage = message
        };
    }

    public void Dispose()
    {
        _ = CancelMatchmaking();

        cancelToken?.Dispose();
    }
}