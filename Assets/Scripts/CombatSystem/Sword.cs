using UnityEngine;

public class Sword : MonoBehaviour
{
    Animator anim;

    private void Start()
    {
        anim = GetComponent<Animator>();
    }

    public void SwordAttack(ItemStatsData itemStats, EnemyStats enemyStats)
    {
        int animAttackIndex = Random.Range(0, 3); // number of attack animations

        anim.SetTrigger("Attacking");
        anim.SetInteger("Attack", animAttackIndex);
        StartCoroutine(enemyStats.DamageMonster(itemStats.GetDamage()));
    }
}