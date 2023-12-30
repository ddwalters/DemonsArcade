using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HallwaySpawn : MonoBehaviour
{

    public GameObject[] hallway_prefabs;
    public GameObject doorPrefab;
    private checkhallcollision checkhall;
    public int attempts;


    // Start is called before the first frame update
    void Start()
    {
        attempts = 0;
        spawnHall();
    }

    public void spawnHall()
    {
        if (attempts < 4)
        {
            int rand = Random.Range(0, hallway_prefabs.Length);
            GameObject prefab = hallway_prefabs[rand];
            GameObject hallway = Instantiate(prefab, transform.position, transform.rotation);
            hallway.transform.SetParent(gameObject.transform);
            attempts++;
        }
        else if (attempts == 4)
        {
            spawnDoor();
            attempts++;
        }
    }

    public void spawnDoor()
    {
        GameObject Door = Instantiate(doorPrefab, transform.position, transform.rotation);
    }

    

    // Update is called once per frame
    void Update()
    {
        
    }
}
