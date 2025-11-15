using System.Threading.Tasks;
using UnityEngine;

public class ClientSingelton : MonoBehaviour
{
    private static ClientSingelton instance;

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

    public ClientGameManager GameManager { get; private set; }

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
