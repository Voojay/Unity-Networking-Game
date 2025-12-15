using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

public static class AuthenticateWrapper 
{
    // Why Static: can call methods like AuthenticateWrapper.Login() without creating an instance.
    // Why not MonoBehaviour: doesn’t need Unity features like Start(), Update(), GameObject, etc.
    // And also, no need to attach it to a GameObject.

    // AuthState Property for getting the authentication state (but cant set)
    public static AuthState AuthState { get; private set; } = AuthState.NotAuthenticated; // this = .... is just the default for AuthState

    public static async Task<AuthState> DoAuth(int maxTries = 5)
    {
        if (AuthState == AuthState.Authenticated) // if already authenticated
        {
            return AuthState;
        }

        if (AuthState == AuthState.Authenticating) // in case it already is authenticating and we dont want this happening at the same time
        {
            Debug.LogWarning("Already Aunthenticating");
            await Authenticating(); // just wait for the process to finish (ignore the return for Task<AuthState>)
            return AuthState; // The AuthState will be changed due to the SignInAnonymousAsync Method
        }

        await SignInAnonymousAsync(maxTries);

        return AuthState;

    }

    private static async Task<AuthState> Authenticating()
    {
        while (AuthState == AuthState.Authenticating || AuthState == AuthState.NotAuthenticated)
        {
            await Task.Delay(200);
        }
        return AuthState;
    }

    private static async Task SignInAnonymousAsync(int maxTries)
    {
        AuthState = AuthState.Authenticating; // set the state to authenticated

        int tries = 0;
        while (AuthState == AuthState.Authenticating && tries < maxTries)
        {
            try
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync(); // no need to enter anything -> will start existing -> For prototyping and monbile games

                if (AuthenticationService.Instance.IsSignedIn && AuthenticationService.Instance.IsAuthorized)
                {
                    AuthState = AuthState.Authenticated;
                    break;
                }

            }
            catch (AuthenticationException ex1) //  Something wrong with login credentials or server logic.
            {
                Debug.Log(ex1);
                AuthState = AuthState.Error;
            }
            catch (RequestFailedException ex2) //  General Unity services/network/server errors (e.g., internet down).
            {
                Debug.Log(ex2);
                AuthState = AuthState.Error;
            }


            tries++;

            await Task.Delay(1000); // should wait a bit after each authentication attempt so that it doesnt immed. happen after (1000 ms)
        }

        if (AuthState != AuthState.Authenticated)
        {
            Debug.LogWarning($"Player was not signed in successfully after {tries} tries");
            AuthState = AuthState.TimeOut;
        }

    }
}


// a custom type that lets you define a set of named constants.
    // Such values must be set at compile time. Cant be changed later in run time.
    // Ex: You can use it like a var: AuthState currentState = AuthState.Authenticating;
    // You can also assign currentState to be a different enum value: currentState = AuthState.Error;
    // CANT DO THIS: AuthState.Authenticated = AuthState.Error; -> changing enum values is not allowed since they are constants
public enum AuthState
{
    NotAuthenticated,
    Authenticating,
    Authenticated,
    Error,
    TimeOut
}
