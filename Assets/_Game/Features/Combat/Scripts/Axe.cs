using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Axe : MonoBehaviour
{
    [SerializeField] Animation anim;

    public void AxeAttack(ItemStatsData itemStats, EnemyStats enemyStats)
    {
        anim.Play();
        if (itemStats == null || enemyStats == null)
            return;

        PlayerEquipment playerEquipment = GetComponentInParent<PlayerEquipment>();
        int attackBonus = playerEquipment != null ? playerEquipment.GetBonus(EquipmentStat.Attack) : 0;
        float damage = Mathf.Max(0f, itemStats.GetDamage() + attackBonus);
        StartCoroutine(enemyStats.DamageMonster(damage));
    }
}
