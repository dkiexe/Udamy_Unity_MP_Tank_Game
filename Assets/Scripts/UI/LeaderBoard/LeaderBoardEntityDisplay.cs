using TMPro;
using Unity.Collections;
using UnityEngine;

public class LeaderBoardEntityDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text displayText;

    private FixedString32Bytes playerName;

    public ulong ClientID { get; private set; }
    public int Coins { get; private set; }

    public void Initialise(ulong clientID, FixedString32Bytes playerName, int coins)
    {
        ClientID = clientID;
        this.playerName = playerName;
        
        updateCoins(coins);
    }

    public void updateCoins(int coins)
    {
        Coins = coins;
        updateText();
    }

    private void updateText()
    {
        displayText.text = $"1. {playerName} ({Coins})";
    }
}
