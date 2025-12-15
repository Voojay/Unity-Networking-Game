using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BountyCoin : Coin
{
    // Different from RespawningCoin because once collected -> will get destroyed permanentlu -> No respawn

    public override int Collect()
    {
        if (!IsServer) // we are client
        {
            Show(false); // coin will disappear
            return 0; // the coin value logic is handled by server
        }

        if (alreadyCollected)
        {
            return 0;
        }

        // State: coin not collected yet + we are server

        alreadyCollected = true;

        Destroy(this.gameObject);

        return coinValue; // Note that this will still be called since Destroy schedules happen at the end of the current frame

    }
}
