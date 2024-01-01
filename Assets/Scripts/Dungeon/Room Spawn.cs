using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomSpawn : MonoBehaviour
{
    private RoomTemplates roomTemplates;
    private checkroomcollision checkroom;

    public GameObject deadEnd;
    public GameObject template;

    public bool collide;
    public int attempts;

    // Start is called before the first frame update
    void Start()
    {
        attempts = 0;
        spawnRoom();
    }

    public bool collideTrue(bool RoomCollide)
    {
        collide = RoomCollide;
        return collide;
    }

    public void spawnRoom()
    {
        // obtain Dungeon Generator object to draw the prefabs from and any other relevant information
        template = GameObject.FindGameObjectWithTag("DungeonGenerator");
        roomTemplates = template.GetComponent<RoomTemplates>();

        // if attemps made to spawn a room is less than 10, proceed on another attempt
        if (attempts < 10)
        {
            // pick a random room prefab to spawn
            GameObject[] roomPrefabs = roomTemplates.RoomPrefabs;

            int rand = Random.Range(0, roomPrefabs.Length);
            GameObject prefab = roomPrefabs[rand];
            GameObject Room = Instantiate(prefab, transform.position, transform.rotation);
            Room.transform.SetParent(gameObject.transform);
            roomTemplates.Rooms.Add(Room);
            checkroomcollision checkroom = Room.GetComponent<checkroomcollision>();

            StartCoroutine(checkCollide(Room));

            int len = roomTemplates.Rooms.Count;
        }
        // give up
        else
        {
            spawnDoor();
        }
        attempts++; // increment 
    }

    public void spawnDoor()
    {
        GameObject Door = Instantiate(deadEnd, transform.position, transform.rotation);
        Door.transform.SetParent(gameObject.transform);
    }

    IEnumerator checkCollide(GameObject Room)
    {
        yield return new WaitForSeconds(0.1f);
        if (collide == true)
        {
            Destroy(Room);
            Debug.Log("Destroy:Room");
            spawnRoom();
        }
    }

}
