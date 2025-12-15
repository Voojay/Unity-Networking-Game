using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HostGameManager : IDisposable
{

    // Can leave this blank since the host is both server + client -> already doing this in the client side
    //  anything the client already handles doesn't need to be repeated in HostGameManager.
    // the host reuses the client logic, and HostGameManager can stay empty unless there's host-specific logic needed
    // This happens when you start the game in host mode using: NetworkManager.Singleton.StartHost();
    // So, the client-side scripts (e.g. ClientGameManager) automatically run on the host, too.

    private const int MaxConnections = 20; // max connections in a relay AND for const -> use capital var names
    private Allocation allocation; // an allocation to a Relay server.
    private string lobbyId;
    private bool isPrivate;
    private NetworkObject playerPrefab;
    public string JoinCode { get; private set; } // so that the GameHUD can read this
    public NetworkServer NetworkServer { get; private set; } // since we are making it public, should become a property

    private const string GameSceneName = "Game";

    // Constructor: for getting the playerprefab
    public HostGameManager(NetworkObject playerPrefab)
    {
        this.playerPrefab = playerPrefab;
    }

    // Three Main Steps: 
    // 1) Get allocation from the relay service and joincode from that allocation
    // 2) Set Relay Service Data in transport with that allocation
    // 3) Create Lobby: Name of Lobby will be the player who created it
    // 4) Create a networkserver obj
    // 5) Create a UserData with their userName from playerPrefs and AuthId from the authenticationservice
    // 6) Set the ConnectionData in NetworkManager's Networkconfigurations by assigning the payload (byte[]) 
    // 7) After all of that, we StartHost() which is a method of the NetworkManager
    // 8) Once we started the host, now we want to sub to the OnClientLeft action in NetworkServer --> Then load the game scene
    public async Task StartHostAsync(bool privateToggleIsOn)
    {

        // Get an allocation to a relay server
        try
        {
            allocation = await RelayService.Instance.CreateAllocationAsync(MaxConnections); // Creates an allocation on an available relay server that can hold MaxConnections number of clients.
        }
        catch (Exception e)
        {
            Debug.Log(e);
            return;
        }

        // Now for the join code
        try
        {
            JoinCode = await Relay.Instance.GetJoinCodeAsync(allocation.AllocationId); // gets the join code (parameter is the allocation.AllocationId and returns a string of the join code)
            Debug.Log("The join code is: " + JoinCode);
        }
        catch (Exception e)
        {
            Debug.Log(e);
            return;
        }

        // UnityTransport = network engine behind the scenes ( handles sending/receiving data between players)
        // gets the only instance of the NetworkManager in your scene
        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        // creating a RelayServerData object that tells Unity how to talk to Unity Relay servers.
        // allocation = contains info about the Relay server you reserved earlier (like IP, port, encryption keys, join code).
        RelayServerData relayServerData = new RelayServerData(allocation, "dtls"); // the protocol used for sending data. udp is fast and common for real-time games (like FPS, platformers, etc).
        transport.SetRelayServerData(relayServerData); // tell the UnityTransport System: Use this Relay server setup to send/receive data for this game session.

        // You are starting the multiplayer session, and you become the host.
        // is both a server and a player.

        // Step 3: Try Creating Lobbies
        try
        {
            // Create CreateLobbyOptions obj
            CreateLobbyOptions lobbyOptions = new CreateLobbyOptions();

            // Customize lobbyOptions
            lobbyOptions.IsPrivate = privateToggleIsOn; // Private or Public Lobbies
            lobbyOptions.Data = new Dictionary<string, DataObject>() // be able to assign vars to a lobby and that other ppl in the lobby can read
            { // all the elements of this dict

                { //1st element of this dict
                    "JoinCode", new DataObject( //"JoinCode" = key of element of dict, DataObject() = value of element of dict
                        visibility: DataObject.VisibilityOptions.Member, // if they are a member of the lobby, they are allowed to read it 
                        value: JoinCode
                    )
                }
            };

            // Create a lobby whose name is the person who created it
            string playerName = PlayerPrefs.GetString(NameSelector.PlayerNameKey, "Missing Name"); // for naming the lobby
            Lobby lobby = await Lobbies.Instance.CreateLobbyAsync($"{playerName}'s Lobby", MaxConnections, lobbyOptions); // Method for creating a lobby
            lobbyId = lobby.Id; // this is for the :heartbeat of the lobby" so that UGS does not delist our lobby after a certain time. (keep the lobby active) -> Use a coroutine

            // SInce coroutine can only be used with classes that inherit from monobehaviour, but this class does not inherit from anything, we can start the couroutine in HostSingleton since it herits from monobehaviour
            HostSingleton.Instance.StartCoroutine(HeartbeatLobby(15)); // 15 seconds is standard according to UGS documentation
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
            return;
        }

        // Step 4: Create a NetworkServer object --> deals with network manager mostly
        // Before we StartHost(): Create a NetworkServer (we created this)
        // once we are gonna start hosting ->  it will hook up the approvalcheck for approving and receiving data from each connection
        NetworkServer = new NetworkServer(NetworkManager.Singleton, playerPrefab);

        // Step 5: Create a UserData with their userName from playerPrefs and AuthId from the authenticationservice
        UserData userData = new UserData // UserData is a serializable class
        {
            userName = PlayerPrefs.GetString(NameSelector.PlayerNameKey, "Missing Name"), // missing name if there is none in PlayerNameKey
            userAuthId = AuthenticationService.Instance.PlayerId
        };


        // Step 6: Set the ConnectionData in NetworkManager's Networkconfigurations by assigning the payload (byte[]) 
        string payload = JsonUtility.ToJson(userData); // turns userData into a JSON string (Ex: {"userName":"Suvijak"})
        byte[] payloadBytes = Encoding.UTF8.GetBytes(payload); // Your JSON is text -> but Netcode wants a byte array

        // When the server calles ApprovalCheck, it receves your payload (request.Payload)
        // The server can then: read bytes -> turn them back to JSON string, convert JSON -> UserData -> check your username
        NetworkManager.Singleton.NetworkConfig.ConnectionData = payloadBytes; // When i connect -> send these bytes along with my connection request

        // Step 7: After all of that, we StartHost() which is a method of the NetworkManager
        NetworkManager.Singleton.StartHost();

        // Step 8: Once we started the host, now we want to sub to the OnClientLeft action in NetworkServer --> Then load the game scene
        NetworkServer.OnClientLeft += HandleClientLeft;

        NetworkManager.Singleton.SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single); // LoadSceneMode.Single): Closes all current loaded Scenes and loads a Scene.

    }



    private IEnumerator HeartbeatLobby(float waitTimeSeconds)
    {
        WaitForSecondsRealtime delay = new WaitForSecondsRealtime(waitTimeSeconds); // wait every 15 seconds 
        while (true)
        {
            Lobbies.Instance.SendHeartbeatPingAsync(lobbyId); // send the ping for UGS to keep our lobby active
            yield return delay;
        }
    }

    public void Dispose() // is called when the server shuts down unexpectedly
    {
        Shutdown();
    }

    public async void Shutdown()
    {
        if (string.IsNullOrEmpty(lobbyId)) { return; } // if we dont have a lobbyId (means it has been shutdown already)-> exit 

        // Current State: we do have a lobbyID

        // Stop the Coroutine for heartbeat lobby
        // nameof(HeartbeatLobby) produces a string also but it's better to use this in case you change the HeartbeatLobby name
        HostSingleton.Instance.StopCoroutine(nameof(HeartbeatLobby));
        try
        {
            await Lobbies.Instance.DeleteLobbyAsync(lobbyId); // wait for deleting the lobby
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }

        lobbyId = string.Empty; // Set lobbyid to empty string


        NetworkServer.OnClientLeft -= HandleClientLeft;

        // Call Dispose for network server
        NetworkServer?.Dispose();
    }

    private async void HandleClientLeft(string authId)
    {
        try
        {
            // want to remove player from lobby -> uses lobby id and player id which is the auth id
            await LobbyService.Instance.RemovePlayerAsync(lobbyId, authId);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }
}
