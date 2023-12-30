using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomSpawn : MonoBehaviour
{
    //private RoomTemplates template;
    private RoomTemplates roomTemplates;
    public GameObject template;
    public int attempts;

    //public List<GameObject> Rooms;

    // Start is called before the first frame update
    void Start()
    {
        attempts = 0;
        spawnRoom();
    }

    public void spawnRoom()
    {
        if (attempts < 10)
        {
            template = GameObject.FindGameObjectWithTag("DungeonGenerator");
            roomTemplates = template.GetComponent<RoomTemplates>();
            GameObject[] roomPrefabs = roomTemplates.RoomPrefabs;


            int len = roomTemplates.Rooms.Count;
            if (len <= 6)
            {
                int rand = Random.Range(0, roomPrefabs.Length);
                GameObject prefab = roomPrefabs[rand];
                GameObject Room = Instantiate(prefab, transform.position, transform.rotation);
                Room.transform.SetParent(gameObject.transform);
                //checkroomcollision checkroom = Room.GetComponent<checkroomcollision>();
                roomTemplates.Rooms.Add(Room);

                //checkroom.onStart = false;
            }
        }
        else
        {

        }
        attempts++;
        
    }

    // Update is called once per frame
    void Update()
    {

    }
}
