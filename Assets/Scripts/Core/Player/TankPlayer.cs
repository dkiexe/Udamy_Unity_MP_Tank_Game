using System;
using Unity.Cinemachine;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class TankPlayer : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private CinemachineCamera TankCam;
    [SerializeField] private SpriteRenderer MinimapIconSprite;
    [field: SerializeField] public Health playerHealth { get; private set; }
    [field: SerializeField] public CoinWallet Wallet { get; private set; }

    [Header("Settings")]
    [SerializeField] private int OwnerCamPriority = 20;
    [SerializeField] private Color OwnerMinimapColor = Color.blue;

    public NetworkVariable<FixedString32Bytes> PlayerName = new NetworkVariable<FixedString32Bytes>();

    public static event Action<TankPlayer> OnPlayerSpawned;
    public static event Action<TankPlayer> OnPlayerDespawned;

    public override void OnNetworkSpawn()
    {
        // CinemachineCamera controls the main camera based on priority.
        // Because each player has their own TankCam, we set the priority 
        // high for the owner of the player object so only their camera takes control.
        // on that client.

        if (IsServer)
        {
            UserData userData = HostSingelton.Instance.GameManager.networkServer.GetUserDataFromClientID
            (
                OwnerClientId
            );
            PlayerName.Value = userData.userName;
            OnPlayerSpawned?.Invoke(this);
        }

        if (IsOwner)
        {
            TankCam.Priority = OwnerCamPriority;
            MinimapIconSprite.color = OwnerMinimapColor;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            OnPlayerDespawned?.Invoke(this);
        }
    }
}
