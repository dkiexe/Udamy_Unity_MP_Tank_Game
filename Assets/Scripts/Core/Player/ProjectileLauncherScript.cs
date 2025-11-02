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
    [SerializeField] private GameObject muzzleFlash;
    [SerializeField] private Collider2D playerCollider;

    [Header("Settings")]
    [SerializeField] private float ProjectileSpeed = 5f;
    [SerializeField] private float fireRate;
    [SerializeField] private float muzzleFlashDuration;

    private bool shouldFire;
    private float previousFireTime;
    private float muzzleFlashTimer;

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
        if (muzzleFlashTimer > 0f)
        {
            muzzleFlashTimer -= Time.deltaTime;

            if (muzzleFlashTimer <= 0f) muzzleFlash.SetActive(false);
        }

        if (!IsOwner) return;
        if (!shouldFire) return;
        // here we are trusting the client to calculate his time to fire.
        if (Time.time < (1 / fireRate + previousFireTime)) return;

        PrimaryFireServerRpc(projectileSpawnPoint.position, projectileSpawnPoint.up);
        SpawnDummyProjectile(projectileSpawnPoint.position, projectileSpawnPoint.up);
        previousFireTime = Time.time;
    }

    private void SpawnDummyProjectile(Vector2 spawnPos, Vector3 direction)
    {
        muzzleFlash.SetActive(true);
        muzzleFlashTimer = muzzleFlashDuration;

        GameObject projectileIntance = Instantiate(
            ClientProjectilePrefab,
            position: spawnPos,
            rotation: Quaternion.identity
            );
        projectileIntance.transform.up = direction;

        // this line insures the player wont shoot itself
        Physics2D.IgnoreCollision(playerCollider, projectileIntance.GetComponent<Collider2D>());

        if (projectileIntance.TryGetComponent<Rigidbody2D>(out Rigidbody2D RB2D))
        {
            RB2D.linearVelocity = RB2D.transform.up * ProjectileSpeed;
        }
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

        // this line insures the player wont shoot itself
        Physics2D.IgnoreCollision(playerCollider, projectileIntance.GetComponent<Collider2D>());
        
        if (projectileIntance.TryGetComponent<Rigidbody2D>(out Rigidbody2D RB2D))
        {
            RB2D.linearVelocity = RB2D.transform.up * ProjectileSpeed;
        }
        
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
