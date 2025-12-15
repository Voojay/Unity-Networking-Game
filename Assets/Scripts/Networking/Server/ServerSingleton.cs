using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Core;
using UnityEngine;

public class ServerSingleton : MonoBehaviour
{
    private static ServerSingleton instance; // for other classes to reference

    public ServerGameManager GameManager { get; private set; }


    // Creating a C# Property with only a Getter
    // properties let you access it like a variable: ClientSingleton thing = ClientSingleton.Instance;
    // Instead of calling it like a method (GetInstance())
    // Must be Capital Naming for properties
    public static ServerSingleton Instance
    {
        get
        {
            if (instance != null) { return instance; }

            // Null -> try getting that obj

            instance = FindObjectOfType<ServerSingleton>(); // Kinda Expensive

            // If it's STILL NULL
            if (instance == null)
            {
                Debug.Log("No Servver Singleton in the scene");
                return null;
            }

            return instance;

        }
    }
    void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    public async Task CreateServer(NetworkObject playerPrefab) 
    {
        // Brief Overview: Since we are interacting with UGS Services, we need to make sure that we have initialized them.
        // We have done this in the past where we do authentication for the player
        // -> BUT the server doesn't go thru authentication like the player

        // We initialize the unity services
        await UnityServices.InitializeAsync();

        // Instantiate the ServerGameManager by passing the following data gotten from the ApplicationData class (one of the imported scripts)
        GameManager = new ServerGameManager(
            ApplicationData.IP(),
            ApplicationData.Port(),
            ApplicationData.QPort(),
            NetworkManager.Singleton,
            playerPrefab
        );
        

    }

    private void OnDestroy() // able to call this since this class inherits from monobehaviour
    {
        GameManager?.Dispose();
    }


}
