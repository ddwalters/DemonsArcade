using UnityEngine;
using UnityEngine.InputSystem;

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
    PlayerModeController mode;

    Sword swordRightHandler;
    Axe axeRightHandler;
    Staff staffRightHandler;

    private void Start()
    {
        weaponsHandler = GetComponent<WeaponsHandler>();

        swordRightHandler = GameObject.Find("RightSword").GetComponent<Sword>();

        var player = GetComponentInParent<PlayerController>();
        if (player == null)
            player = FindAnyObjectByType<PlayerController>();
        mode = PlayerModeController.ForPlayer(player);
        controls = player.GetNewControls();
        controls.BasicActionMap.BasicAttack.performed += OnBasicAttack;
        controls.BasicActionMap.BasicAttack.canceled += OnBasicAttack;
        controls.BasicActionMap.HeavyAttack.performed += OnHeavyAttack;
        controls.BasicActionMap.HeavyAttack.canceled += OnHeavyAttack;
    }

    private void OnDestroy()
    {
        if (controls == null)
            return;

        controls.BasicActionMap.BasicAttack.performed -= OnBasicAttack;
        controls.BasicActionMap.BasicAttack.canceled -= OnBasicAttack;
        controls.BasicActionMap.HeavyAttack.performed -= OnHeavyAttack;
        controls.BasicActionMap.HeavyAttack.canceled -= OnHeavyAttack;
    }

    private void OnBasicAttack(InputAction.CallbackContext context) =>
        basicAttackControl = !context.canceled && mode.AllowsGameplay;
    private void OnHeavyAttack(InputAction.CallbackContext context) =>
        heavyAttackControl = !context.canceled && mode.AllowsGameplay;

    private void Update()
    {
        if (mode == null || !mode.AllowsGameplay)
        {
            basicAttackControl = heavyAttackControl = false;
            return;
        }
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

    public void DamageEnemy(EnemyStats enemyStats)
    {
        if (mode.AllowsGameplay && canAttack && itemStats != null)
            StartCoroutine(enemyStats.DamageMonster(itemStats.GetDamage()));
    }
    public void SetWeaponStats(ItemStatsData itemStats) => this.itemStats = itemStats;
    public void SetAttack(bool status)
    {
        canAttack = status;
        if (!status)
            EndAttack();
    }
    public void EndAttack()
    {
        isAttacking = false;
        basicAttackTriggered = false;
        heavyAttackTriggered = false;
    } 
}
