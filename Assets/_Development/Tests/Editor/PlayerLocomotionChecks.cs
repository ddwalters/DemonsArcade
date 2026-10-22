using System;
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class PlayerLocomotionChecks
{
    private const string Running = "PlayerLocomotionChecks.Running";
    private const string PrefabPath = "Assets/_Game/Features/Player/Prefabs/player.prefab";
    private static IEnumerator routine;
    private static int checks;
    private static int previousFrame;
    private static double deadline;

    static PlayerLocomotionChecks() => EditorApplication.playModeStateChanged += Changed;

    [MenuItem("Tools/Validation/Player Locomotion Assets")]
    public static void CheckAssets()
    {
        checks = 0;
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        var capsule = prefab.GetComponent<CharacterController>();
        var motor = prefab.GetComponent<CharacterMotor>();
        var animator = motor.Animator;
        Require(capsule != null && capsule.enabled && motor != null, "Player has a collision motor.");
        Require(prefab.GetComponent<Rigidbody>() == null && prefab.GetComponent<CapsuleCollider>() == null,
            "Old Rigidbody/capsule no longer compete with the motor.");
        Require(prefab.GetComponent<MeshRenderer>() == null, "Old visible capsule is removed.");
        Require(animator != null && animator.avatar.isHuman && animator.avatar.isValid, "Player has a valid Humanoid body.");
        Require(animator.applyRootMotion && animator.cullingMode == AnimatorCullingMode.AlwaysAnimate,
            "Animator stays active to provide root motion.");
        Require(animator.GetComponent<CharacterRootMotion>() != null && animator.runtimeAnimatorController != null,
            "The body has a controller and root-motion relay.");
        Require(new SerializedObject(motor).FindProperty("animator").objectReferenceValue == animator,
            "Motor references this player's Animator.");
        Require(new SerializedObject(animator.GetComponent<CharacterRootMotion>()).FindProperty("motor").objectReferenceValue == motor,
            "Relay references this player's motor.");
        Require((prefab.GetComponentInChildren<Camera>().cullingMask & (1 << prefab.layer)) == 0,
            "Camera initially avoids drawing the player before the third-person boom is placed.");
        Require(prefab.transform.Find("BodyModel").localPosition == Vector3.down, "Body feet align with the capsule bottom.");
        Require(prefab.GetComponent<PlayerCameraRig>() != null && prefab.GetComponent<PlayerCameraRig>().IsThirdPerson,
            "Player camera remains in third person.");
        Debug.Log($"[Player Locomotion Assets] PASS: {checks} checks.");
    }

    [MenuItem("Tools/Validation/Player Locomotion (Play Mode)")]
    public static void Run()
    {
        if (EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        CheckAssets();
        SessionState.SetString("PlayerLocomotionChecks.PreviousScene", UnityEngine.SceneManagement.SceneManager.GetActiveScene().path);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        SessionState.SetBool(Running, true);
        EditorApplication.isPlaying = true;
    }

    public static void RunBatch()
    {
        CheckAssets();
        PlayerFeatureChecks.RunAll();
        SaveFeatureChecks.RunAll();
        SessionState.SetBool("PlayerLocomotionChecks.Batch", true);
        Run();
    }

    private static void Changed(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Running, false))
            return;

        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            checks = 0;
            previousFrame = -1;
            deadline = EditorApplication.timeSinceStartup + 120;
            routine = Exercise();
            EditorApplication.update += Tick;
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.update -= Tick;
            SessionState.SetBool(Running, false);
            if (SessionState.GetBool("PlayerLocomotionChecks.Batch", false))
            {
                SessionState.SetBool("PlayerLocomotionChecks.Batch", false);
                EditorApplication.Exit(SessionState.GetBool("PlayerLocomotionChecks.Passed", false) ? 0 : 1);
            }
            else
            {
                string previousScene = SessionState.GetString("PlayerLocomotionChecks.PreviousScene", "");
                if (!string.IsNullOrEmpty(previousScene))
                    EditorSceneManager.OpenScene(previousScene);
            }
        }
    }

    private static void Tick()
    {
        try
        {
            if (EditorApplication.timeSinceStartup > deadline)
                throw new TimeoutException("Locomotion checks exceeded 120 seconds.");
            if (previousFrame == Time.frameCount)
                return;

            previousFrame = Time.frameCount;
            if (routine.MoveNext())
                return;

            Debug.Log($"[Player Locomotion Play] PASS: {checks} checks.");
            Finish(true);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Finish(false);
        }
    }

    private static void Finish(bool passed)
    {
        SessionState.SetBool("PlayerLocomotionChecks.Passed", passed);
        EditorApplication.update -= Tick;
        EditorApplication.isPlaying = false;
    }

    private static IEnumerator Exercise()
    {
        Time.timeScale = 1f;
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Check floor";
        floor.layer = LayerMask.NameToLayer("Ground");
        floor.transform.position = new Vector3(0f, -0.5f, 0f);
        floor.transform.localScale = new Vector3(50f, 1f, 50f);
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "Check wall";
        wall.layer = floor.layer;
        wall.transform.position = new Vector3(0f, 1.5f, 3f);
        wall.transform.localScale = new Vector3(4f, 3f, 0.3f);
        var step = GameObject.CreatePrimitive(PrimitiveType.Cube);
        step.layer = floor.layer;
        step.transform.position = new Vector3(-10f, 0.1f, 1f);
        step.transform.localScale = new Vector3(4f, 0.2f, 4f);
        var ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ramp.layer = floor.layer;
        ramp.transform.position = new Vector3(20f, 1f, 0f);
        ramp.transform.rotation = Quaternion.Euler(-20f, 0f, 0f);
        ramp.transform.localScale = new Vector3(4f, 0.2f, 10f);

        var fixture = new GameObject("Isolated movement fixture");
        fixture.SetActive(false);
        var actor = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), fixture.transform);
        foreach (var script in actor.GetComponentsInChildren<MonoBehaviour>(true))
            if (script is not CharacterMotor && script is not CharacterRootMotion)
                Object.DestroyImmediate(script);

        var driver = actor.AddComponent<LocomotionCheckInput>();
        var motor = actor.GetComponent<CharacterMotor>();
        var animator = motor.Animator;
        actor.transform.position = new Vector3(6f, 1.04f, 0f);
        fixture.SetActive(true);
        float until = Time.time + 0.5f;
        while (Time.time < until)
            yield return null;
        Require(motor.IsGrounded, "Character grounds on a solid floor without Rigidbody collision callbacks.");
        Vector3 start = actor.transform.position;
        until = Time.time + 0.6f;
        while (Time.time < until)
            yield return null;
        Require(Vector3.Distance(start, actor.transform.position) < 0.08f, "Idle remains stationary.");

        driver.Input = Vector2.up;
        start = actor.transform.position;
        float walkEnd = Time.time + 1.2f;
        until = Time.time + 0.2f;
        while (Time.time < until)
            yield return null;
        Require(animator.GetCurrentAnimatorClipInfo(0).Count(info => info.weight > 0.05f) >= 2,
            "Idle and walking contribute real animation weights during acceleration.");
        Require(motor.AnimationInput.y > 0.1f && motor.AnimationInput.y < 0.95f,
            "Movement eases through intermediate blend values instead of reaching the endpoint immediately.");
        until = walkEnd;
        while (Time.time < until)
            yield return null;
        float walked = actor.transform.position.z - start.z;
        Debug.Log($"[Player Locomotion Play] Forward walk distance: {walked:F3}m");
        Require(walked > 0.5f && walked < 5f, "Walk clip moves the capsule forward at a plausible scale.");
        Require(Mathf.Abs(actor.transform.position.x - start.x) < 0.15f, "Forward walk does not drift sideways.");
        Require(Vector3.Distance(animator.transform.localPosition, Vector3.down) < 0.02f,
            "Visual body stays attached to the collision capsule.");

        driver.Input = Vector2.zero;
        until = Time.time + 0.4f;
        while (Time.time < until)
            yield return null;
        start = actor.transform.position;
        until = Time.time + 0.4f;
        while (Time.time < until)
            yield return null;
        Require(Vector3.Distance(start, actor.transform.position) < 0.08f, "Releasing movement returns to stationary idle.");

        driver.Input = Vector2.up;
        until = Time.time + 1f;
        while (Time.time < until)
            yield return null;
        driver.Sprint = true;
        start = actor.transform.position;
        float runEnd = Time.time + 1.2f;
        until = Time.time + 0.2f;
        while (Time.time < until)
            yield return null;
        Require(animator.GetCurrentAnimatorClipInfo(0).Count(info => info.weight > 0.05f) >= 2,
            "Walking blends into running without restarting the locomotion state.");
        until = runEnd;
        while (Time.time < until)
            yield return null;
        float ran = actor.transform.position.z - start.z;
        Debug.Log($"[Player Locomotion Play] Run distance: {ran:F3}m");
        Require(ran > walked * 1.15f, "Running gains speed from the running clip.");
        driver.Sprint = false;

        foreach (Vector2 direction in new[] { Vector2.left, Vector2.right, Vector2.down })
        {
            motor.Teleport(new Vector3(6f, 1.04f, 0f), Quaternion.identity);
            driver.Input = direction;
            start = actor.transform.position;
            until = Time.time + 1.2f;
            while (Time.time < until)
                yield return null;
            Vector3 travel = actor.transform.position - start;
            Require(Vector3.Dot(travel, new Vector3(direction.x, 0f, direction.y)) > 0.4f,
                "Directional clip moves in its requested direction: " + direction);
        }

        motor.Teleport(new Vector3(6f, 1.04f, 0f), Quaternion.Euler(0f, 90f, 0f));
        driver.Input = Vector2.up;
        start = actor.transform.position;
        until = Time.time + 1.2f;
        while (Time.time < until)
            yield return null;
        Require(actor.transform.position.x - start.x > 0.5f && Mathf.Abs(actor.transform.position.z - start.z) < 0.15f,
            "Root displacement respects actor facing without applying rotation twice.");

        driver.Input = new Vector2(5f, 5f);
        until = Time.time + 0.5f;
        while (Time.time < until)
            yield return null;
        Require(motor.AnimationInput.magnitude <= 1.01f, "Oversized and diagonal input is clamped before animation blending.");
        driver.Input = Vector2.up;

        motor.Teleport(new Vector3(0f, 1.04f, 0f), Quaternion.identity);
        until = Time.time + 3.5f;
        while (Time.time < until)
            yield return null;
        Require(actor.transform.position.z > 1f && actor.transform.position.z < 2.65f,
            "A solid wall blocks root motion.");

        driver.Input = Vector2.zero;
        motor.Teleport(new Vector3(6f, 1.04f, 0f), Quaternion.identity);
        until = Time.time + 0.4f;
        while (Time.time < until)
            yield return null;
        float groundHeight = actor.transform.position.y;
        motor.RequestJump();
        float apex = groundHeight;
        until = Time.time + 1.8f;
        while (Time.time < until)
        {
            apex = Mathf.Max(apex, actor.transform.position.y);
            yield return null;
        }
        Require(apex - groundHeight > 0.7f, "Buffered jump produces an upward arc.");
        Require(motor.IsGrounded && Mathf.Abs(actor.transform.position.y - groundHeight) < 0.1f,
            "Gravity returns the player to grounded idle.");

        motor.AddImpulse(Vector3.right * 3f + Vector3.up * 2f);
        start = actor.transform.position;
        until = Time.time + 0.4f;
        while (Time.time < until)
            yield return null;
        Require(actor.transform.position.x - start.x > 0.2f, "External knockback works without a Rigidbody.");

        motor.Teleport(new Vector3(-10f, 1.04f, -2f), Quaternion.identity);
        driver.Input = Vector2.up;
        until = Time.time + 3f;
        while (Time.time < until)
            yield return null;
        Require(actor.transform.position.z > -0.8f && actor.transform.position.y > 1.1f,
            "The motor climbs a step within its configured step height.");

        motor.Teleport(new Vector3(20f, 1.04f, -4.5f), Quaternion.identity);
        until = Time.time + 4f;
        while (Time.time < until)
            yield return null;
        Require(actor.transform.position.z > -2f && actor.transform.position.y > 1.4f,
            "The motor climbs a walkable slope.");

        driver.AllowsMovement = false;
        driver.Input = Vector2.up;
        motor.Stop();
        start = actor.transform.position;
        until = Time.time + 0.6f;
        while (Time.time < until)
            yield return null;
        Require(Mathf.Abs(actor.transform.position.z - start.z) < 0.02f, "Menu/death movement gate suppresses root travel.");
        motor.Teleport(new Vector3(10f, 1.04f, -3f), Quaternion.identity);
        until = Time.time + 0.4f;
        while (Time.time < until)
            yield return null;
        Require(Mathf.Abs(actor.transform.position.x - 10f) < 0.02f && Mathf.Abs(actor.transform.position.z + 3f) < 0.02f,
            "Teleport does not resume stale root displacement.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
        checks++;
    }
}

public class LocomotionCheckInput : MonoBehaviour
{
    public Vector2 Input;
    public bool Sprint;
    public bool AllowsMovement = true;

    private void Update() => GetComponent<CharacterMotor>().Tick(Input, Sprint, AllowsMovement, Time.deltaTime);
}
