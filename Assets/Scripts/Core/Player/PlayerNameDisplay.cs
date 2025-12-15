using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using UnityEngine;

public class PlayerNameDisplay : MonoBehaviour // we aint doing any server calls, so monobehaviour
{
    [SerializeField] private TankPlayer player;
    [SerializeField] private TMP_Text playerNameText;
    void Start()
    {
        // Somethimes the player name has already been changed before we subbed to the networkvar
        // So we are going to set the playername immediately before subbing IN CASE it did not sub in time before the player name changed
        HandlePlayerNameChanged(string.Empty, player.PlayerName.Value);
        
        player.PlayerName.OnValueChanged += HandlePlayerNameChanged; // subbing to the PlayerName networkvar
        
    }

    private void HandlePlayerNameChanged(FixedString32Bytes oldName, FixedString32Bytes newName)
    {
        playerNameText.text = newName.ToString();
    }

    void OnDestroy()
    {
        player.PlayerName.OnValueChanged -= HandlePlayerNameChanged; // to prevent memory leaks
    }
}
