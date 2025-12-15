using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class LeaderboardEntityDisplay : MonoBehaviour // A script which is a component of LeaderboardEntity --> The actual rectangles for each player
{
    // Goal: Renders each player's scores, names, and rank on leaderboard

    [SerializeField] TMP_Text displayText;


    // Private vars
    private FixedString32Bytes displayName;


    // Make these public properties so that we can read them for sorting leaderboard later on
    public int TeamIndex { get; private set; }
    public ulong ClientId { get; private set; }
    public int Coins { get; private set; }

    // FOr non teams
    public void Initialise(ulong clientId, FixedString32Bytes displayName, int coins)
    {
        ClientId = clientId;
        this.displayName = displayName;

        UpdateCoins(coins); // Update coin count locally AND also updates the text!
    }

    // For Teams
    public void Initialise(int teamIndex, FixedString32Bytes displayName, int coins)
    {
        TeamIndex = teamIndex;
        this.displayName = displayName;

        UpdateCoins(coins); // Update coin count locally AND also updates the text!
    }

    // For setting the color
    public void SetColor(Color color)
    {
        displayText.color = color;
    }

    public void UpdateCoins(int coins) // coins = new value of coins
    {
        Coins = coins; // update coins locally
        UpdateText(); // Update Text
    }

    public void UpdateText()
    {
        // Set TMP_Text:
        displayText.text = $"{transform.GetSiblingIndex()+1}. {displayName} ({Coins})"; // + 1 since indexes start at 0
    }
    
}
