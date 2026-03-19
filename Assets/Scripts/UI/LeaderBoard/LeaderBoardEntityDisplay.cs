using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class LeaderBoardEntityDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text displayText;
    private FixedString32Bytes displayName;

    public int TeamID { get; private set; }
    public ulong ClientID { get; private set; }
    public int Coins { get; private set; }

    public void Initialise(ulong clientID, FixedString32Bytes displayName, int coins)
    {
        ClientID = clientID;
        this.displayName = displayName;

        updateCoins(coins);
    }

    public void Initialise(int teamID, FixedString32Bytes displayName, int coins)
    {
        TeamID = teamID;
        this.displayName = displayName;

        updateCoins(coins);
    }

    public void SetColor(Color color)
    {
        displayText.color = color;
    }

    public void updateCoins(int coins)
    {
        Coins = coins;
        updateText();
    }

    public void updateText()
    {
        displayText.text = $"{transform.GetSiblingIndex() + 1}. {displayName} ({Coins})";
    }
}
