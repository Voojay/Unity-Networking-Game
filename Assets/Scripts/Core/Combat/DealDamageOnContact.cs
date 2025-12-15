using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


public class DealDamageOnContact : MonoBehaviour // this class is for the server projectile which is the REAL projectile dealing damage
{
    [SerializeField] private Projectile projectile;
    [SerializeField] private int damage = 5;


    private ulong ownerClientId; // ulong = huge non-negative number, used for things like IDs
    

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.attachedRigidbody == null) { return; } // check if there is a rigidBody2D attached to this other game object

        if (projectile.TeamIndex != -1) // true if this is a team game -> implements friendly fire
        {
            // We dont want our own bullet colliding with ourselves:
            if (other.attachedRigidbody.TryGetComponent<TankPlayer>(out TankPlayer player))
            {
                if (player.TeamIndex.Value == projectile.TeamIndex) // the player is the same team as the projectile
                {
                    return; // wont deal damage to the other shot player since they on our team
                }
            }
        }
        

        // We are using .attachedRigidbody instead of gameObject because we wanna get the health component of the object WITH THE RIGIDBODY!!! This is because some parts of the parent-child does and does not have a rigidbody--> we want the gameObject WITH the rigidBody
        // Basically: Tries to get Health from the Rigidbody's GameObject (which could be a parent).
        // Useful when for example, the collider is on a child (tanktreads), but the logic (like health which is on the player parent) is on the Rigidbody parent.\
        // Use .gameObject if you expect the collider itself has Health
        if (other.attachedRigidbody.TryGetComponent<Health>(out Health health)) // try getting the componenet first to see if there is a health
        {
            health.TakeDamage(damage);
        }

    }
}
