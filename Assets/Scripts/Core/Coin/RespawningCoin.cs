using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RespawningCoin : Coin
{

    private Vector3 previousPosition;

    public event Action<RespawningCoin> OnCollected; // So a RespawningCoin can raise this event when they are collected AND CoinSpawner can LISTEN for this

    void Update()
    {
        if (previousPosition != transform.position) // from the position of the previous frame and this frame's position -> the position changed! -> It got respawned
        {
            Show(true);
        }
        previousPosition = transform.position;
    }
    
    public override int Collect()
    {
        if (!IsServer) // IsServer is now avail since we inherit from coin and coin inherits from NetworkBehaviour
        {
            // we are not the server, we are the client
            Show(false); // hide the coin on the client visually
            return 0; // don't actually "collect" anything on the client since the server handles the game logic such as collecting coins

        }

        // So from here on out we are the server

        if (alreadyCollected == true)
        {
            return 0; // in case two clients collect the coin at the same time
        }

        // alreadyCollected == false AND the code running here is the server:

        alreadyCollected = true;

        OnCollected?.Invoke(this);
        return coinValue;

    }

    public void Reset() // after collection, set it back to false
    {
        alreadyCollected = false;
    }

}
