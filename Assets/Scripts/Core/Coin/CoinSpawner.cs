using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CoinSpawner : NetworkBehaviour // the server will handle this
{
    [SerializeField] private RespawningCoin coinPrefab;
    [SerializeField] private int maxCoins = 50;
    [SerializeField] private int coinValue = 10;
    [SerializeField] private Vector2 xSpawnRange; // (min, max) for x pos
    [SerializeField] private Vector2 ySpawnRange; // (min, max) for y pos
    [SerializeField] private LayerMask layerMask; // for physics detection for spawning to see when we spawn the coin, it wont spawn inside certain layers (in this case, we will assign those unallowed layers as the Default Layer -> which has player for example)


    // Private Fields

    private float coinRadius;
    private Collider2D[] coinBuffer = new Collider2D[1]; // array with room for 1 member -> just to see if the drawn cicle (before we spawn the coin) has collided with SOMETHING.

    public override void OnNetworkSpawn()
    {
        if (!IsServer) { return; }

        coinRadius = coinPrefab.GetComponent<CircleCollider2D>().radius;

        for (int i = 0; i < maxCoins; i++)
        {
            SpawnCoin();
        }
    }
    private void SpawnCoin()
    {
        RespawningCoin coinInstance = Instantiate(coinPrefab, GetSpawnPoint(), Quaternion.identity); // Creates a local GameObject in the scene. + Only exists on the machine that called it. So, Other clients won’t see it.
        coinInstance.SetValue(coinValue);

        // Must do .Spawn() AFTER instantiate since .Spawn() only works for already created instances
        coinInstance.GetComponent<NetworkObject>().Spawn(); // Spawn over the network + All clients and the server will now see the same object. + Only the server is allowed to call .Spawn().

        coinInstance.OnCollected += HandleCoinCollected;
    }

    private void HandleCoinCollected(RespawningCoin coin) // change the RespawningCoin to the spawnPoint generated + reset the coin
    {
        coin.transform.position = GetSpawnPoint();
        coin.Reset(); // sets the alreadyCollected back to false
    }

    private Vector2 GetSpawnPoint()
    {
        float x = 0;
        float y = 0;

        while (true) // loop this until you get a valid spawn point
        {
            x = Random.Range(xSpawnRange.x, xSpawnRange.y);
            y = Random.Range(ySpawnRange.x, ySpawnRange.y);

            Vector2 spawnPoint = new Vector2(x, y);

            // NonAlloc = will not allocate memory at run time -> we already provided an array (coinBuffer) -> we will reuse this
            // spawnPoint + CoinRadius = circle that will be imagined 
            // coinBuffer is where the results of this method will go in
            // layerMask is the layer that we will be checking
            int numColliders = Physics2D.OverlapCircleNonAlloc(spawnPoint, coinRadius, coinBuffer, layerMask);
            if (numColliders == 0) // this is a valid spawnPoint
            {
                return spawnPoint;
            }
        }
    }
}
