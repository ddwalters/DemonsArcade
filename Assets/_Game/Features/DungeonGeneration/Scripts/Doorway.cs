using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Doorway : MonoBehaviour
{
    public GameObject plusX;
    public GameObject minX;
    public GameObject plusZ;
    public GameObject minZ;

    public GameObject torchPlusX;
    public GameObject torchMinX;
    public GameObject torchPlusZ;
    public GameObject torchMinZ;

    public Collider collide;

    LoadLevel loadLevel;
    public bool DungeonComplete = false;

    void Start()
    {
        loadLevel = FindAnyObjectByType<LoadLevel>();
        collide = gameObject.GetComponent<Collider>();
    }

    public void destroyPlusX() 
    {
        if (plusX != null)
        {
            plusX.SetActive(false);
        }
    }
    public void destroyMinX()
    {
        if (minX != null)
        {
            minX.SetActive(false);
        }
    }
    public void destroyPlusZ()
    {
        if (plusZ != null)
        {
            plusZ.SetActive(false);
        }
    }
    public void destroyMinZ()
    {
        if (minZ != null)
        {
            minZ.SetActive(false);
        }
    }

    public void placeTorches()
    {
        List<GameObject> activeWalls = new List<GameObject>();

        if (plusX.active) activeWalls.Add(torchPlusX);
        if (minX.active) activeWalls.Add(torchMinX);
        if (plusZ.active) activeWalls.Add(torchPlusZ);
        if (minZ.active) activeWalls.Add(torchMinZ);

        foreach (GameObject torch in activeWalls)
        {
            torch.SetActive(false);
        }

        if (activeWalls.Count > 0)
        {
            int randomIndex = Random.Range(0, activeWalls.Count);
            activeWalls[randomIndex].SetActive(true);
        }
    }

}
