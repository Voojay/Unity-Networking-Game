using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class RespawnHandler : NetworkBehaviour // This obj will be running server code + need to use network callback events 
{
    [SerializeField] private TankPlayer playerPrefab;
    [SerializeField] private float keptCoinPercentage;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) { return; }

        // In case when you are self-hosting, the host might exist before the scene is started
        TankPlayer[] players = FindObjectsByType<TankPlayer>(FindObjectsSortMode.None); // We use Bytype instead cuz in newer versions, it will be deprecated: Extra parameter: Options to specify if and how to sort objects returned by a function
        foreach (TankPlayer player in players)
        {
            HandlePlayerSpawned(player); // any player already in the scene gets manually added by HandlePlayerSpawned
        }

        // Anyone who joins later is added by the event
        TankPlayer.OnPlayerSpawned += HandlePlayerSpawned;
        TankPlayer.OnPlayerDespawned += HandlePlayerDespawned;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer) { return; }
        TankPlayer.OnPlayerSpawned -= HandlePlayerSpawned;
        TankPlayer.OnPlayerDespawned -= HandlePlayerDespawned;
    }

    private void HandlePlayerSpawned(TankPlayer player)
    {
        // Lambda Functions:
        // Ex: x => x + x  -->  Takes x as paramter, returns x + x
        // Since player.Health.OnDie is an event where methods subbed to it must have Health health as parameter; in tbis case, we have TankPlayer player, not health --> Use Lambda Functions
        // So the line below means, create another function (which is hidden) that takes in health, and return HandlePLayerDie(player)  -->  this basically means that health is being ignored here
        player.Health.OnDie += (health) => HandlePlayerDie(player);
    }

    private void HandlePlayerDespawned(TankPlayer player)
    {
        player.Health.OnDie -= (health) => HandlePlayerDie(player);
    }

    private void HandlePlayerDie(TankPlayer player)
    {
        // when one dies -> deduct coins in wallet by percent --> keptCoins = we will set this later once we respawn the player
        int keptCoins = (int) (player.Wallet.TotalCoins.Value * (keptCoinPercentage / 100)); 
        
        // Destroy the player
        Destroy(player.gameObject); // Not Destroy(player.NetworkObject); since Because NetworkObject is just a component — like a script.

        // We need to spawn back the player in the NEXT FRAME --> Use a coroutine
        StartCoroutine(RespawnPlayer(player.OwnerClientId, keptCoins)); // ownerClientId is a public property in NetworkBehaviour
    }

    private IEnumerator RespawnPlayer(ulong ownerClientId, int keptCoins)
    {
        yield return null; // wait til the next frame

        TankPlayer playerInstance = Instantiate(playerPrefab, SpawnPoint.GetRandomSpawnPos(), Quaternion.identity);

        // Since platerInstance is a TankPlayer (which inherits from Networkbehaviour), we must do .NetworkObject.SpawnAsPlayerObject
        playerInstance.NetworkObject.SpawnAsPlayerObject(ownerClientId); // “Spawn this object on the network and make it the player object for this client.”

        // Once the playerInstance is instantiated and over the network -> set the coins in wallet
        playerInstance.Wallet.TotalCoins.Value = keptCoins; 
    }

    
    


    
}
