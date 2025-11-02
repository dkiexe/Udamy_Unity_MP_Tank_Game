using System;
using Unity.Netcode;
using UnityEngine;

public class ProjectileLauncherScript : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private InputReader inputReader;
    [SerializeField] private Transform projectileSpawnPoint;
    [SerializeField] private GameObject ServerProjectilePrefab;
    [SerializeField] private GameObject ClientProjectilePrefab;

    [Header("Settings")]
    [SerializeField] private float ProjectileSpeed = 5f;

    private bool shouldFire;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        inputReader.PrimaryFireEvent += HandlePrimaryFire;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) return;
    }

    private void Update()
    {
        if (!IsOwner) return;
        if (!shouldFire) return;
        PrimaryFireServerRpc(projectileSpawnPoint.position, projectileSpawnPoint.up);
        SpawnDummyProjectile(projectileSpawnPoint.position, projectileSpawnPoint.up);
    }

    private void SpawnDummyProjectile(Vector2 spawnPos, Vector3 direction)
    {
        GameObject projectileIntance = Instantiate(
            ClientProjectilePrefab, 
            position : spawnPos,  
            rotation : Quaternion.identity
            );
        projectileIntance.transform.up = direction;
    }

    [ServerRpc] // this is how you make Remote Procedure Calls(RPC) to the server.
    private void PrimaryFireServerRpc(Vector2 spawnPos, Vector3 direction)
    {
        GameObject projectileIntance = Instantiate(
            ServerProjectilePrefab,
            position: spawnPos,
            rotation: Quaternion.identity
            );
        projectileIntance.transform.up = direction;
        SpwanDummyProjectileClientRpc(spawnPos, direction);
    }

    [ClientRpc] // this is how the server updates its clients after a RPC call.
    private void SpwanDummyProjectileClientRpc(Vector2 spawnPos, Vector3 direction)
    {
        if (IsOwner) return;
        SpawnDummyProjectile(spawnPos, direction);
    }


    private void HandlePrimaryFire(bool shouldFire)
    {
        this.shouldFire = shouldFire;
    }
}
