using System;
using Unity.Cinemachine;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class TankPlayer : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private CinemachineCamera TankCam;
    [SerializeField] private SpriteRenderer MinimapIconSprite;
    [SerializeField] private SpriteRenderer FacingArrowSprite;
    [SerializeField] private Texture2D gameCrosshair;
    [SerializeField] private AudioSource TankEngineSoundSource;
    [SerializeField] private AudioSource TankTurretSoundSource;
    [field: SerializeField] public Health playerHealth { get; private set; }
    [field: SerializeField] public CoinWallet Wallet { get; private set; }

    [Header("Settings")]
    [SerializeField] private int OwnerCamPriority = 20;
    [SerializeField] private Color OwnerMinimapColor = Color.blue;

    public NetworkVariable<FixedString32Bytes> PlayerName = new NetworkVariable<FixedString32Bytes>();
    public NetworkVariable<int> TeamID = new NetworkVariable<int>();
    public NetworkVariable<Color> PlayerColor = new NetworkVariable<Color>(value : default);
    public NetworkVariable<UpgradeStage> CurrentUpgradeStage = new NetworkVariable<UpgradeStage>
        (
            value: UpgradeStage.SingleShot
        );

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
            UserData userData;
            if (IsHost)
            {
                userData = HostSingelton.Instance.GameManager.networkServer.GetUserDataFromClientID
                (
                    OwnerClientId
                );
            }
            else
            {
                userData = ServerSingelton.Instance.GameManager.networkServer.GetUserDataFromClientID
                (
                    OwnerClientId
                );
            }
            PlayerName.Value = userData.userName;
            TeamID.Value = userData.teamId;
            OnPlayerSpawned?.Invoke(this);
        }

        if (IsOwner)
        {
            TankCam.Priority = OwnerCamPriority;
            MinimapIconSprite.color = OwnerMinimapColor;
            FacingArrowSprite.enabled = true;

            // Makes the owner's sound sources 2D so it doesn't change based on the player's position/rotation.
            TankEngineSoundSource.spatialBlend = 0f;
            TankTurretSoundSource.spatialBlend = 0f;

            Cursor.SetCursor(
                gameCrosshair,
                new Vector2(gameCrosshair.width / 2, gameCrosshair.height / 2),
                CursorMode.Auto
                );
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
