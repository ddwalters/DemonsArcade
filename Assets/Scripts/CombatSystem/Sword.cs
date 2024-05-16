using UnityEngine;

public class Sword : MonoBehaviour
{
    [SerializeField] Animation anim;

    public void SwordAttack(ItemStatsData itemStats, EnemyStats enemyStats)
    {
        anim.Play();
        StartCoroutine(enemyStats.DamageMonster(itemStats.GetDamage()));
    }
}