using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ClientGameManager : IDisposable // an interface that has a Dispose Method (we need this cuz normal classes dont have OnDestroy())
{

    private const string menuSceneName = "Menu";

    private JoinAllocation joinAllocation;

    private NetworkClient networkClient;
    private MatchplayMatchmaker matchmaker; // for matchmaking, MatchplayMatchmaker is one of the imported scripts
    public UserData UserData { get; private set; } // maket this a local var to use for payload part and the matchmaking part

    // no need to inherit Monobehaviour if we dont need Start(), Update(), etc. OR it's just a pure C# script for logic, data handling, metworking.
    // Only classes that interact with the Unity GameObject lifecycle need to inherit from MonoBehaviour.

    // Async Methods: Call this method -> Lasts for an unknown amount of time -> won't freeze your game, it'll just go off and do it -> Come back once it is done
    public async Task<bool> InitAsync() // Task<bool> is the return type (basically returns a bool) + Good Practice to have Async at the end of the Method name
    {
        // For Init. ClientGameManager and for authenticating players
        // Before any authentication even happens, you must initialize unityservices first

        await UnityServices.InitializeAsync();

        // Initialize NetworkClient
        networkClient = new NetworkClient(NetworkManager.Singleton);
        // Initialize the MatchplayMatchMaker
        matchmaker = new MatchplayMatchmaker();

        // this returns Task<AuthState> + gives us access to the AuthId
        AuthState authState = await AuthenticateWrapper.DoAuth();

        if (authState == AuthState.Authenticated)
        {
            // Once we have authenticated --> setUserData
            // Set the user's name and authID
            // Note: UserData is a serializable class
            UserData = new UserData 
            {
                userName = PlayerPrefs.GetString(NameSelector.PlayerNameKey, "Missing Name"), // missing name if there is none in PlayerNameKey
                userAuthId = AuthenticationService.Instance.PlayerId
            };
            return true;
        }

        return false;
    }

    public void GoToMenu()
    {
        SceneManager.LoadScene(menuSceneName);
    }

    // This method will start client for dedicated servers, not relays (this is for self-hosted servers)
    // For both ded.servers and self hosted, we will call ConnectClient() since both of these require setting payload and connecting to server
    // not async since we already have the ip and port for this and we just want to start up the client
    private void StartClient(string ip, int port)
    {
        // Get ref to UnityTransport component on NetworkManager
        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        // Now, we set the connection Data with the ip and port
        transport.SetConnectionData(ip, (ushort)port);

        // Note that the two lines above are the equivalent to the try catch and setrelayserverdata in the StartClientAsync method. which is for self hosted

        ConnectClient();

    }

    // this is a method to connect to a server FOR RELAYS (meaning that is for self hosted servers)
    // For dedicated server -> we connect via ip and port
    public async Task StartClientAsync(string joinCode)
    {
        // From here onwards is the code for relays
        try
        {
            joinAllocation = await Relay.Instance.JoinAllocationAsync(joinCode);
        }
        catch (Exception e)
        {
            Debug.Log(e);
            return;
        }

        UnityTransport unityTransport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        RelayServerData relayServerData = new RelayServerData(joinAllocation, "dtls");
        unityTransport.SetRelayServerData(relayServerData);

        ConnectClient(); // For connecting to the server + set payload
    }

    private void ConnectClient()
    {
        // From here onwards is the general code to actually connect to the server and set payload data
    
        string payload = JsonUtility.ToJson(UserData); // turns userData into a JSON string (Ex: {"userName":"Suvijak"})
        byte[] payloadBytes = Encoding.UTF8.GetBytes(payload); // Your JSON is text -> but Netcode wants a byte array

        // When the server calles ApprovalCheck, it receves your payload (request.Payload)
        // The server can then: read bytes -> turn them back to JSON string, convert JSON -> UserData -> check your username
        NetworkManager.Singleton.NetworkConfig.ConnectionData = payloadBytes; // When i connect -> send these bytes along with my connection request


        NetworkManager.Singleton.StartClient(); // method in networkmanager
    }

    // This method will be called in our UI
    // This method is void because we are not going to await this method
    // But, we can call other async methods 
    // Whoever is calling this isn't going to wait for this to be complete before continuing with their code
    // We're just going to hit the find match button -> start matchmaking -> in the meantime u can go do other things
    // Also, this method will have an event passed in
    // Benefits to this: as soon as the match is made, we can trigger the event that's been passed in and let whoever calls it know what matchmaking is finished
    // This method will be called in the MainMenu script
    // NOTE: onMatchmakeResponse is a delegate reference passed in — it could be just one method (OnMatchMade), or several methods combined before being passed.
    public async void MatchmakeAsync(bool isTeamQueue, Action<MatchmakerPollingResult> onMatchmakeResponse)
    {
        if (matchmaker.IsMatchmaking) { return; } // if already matchmaking -> exit

        UserData.userGamePreferences.gameQueue = isTeamQueue ? GameQueue.Team : GameQueue.Solo; // is isTeamQueue = true -> set the gameQueue in preferences to be the Team enum value
        MatchmakerPollingResult matchResult = await GetMatchAsync(); // returns the result of the matchmaking whether it was successful (we got our match) or not
        onMatchmakeResponse?.Invoke(matchResult); // so whoever called this MatchmakeAsync method, the main menu can get back this enum and then display the success or error code on UI
    }
    
    // This method is called in MatchmakeAsync
    // return success if we found a match
    private async Task<MatchmakerPollingResult> GetMatchAsync()
    {
        
        MatchmakingResult matchmakingResult = await matchmaker.Matchmake(UserData); // this method came imported with the matchmaker class + takes in userData for finding match according to our preferences

        if (matchmakingResult.result == MatchmakerPollingResult.Success) // a class var == an enum value both of which are MatchmakerPollingResult
        {
            // Connect to server
            if (matchmakingResult.result == MatchmakerPollingResult.Success)
            {
                StartClient(matchmakingResult.ip, matchmakingResult.port);
            }
        }
        return matchmakingResult.result;
    }

    public async Task CancelMatchmaking() // This method is called in MainMenu in FindMatchPressed() method
    {
        await matchmaker.CancelMatchmaking(); // One of the methods from the imported scripts
    }

    public void Disconnect()
    {
        networkClient.Disconnect();
    }

    

    public void Dispose() // do any clean up for this class when it is called 
    {
        networkClient?.Dispose(); // make sure it is not null -> call Dispose method inside network client -> unsubs from networkManager.OnClientDisconnectCallback
    }

    
}
