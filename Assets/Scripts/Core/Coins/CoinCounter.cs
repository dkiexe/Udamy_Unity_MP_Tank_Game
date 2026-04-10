using System.Collections;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class CoinCounter : NetworkBehaviour
{
    [Header("Refrences")]
    [SerializeField] private TextMeshProUGUI gameGoalTextReferance;

    [Header("Wining Settings")]
    [SerializeField] private int CoinsToWin = 1000;

    private NetworkVariable<FixedString32Bytes> gameGoalText = new NetworkVariable<FixedString32Bytes>();
    private NetworkVariable<Color> gameGoalTextColor = new NetworkVariable<Color>();

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            CoinWallet.OnCoinsCollected += CheckWinner;
        }
        else
        {
            gameGoalText.OnValueChanged += (_, _) => updateGoal();
            gameGoalTextColor.OnValueChanged += (_, _) => updateGoal();
        }
    }

    private void CheckWinner(CoinWallet coinWallet)
    {
        if (!(coinWallet.TotalCoins.Value >= CoinsToWin)) return;

        if (coinWallet.TryGetComponent<TankPlayer>(out TankPlayer tankPlayer))
        {
            SlowTimeClientRPC();
            
            Coroutine slowTimeCoroutine = StartCoroutine(SlowTime());
            
            if (tankPlayer.PlayerColor != null)
            {
                gameGoalTextColor.Value = gameGoalTextReferance.color = tankPlayer.PlayerColor.Value;
            }
            else
            {
                gameGoalTextColor.Value = gameGoalTextReferance.color = Color.gold;
            }
            gameGoalText.Value = gameGoalTextReferance.text = $"Player {tankPlayer.PlayerName.Value} Wins!";
            StartCoroutine(Endgame(tankPlayer, slowTimeCoroutine));
        }
    }

    private IEnumerator Endgame(TankPlayer winner, Coroutine prevCoroutineTask)
    {
        yield return prevCoroutineTask;

        NetworkObject netObj = winner.GetComponent<NetworkObject>();

        if (ServerSingelton.Instance)
        {
            string winnerAuthID = ServerSingelton.Instance.GameManager.networkServer.clientNetworkID_TO_AuthID[netObj.OwnerClientId];

            Invoke(nameof(DisconnectAllClientsFromServer), 4);

            ServerSingelton.Instance.GameManager.StopGameServer("WIN", winnerAuthID);
        }
        else
        {
            string winnerAuthID = HostSingelton.Instance.GameManager.networkServer.clientNetworkID_TO_AuthID[netObj.OwnerClientId];

            Invoke(nameof(DisconnectAllClientsFromHost), 4);

            HostSingelton.Instance.GameManager.Shutdown();

            ClientSingelton.Instance.GameManager.GoToMenu();
        }

    }

    private void updateGoal()
    {
        gameGoalTextReferance.color = gameGoalTextColor.Value;
        gameGoalTextReferance.text = gameGoalText.Value.ToString();
    }

    [ClientRpc]
    private void SlowTimeClientRPC()
    {
        StartCoroutine(SlowTime());
    }

    private IEnumerator SlowTime()
    {
        float current_ts = Time.timeScale;
        float startScale = current_ts;

        while (current_ts > 0f)
        {
            Time.timeScale = current_ts;
            current_ts -= 0.25f;

            yield return new WaitForSecondsRealtime(0.25f);
        }

        Time.timeScale = 0f;
    }

    private void DisconnectAllClientsFromServer()
    {
        ServerSingelton.Instance.GameManager.networkServer.DisconnectAllClients();
    }

    private void DisconnectAllClientsFromHost()
    {
        HostSingelton.Instance.GameManager.networkServer.DisconnectAllClients();
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            CoinWallet.OnCoinsCollected -= CheckWinner;
        }
        else
        {
            gameGoalText.OnValueChanged -= (_, _) => updateGoal();
            gameGoalTextColor.OnValueChanged -= (_, _) => updateGoal();
        }
    }
}
