using System.Collections;
using System.Collections.Generic;
using UnityEditor.PackageManager;
using UnityEngine;

public class DungeonScale : MonoBehaviour
{
    public int scale;
    public float delay;

    GameObject Player;
    LoadLevel loadLevel;

    void Start()
    {
        try
        {
            Player = FindAnyObjectByType<PlayerController>().gameObject;
        }
        catch (System.Exception e)
        {
            Debug.Log(e);
        }
        loadLevel = FindAnyObjectByType<LoadLevel>();
        StartCoroutine(Scale());
    }

    IEnumerator Scale()
    {
        yield return new WaitForSeconds(delay);
        gameObject.transform.localScale = new Vector3(scale, scale, scale);

        GameObject SpawnPoint = GameObject.Find("_SpawnPoint");
        if (Player != null)
        {
            Player.transform.position = SpawnPoint.transform.position;
        }

        BakeRuntime[] bakeRuntime = GetComponentsInChildren<BakeRuntime>();
        foreach (BakeRuntime bake in bakeRuntime)
        {
            bake.Bake();
        }

        loadLevel.DungeonComplete();
    }
}
