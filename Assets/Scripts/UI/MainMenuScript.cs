using UnityEngine;

public class MainMenuScript : MonoBehaviour
{
    public async void StartHost()
    {
        await HostSingelton.Instance.GameManager.StartHostAsync();
    }
}
