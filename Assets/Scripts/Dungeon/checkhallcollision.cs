using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class checkhallcollision : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject roomSpawn;
    public bool Collide;

    void Start()
    {
        
    }

    public void OnTriggerStay(Collider other)
    {
        HallwaySpawn hallspawn = transform.GetComponentInParent<HallwaySpawn>();

        if (other.gameObject.tag == "Hallway" || other.gameObject.tag == "Room")
        {
            Collide = true;
            hallspawn.collideTrue(Collide);
            //Debug.Log(Collide);
            return;
        }
        else if (other.gameObject.tag != "Hallway" && other.gameObject.tag != "Room")
        {
            Collide = false;
            hallspawn.collideTrue(Collide);
            return;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
