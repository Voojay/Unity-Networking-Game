using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class Leaderboard : NetworkBehaviour // in gamehud 
{
    // Goal: In charge of tracking and syncing leaderboard data

    [SerializeField] private Transform leaderboardEntityHolder; // parent whose children are the entity displays
    [SerializeField] private Transform teamLeaderboardEntityHolder; // for the teams --> will turn on if the gameQueue (the enum found in userData) is Team
    [SerializeField] private GameObject teamLeaderboardBackground; 
    [SerializeField] private LeaderboardEntityDisplay leaderboardEntityPrefab;
    [SerializeField] private int entitiesToDisplay = 8; // number of players max on leaderboard UI
    [SerializeField] private Color ownerColor;
    [SerializeField] private string[] teamNames;

    // no need for color array since we have it in our S.O. for our teamcolorlookup
    [SerializeField] private TeamColorLookup teamColorLookup;

    // Now we need to sync to all clients' IDs, names, and how many coins they are on
    // So, we can't use NetworkVariable since this requires only one type. --> Need networklist
    // We can't just do: NetworkList<ulong, string, ...>  -->  we must create our own custom type
    // We do this by creating a LeaderboardEntityState file -> the class will be 'public struct ...'


    // this is now synced from server to client -> so every time an element is added/removed -> will be synced to clients -> can listen for that
    private NetworkList<LeaderboardEntityState> leaderboardEntities; // cant init yet, must init in Awake()
    
    private List<LeaderboardEntityDisplay> entityDisplays = new List<LeaderboardEntityDisplay>(); // Can init here cuz not NetworkList, it is just a normal List

    private List<LeaderboardEntityDisplay> teamEntityDisplays = new List<LeaderboardEntityDisplay>();

    void Awake()
    {
        leaderboardEntities = new NetworkList<LeaderboardEntityState>();
    }

    public override void OnNetworkSpawn()
    {
        // We need to know if any players are already spawned in and what should be added to leaderboard
        // And them we will sub to the event for any that are spawned in after that 

        // For Client: Listen to changes and change UI accordingly
        if (IsClient)
        {
            // Check first if it is a team game queue
            if (ClientSingleton.Instance.GameManager.UserData.userGamePreferences.gameQueue == GameQueue.Team)
            {
                teamLeaderboardBackground.SetActive(true); // by default, it is false -> so turn it true

                for (int i = 0; i < teamNames.Length; i++) // "i" will be the teamIndex!!
                {
                    LeaderboardEntityDisplay entity = Instantiate(leaderboardEntityPrefab, teamLeaderboardEntityHolder);
                    entity.Initialise(i, teamNames[i], 0);
                    Color teamColor = teamColorLookup.GetTeamColor(i);
                    entity.SetColor(teamColor); // sets the display text to be that color
                    teamEntityDisplays.Add(entity);

                }
            }

            leaderboardEntities.OnListChanged += HandleLeaderboardEntitiesChanged; // leaderboardEntities = networklist which has OnListChanged as a public event

            // Loops thru the list of leaderboardEntities to see what entities are already there for the new clients
            // “If I’m a new client, loop through the existing leaderboard list, and create UI entries for all players already there.”
            foreach (LeaderboardEntityState entity in leaderboardEntities)
            {
                // We are manually calling HandleLeaderboardEntitiesChanged since when a new client joins, it missed all the automatic OnListChanged events that already happened in the past.
                // This creates a fake change event, telling the handler: “Pretend the player data (entity) was just added to the leaderboard.” 
                // with a Type of Add (was added to list) and...
                // Value = entity this is the player we want to add to the leaderboard UI
                HandleLeaderboardEntitiesChanged(new NetworkListEvent<LeaderboardEntityState>
                {
                    Type = NetworkListEvent<LeaderboardEntityState>.EventType.Add,
                    Value = entity
                });
            }

        }

        // For Server: Look for already existing TankPlayer objects and handle them + subscribe
        if (IsServer)
        {

            // Find tankplayer objects already in scene
            TankPlayer[] players = FindObjectsByType<TankPlayer>(FindObjectsSortMode.None);

            // Account for everyone already in the server (for weird cases where the host is already in ther server)
            foreach (TankPlayer player in players)
            {
                HandlePlayerSpawned(player);
            }

            TankPlayer.OnPlayerSpawned += HandlePlayerSpawned;
            TankPlayer.OnPlayerDespawned += HandlePlayerDespawned;
        }


    }

    public override void OnNetworkDespawn()
    {
        if (IsClient)
        {
            leaderboardEntities.OnListChanged -= HandleLeaderboardEntitiesChanged; // leaderboardEntities = networklist which has OnListChanged as a public event
        }

        if (IsServer)
        {
            TankPlayer.OnPlayerSpawned -= HandlePlayerSpawned;
            TankPlayer.OnPlayerDespawned -= HandlePlayerDespawned;
        }

    }

    private void HandleLeaderboardEntitiesChanged(NetworkListEvent<LeaderboardEntityState> changeEvent) // NetworkListEvent<> represents a single change in the list (like adding, removing, or modifying an item).
    {
        // checks if this GameObject’s scene is still loaded into memory (if scene was unloaded when a player leaves match or transitions to another scene) -> isLoaded will return false
        // Why? Since this method is subbed to OnListChanged -> when u leave the game, unity does not automatically unsub those events when a scene is unloaded
        // So, the leaderboard UI script  might get called after the scene has been destroyed or during transition or after player left
        // What can go wrong: this handler still gets called --> the UI objects are now destroyed or invalid --> get null reference errors, crashes, or weird ghost updates.
        // So this if statement just says: If this object’s scene is gone, don’t touch anything — just stop. --> this is just a safety net
        if (!gameObject.scene.isLoaded) { return; }

        switch (changeEvent.Type) // examining the type of changeEvent
        {
            case NetworkListEvent<LeaderboardEntityState>.EventType.Add: // if (changeEvent.Type == NetworkListEvent<LeaderboardEntityState>.EventType.Add)

                // check if you already have a display for that entity
                // The below is a lambda function:
                // The whole line means: If there are NOT ANY leaderboardentitydisplay objects (x) that have their clientid == the changevevent's clientid value (which is the one that was added in)  --> All this returns a BOOL
                // In short: “If we don’t already have a display for this new client, create one.” --> If we do have it, it wont instantiate
                if (!entityDisplays.Any(x => x.ClientId == changeEvent.Value.ClientId))
                {
                    LeaderboardEntityDisplay leaderboardEntity = Instantiate(leaderboardEntityPrefab, leaderboardEntityHolder); // Added new obj in the list -> Instantitate the prefab at the transfrom of EntityHolder

                    // Initialise this entity -> set clientId and player name along with updating coins
                    leaderboardEntity.Initialise(
                        changeEvent.Value.ClientId,
                        changeEvent.Value.PlayerName,
                        changeEvent.Value.Coins
                    );

                    // Now we want that if it is us, our color will be different -> it will be the ownerColor in the [serializefield]
                    if (NetworkManager.Singleton.LocalClientId == changeEvent.Value.ClientId) // if ourself = the leaderboardentitystate that was added's clientId --> this also ensures that ONLY US will see this change
                    {
                        leaderboardEntity.SetColor(ownerColor);
                    }

                    entityDisplays.Add(leaderboardEntity);
                }
                break;
            case NetworkListEvent<LeaderboardEntityState>.EventType.Remove: // If it is Remove type

                // return the first entity or default entity (which is null) LeaderboardEntityDisplay you find where the display's clientId is the same as the changeEvent's (the newly added leaderboardentitydisplay) clientId
                LeaderboardEntityDisplay displayToRemove = entityDisplays.FirstOrDefault(x => x.ClientId == changeEvent.Value.ClientId);
                if (displayToRemove != null) // it does exist
                {
                    // We must set its parent to null first or else it will cause problems --> it will no longer be the child of Leaderboard
                    displayToRemove.transform.SetParent(null);

                    // Destroy then remove from list
                    Destroy(displayToRemove.gameObject);
                    entityDisplays.Remove(displayToRemove);

                }
                break;

            // Case where the value was changed -> Coins collected
            case NetworkListEvent<LeaderboardEntityState>.EventType.Value:

                // Go thru the displays -> Find one that matches clientId -> update display
                LeaderboardEntityDisplay displayToUpdate = entityDisplays.FirstOrDefault(x => x.ClientId == changeEvent.Value.ClientId);
                if (displayToUpdate != null)
                {
                    Debug.Log("Coins: " + changeEvent.Value.Coins);
                    displayToUpdate.UpdateCoins(changeEvent.Value.Coins);
                }
                break;
        }

        // Sort is a method in List
        // Parameter for sort in this case is a lambda expression: (x, y) is the parameters and the result is in descending order of Coins (JUM: x = less, y = more)
        // This basically means: Sort the EntityDisplays in according to the number of coins
        entityDisplays.Sort((x, y) => y.Coins.CompareTo(x.Coins));

        // Now we are going to use a for loop to: 1) Set the child to be the same index as the sorted list in the hierarchy + UpdateText()

        for (int i = 0; i < entityDisplays.Count; i++)
        {
            // Sets the child index in hierarchy in the scene
            // We are doing this because we want to Get the sibling index and display that number in the UpdateText()
            entityDisplays[i].transform.SetSiblingIndex(i);
            entityDisplays[i].UpdateText();

            // Since there are only a limited number of players in the leaderboard 
            bool shouldShow = (i <= entitiesToDisplay - 1); // dont forget -1 since indexes start at 0
            entityDisplays[i].gameObject.SetActive(shouldShow);
        }

        // Once everything is sorted out, we also want to implement a feature where even though we are not in the top 8, we will still see ourselves in the leaderboard in 8th 
        // First, find the entityDisplay that belongs to us
        // This line means that in the entityDisplays list, find the first leaderboard display whose clientId matches with MY CLIENT ID (The Singleton.LocalClientId represents our clientId)
        // Note that this will all be done on the client side already since the HandleLeaderboardEntitiesChanged is called using if (IsClient)
        LeaderboardEntityDisplay myDisplay = entityDisplays.FirstOrDefault(x => x.ClientId == NetworkManager.Singleton.LocalClientId);

        if (myDisplay != null) // if our display does exist
        {
            if (myDisplay.transform.GetSiblingIndex() >= (entitiesToDisplay - 1)) // if our rank is below 8th
            {
                // Set the 8th place (not us) -> false and then set our display to be true
                leaderboardEntityHolder.GetChild(entitiesToDisplay - 1).gameObject.SetActive(false);
                myDisplay.gameObject.SetActive(true);
            }
        }

        if (!teamLeaderboardBackground.activeSelf) { return; } // if the teamleaderboard aint active  (meaning that it is not a team game) -> exit 

        // Get the first LeaderboardEntityDisplay whose teamindex is the same one thats changed
        LeaderboardEntityDisplay teamDisplay = teamEntityDisplays.FirstOrDefault(x => x.TeamIndex == changeEvent.Value.TeamIndex);

        if (teamDisplay != null)
        {
            // The player left
            if (changeEvent.Type == NetworkListEvent<LeaderboardEntityState>.EventType.Remove)
            {
                // Remove coins of the player who left
                teamDisplay.UpdateCoins(teamDisplay.Coins - changeEvent.Value.Coins);
            }
            else // The player gets more OR is deducted coins
            {
                // previous value is another type within network list of getting the previous value before it changed to the current
                teamDisplay.UpdateCoins(teamDisplay.Coins + (changeEvent.Value.Coins - changeEvent.PreviousValue.Coins));
            }

            // Dont forget to sort the teamentitydisplays also
            // Sort is a method in List
            // Parameter for sort in this case is a lambda expression: (x, y) is the parameters and the result is in descending order of Coins (JUM: x = less, y = more)
            // This basically means: Sort the EntityDisplays in according to the number of coins
            teamEntityDisplays.Sort((x, y) => y.Coins.CompareTo(x.Coins));

            for (int i = 0; i < teamEntityDisplays.Count; i++)
            {
                // Sets the child index in hierarchy in the scene
                // We are doing this because we want to Get the sibling index and display that number in the UpdateText()
                teamEntityDisplays[i].transform.SetSiblingIndex(i);
                teamEntityDisplays[i].UpdateText();

            }
        }

    }   

    private void HandlePlayerSpawned(TankPlayer player)
    {
        // Follow the following format to create a new LeaderboardEntityState
        leaderboardEntities.Add(new LeaderboardEntityState
        {
            ClientId = player.OwnerClientId, // ownerClientId is a property in Networkbehaviour
            PlayerName = player.PlayerName.Value,
            Coins = 0, // starting num of coins = 0
            TeamIndex = player.TeamIndex.Value
        });

        // PROF WAY for tape 49: 
        // player.Wallet.TotalCoins.OnValueChanged += (oldCoins, newCoins) =>
        //     HandleCoinsChanged(player.OwnerClientId, newCoins);
        // We need lambda expression for this and not just += since the parameters wouldnt match (OnValueChanged -> int, int  while  HandleCoinsChanged takes in ulong and int) 
        // lambda meaning: method parameters: int oldCoins, int newCoins --> inside method: HandleCoinsChanged(player.OwnerClientId, newCoins) --> note that newCoins can be used now since it is passed in in the lambda expression
        // Also, the code after += is a whole entire nameless method with 2 ints as parameters -> no errors!!!


        // In this new improved way -> better that the lambda approach because each lambda creates a new delegate instance (delegate = A variable that can store a function and call it later.)
        // In larger scales, unsubbing from lambda expressions can sometimes fail because that’s a different lambda 
        

        player.OnWalletUpdated += HandleCoinsChanged;  // // Improved way for tape 49
    }


    private void HandlePlayerDespawned(TankPlayer player)
    {
        // Sometimes in later versions, when you play the game layer and it despawns your player, it might make the list become a null -> null check
        if (leaderboardEntities == null) { return; }
        // Go thru the leaderboard and see who matches this entity -> delete them
        foreach (LeaderboardEntityState entity in leaderboardEntities)
        {
            if (entity.ClientId != player.OwnerClientId) { continue; } // if this entity in the loop is NOT the same client id as the player passed in, continue looping

            // State rn: It is the same clientId
            leaderboardEntities.Remove(entity);
            break;
        }

        // PROF way for Tape 49:
        // player.Wallet.TotalCoins.OnValueChanged -= (oldCoins, newCoins) =>
        //     HandleCoinsChanged(player.OwnerClientId, newCoins);


        player.OnWalletUpdated -= HandleCoinsChanged; // Improved way for tape 49
    }
    
    private void HandleCoinsChanged(ulong clientId, int newCoins)
    {
        // Loop thru all the leaderboardEntities -> check if that entity matcges with the clientId of parameter
        for (int i = 0; i < leaderboardEntities.Count; i++)
        {
            if (leaderboardEntities[i].ClientId != clientId) { continue; }

            // Set this entity to be a new LeaderboardEntityState 
            leaderboardEntities[i] = new LeaderboardEntityState
            {
                // Both clientId and PlayerName dont change, so we will keep the original one as it is
                ClientId = leaderboardEntities[i].ClientId,
                PlayerName = leaderboardEntities[i].PlayerName,
                // However, the number of coins do change
                Coins = newCoins,
                TeamIndex = leaderboardEntities[i].TeamIndex 
            };

            return;
            
        }
    }



}
