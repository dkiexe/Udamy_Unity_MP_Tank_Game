using System.Collections;
using Unity.Netcode;
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

    private float serverTimeoutTime = 60f; // server should shutdown if there are no players connected for 60 seconds.

    private Coroutine serverShutDownCorutine;

    public static ServerSingelton Instance
    {
        get
        {
            if (instance != null) { return instance; }
            instance = FindFirstObjectByType<ServerSingelton>();

            if (instance == null)
            {
                return null;
            }

            return instance;
        }
    }

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (GameManager.networkServer.IsServerEmpty)
        {
            if (serverShutDownCorutine == null)
            {
                serverShutDownCorutine = StartCoroutine(TimedShutDown());
            }
        }
        else
        {
            if (serverShutDownCorutine != null)
            {
                StopCoroutine(serverShutDownCorutine);
                serverShutDownCorutine = null;
            }
        }
    }

    private IEnumerator TimedShutDown()
    {
        yield return new WaitForSeconds(serverTimeoutTime);
        GameManager.StopGameServer("IDLE");
    }

    public void CreateServer()
    {
        ApplicationData appdata = new ApplicationData();
        GameManager = new ServerGameManager
            (
                ApplicationData.IP(),
                ApplicationData.Port(),
                ApplicationData.ID(),
                NetworkManager.Singleton
            );
    }
    private void OnDestroy()
    {
        GameManager?.Dispose();
    }
}
