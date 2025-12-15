using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Cinemachine;
using Unity.Collections;
using System;

public class TankPlayer : NetworkBehaviour // this needs to be networkbehaviour cuz we need to know when the tank spawns in and wait to see if we are the owner or not
{
    // this script is for when there are multiple players in the game -> the camera will need to know who to render the game to

    [Header("Reference")]
    [SerializeField] CinemachineVirtualCamera virtualCamera;
    [field: SerializeField] public Health Health { get; private set; } // so that we can do: player.Health.OnDie += 
    [field: SerializeField] public CoinWallet Wallet { get; private set; }
    [SerializeField] private SpriteRenderer minimapIcon;
    [SerializeField] private Texture2D crosshair;

    [Header("Settings")]
    [SerializeField] int ownerPriority = 15;
    [SerializeField] private Color minimapIconColor; // for our own player (will be different from other players)

    // For Player Name
    // Why NetworkVar? --> regular C# variable exists only in memory on the machine running the code
    // If you change a regular variable’s value on the server, the clients won’t know about it unless you manually send an RPC or custom message.
    // So, each player would see only their own local copy of the variable.
    // NetworkVariable is a special type that automatically syncs its value from the server to all clients who observe that object.
    // if you change PlayerName.Value on the server, the new value gets sent to all connected clients
    // Network Vars also have an event: PlayerName.OnValueChanged += OnPlayerNameChanged;
    // FixedString32Bytes are used instead of plain string (this is invalid for network var)
    public NetworkVariable<FixedString32Bytes> PlayerName = new NetworkVariable<FixedString32Bytes>();

    // For syncing what team the player is in:
    // We will be converting the teamId (what the matchmaker uses) to the teamindex 
    public NetworkVariable<int> TeamIndex = new NetworkVariable<int>();


    // Improved Way for tape 49:
    public event Action<ulong, int> OnWalletUpdated;

    // These events are used to notify other parts of the game when any player spawns or despawns.
    // You don’t want each TankPlayer to have its own separate event list.
    // Other things can all subscribe to without needing a reference to a specific player instance.
    // Only the server will be invoking this event 
    // This will be handled in the RespawnHandler Script
    public static event Action<TankPlayer> OnPlayerSpawned;
    public static event Action<TankPlayer> OnPlayerDespawned;

    // Improved way for tape 49
    private void Awake()
    {
        // Forward the NetworkVariable's event to your custom event  
        Wallet.TotalCoins.OnValueChanged += (oldVal, newVal) =>
        {
            OnWalletUpdated?.Invoke(OwnerClientId, newVal);
        };
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer) // if this is the server running the code
        {
            UserData userData = null; // we put this first before the ifs cuz we will use the userData in other parts of this method too

            if (IsHost) // this means if we are self hosted server
            {
                // set the userdata by getting it from networkserver
                userData = HostSingleton.Instance.GameManager.NetworkServer.GetUserDataByClientId(OwnerClientId);
            }
            else // for dedicated server
            {
                // Do similar code as above
                userData = ServerSingleton.Instance.GameManager.NetworkServer.GetUserDataByClientId(OwnerClientId);
            }
            
            PlayerName.Value = userData.userName;

            // Set the team index
            TeamIndex.Value = userData.teamIndex;

            OnPlayerSpawned?.Invoke(this); // whatever method subscribes to this -> it will get this TankPlayer
        }
        if (IsOwner) // if we are the owner to this obj (this is ourselves)
        {
            virtualCamera.Priority = ownerPriority;
            minimapIcon.color = minimapIconColor; // set the icon of our tank to be different from other players
            
            // the click point will be right in the middle of the Texture2D crosshair
            Cursor.SetCursor(crosshair, new Vector2(crosshair.width / 2, crosshair.height / 2), CursorMode.Auto); 
        }
        
        
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            OnPlayerDespawned?.Invoke(this);
        }
    }


}
