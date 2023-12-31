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
        attempts = 0; //setting attempts to 0
        spawnHall();
    }

    public bool collideTrue(bool HallCollide)
    {
        collide = HallCollide;
        //Debug.Log("collide: " + collide);
        return collide;
    }

    public void spawnHall() //selects a random prefab from my list of hallway prefabs and instantiates it
    {
        template = GameObject.FindGameObjectWithTag("DungeonGenerator");
        roomTemplates = template.GetComponent<RoomTemplates>();


        int len = roomTemplates.Rooms.Count;
        if (len <= 6)
        {
            if (attempts < 4)
            {
                int rand = Random.Range(0, hallway_prefabs.Length);
                GameObject prefab = hallway_prefabs[rand];
                GameObject hallway = Instantiate(prefab, transform.position, transform.rotation);
                hallway.transform.SetParent(gameObject.transform);
                checkhallcollision checkhall = hallway.GetComponent<checkhallcollision>();

                //Debug.Log(collide);
                //Debug.Log(checkhall.Collide);

                StartCoroutine(checkcollide(hallway));


            }
            else if (attempts == 4)
            {
                spawnDoor();
            }
            attempts++; //increment
        }
        else if (collide == false)
        {
            spawnDoor();
        }
        

    }


    public void spawnDoor()
    {
        GameObject Door = Instantiate(doorPrefab, transform.position, transform.rotation);
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
            yield return null;
        }
    }

    

    // Update is called once per frame
    void Update()
    {

    }
}
