using UnityEngine;

// Simple arena opponent: approach, face, commit a swing, recover. No learning or LLM.
public class PlaygroundEnemy : MonoBehaviour
{
    public PlaygroundFighter target;
    public PlaygroundFighter fighter;
    public float attackDistance = 1.55f;
    public float turnSpeed = 240f;
    public float reactionDelay = 0.55f;
    public bool pursuing = true;

    private float nextDecision;
    private bool wasAttacking;

    public string State { get; private set; } = "APPROACH";

    private void Update()
    {
        if (target == null || fighter == null)
            return;

        bool active = pursuing && !fighter.IsDead && !target.IsDead && !fighter.arena.RoundOver;
        var mode = target.GetComponent<PlayerModeController>();
        active &= mode == null || mode.AllowsGameplay;
        Vector2 movement = Vector2.zero;
        bool running = false;
        if (active)
        {
            if (wasAttacking && !fighter.IsAttacking)
                nextDecision = Time.time + reactionDelay;

            Vector3 direction = target.transform.position - transform.position;
            direction.y = 0f;
            float distance = direction.magnitude;
            if (!fighter.IsAttacking && direction.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation,
                    Quaternion.LookRotation(direction), turnSpeed * Time.deltaTime);
                bool facing = Vector3.Angle(transform.forward, direction) < 15f;
                bool clear = !Physics.Linecast(transform.position, target.transform.position,
                    fighter.arena.environmentMask, QueryTriggerInteraction.Ignore);
                if (distance <= attackDistance && facing && clear && Time.time >= nextDecision)
                    fighter.TryAttack();
                else if (distance > attackDistance)
                {
                    // Stop at geometry rather than continuing to swing through it.
                    bool blocked = Physics.SphereCast(transform.position, 0.3f, transform.forward,
                        out _, 0.85f, fighter.arena.environmentMask, QueryTriggerInteraction.Ignore);
                    if (!blocked)
                        movement = Vector2.up;
                    running = distance > 3f;
                }
            }
            State = fighter.IsAttacking ? fighter.Phase : Time.time < nextDecision ? "RECOVER" :
                distance > attackDistance ? "APPROACH" : "AIM";
        }
        else
            State = fighter.IsDead ? "DEFEATED" : target.IsDead ? "VICTORY" : "WAIT";

        wasAttacking = fighter.IsAttacking;
        fighter.motor.Tick(movement, running, active, Time.deltaTime);
    }

    public void ResetBrain()
    {
        nextDecision = Time.time + reactionDelay;
        wasAttacking = false;
        State = "APPROACH";
    }
}
