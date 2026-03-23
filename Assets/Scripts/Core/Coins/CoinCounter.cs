using System;
using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class CoinCounter : NetworkBehaviour
{
    [Header("Refrences")]
    [SerializeField] private TextMeshProUGUI gameGoalText;

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
            StartCoroutine(SlowTime());
            if (tankPlayer.PlayerColor != null)
            {
                gameGoalText.color = tankPlayer.PlayerColor.Value; // {_(!)_} Null here when not matchmaking.
            }
            else
            {
                gameGoalText.color = Color.gold;
            }
            gameGoalText.text = $"Player {tankPlayer.PlayerName.Value} Wins!";
            Endgame(tankPlayer);
        }
    }

    private void Endgame(TankPlayer winner)
    {
        throw new NotImplementedException();
    }


    private IEnumerator SlowTime()
    {
        float current_ts, startScale;
        current_ts = startScale = Time.timeScale;

        while (current_ts >= 0)
        {
            Time.timeScale = current_ts;
            current_ts -= 0.25f;
            yield return new WaitForSeconds((current_ts - startScale) * -1);
        }
    }


    public override void OnNetworkDespawn()
    {
        if (!IsServer) return;

        CoinWallet.OnCoinsCollected -= CheckWinner;
    }
}
