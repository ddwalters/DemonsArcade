using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DungeonScale : MonoBehaviour
{
    public int scale;
    public float delay;

    void Start()
    {
        StartCoroutine(Scale());
    }

    IEnumerator Scale()
    {
        yield return new WaitForSeconds(delay);
        gameObject.transform.localScale = new Vector3(scale, scale, scale);
    }
}
