using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class NewMonoBehaviourScript : NetworkBehaviour
{
    [Header("Refrences")]
    [SerializeField] private Image healPowerBar;

    [Header("Settings")]
    // this is responsible for how much game ticks a player can be healed for
    [SerializeField] private int maxHealPower = 30;
    [SerializeField] private float healCooldown = 60f;
    [SerializeField] private float healTickRate = 1f;
    [SerializeField] private int CoinsPerTick = 10;
    [SerializeField] private int HealthPerTick = 20;

    private float remainingCooldown;
    private float tickTimer;

    private List<TankPlayer> playersInZone = new List<TankPlayer>();

    private NetworkVariable<int> HealPower = new NetworkVariable<int>();

    public override void OnNetworkSpawn()
    {
        if (IsClient)
        {
            HealPower.OnValueChanged += HandleHealPowerChanged;
            HandleHealPowerChanged(0, HealPower.Value);
        }
        if (IsServer)
        {
            HealPower.Value = maxHealPower;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsClient)
        {
            HealPower.OnValueChanged -= HandleHealPowerChanged;
        }
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (!IsServer) return;
        if (!col.attachedRigidbody.TryGetComponent(out TankPlayer player)) return;
        playersInZone.Add(player);
    }
    private void OnTriggerExit2D(Collider2D col)
    {
        if (!IsServer) return;
        if (!col.attachedRigidbody.TryGetComponent(out TankPlayer player)) return;
        playersInZone.Remove(player);
    }

    private void Update()
    {
        if (!IsServer) return;
        if (remainingCooldown > 0)
        {
            remainingCooldown -= Time.deltaTime;
            if (remainingCooldown < 0)
            {
                HealPower.Value = maxHealPower;
            }
            else return;
        }

        tickTimer += Time.deltaTime;
        if (tickTimer >= 1 / healTickRate)
        {
            foreach (TankPlayer player in playersInZone)
            {
                if (HealPower.Value == 0) break;
                
                if (player.playerHealth.CurrentHealth.Value == player.playerHealth.MaxHealth) continue;

                if (player.Wallet.TotalCoins.Value < CoinsPerTick) continue;

                player.Wallet.SpendCoins(CoinsPerTick);

                player.playerHealth.RestoreHealth(HealthPerTick);

                HealPower.Value -= 1;

                if (HealPower.Value == 0)
                {
                    remainingCooldown = healCooldown;
                }
            }
            tickTimer = tickTimer % (1 / healTickRate);
        }
    }

    private void HandleHealPowerChanged(int oldHealPower, int newHealPower)
    {
        healPowerBar.fillAmount = (float) newHealPower / maxHealPower;
    }
}
