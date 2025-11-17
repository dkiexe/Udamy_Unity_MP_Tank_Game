using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

public static class AuthenticationWrapper
{
    public static Authstate AuthState { get; private set; } = Authstate.NotAuthenticated;


    public static async Task<Authstate> DoAuth(int maxRetries)
    {
        if (AuthState == Authstate.Authenticated) return AuthState;
        
        if (AuthState == Authstate.Authenticating) // in case we call this function agian.
        {
            Debug.LogWarning("Already authenticating! ");
            await Authenticating();
            return AuthState;
        }
        
        await SignInAnonymouslyAsync(maxRetries);
        return AuthState;
    }

    private static async Task SignInAnonymouslyAsync(int maxRetries)
    {
        int reTries = 0;
        AuthState = Authstate.Authenticating;

        while (AuthState == Authstate.Authenticating && reTries < maxRetries)
        {
            try
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

                if (AuthenticationService.Instance.IsSignedIn && AuthenticationService.Instance.IsAuthorized)
                {
                    AuthState = Authstate.Authenticated;
                    break;
                }
            }
            catch (AuthenticationException authException) // Authentication fail server side
            {
                Debug.LogError(authException.Message);
                AuthState = Authstate.Error;
            }
            catch (RequestFailedException requestException) // no internet exception catch
            {
                Debug.LogError(requestException.Message);
                AuthState = Authstate.Error;
            }
            reTries++;
            await Task.Delay(1000);
        }

        if (AuthState != Authstate.Authenticated) // max tries exceeded handeling
        {
            Debug.LogWarning($"Player was not signed in successfully after : [ {reTries} ] retries.");
            AuthState = Authstate.TimeOut;
        }
    }
    private static async Task<Authstate> Authenticating()
    {
        while (AuthState == Authstate.Authenticating || AuthState == Authstate.NotAuthenticated)
        {
            await Task.Delay(200);
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