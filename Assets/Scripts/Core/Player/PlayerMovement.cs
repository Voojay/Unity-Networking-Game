using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] InputReader inputReader; // S.O.
    [SerializeField] Transform bodyTransform;
    [SerializeField] Rigidbody2D rb;
    [SerializeField] ParticleSystem dustCloud;

    [Header("Settings")]
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float turningRate = 270f; // Unit: Angles
    [SerializeField] private float particleEmissionValue = 10;

    private Vector2 previousMovementInput;
    private Vector3 previousPosition;
    private ParticleSystem.EmissionModule emissionModule;
    private const float ParticleStopThreshold = 0.005f; // for turning off dustcloud when we stand still

    private void Awake()
    {
        emissionModule = dustCloud.emission; // set the emissionmodule to be of the dust cloud's
    }

    public override void OnNetworkSpawn() // Start() but for NetworkBehaviour since Start() happens too early (some vars and objs are not synced yet) -> You can check who the owner is, etc.
    {
        if (!IsOwner) { return; } // if this object is not the owner -> don't do anything else after this line

        inputReader.moveEvent += HandleMove; // Subscribe the HandleMove to the moveEvent so that when the moveEvent is invoked with a vector2, that value will be sent to the HandleMove as the parameter value
    }

    public override void OnNetworkDespawn() // Like the OnDestroy() method but that method happens too late (the var and obj had already been destroyed -> cant check who the owner is)
    {
        if (!IsOwner) { return; }
        inputReader.moveEvent -= HandleMove; // Unsubscribe from this event
    }

    void Update()
    {
        if (!IsOwner) { return; }

        // previousmoveInput.x because for rotating we will focus on the 'a' and 'd' keys which are x-coords
        // turningRate is negative because increasing zRotation -> rotate left but decreasing -> rotate right
        float zRotation = previousMovementInput.x * -turningRate * Time.deltaTime;
        bodyTransform.Rotate(0f, 0f, zRotation);
    }

    void FixedUpdate() // Use for physics-related logic: it runs at fixed intervals and runs at every frame of the PHYSICS engine, not every frame unity engine. Mainly used for RigidBody or other physics
    {
        // Before we overwrite our previousposition with our current position -> should know how far we have moved (get the magnitude):
        // Using sqrmagnitude instead of the normal magnitude cuz sqrts are expensive
        // Concept: stand still -> no dust cloud. But sometimes, there are somethings here and there that dont make the sqrmagnitude of standing still be zero. Do 0.005f instead
        if ((transform.position - previousPosition).sqrMagnitude > ParticleStopThreshold) // we are moving
        {
            emissionModule.rateOverTime = particleEmissionValue;
        }
        else // aint moving -> turn off dust cloud
        {
            emissionModule.rateOverTime = 0;
        }
        // We need to update the previousPos for ALL the players. Not just for yourself only
        previousPosition = transform.position;
        if (!IsOwner) { return; }

        // transform.up is the local y-axis
        // To emphasize, local means that the object will move up based on its current ROTATION!
        rb.velocity = bodyTransform.up * previousMovementInput.y * moveSpeed;
    }

    void HandleMove(Vector2 movementInput)
    {
        previousMovementInput = movementInput;
    }
}
