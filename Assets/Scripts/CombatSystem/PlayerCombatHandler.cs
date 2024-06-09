using UnityEngine;

public class PlayerCombatHandler : MonoBehaviour
{
    bool canAttack;
    bool isAttacking;

    NewControls controls;
    bool basicAttackControl;
    bool heavyAttackControl;

    ItemStatsData itemStats;
    WeaponsHandler weaponsHandler;
    Sword swordHandler;
    Axe axeHandler;
    Shield shieldHandler;
    Staff staffHandler;

    private void Start()
    {
        weaponsHandler = GetComponent<WeaponsHandler>();
        swordHandler = GetComponentInChildren<Sword>();
        axeHandler = GetComponentInChildren<Axe>();
        shieldHandler = GetComponentInChildren<Shield>();
        staffHandler = GetComponentInChildren<Staff>();
        controls = FindAnyObjectByType<PlayerController>().GetNewControls();
    }

    private void Awake()
    {
        controls = new NewControls();

        controls.BasicActionMap.BasicAttack.performed += ctx => basicAttackControl = true;
        controls.BasicActionMap.BasicAttack.canceled += ctx => basicAttackControl = false;

        controls.BasicActionMap.HeavyAttack.performed += ctx => heavyAttackControl = true;
        controls.BasicActionMap.HeavyAttack.canceled += ctx => heavyAttackControl = false;
        
        controls.Enable();
    }

    private void Update()
    {
        if (!canAttack || isAttacking)
        {
            // check attack state, if idle
            //if (swordHandler.currentAnimationState().Equals("MainShortSwordIDLE"))
            //    isAttacking = false;
            return;
        }

        if (basicAttackControl)
            BasicAttack();

        if (heavyAttackControl)
            HeavyAttack();
    }

    private void BasicAttack()
    {
        if (weaponsHandler.GetCurrentRightHandItemStats() == null)
            return;

        isAttacking = true;
        swordHandler.BeginSwordAnimation();
    }

    private void HeavyAttack()
    {

    }

    public void DamageEnemy(EnemyStats enemyStats) => StartCoroutine(enemyStats.DamageMonster(itemStats.GetDamage()));
    public void SetWeaponStats(ItemStatsData itemStats) => this.itemStats = itemStats;
    public void SetAttack(bool status) => canAttack = status;
    public void EndAttack() => isAttacking = false;
}