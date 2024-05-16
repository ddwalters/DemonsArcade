using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Axe : MonoBehaviour
{
    [SerializeField] Animation anim;

    private void AxeAttack(ItemStatsData itemStats, EnemyStats enemyStats)
    {
        anim.Play();
        StartCoroutine(enemyStats.DamageMonster(itemStats.GetDamage()));
    }
}