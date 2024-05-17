using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class replaceRooms : MonoBehaviour
{
    public Generator2D gen2d;
    public GameObject cube;
    public GameObject parent;

    [Header("3x Rooms")]
    public GameObject[] p3x3;
    public GameObject[] p3x4;
    public GameObject[] p3x5;
    public GameObject[] p3x6;
    [Header("4x Rooms")]
    public GameObject[] p4x3;
    public GameObject[] p4x4;
    public GameObject[] p4x5;
    public GameObject[] p4x6;
    [Header("5x Rooms")]
    public GameObject[] p5x3;
    public GameObject[] p5x4;
    public GameObject[] p5x5;
    public GameObject[] p5x6;
    [Header("6x Rooms")]
    public GameObject[] p6x3;
    public GameObject[] p6x4;
    public GameObject[] p6x5;
    public GameObject[] p6x6;


    private void Awake()
    {
        GameObject generator = GameObject.Find("Generator");
        parent = GameObject.Find("DUNGEON");
        Generator2D gen2d = generator.GetComponent<Generator2D>();
        StartCoroutine(replaceRoom());
        Random.InitState(gen2d.seed);
    }

    IEnumerator replaceRoom()
    {
        yield return new WaitForSeconds(0f);
        float x = gameObject.transform.parent.localScale.x;
        float z = gameObject.transform.parent.localScale.z;

        // 3 by rooms

        if (x == 3f && z == 3f)
        {
            int rand = Random.Range(0, p3x3.Length);
            placeRoom(p3x3[rand]);
        }
        else if (x == 3f && z == 4f)
        {
            int rand = Random.Range(0, p3x4.Length);
            placeRoom(p3x4[rand]);
        }
        else if (x == 3f && z == 5f)
        {
            int rand = Random.Range(0, p3x5.Length);
            placeRoom(p3x5[rand]);
        }
        else if (x == 3f && z == 6f)
        {
            int rand = Random.Range(0, p3x6.Length);
            placeRoom(p3x6[rand]);
        }

        // 4 by rooms
        else if (x == 4f && z == 3f)
        {
            int rand = Random.Range(0, p4x3.Length);
            placeRoom(p4x3[rand]);
        }
        else if (x == 4f && z == 4f)
        {
            int rand = Random.Range(0, p4x4.Length);
            placeRoom(p4x4[rand]);
        }
        else if (x == 4f && z == 5f)
        {
            int rand = Random.Range(0, p4x5.Length);
            placeRoom(p4x5[rand]);
        }
        else if (x == 4f && z == 6f)
        {
            int rand = Random.Range(0, p4x6.Length);
            placeRoom(p4x6[rand]);
        }

        // 5 by rooms
        else if (x == 5f && z == 3f)
        {
            int rand = Random.Range(0, p5x3.Length);
            placeRoom(p5x3[rand]);
        }
        else if (x == 5f && z == 4f)
        {
            int rand = Random.Range(0, p5x4.Length);
            placeRoom(p5x4[rand]);
        }
        else if (x == 5f && z == 5f)
        {
            int rand = Random.Range(0, p5x5.Length);
            placeRoom(p5x5[rand]);
        }
        else if (x == 5f && z == 6f)
        {
            int rand = Random.Range(0, p5x6.Length);
            placeRoom(p5x6[rand]);
        }

        // 6 by rooms
        else if (x == 6f && z == 3f)
        {
            int rand = Random.Range(0, p6x3.Length);
            placeRoom(p6x3[rand]);
        }
        else if (x == 6f && z == 4f)
        {
            int rand = Random.Range(0, p6x4.Length);
            placeRoom(p6x4[rand]);
        }
        else if (x == 6f && z == 5f)
        {
            int rand = Random.Range(0, p6x5.Length);
            placeRoom(p6x5[rand]);
        }
        else if (x == 6f && z == 6f)
        {
            int rand = Random.Range(0, p6x6.Length);
            placeRoom(p6x6[rand]);
        }

    }

    public void placeRoom(GameObject prefab)
    {
        Instantiate(prefab, gameObject.transform.position, gameObject.transform.rotation, parent.transform);
        Destroy(cube);
    }

}
