using TMPro;
using Unity.Netcode;
using UnityEngine;

public class CoinCounter : NetworkBehaviour
{
    [Header("Refrences")]
    [SerializeField] private TextMeshPro gameGoalText;

    [Header("Wining Settings")]
    [SerializeField] private int CoinsToWin = 30;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        
        CoinWallet.OnCoinsCollected += CheckWinner;
    }

    private void CheckWinner(CoinWallet coinWallet)
    {
        if (!(coinWallet.TotalCoins.Value >= CoinsToWin)) return;

        if (coinWallet.TryGetComponent<TankPlayer>(out TankPlayer tankPlayer))
        {
            Time.timeScale = 0f;   // Freeze
            if (tankPlayer.PlayerColor != null)
            {
                gameGoalText.color = tankPlayer.PlayerColor.Value; // {_(!)_} Null here when not matchmaking.
            }
            else
            {
                gameGoalText.color = Color.green;
            }
            gameGoalText.text = $"Player {tankPlayer.PlayerName.Value} Wins!";
        }
    }


    public override void OnNetworkDespawn()
    {
        if (!IsServer) return;

        CoinWallet.OnCoinsCollected -= CheckWinner;
    }
}
