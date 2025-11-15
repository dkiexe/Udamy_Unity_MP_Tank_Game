using System.Threading.Tasks;
using UnityEngine;

public class ApplicationController : MonoBehaviour
{
    [SerializeField] private ClientSingelton clientPrefab;
    [SerializeField] private HostSingelton hostPrefab;

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

        }
        else
        {
            ClientSingelton clientSingelton = Instantiate(clientPrefab);
            bool authenticatedClient = await clientSingelton.CreateClient();

            HostSingelton hostSingelton = Instantiate(hostPrefab);
            hostSingelton.CreateHost();

            if (authenticatedClient)
            {
                clientSingelton.GameManager.GoToMenu();
            }
            // in this course we didnt implement what were to happen if authentication failed.
        }
    }
}
