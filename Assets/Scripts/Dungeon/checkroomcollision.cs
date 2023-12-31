using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class checkroomcollision : MonoBehaviour
{
    public bool Collide;
    // Start is called before the first frame update
    void Start()
    {

    }

    public void OnTriggerStay(Collider other)
    {
        RoomSpawn roomspawn = transform.GetComponentInParent<RoomSpawn>();

        if (other.gameObject.tag == "Hallway" || other.gameObject.tag == "Room")
        {
            Collide = true;
            roomspawn.collideTrue(Collide);
            //Debug.Log(Collide);
            return;
        }
        else if (other.gameObject.tag != "Hallway" && other.gameObject.tag != "Room")
        {
            Collide = false;
            roomspawn.collideTrue(Collide);
            return;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
