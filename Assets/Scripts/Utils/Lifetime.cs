using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lifetime : MonoBehaviour
{
    [SerializeField] float lifeTime = 2f;

    void Start()
    {
        Destroy(gameObject, lifeTime); // 2nd parameter: time before destroyin
    }

}
