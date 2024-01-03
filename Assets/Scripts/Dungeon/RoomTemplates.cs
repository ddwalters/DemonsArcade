using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomTemplates : MonoBehaviour
{
    public GameObject[] RoomPrefabs;
    public int MaxRooms;

    public List<GameObject> Rooms;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // Iterate through the list in reverse to safely remove elements
        for (int i = Rooms.Count - 1; i >= 0; i--)
        {
            // Check if the element is null or has been destroyed
            if (Rooms[i] == null || Rooms[i].Equals(null))
            {
                // Remove the empty game object from the list
                Rooms.RemoveAt(i);
            }
        }
    }
}
