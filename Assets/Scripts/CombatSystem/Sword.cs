using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sword : MonoBehaviour
{
    ItemStatsData currentItemStats;

    [SerializeField] Animation anim;

    [SerializeField] GameObject attackCone;
    private void SwordAttack()
    {
        //player anim
        //do damage to players in range, or shoot thingy if hit do damage
    }
}