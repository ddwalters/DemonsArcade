using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMotion : MonoBehaviour
{
    public float rotationSpeed = 1.0f;

    void Update()
    {
        // Rotate the camera in the y-axis (upward) at a constant speed
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }
}
