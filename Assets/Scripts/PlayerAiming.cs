using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerAiming : NetworkBehaviour
{
    [SerializeField] InputReader inputReader; // to get ref. to the aimPosition
    [SerializeField] Transform turretTransform; // to control the rotation of the turret head

    private void LateUpdate() // updates after the usual Update(); use for following mechanisms or applying logic that is dependent on Update()
    {
        if (!IsOwner) { return; }

        Vector2 aimScreenPosition;
        aimScreenPosition = inputReader.aimPosition;

        Vector3 aimWorldPosition;
        aimWorldPosition = Camera.main.ScreenToWorldPoint(aimScreenPosition); // Convert the coords of the computer screen (mouse position in pixels) to the world point in tbe unity world (actual position in the game world). So, it returns a Vector3

        turretTransform.up = new Vector2( // transform.up is a direction (unit vector) while .position is a location in world space
            aimWorldPosition.x - turretTransform.position.x,
            aimWorldPosition.y - turretTransform.position.y);

    }

}
