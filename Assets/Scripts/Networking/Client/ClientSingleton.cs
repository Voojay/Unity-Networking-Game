using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class ClientSingleton : MonoBehaviour
{
    private static ClientSingleton instance; // for other classes to reference

    public ClientGameManager GameManager { get; private set; }


    // Creating a C# Property with only a Getter 
    // properties let you access it like a variable: ClientSingleton thing = ClientSingleton.Instance;
    // Instead of calling it like a method (GetInstance())
    // Must be Capital Naming for properties
    public static ClientSingleton Instance
    {
        get
        {
            if (instance != null) { return instance; }

            // Null -> try getting that obj

            instance = FindObjectOfType<ClientSingleton>(); // Kinda Expensive

            // If it's STILL NULL
            if (instance == null)
            {
                Debug.Log("No Client Singleton in the scene");
                return null;
            }

            return instance;

        }
    }
    void Start() // runs when the ClientSIngleton is instantiated since it's not in the scene
    {
        DontDestroyOnLoad(gameObject);
    }

    public async Task<bool> CreateClient()
    {
        GameManager = new ClientGameManager();
        // await here is NECESSARY because we have to wait for the authentication in this InitAsync method to finish first before going on to the next codes AFTER CreateClient()
        // Without await, the code that comes after CreateClient() might get run before we finish authentication in the InitAsync()
        // Thats why we need to make the InitAsync() be async since we dont really know how long authentication is gonna take
        return await GameManager.InitAsync();  // InitAsync() returns a Task<bool>

    }

    private void OnDestroy() // does not need Dispose() from IDisposable interface since it inherits from Monobehaviour
    {
        GameManager?.Dispose(); // The chain is ClientSIngleton gets destroyed -> ClientGameManger -> calls dispose ->  network client calls dispose -> unsubs method
    }


}
