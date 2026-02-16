using System.Threading.Tasks;
using UnityEngine;

public class ApplicationController : MonoBehaviour
{
    /// <summary>
    /// This class is the heart of the Networking logic and is responsible for identifiying
    /// if the application is running as a dedicated server or as a client/host pattern.
    /// Based on this it will launch the appropriate prefabs that are responsible for
    /// creating instences, effectivly starting the networking session.
    /// </summary>
    [SerializeField] private ClientSingelton clientPrefab;
    [SerializeField] private HostSingelton hostPrefab;
    [SerializeField] private ServerSingelton serverPrefab;

    private async void Start()
    {
        DontDestroyOnLoad(gameObject);

        await LaunchInMode(
            // this Line below checks if we are a dedicated server or not. ( server == GraphicsDeviceType.Null)
            SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null
        );
    }

    private async Task LaunchInMode(bool isDedicatedServer)
    {
        if (isDedicatedServer)
        {
            ServerSingelton serverSingelton = Instantiate(serverPrefab);
            
            serverSingelton.CreateServer();

            await serverSingelton.GameManager.StartGameServer();
        }
        else
        {
            HostSingelton hostSingelton = Instantiate(hostPrefab);
            hostSingelton.CreateHost();

            ClientSingelton clientSingelton = Instantiate(clientPrefab);
            bool authenticatedClient = await clientSingelton.CreateClient();

            if (authenticatedClient)
            {
                clientSingelton.GameManager.GoToMenu();
            }
            // in this course we didnt implement what were to happen if authentication failed.
        }
    }
}
