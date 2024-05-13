using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DungeonScale : MonoBehaviour
{
    public int scale;
    public float delay;

    public GameObject Player;

    void Start()
    {
        StartCoroutine(Scale());
    }

    IEnumerator Scale()
    {
        yield return new WaitForSeconds(delay);
        gameObject.transform.localScale = new Vector3(scale, scale, scale);

        GameObject SpawnPoint = GameObject.Find("_SpawnPoint");

        BakeRuntime[] bakeRuntime = GetComponentsInChildren<BakeRuntime>();
        foreach (BakeRuntime bake in bakeRuntime)
        {
            bake.Bake();
        }

        Player.transform.position = SpawnPoint.transform.position;
    }
}
