using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class LeaderBoardEntityDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text displayText;
    [SerializeField] private Color selfPlayerColor;

    private FixedString32Bytes playerName;

    public ulong ClientID { get; private set; }
    public int Coins { get; private set; }

    public void Initialise(ulong clientID, FixedString32Bytes playerName, int coins)
    {
        ClientID = clientID;
        this.playerName = playerName;

        if (clientID == NetworkManager.Singleton.LocalClientId)
        {
            displayText.color = selfPlayerColor;
        }
        
        updateCoins(coins);
    }

    public void updateCoins(int coins)
    {
        Coins = coins;
        updateText();
    }

    public void updateText()
    {
        displayText.text = $"{transform.GetSiblingIndex() + 1}. {playerName} ({Coins})";
    }
}
