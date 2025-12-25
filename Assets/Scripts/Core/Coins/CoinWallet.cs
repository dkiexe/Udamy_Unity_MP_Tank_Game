using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.LowLevelPhysics2D.PhysicsShape;

public class CoinWallet : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Health health;
    [SerializeField] private BountyCoin bountyCoinPrefab;

    [Header("Settings")]
    [SerializeField] private float bountyPercentage =  0.5f;
    [SerializeField] private int bountyCoinCount = 10;
    [SerializeField] private int minBountyCoinCount = 10;
    [SerializeField] private float coinSpread = 3f;
    [SerializeField] private LayerMask layerMask;

    private Collider2D[] coinBuffer = new Collider2D[1];
    private float coinRadius;
    
    // Here we are making a Contact Filter to filter out collisions with objects.
    private ContactFilter2D ContactFilter = new ContactFilter2D();

    public NetworkVariable<int> TotalCoins = new NetworkVariable<int>();

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        ContactFilter.layerMask = layerMask; // here we specify a collision layer to ignore
        ContactFilter.useLayerMask = true; // here we specify that the ContactFilter would use Layermasks to ignore contacts.
        coinRadius = bountyCoinPrefab.GetComponent<CircleCollider2D>().radius;

        health.OnDie += HandleDie;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer) return;
        health.OnDie -= HandleDie;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.TryGetComponent<Coin>(out Coin CoinComponent))
        {
            int CoinValue = CoinComponent.Collect();
            if (!IsServer) return;
            TotalCoins.Value += CoinValue;
        }
    }
    public void SpendCoins(int Amount)
    {
        TotalCoins.Value -= Amount;
    }

    private void HandleDie(Health health)
    {
        int bountyValue = (int) (TotalCoins.Value * (1 - bountyPercentage));
        int bountyCoinValue = bountyValue / bountyCoinCount;

        if (bountyCoinValue <= minBountyCoinCount) return;

        for (int i = 0; i < bountyCoinCount; i++)
        {

            Vector3 randomSpawnPos = new Vector3(0, 0, 0);

            BountyCoin coinInstance = Instantiate
            (
                bountyCoinPrefab,
                position : GetSpawnPoint(),
                rotation : Quaternion.identity
            );
            
            coinInstance.SetValue(bountyCoinValue);
            coinInstance.NetworkObject.Spawn();
        }
    }

    private Vector2 GetSpawnPoint()
    {
        while (true)
        {
            // using a built in unity tool that creates a circle around a position and gives a random
            // position inside the circle.
            Vector2 spawnPoint = (Vector2) transform.position + 
                UnityEngine.Random.insideUnitCircle * coinSpread;

            int numColliders = Physics2D.OverlapCircle(spawnPoint, coinRadius, ContactFilter, results: coinBuffer);
            if (numColliders == 0) return spawnPoint;
        }
    }
}
