using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class doorwayWalls : MonoBehaviour
{
    public GameObject wall;
    public GameObject door;
    public Collider collider;

    void Start()
    {
        collider = gameObject.GetComponent<Collider>();
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Hallway"))
        {
            wall.SetActive(false);
        }
        else if (other.CompareTag("Doorway"))
        {
            wall.SetActive(false);
            door.SetActive(true);
        }
    }
}
