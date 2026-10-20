using UnityEngine;

public class PlayerCombatHandler : MonoBehaviour
{
    bool canAttack;
    bool isAttacking;

    NewControls controls;
    bool basicAttackControl;
    bool heavyAttackControl;

    bool basicAttackTriggered;
    bool heavyAttackTriggered;

    ItemStatsData itemStats;
    WeaponsHandler weaponsHandler;

    Sword swordRightHandler;
    Axe axeRightHandler;
    Staff staffRightHandler;

    private void Start()
    {
        weaponsHandler = GetComponent<WeaponsHandler>();

        swordRightHandler = GameObject.Find("RightSword").GetComponent<Sword>();

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
            return;

        if (basicAttackControl && !basicAttackTriggered)
        {
            BasicAttack();
            basicAttackTriggered = true;
        }

        if (heavyAttackControl && !heavyAttackTriggered)
        {
            HeavyAttack();
            heavyAttackTriggered = true;
        }
    }

    private void BasicAttack()
    {
        if (weaponsHandler.GetCurrentRightHandItemStats() == null)
            return;

        isAttacking = true;
        swordRightHandler.BeginSwordAnimation();
    }

    private void HeavyAttack()
    {

    }

    public void DamageEnemy(EnemyStats enemyStats) => StartCoroutine(enemyStats.DamageMonster(itemStats.GetDamage()));
    public void SetWeaponStats(ItemStatsData itemStats) => this.itemStats = itemStats;
    public void SetAttack(bool status) => canAttack = status;
    public void EndAttack()
    {
        isAttacking = false;
        basicAttackTriggered = false;
        heavyAttackTriggered = false;
    } 
}