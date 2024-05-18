using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DungeonLoop : MonoBehaviour
{
    public GameObject startPoint;
    public float moveSpeed = 1f;
    public float resetZThreshold = -4f; // Z position threshold for resetting

    private float startZ;

    void Start()
    {
        startPoint = GameObject.Find("start");
        startZ = startPoint.transform.position.z;
    }

    void Update()
    {
        // Move the tile towards the camera using deltaTime to account for frame rate changes
        transform.Translate(Vector3.back * moveSpeed * Time.deltaTime);

        // Check if the tile's z position is less than the reset threshold
        if (transform.position.z <= resetZThreshold)
        { 
            // Reset the tile to the start point using only the z position
            Vector3 newPosition = transform.position;
            newPosition.z = startZ;
            transform.position = newPosition;
        }
    }
}
