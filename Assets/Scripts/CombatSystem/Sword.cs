using System.Collections;
using UnityEngine;

public class Sword : MonoBehaviour
{
    Animator anim;
    PlayerCombatHandler combatHandler;
    WeaponCollisionCheck collisionCheck;

    bool animRunning;

    private void Start()
    {
        anim = GetComponent<Animator>();
        combatHandler = FindAnyObjectByType<PlayerCombatHandler>();
    }

    private void Update()
    {
        if (!animRunning)
            return;

        if (anim.GetCurrentAnimatorClipInfo(0)[0].clip.name.Equals("MainShortSwordIDLE"))
            combatHandler.EndAttack();
    }

    public void BeginSwordAnimation()
    {
        Debug.Log("Animation begin");
        int animAttackIndex = Random.Range(0, 3); // number of attack animations

        collisionCheck = GetComponentInChildren<WeaponCollisionCheck>();
        collisionCheck.SetCanDamage(true);

        anim.SetTrigger("Attacking");
        anim.SetInteger("Attack", animAttackIndex);

        animRunning = true;
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