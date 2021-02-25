using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerMove : MonoBehaviour
{
    public float speed = 10.0f;

    void Update()
    {
        // Get the horizontal and vertical axis.
        // By default they are mapped to the arrow keys.
        // The value is in the range -1 to 1
        float ztranslation = Input.GetAxis("Vertical") * speed;
        float xtranslation = Input.GetAxis("Horizontal") * speed;

        // Make it move 10 meters per second instead of 10 meters per frame...
        ztranslation *= Time.deltaTime;
        xtranslation *= Time.deltaTime;

        // Move translation along the object's z-axis
        transform.Translate(xtranslation, 0, ztranslation);

        // Rotate around our y-axis
        // transform.Rotate(0, xtranslation, 0);
    }
}
