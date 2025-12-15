using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class GameHUD : NetworkBehaviour // becuz we want to sync a networkvar:L joinCode
{
    [SerializeField] private TMP_Text lobbyCodeText;

    // Empty string cuz for some reason without it, game crashes
    NetworkVariable<FixedString32Bytes> lobbyCode = new NetworkVariable<FixedString32Bytes>("");

    // Note: For IsHost, IsClient, and IsServer:
    // A host = true for all
    // regular client = false,true,false
    // Dedicated server = false, false, true
    public override void OnNetworkSpawn()
    {
        if (IsClient)
        {
            lobbyCode.OnValueChanged += HandleLobbyCodeChanged;
            HandleLobbyCodeChanged(string.Empty, lobbyCode.Value); // we should also call it manually just to be safe
        }
        if (!IsHost) { return; }

        lobbyCode.Value = HostSingleton.Instance.GameManager.JoinCode;

    }

    public override void OnNetworkDespawn()
    {
        lobbyCode.OnValueChanged -= HandleLobbyCodeChanged;
    }

    private void HandleLobbyCodeChanged(FixedString32Bytes oldCode, FixedString32Bytes newCode)
    {
        lobbyCodeText.text = newCode.ToString();
    }

    // only applies for self hosted (if dedicated server -> Leaving Game is not a thing)
    // This method is for the host leaving the game, not for regular clients/players
    public void LeaveGame()
    {

        // If we are host -> Hit Leave Game -> Host shutdowns then Client disconnects 
        // If we are client -> Hit leave game -> HostGameManager.Shutdown() wont be called so only client will go to main menu while host stays in the game still
        if (NetworkManager.Singleton.IsHost) // checks if this client is running as the host (i.e. acting as both server and client). It doesn’t directly tell you whether the game is self-hosted or dedicated
        {
            HostSingleton.Instance.GameManager.Shutdown(); // for thie host shut down
        }

        // Now for the client shut down
        ClientSingleton.Instance.GameManager.Disconnect(); // Notice that in ClientGameManager -> No shutdown logic -> Propagate to Networkclient which has the shutdown logic
    }


}
