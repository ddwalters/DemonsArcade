using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class roomDoors : MonoBehaviour
{
    public GameObject wall;
    public Collider collider;

    void Start()
    {
        collider = gameObject.GetComponent<Collider>();
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Halldoor"))
        {
            wall.SetActive(false);
        }
    }

}
