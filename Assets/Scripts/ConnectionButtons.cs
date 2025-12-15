using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ConnectionButtons : MonoBehaviour
{

    public void HostServer()
    {
        NetworkManager.Singleton.StartHost(); // Access then Singleton NetworkManager -> Call the StartHost() Method
    }
    public void JoinServer()
    {
        NetworkManager.Singleton.StartClient(); // Access then Singleton NetworkManager -> Call the StartClient() Method
    }

    
}
