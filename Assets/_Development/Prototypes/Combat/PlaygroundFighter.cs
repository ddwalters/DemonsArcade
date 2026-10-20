using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// A deliberately small, development-only full-body combat prototype.
public class PlaygroundFighter : MonoBehaviour
{
    public bool isPlayer;
    public float maximumHealth = 60f;
    public float damage = 20f;
    public CharacterMotor motor;
    public Transform weapon;
    public PlaygroundArena arena;

    [Header("Sword attack (normalized animation time)")]
    [Range(0f, 1f)] public float activeStart = 0.24f;
    [Range(0f, 1f)] public float activeEnd = 0.52f;
    public float reach = 1.85f;
    public float arc = 110f;
    public float staminaCost = 12f;

    private readonly HashSet<PlaygroundFighter> struck = new HashSet<PlaygroundFighter>();
    private readonly Collider[] candidates = new Collider[16];
    private PlayerStatsManager stats;
    private PlayerModeController mode;
    private NewControls controls;
    private float health;
    private float attackTime;
    private bool attacking;
    private bool enteredAttack;
    private bool dead;
    private Quaternion attackRotation;
    private Renderer[] bodyRenderers;
    private Color[] bodyColors;
    private MaterialPropertyBlock hitTint;
    private float flashUntil;

    public bool IsDead => dead;
    public bool IsAttacking => attacking;
    public bool IsActiveWindow { get; private set; }
    public int HitsLanded { get; private set; }
    public int AttacksStarted { get; private set; }
    public float Health => stats != null ? stats.GetPlayerStats().currentHealth : health;
    public float MaxHealth => stats != null ? stats.GetPlayerStats().maxHealth : maximumHealth;
    public string Phase => dead ? "DEFEATED" : !attacking ? "READY" : IsActiveWindow ? "SWING" :
        enteredAttack && motor.Animator.GetCurrentAnimatorStateInfo(0).normalizedTime > activeEnd ? "RECOVERY" : "WINDUP";

    private void Awake()
    {
        motor = GetComponent<CharacterMotor>();
        health = maximumHealth;
        bodyRenderers = motor.Animator.GetComponentsInChildren<Renderer>();
        bodyColors = new Color[bodyRenderers.Length];
        hitTint = new MaterialPropertyBlock();
        for (int index = 0; index < bodyRenderers.Length; index++)
            bodyColors[index] = bodyRenderers[index].sharedMaterial.GetColor("_BaseColor");
        if (isPlayer)
        {
            stats = GetComponent<PlayerStatsManager>();
            mode = GetComponent<PlayerModeController>();
        }
    }

    private void Start()
    {
        if (!isPlayer)
            return;

        mode = GetComponent<PlayerModeController>();
        controls = GetComponent<PlayerController>().GetNewControls();
        controls.BasicActionMap.BasicAttack.performed += AttackInput;
    }

    private void AttackInput(InputAction.CallbackContext context) => TryAttack();

    public bool TryAttack()
    {
        if (dead || attacking || !motor.IsGrounded || (mode != null && !mode.AllowsGameplay) ||
            (arena != null && arena.RoundOver))
            return false;

        if (stats != null)
        {
            if (stats.GetStamina() < staminaCost)
                return false;
            stats.SetStaminaUse(true);
            stats.UseStamina(staminaCost);
            stats.SetStaminaUse(false);
        }

        attacking = true;
        enteredAttack = false;
        IsActiveWindow = false;
        attackTime = Time.time;
        attackRotation = transform.rotation;
        struck.Clear();
        AttacksStarted++;
        motor.SetActionMotion(true);
        motor.Animator.CrossFadeInFixedTime("SwordAttack", 0.12f, 0, 0f);
        return true;
    }

