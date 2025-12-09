using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;

public class TankPlayer : NetworkBehaviour
{
    [Header("Refrences")]
    [SerializeField] private CinemachineCamera TankCam;

    [Header("Settings")]
    [SerializeField] private int OwnerCamPriority = 20;
    
    public override void OnNetworkSpawn()
    {
        // CinemachineCamera controls the main camera based on priority.
        // Because each player has their own TankCam, we set the priority 
        // high for the owner of the player object so only their camera takes control.
        // on that client.
        if (!IsOwner) return;
        TankCam.Priority = OwnerCamPriority;
    }
}
