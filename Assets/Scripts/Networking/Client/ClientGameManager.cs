using System;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using static UnityEngine.RuleTile.TilingRuleOutput;

public class ClientGameManager
{
    private JoinAllocation allocation;

    private const string MenuSceneName = "Menu";

    public async Task<bool> InitAsync()
    {
        // initalize unity services, this must be done every time when wanting to use unity services.
        await UnityServices.InitializeAsync();

        // now we do our own anonimus player authentication using UGS ( unity game services )
        Authstate AuthState = await AuthenticationWrapper.DoAuth(5);

        if (AuthState == Authstate.Authenticated) return true; // returns true if the auth was a succsess
        return false; // returns false if the auth was failed
    }

    public void GoToMenu()
    {
        SceneManager.LoadScene(MenuSceneName);
    }

    public async Task StartClientAsync(string joinCode)
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

        // making a new user data object to then convert to json and send to the server.
        UserData userData = new UserData
        {
            userName = PlayerPrefs.GetString(
                NameSelector.PLAYERNAMEKEY,
                "Guest"
                )
        };
        
        // converting the user class to a json object for sirialization.
        string payload = JsonUtility.ToJson(userData);

        // converting the json string to a byte array to be sent as a connection payload.

        byte[] payloadbytes = Encoding.UTF8.GetBytes(payload);

        // setting the connection data to be sent to the server on connect.
        NetworkManager.Singleton.NetworkConfig.ConnectionData = payloadbytes;

        // Now starting host on the relay service given by unity instead of a local server.
        NetworkManager.Singleton.StartClient();

        // Here there is no scene change because the server takes care of this for all clients.
    }
}
