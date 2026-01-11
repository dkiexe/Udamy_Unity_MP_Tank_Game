using Unity.Netcode;
using UnityEngine;

public class GameHUD : MonoBehaviour
{
    public void LeaveGame()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            HostSingelton.Instance.GameManager.Shutdown();
        }

        ClientSingelton.Instance.GameManager.Disconnect();
    }
}
