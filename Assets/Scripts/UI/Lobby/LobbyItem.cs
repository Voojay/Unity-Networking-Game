using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class LobbyItem : MonoBehaviour
{
    // Goal:
    // be told which lobby we represent
    // and update the UI accordingly

    [SerializeField] private TMP_Text lobbyNameText;
    [SerializeField] private TMP_Text lobbyPlayersText;
    private LobbiesList lobbiesList; // this script is in charge of being able to make us join/connect to the lobby that we press the join button
    private Lobby lobby;
    public void Initialize(LobbiesList lobbiesList, Lobby lobby) // for 
    {
        this.lobbiesList = lobbiesList;
        this.lobby = lobby;
        lobbyNameText.text = lobby.Name;
        lobbyPlayersText.text = $"{lobby.Players.Count}/{lobby.MaxPlayers}";
    }

    public void Join()
    {
        lobbiesList.JoinAsync(lobby); // method in the lobbiesList script
    }
}
