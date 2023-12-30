using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class checkroomcollision : MonoBehaviour
{
    public bool onStart;
    public bool collide;

    // Start is called before the first frame update
    void Start()
    {
        //onStart = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        RoomSpawn roomSpawn = transform.GetComponentInParent<RoomSpawn>();
        if (onStart == true)
        {
            if (other.gameObject.tag == "Hallway")
            {
                collide = true;
                Debug.Log("Room:collide:hall");
                //roomSpawn.spawnRoom();
                //Destroy(gameObject);
            }
            else if (other.gameObject.tag == "Room")
            {
                collide = true;
                Debug.Log("Room:collide:room");
                //roomSpawn.spawnRoom();
                //Destroy(gameObject);
            }
        }
        onStart = false;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
