using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class NameSelector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_InputField nameField;
    [SerializeField] private Button connectButton;

    [Header("Settings")]
    [SerializeField] private int minNameLength = 1;
    [SerializeField] private int maxNameLength = 20;

    public const string PLAYERNAMEKEY = "PlayerName";

    private void Start()
    {
        // this if block gives a dedicated server the ability to skip the name selection screen
        if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) 
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
            return;
        }
         
        nameField.text = PlayerPrefs.GetString(PLAYERNAMEKEY, string.Empty);
        HandleNameChanged();
    }
    public void HandleNameChanged() 
    {
        connectButton.interactable =
            nameField.text.Length >= minNameLength &&
            nameField.text.Length <= maxNameLength;
    }

    public void Connect()
    {
        PlayerPrefs.SetString(PLAYERNAMEKEY, nameField.text);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }
}
