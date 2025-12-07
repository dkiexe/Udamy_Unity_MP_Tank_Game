using System.Threading.Tasks;
using UnityEngine;

public class ClientSingelton : MonoBehaviour
{
    /// <summary>
    ///  This class is attatched to a Client game manager prefab
    ///  and is tasked with being a singleton that persists between scenes
    ///  and represents client.
    ///  
    ///  This class also holds a reference to the ClientGameManager, and calls its init method.
    /// </summary>
    private static ClientSingelton instance;

    public ClientGameManager GameManager { get; private set; }

    public static ClientSingelton Instance
    {
        get
        {
            if (instance != null) { return instance; }
            instance = FindFirstObjectByType<ClientSingelton>();

            if (instance == null)
            {
                Debug.LogError("No ClientSingleton in the scene...");
                return null;
            }

            return instance;
        }
    }

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    public async Task<bool> CreateClient()
    {
        GameManager = new ClientGameManager();

        return await GameManager.InitAsync();
    }
}
