using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Unity.Mathematics;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class NetworkServer : IDisposable
{
    // Has Server specific logic
    // Stores data of people on the server
    // runs the game logic + decides whats real
    // will create an instance inside our server manager and host manager

    private NetworkManager networkManager;
    private NetworkObject playerPrefab;
    public Action<string> OnClientLeft; // for hostgamemanager to hangle -> Lobby count goes down
    public Action<UserData> OnUserJoined;
    public Action<UserData> OnUserLeft;

    private Dictionary<ulong, string> clientIdToAuthId = new Dictionary<ulong, string>();
    private Dictionary<string, UserData> authIdToUserData = new Dictionary<string, UserData>();

    public NetworkServer(NetworkManager networkManager, NetworkObject playerPrefab) // constructor
    {
        this.networkManager = networkManager;
        this.playerPrefab = playerPrefab;

        // .ConnectionApprovalCallback = Action and it is to approve the connection (we checked this in inspector in networkmanager obj in unity)
        // Whenever someone connects to our server, it will give us info about that connection: ConnectionApprovalRequest (data coming in) and Response (we can set this to say if we approve/no)
        networkManager.ConnectionApprovalCallback += ApprovalCheck;
        networkManager.OnServerStarted += OnNetworkReady; // This callback is invoked when the local server is started and listening for incoming connections.
    }

    // Opens our server and have clients connect
    // Returns bool of whether or not it was successful to starting up the server
    public bool OpenConnection(string ip, int port)
    {
        // Get ref to the UnityTransport on the networkManager in hierarchy
        UnityTransport transport = networkManager.gameObject.GetComponent<UnityTransport>();
        // we are casting to a ushort = unsigned short: ranges from 0 to 65535 (unsigned 16-bit integer)
        // requires ip, port (ushort)
        transport.SetConnectionData(ip, (ushort)port);
        // StartServer() returns true if networkmanager has started in server mode successfully
        return networkManager.StartServer();
    }

    private void OnNetworkReady()
    {
        networkManager.OnClientDisconnectCallback += OnClientDisconnect; // The callback to invoke when a client disconnects. This callback is only ran on the server and on the local client that disconnects.

    }

    private void OnClientDisconnect(ulong clientId) // remove their clientId when they disconnect
    {
        if (clientIdToAuthId.TryGetValue(clientId, out string authId)) // try to get the value from clientId and if successfull, return as authId
        {
            clientIdToAuthId.Remove(clientId);

            // For UserLeft in the servergamemanager
            // Be sure to call this before the Remove(authId)
            OnUserLeft?.Invoke(authIdToUserData[authId]);

            authIdToUserData.Remove(authId);

            // When player leaves, invoke the OnClientLeft action.
            OnClientLeft?.Invoke(authId); // requires auth id so that we can go to the lobby service -> remove person from lobby (auth id is the id needed for interacting with the lobby service)

            
        }
    }



    // When a player joins, you might want to check: Are they banned, right game version?, Did they send a username?, too many players?
    // This method is called automatically by Netcode whenever a new player tries to connect
    // this method should not be async since  It’s a synchronous, immediate yes/no decision. If you need async logic → do it before connection approval.
    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response) // request = info about connection, response = where u tell Netcode approved = true/false
    {
        // We need the data to send too -> hence, the user data (will be a normal class since we are not putting anything in there)
        // request.Payload = player's info (username, chosen character, game version) BUT it is in Byte[]
        string payload = System.Text.Encoding.UTF8.GetString(request.Payload); // Use UT9 decoding to turn Byte[] to string
        UserData userData = JsonUtility.FromJson<UserData>(payload); // know that JSON is kinda like this: {"userName":"Suvijak", "age":20 } -> and u assign this to userData -> it will match the same names of fields and assign the values (no same names -> no error (field left empty))

        // When someone connects, we would store their Ids and UserData
        // When someone leaves the server, we should delete
        clientIdToAuthId[request.ClientNetworkId] = userData.userAuthId; // Assign key and its value 
        authIdToUserData[userData.userAuthId] = userData;

        // For UserJoined in the servergamemanager
        OnUserJoined?.Invoke(userData);

        // Ignore the Task return type, no need to await this
        _ = SpawnPlayerDelayed(request.ClientNetworkId);

        // Whether or not the client was approved (let them finish the connection) 
        response.Approved = true;

        // We already instantiated and spawned the object in SpawnPlayerDelayed
        response.CreatePlayerObject = false; 
    }

    // Handle the spawning itself and add the delay before spawning
    // As soon as you connect to server -> waits one second -> then it spawns your player
    // We do this cuz sometimes there is a race condition where when u connect and spawn --> other ppl might not see you (but when u respawn, the other players can see u now)
    private async Task SpawnPlayerDelayed(ulong clientId)
    {
        await Task.Delay(1000);

        // GameObject. because we are not in monobehaviour
        NetworkObject playerInstance = GameObject.Instantiate(playerPrefab, SpawnPoint.GetRandomSpawnPos(), quaternion.identity);
        playerInstance.SpawnAsPlayerObject(clientId);
    }

    // Get UserData by ulong clientId
    public UserData GetUserDataByClientId(ulong clientId)
    {
        if (clientIdToAuthId.TryGetValue(clientId, out string authId))
        {
            if (authIdToUserData.TryGetValue(authId, out UserData data))
            {
                return data;
            }

            return null;
        }
        Debug.Log("User Data returned null");
        return null;

    }

    public void Dispose() // for unsubbing -> to shutdown cleanly
    {
        if (networkManager == null) { return; }

        networkManager.ConnectionApprovalCallback -= ApprovalCheck;
        networkManager.OnClientDisconnectCallback -= OnClientDisconnect;
        networkManager.OnServerStarted -= OnNetworkReady;

        // Just to be 10000% sure:
        if (networkManager.IsListening) // if it is listening to anything
        {
            networkManager.Shutdown();
        }
    }

    
}
