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

    private NetworkVariable<Vector3> SpawnPos = new NetworkVariable<Vector3>();
    
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

            SpawnPos.Value = HostSingelton.Instance.GameManager.networkServer.GetSpawnPosForClient(OwnerClientId);
            PlayerName.Value = userData.userName;
        }

        if (IsOwner)
        {
            TankCam.Priority = OwnerCamPriority;
        }
    }
    private void Start()
    {
        gameObject.transform.position = SpawnPos.Value;
    }
}
