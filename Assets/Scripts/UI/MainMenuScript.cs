using TMPro;
using UnityEngine;

public class MainMenuScript : MonoBehaviour
{
    [SerializeField] private TMP_InputField joinCodeField;

    public async void StartHost()
    {
        await HostSingelton.Instance.GameManager.StartHostAsync();
    }

    public async void StartClient()
    {
        await ClientSingelton.Instance.GameManager.StartClientAsync(joinCodeField.text);
    }
}
