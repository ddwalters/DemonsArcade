using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class checkhallcollision : MonoBehaviour
{
    // Start is called before the first frame update
    public bool collide;

    void Start()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        HallwaySpawn hallspawn = transform.GetComponentInParent<HallwaySpawn>();
        //Debug.Log("collision");
        //|| other.gameObject.tag == "Room"
        if (other.gameObject.tag == "Hallway" || other.gameObject.tag == "Room")
        {
            Debug.Log("Hall:collide");
            Destroy(this.gameObject);
            hallspawn.spawnHall();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
