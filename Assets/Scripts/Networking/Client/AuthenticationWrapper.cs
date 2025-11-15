using System.Threading.Tasks;
using Unity.Services.Authentication;
using UnityEngine;

public static class AuthenticationWrapper
{
    public static Authstate AuthState { get; private set; } = Authstate.NotAuthenticated;


    public static async Task<Authstate> DoAuth(int maxtries = 5)
    {
        if (AuthState == Authstate.Authenticated) return AuthState;
        int tries = 0;

        AuthState = Authstate.Authenticating;

        while (AuthState == Authstate.Authenticating && tries < maxtries)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

            if (AuthenticationService.Instance.IsSignedIn && AuthenticationService.Instance.IsAuthorized)
            {
                AuthState = Authstate.Authenticated;
                break;
            }
            tries++;
            await Task.Delay(1000);
        }

        return AuthState;
    }
}

public enum Authstate
{
    NotAuthenticated,
    Authenticating,
    Authenticated,
    Error,
    TimeOut,
}