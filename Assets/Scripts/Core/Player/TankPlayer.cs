using Unity.Cinemachine;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class TankPlayer : NetworkBehaviour
{
    [Header("Refrences")]
    [SerializeField] private CinemachineCamera TankCam;

    [Header("Settings")]
    [SerializeField] private int OwnerCamPriority = 20;

    public NetworkVariable<FixedString32Bytes> PlayerName = new NetworkVariable<FixedString32Bytes>();
    
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
        }

        if (IsOwner)
        {
            TankCam.Priority = OwnerCamPriority;
        }
    }
}
