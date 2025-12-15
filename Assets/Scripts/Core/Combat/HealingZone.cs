using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class HealingZone : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Image healPowerBar; // for filling the amount of heals the pad has left

    [Header("Settings")]
    [SerializeField] private int maxHealPower = 30; // 30 Ticks of heals before going into cooldown
    [SerializeField] private float healCooldown = 60f; // how muchc time it takes to cooldown
    [SerializeField] private float healTickRate = 1f; // In tick/time unit
    [SerializeField] private int coinPerTick = 10; // how many coins for one tick
    [SerializeField] private int healthPerTick = 10; // How much health it restores per tick

    // Private fields
    private List<TankPlayer> playersInZone = new List<TankPlayer>();

    // Server is in charge of healpower -> then clients will update their UI accordingly
    private NetworkVariable<int> HealPower = new NetworkVariable<int>(); // int since healpower is in ticks unit

    private float remainingCooldown; // how much time is left for cooldown
    private float tickTimer; // how long has it been SINCE THE LAST TICK

    public override void OnNetworkSpawn()
    {
        // For UI stuff (showing how much healpower the pad has left), only the client needs to care about this
        // When a client connects → the HealingZone is spawned on that client too, and OnNetworkSpawn ALSO RUNS on the client’s side.
        if (IsClient)
        {
            HealPower.OnValueChanged += HandleHealPowerChanged; // Don't forget to do .OnValueChanged

            // When the client spawns, some healing pads might be partially used already -> We have to update the UI like this:
            HandleHealPowerChanged(0, HealPower.Value); // Set to UI to show the current HealPower.Value
        }

        // When the server spawns the HealingZone → OnNetworkSpawn runs on the server.
        if (IsServer) // when the game session actually starts
        {
            HealPower.Value = maxHealPower;// Since this is the server -> must init and set all healing pads to be in full max healpower
        }

    }
    public override void OnNetworkDespawn()
    {
        if (IsClient)
        {
            HealPower.OnValueChanged -= HandleHealPowerChanged; 
        }
    }
    private void OnTriggerEnter2D(Collider2D other)
    {

        if (!IsServer) { return; } // This is game logic -> only server should be handling this

        // Must do attachedRigidbody to get the rigidbody component to -> then get the TanKplayer
        // This is because the TankPlayer component is in the Player parent in hierarchy. The rigidbody is a component of this.
        // Without attached rigidbody, it wont get it because the collider2D is a component of TankTreads, NOT player.


        if (!other.attachedRigidbody.TryGetComponent<TankPlayer>(out TankPlayer player)) { return; } // if it is not a player -> exit



        playersInZone.Add(player);
        Debug.Log($"Entered Healing Zone: {player.PlayerName.Value}");

    }
    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsServer) { return; } // This is game logic -> only server should be handling this

        if (!other.attachedRigidbody.TryGetComponent<TankPlayer>(out TankPlayer player)) { return; } // if it is not a player -> exit

        playersInZone.Remove(player);
        Debug.Log($"Exited Healing Zone: {player.PlayerName.Value}");
    }

    // for checking who is in the zone
    private void Update()
    {
        // Only the server should be running any of this logic
        if (!IsServer) { return; }

        if (remainingCooldown > 0f)
        {
            remainingCooldown -= Time.deltaTime;

            if (remainingCooldown <= 0f)
            {
                HealPower.Value = maxHealPower;
            }
            else
            {
                return;
            }
        }

        // Current State: Cooldown is OFF and the HealPower is not zero
        tickTimer += Time.deltaTime;


        // sinc heal tick rate is in tick/time -> we want time/tick -> 1/healtickrate
        if (tickTimer >= 1 / healTickRate) // allow for another tick
        {
            foreach (TankPlayer player in playersInZone)
            {
                if (HealPower.Value == 0) { break; } // Zone does not have any heal power left -> break 

                if (player.Health.CurrentHealth.Value == player.Health.MaxHealth)
                {
                    Debug.Log($"Can't Heal: At max health");
                    continue; // player already at max health --> continue (go to next player in foreach loop)
                }


                if (player.Wallet.TotalCoins.Value < coinPerTick)
                {
                    Debug.Log($"Can't heal: {player.PlayerName.Value} does not have enough coins!");
                    continue;
                }

                player.Health.RestoreHealth(healthPerTick);
                player.Wallet.SpendCoins(coinPerTick);

                HealPower.Value -= 1;

                if (HealPower.Value == 0)
                {
                    remainingCooldown = healCooldown; // the next loop now wont execute
                }
            }

            // Now that we have healed everyone, we now have to account for the leftover time
            // Since one frame might overshot the time per tick --> we need to get that leftover time and add it to the tick timer
            // Let's say that time per tick is 0.5 -> and the frame lasted 0.55 seconds -> we need to carry that extra 0.05 seconds to tick timer
            // This is so that our ticks wont be offsync
            tickTimer = tickTimer % (1 / healTickRate);
        }


        

    }

    private void HandleHealPowerChanged(int oldHealPower, int newHealPower)
    {
        healPowerBar.fillAmount = (float)newHealPower / maxHealPower;
    }

}
