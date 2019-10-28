// Script:      PlayerController.cs
// Description: A script to handle the player's movement.
// Author(s):   Tyler Cole
// Date:        10/27/2019

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public string horizontalAxis = "Horizontal", verticalAxis = "Vertical";
    public float speed = 2.0f;
    public Transform tiltTransform;


    // Update is called once per frame
    void Update()
    {
        GetComponent<Rigidbody>().AddForce(tiltTransform.right * Input.GetAxis(horizontalAxis) * speed);
        GetComponent<Rigidbody>().AddForce(tiltTransform.forward * Input.GetAxis(verticalAxis) * speed);
    }
}
