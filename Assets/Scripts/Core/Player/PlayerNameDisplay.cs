using System;
using TMPro;
using Unity.Collections;
using UnityEngine;

public class PlayerNameDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TankPlayer tankPlayer;
    [SerializeField] private TMP_Text playerNameText;

    private void Start()
    {
        HandlePlayerNameChanged(string.Empty, tankPlayer.PlayerName.Value);
        tankPlayer.PlayerName.OnValueChanged += HandlePlayerNameChanged;
    }

    private void HandlePlayerNameChanged(FixedString32Bytes oldName, FixedString32Bytes newName)
    {
        playerNameText.text = newName.Value;
    }

    private void OnDestroy()
    {
        tankPlayer.PlayerName.OnValueChanged -= HandlePlayerNameChanged;
    }
}
