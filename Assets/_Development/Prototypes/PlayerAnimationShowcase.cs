using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

// Choreography for reviewing the imported clips, not a gameplay action/death system.
public class PlayerAnimationShowcase : MonoBehaviour
{
    public PlayerAnimationTest demo;
    public RuntimeAnimatorController controller;
    public Image fadeOverlay;

    private Coroutine routine;
    private CharacterMotor motor;
    private ShowcaseRootMotion rootMotion;
    private GameObject step;
    private float fade;

    public bool IsRunning => routine != null;
    public bool Completed { get; private set; }
    public int ActionCount { get; private set; }
    public int TransitionFrames { get; private set; }
    public string Phase { get; private set; }

    public void Play()
    {
        Cancel();
        Completed = false;
        ActionCount = 0;
        TransitionFrames = 0;
        motor = demo.player.GetComponent<CharacterMotor>();
        rootMotion = motor.Animator.GetComponent<ShowcaseRootMotion>();
        if (rootMotion == null)
            rootMotion = motor.Animator.gameObject.AddComponent<ShowcaseRootMotion>();
        rootMotion.capsule = demo.player.GetComponent<CharacterController>();
        demo.BeginShowcase(controller);
        motor.Teleport(new Vector3(10f, 1.04f, -30f), Quaternion.identity);
        routine = StartCoroutine(Sequence());
    }

    public void Cancel()
    {
        if (routine != null)
            StopCoroutine(routine);
        routine = null;
        fade = 0f;
        if (rootMotion != null)
        {
            rootMotion.activeAction = false;
            rootMotion.useGravity = false;
        }
        if (step != null)
            Destroy(step);
        if (demo != null && motor != null)
            demo.SetAuthoredAction(false);
    }

    private void LateUpdate()
    {
        if (fadeOverlay != null)
            fadeOverlay.color = new Color(0f, 0f, 0f, fade);
        if (IsRunning && demo.Animator.IsInTransition(0))
            TransitionFrames++;
    }

    private IEnumerator Sequence()
    {
        yield return Move("IDLE / WALK / RUN", 2f, Vector2.zero, Vector2.up);
        yield return Move("WALK INTO RUN", 2.5f, Vector2.up * 0.5f, Vector2.up, true);
        yield return Action("Running Jump", "RUNNING JUMP", 0.10f);
        yield return Ground("LAND BACK INTO RUNNING", Vector2.up, true, 1.5f);
        yield return Move("RUN BACK TO WALK", 2f, Vector2.up, Vector2.up * 0.5f, true);
        yield return Move("EASE TO A STOP", 1.5f, Vector2.up, Vector2.zero);
        yield return Action("Jumping", "STANDING JUMP / LAND", 0.16f);
        yield return Ground("RETURN TO IDLE", Vector2.zero, false, 0.8f);
        yield return Move("BACKWARD WALK", 1.5f, Vector2.zero, Vector2.down);
        yield return Action("Jump Backward", "BACKWARD JUMP", 0.10f);
        yield return Ground("LAND INTO BACKWARD WALK", Vector2.down, false, 1f);
        yield return Move("STRAFE / DIRECTION BLENDING", 3f, Vector2.left, Vector2.right);
        yield return Move("SETTLE TO IDLE", 1f, Vector2.right, Vector2.zero);
        yield return Action("Start Walking", "AUTHORED WALK START", 0.16f);
        yield return Ground("WALKING", Vector2.up, false, 1f);
        yield return Action("Walking Turn 180", "WALKING TURN", 0.16f);
        yield return Ground("WALK OUT OF TURN", Vector2.up, false, 1f);
        yield return Action("Stop Walking", "AUTHORED WALK STOP", 0.16f);
        yield return Ground("REST", Vector2.zero, false, 0.8f);

        yield return ResetStage("STAGED DROP / LANDING");
        step = GameObject.CreatePrimitive(PrimitiveType.Cube);
        step.name = "Authored step-down reference (visual only)";
        step.GetComponent<Collider>().enabled = false;
        step.transform.position = demo.player.transform.position + new Vector3(0f, -0.54f, 0f);
        step.transform.localScale = new Vector3(1.2f, 1f, 1.2f);
        yield return Action("Jumping Down", "STEP-DOWN JUMP", 0.14f);
        yield return Ground("RECOVER FROM STEP-DOWN", Vector2.zero, false, 0.6f);
        Destroy(step);
        yield return ResetStage("STAGED FALL / LANDING", true);
        SetPhase("FALLING / LANDING", "Staged fall / gravity and capsule collision / authored landing recovery");
        while (!rootMotion.capsule.isGrounded)
            yield return null;
        rootMotion.useGravity = false;
        yield return Action("Falling To Landing", "LANDING RECOVERY", 0.12f);
        yield return Ground("RECOVER INTO WALKING", Vector2.up, false, 1.5f);

        yield return ResetStage("WEAPON MOVEMENT / ACTION PREVIEW");
        demo.sword.SetActive(true);
        yield return Action("Withdrawing Sword", "DRAW SWORD", 0.18f);
        yield return Action("CombatIdle", "COMBAT IDLE", 0.20f, 0.8f);
        yield return Action("Run With Sword", "RUN WITH SWORD", 0.24f, 2f);
        yield return Action("Standing Melee Run Jump Attack", "RUN / JUMP ATTACK", 0.14f);
        yield return Action("CombatIdle", "RECOVER TO COMBAT IDLE", 0.20f, 0.8f);
        yield return Action("Stable Sword Outward Slash", "SWORD SLASH", 0.14f);
        yield return Action("CombatIdle", "RECOVERY", 0.20f, 0.6f);
        yield return Action("Sheathing Sword", "SHEATHE SWORD", 0.18f);
        demo.sword.SetActive(false);
        yield return Ground("RETURN TO LOCOMOTION", Vector2.zero, false, 0.8f);

        yield return Death("Falling Back Death", "STAGED DEATH 1 / HIT FROM THE FRONT", false);
        yield return Death("Falling Forward Death", "STAGED DEATH 2 / HIT FROM BEHIND", false);
        yield return Death("Flying Back Death", "STAGED DEATH 3 / HEAVY HIT WHILE RUNNING", true);
        Phase = "SHOWCASE COMPLETE";
        demo.caption.text = Phase;
        demo.details.text = "Three staged deaths held at their final pose. Gameplay death / respawn remains separate.";
        Completed = true;
        routine = null;
        Debug.Log($"[Action Showcase] Complete: {ActionCount} actions, {TransitionFrames} crossfade frames.");
    }

