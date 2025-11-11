using Unity.Netcode;
using UnityEngine;

public class CoinWallet : NetworkBehaviour
{
    public NetworkVariable<int> TotalCoins = new NetworkVariable<int>();

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
}
