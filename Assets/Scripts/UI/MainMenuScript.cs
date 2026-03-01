using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuScript : MonoBehaviour
{
    [SerializeField] private TMP_Text queueStatusText;
    [SerializeField] private TMP_Text queueTimerText;
    [SerializeField] private TMP_Text findMatchButtonText;
    [SerializeField] private TMP_InputField joinCodeField;
    [SerializeField] private Button MM_Button;
    [SerializeField] private Button HostButton;
    [SerializeField] private Button ClientButton;
    [SerializeField] private Button LobbiesButton;
    [SerializeField] private Button LanButton;

    public bool TeamQueueEnabled { get; set; } = false;

    private void Start()
    {
        SetButtonsActive(true);
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto); // Reset the cursor back to default mouse.
        queueStatusText.text = string.Empty;
        queueTimerText.text = string.Empty;
    }

    public async void StartRelayHost()
    {
        SetButtonsActive(false, true);
        await HostSingelton.Instance.GameManager.StartRelayHostAsync();
        SetButtonsActive(true, true);
    }

    public async void StartRelayClient()
    {
        SetButtonsActive(false);
        await ClientSingelton.Instance.GameManager.StartRelayClientAsync(joinCodeField.text);
        SetButtonsActive(true);
    }

    public async void StartMatchMakerClient()
    {
        SetButtonsActive(false);
        await ClientSingelton.Instance.GameManager.StartMatchmakerClientAsync(
            queueStatusText, 
            queueTimerText,
            findMatchButtonText,
            TeamQueueEnabled
            );
        SetButtonsActive(true);
    }

    public void StartLanPlay()
    {
        SetButtonsActive(false);
        if (!LanPortCheck.IsPortUsed())
        {
            HostSingelton.Instance.GameManager.StartLanHost();
        }
        else
        {
            ClientSingelton.Instance.GameManager.StartClient();
        }
    }

    private void SetButtonsActive(bool status, bool includeMM = false)
    {
        if (includeMM) MM_Button.interactable = status;
        HostButton.interactable = status;
        ClientButton.interactable = status;
        LobbiesButton.interactable = status;
        LanButton.interactable = status;
    }
}
