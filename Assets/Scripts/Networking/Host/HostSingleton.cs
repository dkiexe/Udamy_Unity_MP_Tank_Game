using System.Threading.Tasks;
using UnityEngine;

public class HostSingelton : MonoBehaviour
{
    private static HostSingelton instance;

    public static HostSingelton Instance
    {
        get
        {
            if (instance != null) { return instance; }
            instance = FindFirstObjectByType<HostSingelton>();

            if (instance == null)
            {
                Debug.LogError("No HostSingelton in the scene...");
                return null;
            }

            return instance;
        }
    }

    private HostGameManager gameManager;

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    public void CreateHost()
    {
        gameManager = new HostGameManager();
    }
}
