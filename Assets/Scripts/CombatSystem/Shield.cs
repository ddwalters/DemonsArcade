using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shield : MonoBehaviour
{
    ItemStatsData currentItemStats;

    [SerializeField] Animation anim;

    [SerializeField] GameObject attackCone;
    private void ShieldBlock()
    {
        //play anim (as long as button held move sheild up)
        //if hit with shield up subtract damage or somethin
    }
}