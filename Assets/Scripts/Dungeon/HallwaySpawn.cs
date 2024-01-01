using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HallwaySpawn : MonoBehaviour
{
    private RoomTemplates roomTemplates;
    public GameObject template;
    public GameObject[] hallway_prefabs;
    public GameObject doorPrefab;
    private checkhallcollision checkhall;
    private checkroomcollision checkroom;

    private GameObject Door;

    public bool collide;
    public int attempts;

    // Start is called before the first frame update
    void Start()
    {
        attempts = 0; // setting attempts to 0
        spawnHall();
    }

    public bool collideTrue(bool HallCollide)
    {
        collide = HallCollide;
        return collide;
    }

    public void spawnHall()
    {
        // obtain Dungeon Generator object to draw the prefabs from and any other relevant information
        template = GameObject.FindGameObjectWithTag("DungeonGenerator");
        roomTemplates = template.GetComponent<RoomTemplates>();

        // check if there are enough rooms before making more
        int len = roomTemplates.Rooms.Count;
        if (len <= 6)
        {
            // if attemps made to spawn a room is less than [num], proceed on another attempt
            if (attempts < 4)
            {
                // pick a random hallway prefab to spawn
                int rand = Random.Range(0, hallway_prefabs.Length);
                GameObject prefab = hallway_prefabs[rand];
                GameObject hallway = Instantiate(prefab, transform.position, transform.rotation);
                hallway.transform.SetParent(gameObject.transform);
                checkhallcollision checkhall = hallway.GetComponent<checkhallcollision>();

                StartCoroutine(checkcollide(hallway));
            }
            // give up
            else if (attempts == 4)
            {
                spawnDoor();
            }
            attempts++; // increment
        }
        else if (collide == false)
        {
            spawnDoor();
        }
    }

    public void spawnDoor()
    {
        Door = Instantiate(doorPrefab, transform.position, transform.rotation);
        Door.transform.SetParent(gameObject.transform);
    }

    IEnumerator checkcollide(GameObject hallway)
    {
        yield return new WaitForSeconds(1);
        if (collide == true)
        {
            Destroy(hallway);
            Debug.Log("Destroy:Hallway");
            spawnHall();
        }
    }

}
