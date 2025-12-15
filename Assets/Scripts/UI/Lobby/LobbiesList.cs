using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.VisualScripting;
// using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

public class LobbiesList : MonoBehaviour
{

    [SerializeField] private Transform lobbyItemParent; // Content
    [SerializeField] private LobbyItem lobbyItemPrefab;
    [SerializeField] private MainMenu mainMenu;
    private bool isRefreshing = false;
    private void OnEnable() // for refreshing the list
    {
        RefreshList();
    }

    public async void RefreshList()
    {
        if (isRefreshing) { return; } // if already refreshing, exit

        isRefreshing = true; // start Refreshing

        try
        {
            QueryLobbiesOptions options = new QueryLobbiesOptions();  // to get the list of lobbies
            options.Count = 25; // number of results (lobbies) to return -> if u had more than 25 lobbies, then move those extra lobbies to a second page
            options.Filters = new List<QueryFilter>() // filter what lobbies are shown to us (with a list of QueryFilter objects)
            {
                new QueryFilter( // 1st filter
                    field: QueryFilter.FieldOptions.AvailableSlots, // go check the avail spots in this lobby and see if it is...
                    op: QueryFilter.OpOptions.GT, // ...greater than ...
                    value: "0" // ...zero.
                ),
                new QueryFilter(
                    field: QueryFilter.FieldOptions.IsLocked, // if the lobby is locked, the lobby wont show up
                    op: QueryFilter.OpOptions.EQ,
                    value: "0" // zero in this case means false. (so if it is NOT locked, it will show up)
                )
            };

            // Get the Lobbies with such options and filters:
            QueryResponse lobbies = await Lobbies.Instance.QueryLobbiesAsync(options);

            // Destroy all the buttons for refreshing (performance wise kinda bad but doesnt matter since refreshing does not happen all the time, only when we click refresh)
            foreach (Transform child in lobbyItemParent)
            {
                Destroy(child.gameObject);
            }

            // Now instantiate the lobbyitems for each lobby in the lobbies we got after fltering (.Results = list of lobbies returned to us))
            foreach (Lobby lobby in lobbies.Results)
            {
                LobbyItem lobbyItem = Instantiate(lobbyItemPrefab, lobbyItemParent); // spawn the prefab in as the child of the lobbyItemParent
                lobbyItem.Initialize(this, lobby); // a method in lobbyItem that we created -> go look at it urself
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }

        
        isRefreshing = false; // Stop Refreshing
    }

    // grabs the join async method from mainmenu script
    public void JoinAsync(Lobby lobby)
    {
        mainMenu.JoinAsync(lobby);
    }
}
