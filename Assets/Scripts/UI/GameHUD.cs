using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class GameHUD : NetworkBehaviour
{
    [SerializeField] private TMP_Text privateLobbyJoinCode;

    public NetworkVariable<FixedString32Bytes> JoinCodeSynced = new NetworkVariable<FixedString32Bytes>(
        default,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        JoinCodeSynced.OnValueChanged += ChangePrivateJoinCode;
        ChangePrivateJoinCode(string.Empty, JoinCodeSynced.Value); // For players joining late.

        if (IsHost)
        {
            JoinCodeSynced.Value = HostSingelton.Instance.GameManager.JoinCode;
        }
    }

    private void ChangePrivateJoinCode(FixedString32Bytes _, FixedString32Bytes newVal)
    {
        privateLobbyJoinCode.text = newVal.Value.ToString();
    }


    public void LeaveGame()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            HostSingelton.Instance.GameManager.Shutdown();
        }

        ClientSingelton.Instance.GameManager.Disconnect();
    }

    public override void OnNetworkDespawn()
    {
        JoinCodeSynced.OnValueChanged -= ChangePrivateJoinCode;
    }
}
