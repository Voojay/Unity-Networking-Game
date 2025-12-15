using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkClient : IDisposable // an interface that has a Dispose Method --> we need this cuz normal classes dont have OnDestroy()
{

    // connects to the server
    // Plays the game and sends input to server
    private NetworkManager networkManager;
    private const string MenuSceneName = "Menu";

    public NetworkClient(NetworkManager networkManager) // constructor
    {
        this.networkManager = networkManager;

        networkManager.OnClientDisconnectCallback += OnClientDisconnect; // .OnClientDisconnectCallback is triggered when a client disconnects
    }


    // Check who disconnected + Return to main menu if needed + shutdown local network client cleanly
    private void OnClientDisconnect(ulong clientId) // remove their clientId when they disconnect
    {

        // Check 1: When a client disconnects, check if that client is a host/server. If we are NOT the host/server AND the local client Id is the current client's own ID (this means that some other client disconnected), dont do anything!
        if (clientId != 0 && clientId != networkManager.LocalClientId) { return; } // 0 is usually the host/server

        // Current State: We are the host OR the client itself has disconnected

        Disconnect();
    }
    
    public void Disconnect() // Load Menu Scene
    {
        // Load Main Menu IF we are not already on it
        if (SceneManager.GetActiveScene().name != MenuSceneName)
        {
            SceneManager.LoadScene(MenuSceneName);
        }
        
        // If you are still marked as connected -> shutdown (This will Clean Up network state, Unregister Transport + Disable any spawned network objects and reset client/host mode)
        if (networkManager.IsConnectedClient)
        {
            networkManager.Shutdown();
        }
    }

    public void Dispose() // do any clean up for this class when it is called 
    {
        // Unsubscribe from onclientdisconnect -> this is to shut down cleanly (because unity wont do this for us)
        if (networkManager != null)
        {
            networkManager.OnClientDisconnectCallback -= OnClientDisconnect;
        }
    }

    
}
