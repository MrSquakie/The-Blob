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
    public Rigidbody rb => GetComponentInChildren<Rigidbody>();

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.W))
        {
            rb.AddForce(transform.forward * 1000f);
        }
       
    }
}
