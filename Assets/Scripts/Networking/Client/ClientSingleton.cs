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

    private ClientGameManager gameManager;

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    public async Task CreateClient()
    {
        gameManager = new ClientGameManager();

        await gameManager.InitAsync();
    }
}
