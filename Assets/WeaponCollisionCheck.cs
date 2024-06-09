using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponCollisionCheck : MonoBehaviour
{
    bool canDamage;

    Sword sword;

    private void Awake()
    {
        sword = FindAnyObjectByType<Sword>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag != "Enemy" || !canDamage)
            return;

        var enemyStats = other.GetComponent<EnemyStats>();

        if (enemyStats != null )
        {
            sword.SwordAttack(other.GetComponent<EnemyStats>());
            canDamage = false;
        }
    }

    public void SetCanDamage(bool status) => canDamage = status;
}
