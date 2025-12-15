using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

public class HostSingleton : MonoBehaviour
{
    private static HostSingleton instance; // for other classes to reference

    public HostGameManager GameManager { get; private set; }


    // Creating a C# Property with only a Getter
    // properties let you access it like a variable: ClientSingleton thing = ClientSingleton.Instance;
    // Instead of calling it like a method (GetInstance())
    // Must be Capital Naming for properties
    public static HostSingleton Instance
    {
        get
        {
            if (instance != null) { return instance; }

            // Null -> try getting that obj

            instance = FindObjectOfType<HostSingleton>(); // Kinda Expensive

            // If it's STILL NULL
            if (instance == null)
            {
                Debug.Log("No Host Singleton in the scene");
                return null;
            }

            return instance;

        }
    }
    void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    public void CreateHost(NetworkObject playerPrefab)
    {
        GameManager = new HostGameManager(playerPrefab);
        // await here is NECESSARY because we have to wait for the authentication in this InitAsync method to finish first before going on to the next codes AFTER CreateClient()
        // Without await, the code that comes after CreateClient() might get run before we finish authentication in the InitAsync()
        // Thats why we need to make the InitAsync() be async since we dont really know how long authentication is gonna take
        // await gameManager.InitAsync(); No need for this anymore for host since host is also a client

    }

    private void OnDestroy() // able to call this since this class inherits from monobehaviour
    {
        GameManager?.Dispose();
    }


}
