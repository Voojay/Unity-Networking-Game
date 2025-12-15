using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ApplicationController : MonoBehaviour
{
    [SerializeField] private ClientSingleton clientPrefab;
    [SerializeField] private HostSingleton hostPrefab;
    [SerializeField] private ServerSingleton serverPrefab;
    [SerializeField] private NetworkObject playerPrefab;
    private ApplicationData applicationData;
    private const string GameSceneName = "Game";

    private async void Start() // await ... inside a method -> this method must be async, hence the async for Start()
    {
        DontDestroyOnLoad(gameObject);

        // SystemInfo.graphicsDeviceType returns the type of graphics backend Unity is using (like Direct3D11, Vulkan, or Null).
        // GraphicsDeviceType.Null means there’s no GPU/graphics backend (headless mode).
        // This line is checking: "Are we running without a graphics device?" 
        // If graphics = Null ➜ true ➜ run in server mode (has dedicated server -> for UGS )
        // Else ➜ false ➜ run in client/game mode (not in headless server mode — you're running on a device with a GPU (like a client or a host).)
        // So that instance is either a Client or a Host (Client + server combined) BUT not a dedicated server
        await LaunchInMode(SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null); // Must be await since it is also an async method
    }

    private async Task LaunchInMode(bool isDedicatedServer)
    {
        if (isDedicatedServer)
        {
            // We need to cap the fps of the server itself too. Or else, if we don't -> fps will be uncapped -> server uses too much processing power to get the highes fps --> could lead to crashes
            // Specifies the target frame rate at which Unity tries to render your game.
            Application.targetFrameRate = 60;
            // Instantiate the application data
            applicationData = new ApplicationData();

            // spawn in the server prefab and sign this to the ServerSingleton variable
            ServerSingleton serverSingleton = Instantiate(serverPrefab);

            // In unity, you can load a scene asynchronously
            // So, you can wait for a scene to finish loading, then run some other code
            // The issue is, you have to do it in a coroutine method, u cant do it in an async method
            StartCoroutine(LoadGameSceneAsync(serverSingleton));
        }
        else // Not dedicated server -> so we want to run in client/game mode
        {

            HostSingleton hostSingleton = Instantiate(hostPrefab);
            hostSingleton.CreateHost(playerPrefab); // this creates the ClientGameManager


            ClientSingleton clientSingleton = Instantiate(clientPrefab);
            bool authenticated = await clientSingleton.CreateClient(); // this creates the ClientGameManager + this method returns a Task<bool>


            // Once the awaiting for authentication is done and authenticated == true -> Go to Main Menu
            if (authenticated)
            {
                clientSingleton.GameManager.GoToMenu();
            }

        }
    }

    // The reason we are doing this is because we want the server will start in the gameplay scene before anyone can connect
    // This fixes a scene-loading race condition by making sure the server is already in the correct scene before any clients are allowed to connect.
    // Such race condition is: The client might be halfway through loading or syncing one scene...
    // But the server switches to another scene mid-process.
    // Thus the fix helps solve that the server now is in the correct scene and theres no mid connection scene switching
    private IEnumerator LoadGameSceneAsync(ServerSingleton serverSingleton)
    {
        // we are now loading the scene asyncly
        // Wait for this game scene to finsih loading first
        AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(GameSceneName);

        while (!asyncOperation.isDone) // if the async opertaion is not done
        {
            yield return null; // check next frame for this while loop
        }

        // At this point, the scene has been loaded

        // Call the CreateServer() async method 
        // This method tries to connect to UGS Services
        Task createServerTask = serverSingleton.CreateServer(playerPrefab);

        // Wait Until takes in a function (we will use lambda) that returns a bool (true = wait completed)
        // So this means wait until the createservertask is completed
        yield return new WaitUntil(() => createServerTask.IsCompleted);

        // So far, the game is not actually running as a server. Thus, people can not connect to it.
        // This method is a method we created ourselves:
        Task startServerTask = serverSingleton.GameManager.StartGameServerAsync();
        yield return new WaitUntil(() => startServerTask.IsCompleted);
    }
}
