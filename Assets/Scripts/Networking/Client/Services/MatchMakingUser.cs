using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class MatchMakingUser
{
    public bool isQueuedMatchMaking = false;

    private CancellationTokenSource MM_cancelSource;

    private Coroutine clearTxtCoroutine;

    private Coroutine QueueTimerCorutine;

    public async Task<bool> StartMatchmakingUserAsync(
        string authID,
        TMP_Text queueStatusText,
        TMP_Text queueTimerText,
        TMP_Text findMatchButtonText
        )
    {
        if (isQueuedMatchMaking) // If we are already matchmaking Cancel it.
        {
            MM_cancelSource.Cancel();
            MM_cancelSource.Dispose();
            StopMatchSearch("Canceled.", queueStatusText, queueTimerText);
            isQueuedMatchMaking = false;
            findMatchButtonText.text = "Find Match!";
            return false;
        }

        isQueuedMatchMaking = true;
        findMatchButtonText.text = "Cancel";

        MM_cancelSource = new CancellationTokenSource();

        string userName = PlayerPrefs.GetString(
            NameSelector.PLAYERNAMEKEY,
            "Guest"
        );

        TCP_MatchMakingClient MatchMakingClient = new TCP_MatchMakingClient();

        if (clearTxtCoroutine != null) ClientSingelton.Instance.StopCoroutine(clearTxtCoroutine);

        queueStatusText.text = "Connecting To MatchMaking Server...";

        QueueTimerCorutine = ClientSingelton.Instance.StartCoroutine(TimeUtils.QueueTimer(queueTimerText));

        NetworkOperationResult NOP_Login = await MatchMakingClient.LogInAsync(authID, userName,MM_cancelSource.Token);

        if (!NOP_Login.success)
        {
            StopMatchSearch(NOP_Login.message[0], queueStatusText, queueTimerText);
            findMatchButtonText.text = "Find Match!";
            return false;
        }

        queueStatusText.text = "Searching For Game...";

        NetworkOperationResult NOP_Assignment = await MatchMakingClient.AwaitServerAssignmentAsync(MM_cancelSource.Token);

        if (!NOP_Assignment.success)
        {
            StopMatchSearch(NOP_Assignment.message[0], queueStatusText, queueTimerText);
            findMatchButtonText.text = "Find Match!";
            return false;
        }

        string CMD = NOP_Assignment.message[0];
        Memory<string> Args = NOP_Assignment.message.AsMemory(1);

        bool MatchSucsess = false;

        switch (CMD)
        {
            case "CONNECT":
                {
                    queueStatusText.text = "Connecting...";
                    string ip = Args.Span[0];
                    int port = int.Parse(Args.Span[1]);

                    UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                    transport.SetConnectionData(ip, (ushort)port);
                    MatchSucsess = true;
                    break;
                }
            case "BANNED":
                {
                    queueStatusText.text = "MatchMaking Servers Refused ";
                    break;
                }
            default:
                {
                    if (CMD == string.Empty) break;
                    Debug.LogError($"MatchMaking Server Responded with {CMD}, This Message Header Is unhandled!");
                    break;
                }
        }
        isQueuedMatchMaking = false;
        MM_cancelSource.Dispose();
        await MatchMakingClient.DisposeAsync(); // Not needed anymore after getting server assignment.
        return MatchSucsess;
    }

    private void StopMatchSearch(string reason, TMP_Text queueStatusText, TMP_Text queueTimerText)
    {
        queueStatusText.text = reason;
        clearTxtCoroutine = ClientSingelton.Instance.StartCoroutine(clearMatchMakingUI(queueStatusText, queueTimerText));
        ClientSingelton.Instance.StopCoroutine(QueueTimerCorutine);
        isQueuedMatchMaking = false;
    }

    private IEnumerator clearMatchMakingUI(TMP_Text queueStatusText, TMP_Text queueTimerText)
    {
        yield return new WaitForSeconds(5);
        queueStatusText.text = string.Empty;
        queueTimerText.text = string.Empty;
    }
}