using Unity.Netcode;
using UnityEngine;

public class CoinSpawner : NetworkBehaviour
{
    [Header("Referances")]
    [SerializeField] private RespawningCoin coinPrefab;

    [Header("Settings")]
    [SerializeField] private int maxCoins = 50;

    [SerializeField] private int coinValue = 10;

    [SerializeField] private Vector2 xSpawnRange;

    [SerializeField] private Vector2 ySpawnRange;

    // this specifies which layers we want to check agienst when doing a physics detection.
    // by doing so we prevent the coins from spawning in walls
    [SerializeField] private LayerMask layerMask;

    private float CoinRadius;
    private Collider2D[] CoinBuffer = new Collider2D[1];

    // Here we are making a Contact Filter to filter out collisions with objects.
    private ContactFilter2D ContactFilter = new ContactFilter2D();

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        ContactFilter.layerMask = layerMask; // here we specify a collision layer to ignore
        ContactFilter.useLayerMask = true; // here we specify that the ContactFilter would use Layermasks to ignore contacts.
        CoinRadius = coinPrefab.GetComponent<CircleCollider2D>().radius;

        for (int i = 0; i < maxCoins; i++)
        {
            SpawnCoin();
        }
    }

    private void SpawnCoin()
    {
        RespawningCoin CoinInstance = Instantiate
        (
            coinPrefab, 
            position : GetSpawnPoint(), 
            rotation: Quaternion.identity
        );
        CoinInstance.SetValue(coinValue);
        CoinInstance.GetComponent<NetworkObject>().Spawn();

        CoinInstance.OnCollected += HandleCoinCollected;
    }

    private void HandleCoinCollected(RespawningCoin coin)
    {
        coin.transform.position = GetSpawnPoint();
        coin.Reset();
    }

    private Vector2 GetSpawnPoint()
    {
        float x = 0;
        float y = 0;
        while (true)
        {
            x = Random.Range(xSpawnRange.x, xSpawnRange.y);
            y = Random.Range(ySpawnRange.x, ySpawnRange.y);
            Vector2 spawnPoint = new Vector2(x, y);

            int numColliders = Physics2D.OverlapCircle(spawnPoint, CoinRadius, ContactFilter, results : CoinBuffer);
            if (numColliders == 0) return spawnPoint;
        }
    }
}