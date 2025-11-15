using System;
using System.Threading.Tasks;
using Unity.Services.Core;
using UnityEngine.SceneManagement;

public class ClientGameManager
{

    private const string MenuSceneName = "Menu";

    public async Task<bool> InitAsync()
    {
        // initalize unity services, this must be done every time when wanting to use unity services.
        await UnityServices.InitializeAsync();

        // now we do our own anonimus player authentication using UGS ( unity game services )
        Authstate AuthState = await AuthenticationWrapper.DoAuth();

        if (AuthState == Authstate.Authenticated) return true; // returns true if the auth was a succsess
        return false; // returns false if the auth was failed
    }

    public void GoToMenu()
    {
        SceneManager.LoadScene(MenuSceneName);
    }
}
