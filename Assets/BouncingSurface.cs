using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BouncingSurface : MonoBehaviour
{
    public float strength = 1.0f;
    
    private void OnTriggerStay(Collider other)
    {
        var go = other.gameObject;
        Debug.Log("Collided");
        if (other.gameObject.layer == LayerMask.NameToLayer("Sphere")) {
            go.GetComponent<Rigidbody>().velocity += Vector3.up * strength;
            Debug.Log("bouncing ball: " + go);
        }
    }
}
