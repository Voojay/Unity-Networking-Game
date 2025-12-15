
using System.Collections.Generic;
using UnityEngine;


public class SpawnPoint : MonoBehaviour
{

    // Static: They belong to the class itself, not to any specific instance (object) of the class.
    // If you had several instances of SpawnManager in your game scene, each instance would have its own separate list if the list were not static.
    // But you likely want one single list of spawn points for your entire game. So it makes sense to store them in a static list

    // NOTE
    // Since when the host starts to host a game -> The host is spawned in before/during the scene loads, so the host will spawn in the middle
    // This is because the spawnpoints will exist after the scene is finished loading -> The client joins/dies OR host dies then respawns 0> respawn at spawnpoint
    // However, this is just for self-hosted. If with dedicated servers, the scene already exists prior to the host creating a game. So the host will spawn at a random spawnpoint!

    // To make the spawnpoints work, add a response.position in the networkserver script!

    private static List<SpawnPoint> spawnPoints = new List<SpawnPoint>();
    public static Vector3 GetRandomSpawnPos()
    {
        if (spawnPoints.Count == 0)
        {
            Debug.Log("No SpawnPoints!");
            return Vector3.zero;
        }
        return spawnPoints[Random.Range(0, spawnPoints.Count)].transform.position;
    }

    private void OnEnable() // This function is called when the object becomes enabled and active
    {
        spawnPoints.Add(this); // when this SpawnPoint is enabled, add this spawnpoint in the list
    }
    private void OnDisable() // when this spawnpoint is disabled -> Remove from list
    {
        spawnPoints.Remove(this);
    }

    // This method is for the editor: It is to draw a gizmos whenever we select the SpawnPoint Obj: only in scene view, players cant see
    // If you select all, all the gizmos will be shown as well OR even if u select the parent that contains the gameObjects!
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue; // set the gizmos to be blue
        Gizmos.DrawSphere(transform.position, 1); // position of where to draw AND radius of sphere
    }


}
