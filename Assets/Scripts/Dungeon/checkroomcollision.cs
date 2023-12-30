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
        if (other.gameObject.tag == "Hallway" || other.gameObject.tag == "Room")
        {
            Collide = true;
            return;
        }
        else if (other.gameObject.tag != "Hallway" && other.gameObject.tag != "Room")
        {
            Collide = false;
            return;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
