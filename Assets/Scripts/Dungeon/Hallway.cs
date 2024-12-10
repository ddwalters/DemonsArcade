using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hallway : MonoBehaviour
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

    // Update is called once per frame
    void Update()
    {
        
    }

    public void destroyPlusX() => plusX.SetActive(false);
    public void destroyMinX() => minX.SetActive(false);
    public void destroyPlusZ() => plusZ.SetActive(false);
    public void destroyMinZ() => minZ.SetActive(false);

    public void placeTorches()
    {
        if (plusX.active)
        {
            bool rand = Random.value < 0.5f; // 50/50 chance
            torchPlusX.SetActive(rand);
        }
        if (minX.active)
        {
            bool rand = Random.value < 0.5f; // 50/50 chance
            torchMinX.SetActive(rand);
        }
        if (plusZ.active)
        {
            bool rand = Random.value < 0.5f; // 50/50 chance
            torchPlusZ.SetActive(rand);
        }
        if (minZ.active)
        {
            bool rand = Random.value < 0.5f; // 50/50 chance
            torchMinZ.SetActive(rand);
        }
    }

}
