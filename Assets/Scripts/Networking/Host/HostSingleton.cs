using System.Threading.Tasks;
using UnityEngine;

public class HostSingelton : MonoBehaviour
{
    private static HostSingelton instance;

    public HostGameManager GameManager { get; private set; }

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

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    public void CreateHost()
    {
        GameManager = new HostGameManager();
    }
}
