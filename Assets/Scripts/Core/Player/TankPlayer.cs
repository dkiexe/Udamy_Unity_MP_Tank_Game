using System;
using Unity.Cinemachine;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class TankPlayer : NetworkBehaviour
{
    [Header("Refrences")]
    [SerializeField] private CinemachineCamera TankCam;
    [field: SerializeField] public Health playerHealth { get; private set; }
    [field: SerializeField] public CoinWallet Wallet { get; private set; }

    [Header("Settings")]
    [SerializeField] private int OwnerCamPriority = 20;

    public NetworkVariable<FixedString32Bytes> PlayerName = new NetworkVariable<FixedString32Bytes>();
    public NetworkVariable<Vector3> SpawnPos = new NetworkVariable<Vector3>();

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
            PlayerSpawnHandler.ReassignSpawnPosClient(this);
            OnPlayerSpawned?.Invoke(this);
        }

        if (IsOwner)
        {
            // This line makes the client's player teleport and take a position accoarding to a server, 
            // the server cannot do this directly becasue it lacks authority to move the client.
            GetComponent<NetworkTransform>().Teleport(
                SpawnPos.Value, 
                Quaternion.identity, 
                transform.localScale
            );
            TankCam.Priority = OwnerCamPriority;
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
