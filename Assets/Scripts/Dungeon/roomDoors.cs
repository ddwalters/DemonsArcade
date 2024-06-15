using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class roomDoors : MonoBehaviour
{
    public GameObject wall;
    public GameObject door;
    public GameObject parent;
    public Collider collide;

    LoadLevel loadLevel;
    public bool DungeonComplete = false;

    void Start()
    {
        collide = gameObject.GetComponent<Collider>();
        loadLevel = FindAnyObjectByType<LoadLevel>();
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Halldoor"))
        {
            wall.SetActive(false);
            door.SetActive(true);
        }
    }

    private void Update()
    {
        if (loadLevel.dungeonComplete == true && DungeonComplete != true)
        {
            DungeonComplete = true;
            Destroy(wall.GetComponent<Rigidbody>());
            Destroy(parent.GetComponent<Rigidbody>());
            Destroy(gameObject);
        }
    }

}
