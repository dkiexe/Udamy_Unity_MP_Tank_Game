using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerSpawnHandler : NetworkBehaviour
{
    [Header("Player Referance")]
    [SerializeField] private TankPlayer playerPrefab;

    [Header("Respawn Settings")]
    [SerializeField] private float coinPercentageLossOnDeath = 0.5f; 

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            TankPlayer[] players = FindObjectsByType<TankPlayer>(FindObjectsSortMode.None);

            foreach (TankPlayer player in players)
            {
                HandlePlayerSpawn(player);
            }

            TankPlayer.OnPlayerSpawned += HandlePlayerSpawn;
            TankPlayer.OnPlayerDespawned += HandlePlayerDeSpawn;
        }
    }
    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            TankPlayer.OnPlayerSpawned -= HandlePlayerSpawn;
            TankPlayer.OnPlayerDespawned -= HandlePlayerDeSpawn;
        }
    }
    private void HandlePlayerSpawn(TankPlayer player)
    {
        // Bypassing an uneeded argument with a throw-away variable and a lambda
        player.playerHealth.OnDie += (_) => HandlePlayerDie(player);
    }

    private void HandlePlayerDeSpawn(TankPlayer player)
    {
        // Bypassing an uneeded argument with a throw-away variable and a lambda
        player.playerHealth.OnDie -= (_) => HandlePlayerDie(player);
    }

    private void HandlePlayerDie(TankPlayer player)
    {
        Destroy(player.gameObject);

        int coinValueAfterDeath = (int) (player.Wallet.TotalCoins.Value * (1 - coinPercentageLossOnDeath));

        StartCoroutine(SpawnPlayerNextFrame(
            player.OwnerClientId,
            coinValueAfterDeath
            ));
    }

    private IEnumerator SpawnPlayerNextFrame(ulong ownerClientID, int coinValueAfterDeath)
    {
        yield return null;

        TankPlayer newPlayerGameObject = Instantiate
            (
                playerPrefab
            );

        newPlayerGameObject.NetworkObject.SpawnAsPlayerObject(ownerClientID);
        ReassignSpawnPosClient(newPlayerGameObject);
        newPlayerGameObject.Wallet.TotalCoins.Value += coinValueAfterDeath;
    }

    public static void ReassignSpawnPosClient(TankPlayer tankPlayer)
    {
        tankPlayer.SpawnPos.Value = SpawnPoint.GetRandomSpawnPos();
    }
}