    private void Update()
    {
        if (!dead && Health <= 0f)
            Die();
        if (dead || !attacking || (mode != null && !mode.AllowsGameplay))
            return;

        var state = motor.Animator.GetCurrentAnimatorStateInfo(0);
        if (state.IsName("SwordAttack"))
        {
            enteredAttack = true;
            IsActiveWindow = state.normalizedTime >= activeStart && state.normalizedTime <= activeEnd;
            if (IsActiveWindow)
                Strike();
            if (state.normalizedTime >= 0.94f)
                EndAttack();
        }
        else if (enteredAttack || Time.time - attackTime > 3f)
            EndAttack();
    }

    private void LateUpdate()
    {
        // Facing is committed during a swing; attacks cannot track a target behind the actor.
        if (attacking)
            transform.rotation = attackRotation;
        for (int index = 0; index < bodyRenderers.Length; index++)
        {
            hitTint.SetColor("_BaseColor", Time.time < flashUntil ? Color.white : bodyColors[index]);
            bodyRenderers[index].SetPropertyBlock(hitTint);
        }
    }

    private void Strike()
    {
        int count = Physics.OverlapSphereNonAlloc(transform.position, reach, candidates, ~0,
            QueryTriggerInteraction.Ignore);
        for (int index = 0; index < count; index++)
        {
            var victim = candidates[index].GetComponentInParent<PlaygroundFighter>();
            if (victim == null || victim == this || victim.dead || struck.Contains(victim))
                continue;

            Vector3 direction = victim.transform.position - transform.position;
            if (Mathf.Abs(direction.y) > 1.2f)
                continue;
            direction.y = 0f;
            if (direction.magnitude > reach || Vector3.Angle(transform.forward, direction) > arc * 0.5f)
                continue;
            // Only the environment blocks the strike, not either actor's capsule.
            if (Physics.Linecast(transform.position, victim.transform.position, arena.environmentMask,
                QueryTriggerInteraction.Ignore))
                continue;

            struck.Add(victim);
            HitsLanded++;
            victim.TakeDamage(damage);
            arena.ReportHit(this, victim, damage);
        }
    }

    public void TakeDamage(float amount)
    {
        if (dead || amount <= 0f)
            return;
        flashUntil = Time.time + 0.12f;
        if (stats != null)
            stats.DamagePlayer(amount);
        else
            health = Mathf.Max(0f, health - amount);
        if (Health <= 0f)
            Die();
    }

    private void Die()
    {
        dead = true;
        attacking = false;
        IsActiveWindow = false;
        motor.SetActionMotion(true, allowWhenMovementBlocked: true);
        motor.Stop();
        motor.Animator.CrossFadeInFixedTime("Death", 0.12f, 0, 0f);
        arena?.ReportDeath(this);
    }

    private void EndAttack()
    {
        attacking = false;
        IsActiveWindow = false;
        motor.SetActionMotion(false);
        motor.Animator.CrossFadeInFixedTime("Grounded", 0.18f);
    }

    public void ResetFighter(Vector3 position, Quaternion rotation)
    {
        dead = false;
        attacking = false;
        enteredAttack = false;
        IsActiveWindow = false;
        HitsLanded = AttacksStarted = 0;
        health = maximumHealth;
        stats?.ResetResources();
        motor.SetActionMotion(false);
        motor.Teleport(position, rotation);
        motor.Animator.Rebind();
        motor.Animator.Update(0f);
        motor.Animator.Play("Grounded", 0, 0f);
    }

    private void OnDestroy()
    {
        if (controls != null)
            controls.BasicActionMap.BasicAttack.performed -= AttackInput;
        if (motor != null)
            motor.SetActionMotion(false);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = IsActiveWindow ? Color.red : Color.yellow;
        Vector3 left = Quaternion.AngleAxis(-arc * 0.5f, Vector3.up) * transform.forward;
        Vector3 right = Quaternion.AngleAxis(arc * 0.5f, Vector3.up) * transform.forward;
        Gizmos.DrawRay(transform.position, left * reach);
        Gizmos.DrawRay(transform.position, right * reach);
        Gizmos.DrawWireSphere(transform.position, reach);
    }
}
