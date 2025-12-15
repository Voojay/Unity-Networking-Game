using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;

public class ProjectileLauncher : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private TankPlayer tankPlayer; // for friendly fire --> we know what team this projectile is on
    [SerializeField] private InputReader inputReader;
    [SerializeField] private Transform projectileSpawnPoint;
    [SerializeField] private GameObject serverProjectilePrefab;
    [SerializeField] private GameObject clientProjectilePrefab;
    [SerializeField] private GameObject muzzleFlash; // the fire every time u shoot
    [SerializeField] private Collider2D playerCollider; // projectiles collide with other players BUT not our own collider
    [SerializeField] private CoinWallet wallet;

    [Header("Settings")]
    [SerializeField] private float projectileSpeed;
    [SerializeField] private float fireRate; // Number of shots PER SECOND
    [SerializeField] private float muzzleFlashDuration;
    [SerializeField] private int costToFire;


    // Private Fields
    bool shouldFire;
    private float muzzleFlashTimer;
    private float timer;
    private bool isPointerOverUI; // to prevent us shooting a bullet when we press the leave game button


    protected override void OnNetworkPostSpawn()
    {
        if (!IsOwner) { return; }

        inputReader.primaryFireEvent += HandlePrimaryFire; // Subscribe to event
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) { return; }

        inputReader.primaryFireEvent -= HandlePrimaryFire; // Unsubscribe to event

    }
    void Update()
    {
        // Before we do the checks, the muzzle flash will be counting down all the time
        if (muzzleFlashTimer > 0f)
        {
            muzzleFlashTimer -= Time.deltaTime;
            if (muzzleFlashTimer <= 0f)
            {
                muzzleFlash.SetActive(false);
            }
        }

        // Checks
        if (!IsOwner) { return; }

        // This is our player obj
        // current = the current EventSystem we using
        // This will check any UI that has raycast enabled -> if it does, then our pointer is considered over the UI
        // Note that for the images of minimap and leaderboard and the leaderboard's prefab's textmeshpro -> raycast target is off since we want to still be able to shoot while hovering over leaderboard and minimap
        isPointerOverUI = EventSystem.current.IsPointerOverGameObject();

        if (timer > 0) { timer -= Time.deltaTime; }

        if (!shouldFire) { return; } // if we aint firing, return out

        if (timer > 0) { return; } // if the timer for the cooldown projectile time has not run out, exit.

        if (wallet.TotalCoins.Value < costToFire) { return; }
        // WE are firing and we ARE the owner

        // takes in two paras: The spawn point (which is gonna be local to the prefab + the duirection of where it is POINTING)
        PrimaryFireServerRpc(projectileSpawnPoint.position, projectileSpawnPoint.up); // for the other clients to see ur projectile
        SpawnDummyProjectile(projectileSpawnPoint.position, projectileSpawnPoint.up, tankPlayer.TeamIndex.Value); // for yourself to see so that firing is instant with no ping
        timer = 1 / fireRate; // 1/fireRate is the TIME per shot
    }
    // Tell the server that we are shooting -> Create a server RPC  (Remote Procedure Call)
    // a method that runs on the server, but is called by a client. --> Basically a client to server communication
    // Use it when the client wants to request the server to do something (like spawn an object, apply damage, etc).
    // Must be marked with [ServerRpc].
    // This method basically spawns in the REAL projectile that will actually do damage to the player 

    private void HandlePrimaryFire(bool shouldFire)
    {
        // We ignore the shooting of projectile if our pointer is over the UI
        if (shouldFire && isPointerOverUI)
        {
            return;
        }

        this.shouldFire = shouldFire;
    }

    [ServerRpc] // Client to server
    void PrimaryFireServerRpc(Vector3 spawnPos, Vector3 direction) // spawns the real projectile
    {
        if (wallet.TotalCoins.Value < costToFire) { return; }

        // We do have enough to fire
        wallet.SpendCoins(costToFire);


        GameObject projectileInstance = Instantiate(
            serverProjectilePrefab,
            spawnPos,
            Quaternion.identity); // no rotation -> we change this in next line
        projectileInstance.transform.up = direction;

        Physics2D.IgnoreCollision(playerCollider, projectileInstance.GetComponent<Collider2D>()); // to prevent collision between this player and the spawned in collider

        // For friendly fire: assign the projectile that was instantiated with the teamindex
        // Note that the proj launcher is a component of the player 
        if (projectileInstance.TryGetComponent<Projectile>(out Projectile projectile)) // note that Projectile is a script attached to both the client and server projectiles
        {
            projectile.Initialize(tankPlayer.TeamIndex.Value);
        }                                                                                                    // Server will now call a ClientRPC method to send back to the clients

        if (projectileInstance.TryGetComponent<Rigidbody2D>(out Rigidbody2D rb)) // Use a trygetcomponent to see if there is a comp. -> if there is not, then skip. If there is -> use the rb as the component -> executes the code in the if statement
        {
            rb.velocity = rb.transform.up * projectileSpeed;
        }
        
        SpawnDummyProjectileClientRpc(spawnPos, direction, tankPlayer.TeamIndex.Value);
    }

    [ClientRpc] // server to client
    void SpawnDummyProjectileClientRpc(Vector3 spawnPos, Vector3 direction, int teamIndex)
    {
        if (IsOwner) { return; } // if we are the owner, we will exit since we already spawned the dummy projectile in the method below. This is for the other clients!!

        // Other clients will execute

        SpawnDummyProjectile(spawnPos, direction, teamIndex);

    }

    // Spawns in a 'dummy' -> Clients can see instantly after firing -> but this proj. isnt the one doing damage
    // WE have this just so that their is no ping in firing -> So this is basically pure visual
    void SpawnDummyProjectile(Vector3 spawnPos, Vector3 direction, int teamIndex) // For spawning the projectile so the client can see
    {

        muzzleFlash.SetActive(true);
        muzzleFlashTimer = muzzleFlashDuration; // start timer for muzzle flash visual -> Update() will start ticking this down



        GameObject projectileInstance = Instantiate(
            clientProjectilePrefab,
            spawnPos,
            Quaternion.identity); // no rotation -> we change this in next line
        projectileInstance.transform.up = direction;

        Physics2D.IgnoreCollision(playerCollider, projectileInstance.GetComponent<Collider2D>());

        if (projectileInstance.TryGetComponent<Projectile>(out Projectile projectile)) 
        {
            projectile.Initialize(teamIndex); // the client now has init the team index to the projectileinstance thru the projectile script
        } 

        if (projectileInstance.TryGetComponent<Rigidbody2D>(out Rigidbody2D rb)) // Use a trygetcomponent to see if there is a comp. -> if there is not, then skip. If there is -> use the rb as the component -> executes the code in the if statement
        {
            rb.velocity = rb.transform.up * projectileSpeed;
        }
    }

    
}
