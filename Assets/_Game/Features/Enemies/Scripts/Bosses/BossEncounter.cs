using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossEncounter : MonoBehaviour
{
    public LayerMask playerLayer;
    public KingSlime kingSlime;
    private Collider coll;

    private void Start()
    {
        coll = GetComponent<Collider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & playerLayer) != 0)
        {
            StartCoroutine(kingSlime.startFight());
        }
    }
}
