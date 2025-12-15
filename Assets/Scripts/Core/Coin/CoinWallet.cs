using System.Collections;
using System.Collections.Generic;
// using System.Numerics;


using Unity.Netcode;
using UnityEngine;

public class CoinWallet : NetworkBehaviour
{
    // Most of the refs and settings here are for bounty coins
    [Header("References")]
    [SerializeField] private BountyCoin coinPrefab;
    [SerializeField] private Health health; // so we can sub to the OnDie Event -> know when player dies -> bounty coins will explode from player

    [Header("Settings")]
    [SerializeField] private int bountyCoinCount = 10;
    [SerializeField] private int minBountyCoinValue = 5; // This is the value of one bounty coin () if a player dies and has less than 50 Coin Value, they wont drop anything )
    [SerializeField] private float bountyPercentage = 50f; // the percent of bounty coins we drop on the floor when we die
    [SerializeField] private float coinSpread = 3f; // how far the dropped bounty coins spreads
    [SerializeField] LayerMask layerMask; // we need this because we dont want the bounty coin to spawn in walls, rocks, etc.

    private float coinRadius; // the radius of the coin in its collider2D
    private Collider2D[] coinBuffer = new Collider2D[1]; // array with room for 1 member -> just to see if the drawn cicle (before we spawn the coin) has collided with SOMETHING (this smth is the member of the array).

    public override void OnNetworkSpawn()
    {
        // For spawning bounty coins, we want this to ONLY BE SERVER LOGIC
        if (!IsServer) { return; }

        coinRadius = coinPrefab.GetComponent<CircleCollider2D>().radius; // get the raiud of the coin
        health.OnDie += HandleDie;

    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer) { return; }
        health.OnDie -= HandleDie;
    }


    public NetworkVariable<int> TotalCoins = new NetworkVariable<int>();

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<Coin>(out Coin coin))
        {

            int coinValue = coin.Collect(); // exec this before IsServer since if this is a client -> the Collect() hides the renderer

            // When we add coins to the wallet, we want the server to handle that
            if (!IsServer) { return; }

            TotalCoins.Value += coinValue;
        }
    }

    public void SpendCoins(int amount)
    {
        if (TotalCoins.Value < amount) { return; }
        TotalCoins.Value -= amount;
    }

    private void HandleDie(Health health)
    {
        int bountyValue = (int)(TotalCoins.Value * (bountyPercentage / 100f));
        int bountyCoinValue = bountyValue / bountyCoinCount; // bountyCoinCount is constant during the whole game session and bountycoinvalue changes depending on how much money did the player who died have

        if (bountyCoinValue < minBountyCoinValue) { return; } // dont instantiate

        for (int i = 0; i < bountyCoinCount; i++)
        {
            BountyCoin coinInstance = Instantiate(coinPrefab, GetSpawnPoint(), Quaternion.identity);

            // Dont forget to set the value of the coin too! (This method is in the Coin class)
            coinInstance.SetValue(bountyCoinValue);

            // Dont forget that not only do we have to instantiate, we have to spawn the coin over the network too!!
            // No need to GetComponent since BountyCoin inherits from Coin which inherits from NetworkBehaviour
            coinInstance.NetworkObject.Spawn(); 
        }
    }

    private Vector2 GetSpawnPoint()
    {

        while (true)
        {
            // This will randomize the spawnpoint within the unitcircle (radius = 1) * coinSpread where the centerpoint is the transform.position (the coinwallet which is attached to the just died player)
            Vector2 spawnPoint = (Vector2) transform.position + UnityEngine.Random.insideUnitCircle * coinSpread;

            // NonAlloc = will not allocate memory at run time -> we already provided an array (coinBuffer) -> we will reuse this
            // spawnPoint + CoinRadius = circle that will be imagined 
            // coinBuffer is where the results of this method will go in
            // layerMask is the layer that we will be checking
            int numColliders = Physics2D.OverlapCircleNonAlloc(spawnPoint, coinRadius, coinBuffer, layerMask);

            if (numColliders == 0)
            {
                return spawnPoint;
            }
        }
    }
}
