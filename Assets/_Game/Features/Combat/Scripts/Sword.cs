using UnityEngine;

public class Sword : MonoBehaviour
{
    Animator _anim;
    PlayerCombatHandler _combatHandler;
    WeaponCollisionCheck _collisionCheck;

    bool animRunning;

    private void Start()
    {
        _anim = GetComponent<Animator>();
        _combatHandler = FindAnyObjectByType<PlayerCombatHandler>();
    }

    private void Update()
    {
        if (!animRunning)
            return;

        if (_anim.GetCurrentAnimatorClipInfo(0)[0].clip.name.Equals("MainShortSwordIDLE"))
            _combatHandler.EndAttack();
    }

    public void BeginSwordAnimation()
    {
        Debug.Log("Animation begin");
        int animAttackIndex = Random.Range(0, 3); // number of attack animations

        _collisionCheck = GetComponentInChildren<WeaponCollisionCheck>();
        _collisionCheck.SetCanDamage(true);

        _anim.SetTrigger("Attacking");
        _anim.SetInteger("Attack", animAttackIndex);

        animRunning = true;
    }

    public void SwordAttack(EnemyStats enemyStats) => _combatHandler.DamageEnemy(enemyStats);

    // Called in unity
    public void OnAttackAnimationEnd()
    {
        Debug.Log("animation end");
        _combatHandler.EndAttack();
        _collisionCheck.SetCanDamage(false);
    }
}