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

    private List<TankPlayer> playersInZone = new List<TankPlayer>();

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (!IsServer) return;
        if (!col.attachedRigidbody.TryGetComponent(out TankPlayer player)) return;
        playersInZone.Add(player);
        Debug.Log($"Player: {player.PlayerName.Value} has entered a healing pad");
    }
    private void OnTriggerExit2D(Collider2D col)
    {
        if (!IsServer) return;
        if (!col.attachedRigidbody.TryGetComponent(out TankPlayer player)) return;
        playersInZone.Remove(player);
        Debug.Log($"Player: {player.PlayerName.Value} has exited a healing pad");
    }
}
