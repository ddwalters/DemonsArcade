using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class roomDoors : MonoBehaviour
{
    public GameObject wall;
    public Collider collide;

    void Start()
    {
        collide = gameObject.GetComponent<Collider>();
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Halldoor"))
        {
            wall.SetActive(false);
        }
    }

}
