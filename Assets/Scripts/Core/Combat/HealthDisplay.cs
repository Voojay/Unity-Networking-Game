using System.Collections;
using System.Collections.Generic;
// using Microsoft.Unity.VisualStudio.Editor;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class HealthDisplay : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Health health;
    [SerializeField] private Image healthBarImage;

    public override void OnNetworkSpawn()
    {
        // IsOwner = True only for the client that "owns" this networked object. + Ownership is usually assigned when a player spawns a networked object. 
        // IsServer = True only on the server (and also on the host, since the host is both server and client).
        if (!IsClient) { return; } // True on all clients (including the host). + all clients return true, whether they own the object or not. --> Want this because HealthDisplay should be for EVERYONE
        health.CurrentHealth.OnValueChanged += HandleHealthChanged;

        // we need this next line so that the health bar actually is initialized (to the max health) and filled
        // This is because currentHealth value is already synced (the value is 100 for example) BUT HandleHealthChanged hasn’t been called yet
        // Without this, the health bar UI might stay blank or wrong until health actually changes for the first time.
        HandleHealthChanged(0, health.CurrentHealth.Value); // the value is the max health already from the OnNetworkSpawn in Health.cs
    }

    public override void OnNetworkDespawn()
    {
        if (!IsClient) { return; }
        health.CurrentHealth.OnValueChanged -= HandleHealthChanged;
    }

    private void HandleHealthChanged(int oldHealth, int newHealth) // this method subscribes to the currentHealth NetworkVariable
    {
        healthBarImage.fillAmount = (float)newHealth / health.MaxHealth;
        
    }
}
