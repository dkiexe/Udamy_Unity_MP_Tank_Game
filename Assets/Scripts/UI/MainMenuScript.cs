using TMPro;
using UnityEngine;

public class MainMenuScript : MonoBehaviour
{
    [SerializeField] private TMP_Text queueStatusText;
    [SerializeField] private TMP_Text queueTimerText;
    [SerializeField] private TMP_Text findMatchButtonText;
    [SerializeField] private TMP_InputField joinCodeField;

    private void Start()
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto); // Reset the cursor back to default mouse.
        queueStatusText.text = string.Empty;
        queueTimerText.text = string.Empty;
    }

    public async void StartRelayHost()
    {
        await HostSingelton.Instance.GameManager.StartRelayHostAsync();
    }

    public async void StartRelayClient()
    {
        await ClientSingelton.Instance.GameManager.StartRelayClientAsync(joinCodeField.text);
    }

    public async void StartMatchMakerClient()
    {
        await ClientSingelton.Instance.GameManager.StartMatchmakerClientAsync(queueStatusText, queueTimerText);
    }

    public void StartLanPlay()
    {
        if (!LanPortCheck.IsPortUsed())
        {
            HostSingelton.Instance.GameManager.StartLanHost();
        }
        else
        {
            ClientSingelton.Instance.GameManager.StartClient();
        }
    }
}
