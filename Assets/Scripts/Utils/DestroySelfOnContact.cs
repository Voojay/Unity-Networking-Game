using System.Collections;
using System.Collections.Generic;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class DestroySelfOnContact : MonoBehaviour
{
    [SerializeField] private Projectile projectile;
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (projectile.TeamIndex != -1) // true if team game (friendly fire is off)
        {
            if (other.attachedRigidbody != null)
            {
                if (other.attachedRigidbody.TryGetComponent<TankPlayer>(out TankPlayer tankPlayer)) // get the tankplayer that the bullet hit
                {
                    if (tankPlayer.TeamIndex.Value == projectile.TeamIndex) // this projectile hits the other player that is on the same team
                    {
                        return; // the projectile keeps moving thru the other player without being destroyed
                    }
                }
            }
            
        }
        Destroy(gameObject);
    }
}
