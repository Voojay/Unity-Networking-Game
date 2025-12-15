using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public abstract class Coin : NetworkBehaviour // abstract = this class will never exist as a component of any object -> Only the child of this class (Ex: Respawn coin, Bounty coin, etc.)
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    protected int coinValue = 10; // protected = the subclasses (children) can read this value!!
    protected bool alreadyCollected; // in case two players collect a coin in the same frame
    public abstract int Collect(); // no need to put any code in here cuz it is an abstract method
                                   // So basically any child that implements this class must implement this method

    public void SetValue(int value)
    {
        coinValue = value;
    }

    protected void Show(bool show) // the child of this abstract class uses this method to show/not show the coin -> Use protected
    {
        spriteRenderer.enabled = show;
    }
}
