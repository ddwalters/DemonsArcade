using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class replaceStart : MonoBehaviour
{
    public GameObject startCube;
    public GameObject cube;
    public GameObject parent;

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
        placeRoom(startCube);

    }

    public void placeRoom(GameObject prefab)
    {
        Instantiate(prefab, gameObject.transform.position, gameObject.transform.rotation, parent.transform);
        Destroy(cube);
    }

}
