using UnityEngine;

public class HostSingelton : MonoBehaviour
{
    /// <summary>
    ///  This class is attatched to a Host game manager prefab
    ///  and is tasked with being a singleton that persists between scenes
    ///  and represents a Host.
    ///  
    ///  This class also holds a reference to the HostGameManager, and creates an instance of that object.
    /// </summary>
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
