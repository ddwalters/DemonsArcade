using System.Collections;
using UnityEngine;

// Development-only blend preview. It drives the same movement motor as gameplay.
public class PlayerAnimationShowcase : MonoBehaviour
{
    public PlayerAnimationTest demo;

    private Coroutine routine;

    public bool IsRunning => routine != null;

    public void Play()
    {
        Stop();
        demo.BeginShowcase();
        routine = StartCoroutine(Sequence());
    }

    public void Stop()
    {
        if (routine == null)
            return;

        StopCoroutine(routine);
        routine = null;
    }

    private IEnumerator Sequence()
    {
        yield return Move("IDLE", Vector2.zero, false, 0.8f);
        yield return Move("IDLE TO WALK", Vector2.up, false, 1.8f);
        yield return Move("WALK TO RUN", Vector2.up, true, 2f);

        demo.Jump();
        yield return Move("RUNNING JUMP", Vector2.up, true, 1.5f);
        yield return Move("RUN BACK TO WALK", Vector2.up, false, 1.5f);
        yield return Move("STRAFE BLEND", Vector2.left, false, 1.5f);
        yield return Move("EASE TO IDLE", Vector2.zero, false, 1.2f);

        demo.caption.text = "MOVEMENT SHOWCASE COMPLETE";
        demo.details.text = "The motor controls movement and turning; animations follow measured speed.";
        routine = null;
    }

    private IEnumerator Move(string label, Vector2 input, bool sprint, float duration)
    {
        demo.caption.text = label;
        demo.details.text = "Live speed blend  |  Motor movement and turning are independent of animation playback";
        demo.SetDrive(input, sprint);

        for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            yield return null;
    }
}
