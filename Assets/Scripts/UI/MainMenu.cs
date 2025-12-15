using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private TMP_Text queueStatusText;
    [SerializeField] private TMP_Text queueTimeText;
    [SerializeField] private TMP_Text findMatchButtonText;
    [SerializeField] private TMP_InputField joinCodeField;
    [SerializeField] private Toggle teamToggle;
    [SerializeField] private Toggle privateToggle;

    // For matchmaking
    private bool isMatchmaking; // to know if we are currently matchmaking or not
    private bool isCancelling; // this will be used with async cuz cancelling takes some time
    private bool isBusy; // so that you dont do multiple things at once: ex: matchmaking and then pressing host button
    private float timeInQueue;

    private void Start()
    {
        if (ClientSingleton.Instance == null) { return; } // safety check to see if clientsingleton has been instantiated or not

        // Sets the mouse cursor back to system default
        // null means no custom texture --> uses OS default cursor
        // Vector2.zero sets the cursor's click point (the hotspot) to the top-lefy corner (0,0)
        //CursorMode.Auto lets Unity decide whether to use hardware or software cursors depending on the platform.
        // In other words, it ensures your menu uses a normal cursor rather than some custom in-game one.
        // We will be setting the crosshair (the + or x symbol shown for where you are shooting/looking at on the screen) in TankPlayer
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

        // Set the starting texts as such to be an empty string ""
        queueStatusText.text = string.Empty;
        queueTimeText.text = string.Empty;
    }

    // For the timer during matchmaking logic
    void Update()
    {
        if (isMatchmaking)
        {
            timeInQueue += Time.deltaTime;

            // Now we have to convert the time to minutes
            // Luckily, we already have a class for that
            // Example: timeInQueue = 125
            TimeSpan ts = TimeSpan.FromSeconds(timeInQueue); // ts.Minutes = 2, ts.Seconds = 5
            // the 0 and 1 are telling us that the 0th and 1st parameter (ts.minutes, ts.seconds) will be for the 1st and 2nd {}
            // After the colon, 00, it is the placeholder for the number of digits you want (in this case, we want two digits to be displayed)
            queueTimeText.text = string.Format("{0:00}:{1:00}", ts.Minutes, ts.Seconds);
        }
    }

    // For the FInd Match Button
    public async void FindMatchPressed()
    {
        // No need to check for isBusy in case we want to cancel the matchmaking
        // Check 1: If we are already cancelling --> exit
        if (isCancelling) { return; }
        // Check 2: If we press button are already matchmaking --> the button's text was cancel -> so we are intending to cancel
        if (isMatchmaking)
        {
            // Since if we are already matchmaking -> the findMatchNButton text would be Cancel
            queueStatusText.text = "Cancelling...";
            isCancelling = true;

            // API call: Cancel Matchmaking
            await ClientSingleton.Instance.GameManager.CancelMatchmaking();
            isCancelling = false; // since the above code is an await -> that would be done before setting isCancelling to false
            isMatchmaking = false; // if we successfully cancelled -> then we also not finding match
            isBusy = false; // cancelled matchmaking -> no longer busy
            findMatchButtonText.text = "Find Match";
            queueStatusText.text = string.Empty;
            queueTimeText.text = string.Empty;
            return;
        }

        if (isBusy) { return; }

        // Start Queue
        // MatchmakeAsync is a method in CLientGameManager that takes in an Action<MatchmakerPollingResult>
        // onMatchmakeResponse is a delegate reference passed in — it could be just one method (OnMatchMade), or several methods combined before being passed.
        // On the start button, once we press it --> we are queuing -> the text should be changed to Cancel
        // In MatchmakeAsync, it invokes MatchmakerPollingResult, so be sure to pass in the method that has a MatchmakerPollingResult parameter
        // This also passes the bool for teamToggle to see if we want to matchmake with teamqueues or not
        ClientSingleton.Instance.GameManager.MatchmakeAsync(teamToggle.isOn, OnMatchMade);
        findMatchButtonText.text = "Cancel";
        queueStatusText.text = "Searching...";
        // Don't forget to reset the timer to zero when we start queue
        timeInQueue = 0f;
        isMatchmaking = true;
        isBusy = true; // now we are busy

    }

    private void OnMatchMade(MatchmakerPollingResult result) // this method is called when it is invoked in MatchmakeAsync
    {
        switch (result) // check the resukt
        {
            case MatchmakerPollingResult.Success:
                queueStatusText.text = "Connecting...";
                break;
            case MatchmakerPollingResult.TicketCreationError:
                queueStatusText.text = "TicketCreationError";
                break;
            case MatchmakerPollingResult.TicketCancellationError:
                queueStatusText.text = "TicketCancellationError";
                break;
            case MatchmakerPollingResult.TicketRetrievalError:
                queueStatusText.text = "TicketRetrievalError";
                break;
            case MatchmakerPollingResult.MatchAssignmentError:
                queueStatusText.text = "MatchAssignmentError";
                break;
        }
    }


    // For the Host Button
    public async void StartHost()
    {
        if (isBusy) { return; } // if we are already busy -> exit

        isBusy = true; // we were not busy before -> now we are

        // Parameter: whether to make the lobby private or public
        await HostSingleton.Instance.GameManager.StartHostAsync(privateToggle.isOn);

        isBusy = false; // once we finished awaiting, we are no longer busy
    }

    // For the Client Button
    public async void StartClient()
    {
        if (isBusy) { return; } // if we are already busy -> exit

        isBusy = true; // we were not busy before -> now we are

        await ClientSingleton.Instance.GameManager.StartClientAsync(joinCodeField.text);

        isBusy = false; // once we finished awaiting, we are no longer busy
    }

    // This is for joining into a lobby from the lobbies list 
    // We also need to check if we are busy or not. Hence, we need JoinAsync here.
    public async void JoinAsync(Lobby lobby) // void in this case means that await JoinAsync() is not allowed but awaiting another method inside JoinAsync() is allowed
    {

        if (isBusy) { return; }

        isBusy = true;


        try
        {
            Lobby joiningLobby = await Lobbies.Instance.JoinLobbyByIdAsync(lobby.Id); // lobby and joiningLobby refer to the same lobby but joiningLobby has the code to join the allocation but lobby doesnt (it's null)
            string joinCode = joiningLobby.Data["JoinCode"].Value; // access the Value of the "JoinCode" key (remember in out HostGameManager, our lobby had a Data which is a dictionary)

            await ClientSingleton.Instance.GameManager.StartClientAsync(joinCode);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }

        isBusy = false; // no longer joining
    }
}
