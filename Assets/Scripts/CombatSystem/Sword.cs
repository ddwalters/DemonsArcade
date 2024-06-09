using System.Collections;
using UnityEngine;

public class Sword : MonoBehaviour
{
    Animator anim;
    PlayerCombatHandler combatHandler;
    WeaponCollisionCheck collisionCheck;

    private void Start()
    {
        anim = GetComponent<Animator>();
        combatHandler = FindAnyObjectByType<PlayerCombatHandler>();
    }

    public void BeginSwordAnimation()
    {
        Debug.Log("Animation begin");
        int animAttackIndex = Random.Range(0, 3); // number of attack animations

        collisionCheck = GetComponentInChildren<WeaponCollisionCheck>();
        collisionCheck.SetCanDamage(true);

        anim.SetTrigger("Attacking");
        anim.SetInteger("Attack", animAttackIndex);
    }

    public void SwordAttack(EnemyStats enemyStats) => combatHandler.DamageEnemy(enemyStats);

    // Called in unity
    public void OnAttackAnimationEnd() 
    {
        Debug.Log("animation end");
        combatHandler.EndAttack();
        collisionCheck.SetCanDamage(false);
    }
}