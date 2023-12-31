using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomSpawn : MonoBehaviour
{
    //private RoomTemplates template;
    private RoomTemplates roomTemplates;
    private checkroomcollision checkroom;
    public GameObject deadEnd;
    public GameObject template;

    public bool collide;
    public int attempts;

    //public List<GameObject> Rooms;

    // Start is called before the first frame update
    void Start()
    {
        attempts = 0;
        spawnRoom();
    }

    public bool collideTrue(bool RoomCollide)
    {
        collide = RoomCollide;
        //Debug.Log("collide: " + collide);
        return collide;
    }

    public void spawnRoom()
    {
        if (attempts < 10)
        {
            template = GameObject.FindGameObjectWithTag("DungeonGenerator");
            roomTemplates = template.GetComponent<RoomTemplates>();
            GameObject[] roomPrefabs = roomTemplates.RoomPrefabs;

            int rand = Random.Range(0, roomPrefabs.Length);
            GameObject prefab = roomPrefabs[rand];
            GameObject Room = Instantiate(prefab, transform.position, transform.rotation);
            Room.transform.SetParent(gameObject.transform);
            roomTemplates.Rooms.Add(Room);
            checkroomcollision checkroom = Room.GetComponent<checkroomcollision>();

            StartCoroutine(checkcollide(Room));

            int len = roomTemplates.Rooms.Count;
            if (len <= 4)
            {
                //if enough rooms spawn a room
            }
        }
        else
        {
            spawnDoor();
        }
        attempts++;
        
    }

    public void spawnDoor()
    {
        GameObject Door = Instantiate(deadEnd, transform.position, transform.rotation);
        Door.transform.SetParent(gameObject.transform);

    }

    IEnumerator checkcollide(GameObject Room)
    {
        yield return new WaitForSeconds(0.1f);
        if (collide == true)
        {
            Destroy(Room);
            Debug.Log("Destroy:Room");
            spawnRoom();
            yield return null;
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
