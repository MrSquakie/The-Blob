// Script:      CameraController.cs
// Description: A script to handle the camera's movement including going into objects.
// Author(s):   Tyler Cole
// Date:        10/27/2019

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public LayerMask collisionMask; // the layer where all the enviornment is in
    public Transform playerHitbox;
    public float collisionCushion = 0.35f, maxCameraOffset = 5f, currentTilt = 10f;
    private float cameraDistance, adjustedDistance;

    public Transform tilt;
    Ray camRay;
    RaycastHit camRayHit;

    private void Start()
    {
        adjustedDistance = -maxCameraOffset;
        transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, adjustedDistance);
    }

    // Update is called once per frame
    void Update()
    {
        tilt.position = playerHitbox.position;
        tilt.eulerAngles = new Vector3(currentTilt, Input.mousePosition.x, tilt.eulerAngles.z);
        cameraDistance = Mathf.Abs(transform.localPosition.z);

        CameraRayCollisions();
        MouseScroll();

        transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, adjustedDistance);
    }

    void MouseScroll()
    {
        if (adjustedDistance > 0f)
        {
            adjustedDistance = 0f;
        }
        else if(adjustedDistance < -maxCameraOffset)
        {
            adjustedDistance = -maxCameraOffset;
        }
        else
        {
            adjustedDistance += -(Input.mouseScrollDelta.y) * -0.1f;
        }
    }

    void CameraRayCollisions()
    {
        float camDistance = cameraDistance + collisionCushion;
        camRay.origin = playerHitbox.position;
        camRay.direction = (playerHitbox.position+transform.position);

        if(Physics.Raycast(camRay, out camRayHit,camDistance,collisionMask))
        {
            adjustedDistance = -(Vector3.Distance(camRay.origin,camRayHit.point) - collisionCushion);
        }
        else
        {
            Debug.DrawRay(camRay.origin, camRay.direction, Color.red);
            adjustedDistance = -cameraDistance;
        }
    }
}