    private IEnumerator Move(string label, float duration, Vector2 from, Vector2 to, bool sprint = false)
    {
        SetPhase(label, "Live locomotion blend tree / animation root travel + collision motor");
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
        {
            demo.SetDrive(Vector2.Lerp(from, to, Mathf.SmoothStep(0f, 1f, elapsed / duration)), sprint);
            yield return null;
        }
    }

    private IEnumerator Ground(string label, Vector2 input, bool sprint, float duration)
    {
        rootMotion.activeAction = false;
        motor.Stop();
        demo.SetAuthoredAction(false);
        demo.SetDrive(input, sprint);
        demo.Animator.CrossFadeInFixedTime("Grounded", 0.18f);
        yield return Move(label, duration, input, input, sprint);
    }

    private IEnumerator Action(string clip, string label, float blend, float duration = -1f)
    {
        SetPhase(label, "Staged authored clip / blended transitions / gameplay jump and combat timing not connected");
        demo.SetAuthoredAction(true);
        rootMotion.activeAction = true;
        demo.Animator.CrossFadeInFixedTime(clip, blend, 0, 0f);
        ActionCount++;
        if (duration < 0f)
            duration = demo.previews.First(preview => preview.name == clip).clip.length;
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            yield return null;
    }

    private IEnumerator Death(string clip, string label, bool running)
    {
        yield return ResetStage(label);
        yield return Move(running ? "RUNNING BEFORE IMPACT" : "IDLE BEFORE IMPACT", 1.6f,
            Vector2.zero, running ? Vector2.up : Vector2.zero, running);
        yield return Action(clip, label, 0.12f);
        SetPhase(label, "Staged fatal hit / final pose held / reset between scenarios");
        yield return new WaitForSeconds(1.1f);
    }

    private IEnumerator ResetStage(string label, bool falling = false)
    {
        for (float elapsed = 0f; elapsed < 0.2f; elapsed += Time.deltaTime)
        {
            fade = elapsed / 0.2f;
            yield return null;
        }
        fade = 1f;
        rootMotion.activeAction = false;
        rootMotion.useGravity = false;
        motor.Teleport(new Vector3(10f, falling ? 6.04f : 1.04f, -15f), Quaternion.identity);
        demo.sword.SetActive(false);
        demo.SetAuthoredAction(falling);
        demo.SetDrive(Vector2.zero, false);
        demo.Animator.Play(falling ? "Falling Idle" : "Grounded", 0, 0f);
        if (falling)
        {
            rootMotion.activeAction = true;
            rootMotion.useGravity = true;
            rootMotion.verticalSpeed = 0f;
            ActionCount++;
        }
        SetPhase(label, "Scenario reset");
        yield return new WaitForSeconds(0.25f);
        for (float elapsed = 0f; elapsed < 0.2f; elapsed += Time.deltaTime)
        {
            fade = 1f - elapsed / 0.2f;
            yield return null;
        }
        fade = 0f;
    }

    private void SetPhase(string label, string explanation)
    {
        Phase = label;
        demo.caption.text = label;
        demo.details.text = explanation;
        Debug.Log("[Action Showcase] " + label);
    }

}
