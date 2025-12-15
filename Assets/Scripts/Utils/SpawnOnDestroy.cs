using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// For the dustcloud of the bullet
public class SpawnOnDestroy : MonoBehaviour
{
    [SerializeField] private GameObject prefab; // dustcloud

    private void OnDestroy()
    {
        // If not: the scene that THIS GAMEOBJECT is in is loaded (isloaded returns true after loading has completed and objects have been enabled)
        if (!gameObject.scene.isLoaded)
        {
            return; // so that when we press leave (the scene will start changing to mainmenu) -> the dustcloud wont appear when the bullet is destroyed (happens when we leave the game) --> just in case u know?
        }
        Instantiate(prefab, transform.position, Quaternion.identity);
    }
}
