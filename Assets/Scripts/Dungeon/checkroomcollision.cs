using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class checkroomcollision : MonoBehaviour
{
    public bool Collide;

    private void OnTriggerStay(Collider other)
    {
        RoomSpawn roomSpawn = transform.GetComponentInParent<RoomSpawn>();

        // if colliding with a room/hallway, relay that info to the spawn node
        if (other.CompareTag("Hallway") || other.CompareTag("Room"))
        {
            Collide = true;
        }
        else
        {
            Collide = false;
        }

        roomSpawn.collideTrue(Collide);
    }
}
