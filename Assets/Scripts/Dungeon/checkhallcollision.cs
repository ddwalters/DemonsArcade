using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class checkhallcollision : MonoBehaviour
{
    public bool Collide;

    private void OnTriggerStay(Collider other)
    {
        HallwaySpawn hallSpawn = transform.GetComponentInParent<HallwaySpawn>();

        // if colliding with a room/hallway, relay that info to the spawn node
        if (other.CompareTag("Hallway") || other.CompareTag("Room"))
        {
            Collide = true;
        }
        else
        {
            Collide = false;
        }

        hallSpawn.collideTrue(Collide);
    }
}
