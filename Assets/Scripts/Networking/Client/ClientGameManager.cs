using System;
using System.Collections;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ClientGameManager : IDisposable
{
    /// <summary>
    /// This class is a logic class that handles the following:
    /// 1) client side Unity Game Services (UGS) authentication.
    /// 2) UGS client initialization.
    /// 3) Joining a UGS relay server as a UGS client.
    /// 4) Loading the menu scene for the client.
    /// </summary>

    private JoinAllocation allocation;

    private NetworkClient networkClient;

    private const string MenuSceneName = "Menu";

    private Coroutine clearTxtCoroutine;

    private Coroutine QueueTimerCorutine;

    private bool isQueuedMatchMaking = false;

    private CancellationTokenSource MM_cancelSource;

    public async Task<bool> InitAsync()
    {
        // initalize unity services, this must be done every time when wanting to use unity services.
        await UnityServices.InitializeAsync();

        networkClient = new NetworkClient(NetworkManager.Singleton);

        // now we do our own anonimus player authentication using UGS ( unity game services )
        Authstate AuthState = await AuthenticationWrapper.DoAuth(5);

        if (AuthState == Authstate.Authenticated) return true; // returns true if the auth was a succsess
        return false; // returns false if the auth was failed
    }

    public void GoToMenu()
    {
        SceneManager.LoadScene(MenuSceneName);
    }

    public async Task StartRelayClientAsync(string joinCode)
    {
        try
        {
            allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
        }
        catch (Exception ex)
        {
            Debug.Log(ex);
            return;
        }
        
        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        // Unity struct that contains all the connection information the game needs to connect to Unity Relay using the Unity Transport(UTP) networking layer.
        RelayServerData relayServerData = AllocationUtils.ToRelayServerData(allocation, "dtls");

        transport.SetRelayServerData(relayServerData); // notifying the NetworkManager's transport object about the relay server

        StartClient();

        // Here there is no scene change because the server takes care of this for all clients.
    }

    public void StartClient()
    {
        // making a new user data object to then convert to json and send to the server.
        UserData userData = new UserData
        {
            userName = PlayerPrefs.GetString(
                NameSelector.PLAYERNAMEKEY,
                "Guest"
                ),
            userAuthId = AuthenticationService.Instance.PlayerId
        };

        // converting the user class to a json object for sirialization.
        string payload = JsonUtility.ToJson(userData);

        // converting the json string to a byte array to be sent as a connection payload.

        byte[] payloadbytes = Encoding.UTF8.GetBytes(payload);

        // setting the connection data to be sent to the server on connect.
        NetworkManager.Singleton.NetworkConfig.ConnectionData = payloadbytes;

        // Now starting host on the relay service given by unity instead of a local server.
        NetworkManager.Singleton.StartClient();
    }

    public async Task StartMatchmakerClientAsync(
        TMP_Text queueStatusText,
        TMP_Text queueTimerText,
        TMP_Text findMatchButtonText
        )
    {
        if (isQueuedMatchMaking) // If we are already matchmaking Cancel it.
        {
            MM_cancelSource.Cancel();
            StopMatchSearch("Canceled.", queueStatusText, queueTimerText);
            isQueuedMatchMaking = false;
            findMatchButtonText.text = "Find Match!";
            return;
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

        NetworkOperationResult NOP_Login = await MatchMakingClient.LogInAsync(userName, MM_cancelSource.Token);
        
        if (!NOP_Login.success)
        {
            StopMatchSearch(NOP_Login.message[0], queueStatusText, queueTimerText);
            findMatchButtonText.text = "Find Match!";
            return;
        }
        
        queueStatusText.text = "Searching For Game...";

        NetworkOperationResult NOP_Assignment = await MatchMakingClient.AwaitServerAssignmentAsync(MM_cancelSource.Token);

        if (!NOP_Assignment.success)
        {
            StopMatchSearch(NOP_Assignment.message[0], queueStatusText, queueTimerText);
            findMatchButtonText.text = "Find Match!";
            return;
        }

        string CMD = NOP_Assignment.message[0];
        Memory<string> Args = NOP_Assignment.message.AsMemory(1);

        switch (CMD)
        {
            case "CONNECT":
                {
                    queueStatusText.text = "Connecting...";
                    string ip = Args.Span[0];
                    int port = int.Parse(Args.Span[1]);

                    UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                    transport.SetConnectionData(ip, (ushort)port);
                    StartClient();
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
        await MatchMakingClient.DisposeAsync(); // Not needed anymore after getting server assignment.
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

    public void Disconnect()
    {
        networkClient.Disconnect();
    }

    public void Dispose()
    {
        if (MM_cancelSource != null) MM_cancelSource.Dispose();
        networkClient?.Dispose();
    }
}
