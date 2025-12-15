using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Health : NetworkBehaviour
{
    // Since we want the health to be seen by everyone, it must be a networkvariable
    // instead of [SerializeField] private int maxHealth = 100;
    // We make it public (so other scripts can get but we dont want them to change it) -> make it a property {get ; private set;} -> 
    // properties are not shown in inspector by default (we want it to show in inspector so that we can edit it) --> [field: SerializeField] (so that u can see in inspector) + also make the name start with capital 
    [field: SerializeField] public int MaxHealth { get; private set; } = 100;
    public NetworkVariable<int> CurrentHealth = new NetworkVariable<int>(); // we will change the values on the server side
    // Note that NetworkVariables can be subbed and unsubbed using the .OnValueChanged
    // Parameters of such methods must be the ___ previousValue, ____ newValue where ____ can be int,float,bool,Vector3, etc.

    // Private Fields
    private bool isDead; // to not die twice+

    public event Action<Health> OnDie; //  a delegate type = declaring a public variable OnDie that can store a method (or multiple methods) that take a Health object as a parameter.
    public override void OnNetworkSpawn()
    {
        if (!IsServer) { return; }

        // Is a server (in this case: the host)

        CurrentHealth.Value = MaxHealth;
    }

    

    public void TakeDamage(int damageValue)
    {
        ModifyHealth(-damageValue);
    }

    public void RestoreHealth(int healValue)
    {
        ModifyHealth(healValue);
    }

    public void ModifyHealth(int value)
    {
        if (isDead) { return; }

        CurrentHealth.Value = Mathf.Clamp(CurrentHealth.Value + value, 0, MaxHealth);

        // After our currentvalue is set -> just check if it has reached zero yet

        if (CurrentHealth.Value <= 0)
        {
            Debug.Log("OnDie Invoked");
            OnDie?.Invoke(this);
            isDead = true;
        } 
    }
}
