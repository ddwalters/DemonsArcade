using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class hallwayWalls : MonoBehaviour
{
    public GameObject wall;
    public Collider collide;

    void Start()
    {
        collide = gameObject.GetComponent<Collider>();
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Hallway"))
        {
            wall.SetActive(false);
        }
    }

}
