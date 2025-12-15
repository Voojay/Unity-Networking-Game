using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// this means that if we add this particle script to an obj, if that obj doesnt have a particle system -> it will automatically add it
// we doing this because we may want to have other particles have this script too, not just the trails
[RequireComponent(typeof(ParticleSystem))] 
public class ParticleAligner : MonoBehaviour
{

    private ParticleSystem.MainModule particleSystemMain;
    void Start()
    {
        particleSystemMain = GetComponent<ParticleSystem>().main; // .main is the top-part settings of the particle system component
    }


    void Update()
    {
        // .StartRotation: The initial rotation of particles when the Particle System first spawns the PARTICLES (not system btw).
        // transform.rotation.eulerAngles.z: Gets the object’s rotation angle around the Z-axis in degrees.
        // Why not do rotation.z? because that will be quaternion -> hand to handle
        // BUT since particleSystemMain.startRotation is in radians -> we need to conver the eulerangle (which is deg) to rad by multiple by Mathf.Deg2Rad (a number to multiply to convert to rad) 
        particleSystemMain.startRotation = -transform.rotation.eulerAngles.z * Mathf.Deg2Rad;
    }
}
