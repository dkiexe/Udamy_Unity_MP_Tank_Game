using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Core;
using UnityEngine;

public class ServerSingelton : MonoBehaviour
{
    /// <summary>
    ///  This class is attatched to a Server game manager prefab
    ///  and is tasked with being a singleton that persists between scenes
    ///  and represents a Host.
    ///  
    ///  This class also holds a reference to the HostGameManager, and creates an instance of that object.
    /// </summary>
    private static ServerSingelton instance;

    public ServerGameManager GameManager { get; private set; }

    public static ServerSingelton Instance
    {
        get
        {
            if (instance != null) { return instance; }
            instance = FindFirstObjectByType<ServerSingelton>();

            if (instance == null)
            {
                Debug.LogError("No ServerSingelton in the scene...");
                return null;
            }

            return instance;
        }
    }

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    public void CreateServer()
    {
        ApplicationData appdata = new ApplicationData();
        GameManager = new ServerGameManager
            (
                ApplicationData.IP(),
                ApplicationData.Port(),
                NetworkManager.Singleton
            );
    }
    private void OnDestroy()
    {
        GameManager?.Dispose();
    }
}
