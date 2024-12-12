using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class doorwayWalls : MonoBehaviour
{
    public GameObject wall;

    public Collider collide;

    LoadLevel loadLevel;
    public bool DungeonComplete = false;

    void Start()
    {
        loadLevel = FindAnyObjectByType<LoadLevel>();
        collide = gameObject.GetComponent<Collider>();
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
            
        }
    }

    private void Update()
    {
        if (loadLevel != null)
        {
            if (loadLevel.dungeonComplete == true && DungeonComplete != true)
            {
                DungeonComplete = true;
                Destroy(wall.GetComponent<Rigidbody>());
                Destroy(gameObject);
            }
        }
    }
}
