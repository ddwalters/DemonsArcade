using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class hallwayWalls : MonoBehaviour
{
    public GameObject wall;
    public Collider collide;

    LoadLevel loadLevel;
    public bool DungeonComplete = false;

    void Start()
    {
        loadLevel = FindAnyObjectByType<LoadLevel>();
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Hallway"))
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
            }
        }
    }
}
