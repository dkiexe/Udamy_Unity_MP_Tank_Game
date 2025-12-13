using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerSpawnHandler : NetworkBehaviour
{
    [Header("Player Referance")]
    [SerializeField] private NetworkObject playerPrefab;

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

        StartCoroutine(SpawnPlayerNextFrame(player.OwnerClientId));
    }

    private IEnumerator SpawnPlayerNextFrame(ulong ownerClientID)
    {
        yield return null;

        NetworkObject newPlayerGameObject = Instantiate
            (
                playerPrefab
            );
        ReassignSpawnPosClient(newPlayerGameObject.GetComponent<TankPlayer>());
        newPlayerGameObject.SpawnAsPlayerObject(ownerClientID);
    }

    public static void ReassignSpawnPosClient(TankPlayer tankPlayer)
    {
        tankPlayer.SpawnPos.Value = SpawnPoint.GetRandomSpawnPos();
    }
}
