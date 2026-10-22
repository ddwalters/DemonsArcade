using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class PlayerLocomotionChecks
{
    private const string Running = "PlayerLocomotionChecks.Running";
    private const string Batch = "PlayerLocomotionChecks.Batch";
    private const string PrefabPath = "Assets/_Game/Features/Player/Prefabs/player.prefab";
    private const string ControllerPath = "Assets/_Game/Features/Player/Animations/Controllers/PlayerLocomotion.controller";
    private const string ClipRoot = "Assets/_Game/Shared/Animations/Humanoid/Clips/Locomotion/";

    private static readonly Stack<IEnumerator> routines = new Stack<IEnumerator>();
    private static int checks;
    private static int previousFrame;
    private static double deadline;

    static PlayerLocomotionChecks() => EditorApplication.playModeStateChanged += Changed;

    [MenuItem("Tools/Validation/Player Locomotion Assets")]
    public static void CheckAssets()
    {
        checks = 0;
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Require(prefab != null, "Player prefab is available.");
        if (prefab == null)
            return;

        var capsule = prefab.GetComponent<CharacterController>();
        var motor = prefab.GetComponent<CharacterMotor>();
        Require(capsule != null && capsule.enabled && motor != null, "Player has one enabled collision motor.");
        if (capsule == null || motor == null)
            return;
        Require(prefab.GetComponent<Rigidbody>() == null && prefab.GetComponent<CapsuleCollider>() == null,
            "Player collision is owned by its CharacterController.");
        Require(prefab.GetComponent<MeshRenderer>() == null && prefab.GetComponent<MeshFilter>() == null,
            "The old root capsule mesh is removed.");

        var body = prefab.transform.Find("BodyModel");
        Require(body != null && Vector3.Distance(body.localPosition, Vector3.down) < 0.01f,
            "The visual body stays aligned with the capsule feet.");
        var animator = motor.Animator;
        Require(animator == null || (animator.avatar != null && animator.avatar.isHuman && animator.avatar.isValid),
            "An optional player Animator uses a valid Humanoid Avatar when present.");
        Require(animator == null || !animator.applyRootMotion,
            "Animator playback never moves the collision root.");
        Require(animator == null || animator.runtimeAnimatorController != null,
            "The optional player Animator has a locomotion controller.");
        if (animator != null)
        {
            var motorData = new SerializedObject(motor);
            Require(motorData.FindProperty("animator").objectReferenceValue == animator,
                "The motor's optional Animator reference points to the body.");
        }

        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        Require(controller != null, "The locomotion controller asset is available.");
        Require(controller.parameters.Any(parameter =>
                parameter.name == "MoveX" && parameter.type == AnimatorControllerParameterType.Float) &&
            controller.parameters.Any(parameter =>
                parameter.name == "MoveY" && parameter.type == AnimatorControllerParameterType.Float) &&
            controller.parameters.Any(parameter =>
                parameter.name == "Grounded" && parameter.type == AnimatorControllerParameterType.Bool),
            "The controller exposes speed and grounded parameters.");

        var blend = AssetDatabase.LoadAllAssetsAtPath(ControllerPath).OfType<BlendTree>().FirstOrDefault();
        Require(blend != null && blend.blendType == BlendTreeType.Simple1D && blend.blendParameter == "MoveY",
            "Ground locomotion blends from one speed value.");
        if (blend != null)
        {
            var children = blend.children;
            Require(children.Length == 3 &&
                Mathf.Approximately(children[0].threshold, 0f) &&
                Mathf.Approximately(children[1].threshold, 1f) &&
                Mathf.Approximately(children[2].threshold, 2f),
                "Idle, walk, and run use speed thresholds 0, 1, and 2.");
            string[] paths = children.Select(child => AssetDatabase.GetAssetPath(child.motion)).ToArray();
            Require(paths.Contains(ClipRoot + "CalmIdle.fbx") &&
                paths.Contains(ClipRoot + "Walking.fbx") &&
                paths.Contains(ClipRoot + "Running.fbx"),
                "The speed blend uses idle, walking, and running clips.");
        }

        var camera = prefab.GetComponentInChildren<Camera>(true);
        Require(camera != null && (camera.cullingMask & (1 << prefab.layer)) == 0,
            "The player camera is configured for the third-person view.");
        var cameraRig = prefab.GetComponent<PlayerCameraRig>();
        Require(cameraRig != null && cameraRig.IsThirdPerson,
            "Player camera remains in third person.");
        Debug.Log($"[Player Locomotion Assets] PASS: {checks} checks.");
    }

    [MenuItem("Tools/Validation/Player Locomotion (Play Mode)")]
    public static void Run()
    {
        BeginPlayModeChecks(false);
    }

    public static void RunBatch()
    {
        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Exit Play Mode before running batch validation.");
        if (EditorSceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("Batch validation requires a clean active scene.");

        CheckAssets();
        PlayerFeatureChecks.RunAll();
        SaveFeatureChecks.RunAll();
        SessionState.SetBool(Batch, true);
        BeginPlayModeChecks(true);
    }

    private static void BeginPlayModeChecks(bool batch)
    {
        if (EditorApplication.isPlaying)
            return;
        if (!batch && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        if (!batch)
            CheckAssets();
        SessionState.SetString("PlayerLocomotionChecks.PreviousScene",
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().path);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        SessionState.SetBool(Running, true);
        EditorApplication.isPlaying = true;
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
            routines.Clear();
            routines.Push(Exercise());
            EditorApplication.update += Tick;
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.update -= Tick;
            SessionState.SetBool(Running, false);
            if (SessionState.GetBool(Batch, false))
            {
                SessionState.SetBool(Batch, false);
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
            while (routines.Count > 0)
            {
                var current = routines.Peek();
                if (!current.MoveNext())
                {
                    routines.Pop();
                    continue;
                }

                if (current.Current is IEnumerator nested)
                {
                    routines.Push(nested);
                    continue;
                }

                return;
            }

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
        routines.Clear();
        EditorApplication.update -= Tick;
        EditorApplication.isPlaying = false;
    }

    private static IEnumerator Exercise()
    {
        Time.timeScale = 1f;
        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer < 0)
            groundLayer = 0;

        var floor = Cube("Check floor", new Vector3(0f, -0.5f, 0f), new Vector3(50f, 1f, 50f), groundLayer);
        var wall = Cube("Check wall", new Vector3(0f, 1.5f, 3f), new Vector3(4f, 3f, 0.3f), groundLayer);
        var step = Cube("Check step", new Vector3(-10f, 0.1f, 1f), new Vector3(4f, 0.2f, 4f), groundLayer);
        var ramp = Cube("Check slope", new Vector3(20f, 1f, 0f), new Vector3(4f, 0.2f, 10f), groundLayer);
        ramp.transform.rotation = Quaternion.Euler(-20f, 0f, 0f);
        var ledge = Cube("Coyote ledge", new Vector3(30f, -0.5f, 0f), new Vector3(4f, 1f, 2f), groundLayer);

        var fixture = new GameObject("Isolated movement fixtures");
        fixture.SetActive(false);
        var actor = (GameObject)Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), fixture.transform);
        actor.name = "Player with disabled animator fixture";
        foreach (var script in actor.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (script is not CharacterMotor && script is not PlayerCameraRig)
                Object.DestroyImmediate(script);
        }

        var motor = actor.GetComponent<CharacterMotor>();
        var animator = motor.Animator;
        var cameraRig = actor.GetComponent<PlayerCameraRig>();
        if (cameraRig != null)
            cameraRig.enabled = false;
        var driver = actor.AddComponent<LocomotionCheckInput>();
        driver.Configure(motor, cameraRig, true);

        var bareActor = new GameObject("Capsule without Animator fixture");
        bareActor.transform.SetParent(fixture.transform, false);
        bareActor.transform.position = new Vector3(14f, 1.04f, 0f);
        var bareController = bareActor.AddComponent<CharacterController>();
        ConfigureCapsule(bareController);
        var bareMotor = bareActor.AddComponent<CharacterMotor>();
        var bareDriver = bareActor.AddComponent<LocomotionCheckInput>();
        bareDriver.Configure(bareMotor, null, false);

        actor.transform.position = new Vector3(6f, 1.04f, 0f);
        fixture.SetActive(true);
        cameraRig.ResetView();
        yield return Wait(0.45f);
        Require(motor.IsGrounded && bareMotor.IsGrounded,
            "Both the visual player and a capsule without an Animator ground on a solid floor.");

        driver.Input = Vector2.zero;
        Vector3 start = actor.transform.position;
        yield return Wait(0.3f);
        Require(PlanarDistance(start, actor.transform.position) < 0.04f,
            "Idle holds position while the motor has no movement input.");

        bareDriver.Input = Vector2.up;
        start = bareActor.transform.position;
        yield return Wait(0.8f);
        Require(bareMotor.Animator == null && bareActor.transform.position.z - start.z > 1.1f,
            "A capsule with no Animator travels under motor control.");
        Require(bareMotor.PlanarSpeed > 1.9f,
            "Animator-free movement reaches the configured walk speed.");
        bareDriver.Input = Vector2.zero;

        if (animator != null)
        {
            animator.enabled = false;
            cameraRig.ResetView();
            driver.Input = Vector2.up;
            start = actor.transform.position;
            yield return Wait(0.8f);
            Require(actor.transform.position.z - start.z > 1.1f,
                "Disabling the body Animator does not stop capsule travel.");
            animator.enabled = true;
            animator.speed = 1f;
            yield return Wait(0.2f);
        }

        Vector3 walkStart = new Vector3(6f, 1.04f, 0f);
        motor.Teleport(walkStart, Quaternion.identity);
        cameraRig.ResetView();
        if (animator != null)
            animator.speed = 0.25f;
        driver.Input = Vector2.up;
        driver.Sprint = false;
        start = actor.transform.position;
        yield return Wait(0.9f);
        float slowAnimationTravel = PlanarDistance(start, actor.transform.position);
        float walkSpeed = motor.ActualPlanarSpeed;
        Require(walkSpeed > 1.9f && walkSpeed < 2.4f,
            "Walking reaches the configured speed near 2.2 m/s.");
        Require(motor.AnimationInput.x == 0f && motor.AnimationInput.y > 0.9f && motor.AnimationInput.y < 1.1f,
            "Walking reports a forward gait value near one.");

        motor.Teleport(walkStart, Quaternion.identity);
        cameraRig.ResetView();
        if (animator != null)
            animator.speed = 3f;
        start = actor.transform.position;
        yield return Wait(0.9f);
        float fastAnimationTravel = PlanarDistance(start, actor.transform.position);
        Require(fastAnimationTravel > 1.4f && Mathf.Abs(fastAnimationTravel - slowAnimationTravel) < 0.2f,
            "Changing Animator playback speed does not change measured travel.");
        if (animator != null)
            animator.speed = 1f;

        Vector3 diagonalStart = new Vector3(6f, 1.04f, 0f);
        motor.Teleport(diagonalStart, Quaternion.identity);
        cameraRig.ResetView();
        driver.Input = new Vector2(1f, 1f);
        yield return Wait(0.55f);
        Require(motor.PlanarSpeed <= 2.3f && motor.ActualPlanarSpeed <= 2.4f,
            "Full diagonal input stays within the walk speed cap.");

        driver.Input = new Vector2(0f, 0.5f);
        yield return Wait(0.35f);
        Require(motor.PlanarSpeed > 0.8f && motor.PlanarSpeed < 1.3f,
            "Half-strength analog input produces proportionally lower movement speed.");

        driver.Input = Vector2.up;
        driver.Sprint = true;
        yield return Wait(0.7f);
        Require(motor.ActualPlanarSpeed > 4.5f && motor.ActualPlanarSpeed <= 5.1f,
            "Sprinting reaches the configured speed near 5 m/s.");
        Require(motor.AnimationInput.x == 0f && motor.AnimationInput.y > 1.8f && motor.AnimationInput.y <= 2.05f,
            "Sprinting reports a forward gait value near two.");
        driver.Sprint = false;

        if (animator != null)
        {
            animator.speed = 1f;
            yield return CheckPoseDrift(actor, animator, driver, false, "Walking");
            yield return CheckPoseDrift(actor, animator, driver, true, "Running");
            driver.Sprint = false;
            Require(Vector3.Distance(animator.transform.localPosition, Vector3.down) < 0.02f,
                "The animated body remains attached to the collision capsule.");
        }

        cameraRig.ResetView();
        cameraRig.Look(new Vector2(90f, 0f));
        Vector3 cameraForward = cameraRig.GetMovementDirection(Vector2.up).normalized;
        Require(cameraForward.x > 0.99f && Mathf.Abs(cameraForward.z) < 0.01f,
            "Camera-relative forward follows a quarter-turn camera yaw.");

        motor.Teleport(walkStart, Quaternion.identity);
        driver.Input = Vector2.up;
        start = actor.transform.position;
        yield return Wait(0.65f);
        Vector3 forwardTravel = actor.transform.position - start;
        Require(forwardTravel.x > 0.9f && Mathf.Abs(forwardTravel.z) < 0.1f,
            "Camera-relative forward input moves in the camera's world direction.");
        Require(Vector3.Dot(actor.transform.forward, cameraForward) > 0.98f,
            "The motor turns the player toward camera-relative forward input.");
        Require(Vector3.Dot(cameraRig.GetMovementDirection(Vector2.up).normalized, cameraForward) > 0.99f,
            "The camera's world yaw stays fixed while the motor turns the player.");

        driver.Input = Vector2.down;
        start = actor.transform.position;
        yield return Wait(0.65f);
        Vector3 backwardTravel = actor.transform.position - start;
        Require(backwardTravel.x < -0.9f && Mathf.Abs(backwardTravel.z) < 0.1f,
            "Camera-relative backward input moves in its requested world direction.");
        Require(Vector3.Dot(actor.transform.forward, -cameraForward) > 0.98f,
            "Backward input turns the player to face its travel direction.");

        motor.Teleport(new Vector3(0f, 1.04f, 0f), Quaternion.identity);
        cameraRig.ResetView();
        driver.Input = Vector2.up;
        yield return Wait(2.1f);
        Require(actor.transform.position.z > 1.25f && actor.transform.position.z < 2.6f,
            "The CharacterController blocks movement at a wall.");

        driver.Input = new Vector2(1f, 1f);
        start = actor.transform.position;
        yield return Wait(0.75f);
        Require(actor.transform.position.x - start.x > 0.45f &&
            actor.transform.position.z < 2.6f,
            "Diagonal input slides along a wall while the wall blocks forward travel.");

        driver.Input = Vector2.zero;
        start = actor.transform.position;
        yield return Wait(0.35f);
        Require(PlanarDistance(start, actor.transform.position) < 0.06f,
            "Releasing input settles the player against a wall.");

        motor.Teleport(new Vector3(6f, 1.04f, 0f), Quaternion.identity);
        cameraRig.ResetView();
        yield return Wait(0.25f);
        motor.RequestJump();
        driver.Input = Vector2.zero;
        yield return Wait(0.12f);
        Require(actor.transform.position.y > 1.35f,
            "Jump begins with an upward arc from grounded movement.");
        driver.AllowsMovement = false;
        motor.Stop();
        float blockedZ = actor.transform.position.z;
        float blockedY = actor.transform.position.y;
        yield return Wait(0.2f);
        Require(actor.transform.position.y < blockedY && Mathf.Abs(actor.transform.position.z - blockedZ) < 0.06f,
            "Menu or death movement blocking preserves airborne gravity and suppresses horizontal travel.");
        float landingDeadline = Time.time + 1.5f;
        while (!motor.IsGrounded && Time.time < landingDeadline)
            yield return null;
        Require(motor.IsGrounded && Mathf.Abs(actor.transform.position.y - 1.04f) < 0.12f,
            "The player lands while menu or death movement remains blocked.");

        driver.Input = Vector2.up;
        motor.Stop();
        start = actor.transform.position;
        yield return Wait(0.4f);
        Require(PlanarDistance(start, actor.transform.position) < 0.04f,
            "Stop clears grounded momentum while movement is blocked.");
        motor.Teleport(new Vector3(10f, 1.04f, -3f), Quaternion.Euler(0f, 90f, 0f));
        yield return Wait(0.25f);
        Require(Mathf.Abs(actor.transform.position.x - 10f) < 0.03f &&
            Mathf.Abs(actor.transform.position.z + 3f) < 0.03f,
            "Teleport clears stale movement while movement is blocked.");
        driver.AllowsMovement = true;

        var motorData = new SerializedObject(motor);
        var buffer = motorData.FindProperty("jumpBufferTime");
        float originalBuffer = buffer.floatValue;
        buffer.floatValue = 0.5f;
        motorData.ApplyModifiedPropertiesWithoutUndo();
        motor.Teleport(new Vector3(6f, 1.5f, 0f), Quaternion.identity);
        cameraRig.ResetView();
        driver.Input = Vector2.zero;
        yield return Wait(0.05f);
        Require(!motor.IsGrounded, "The buffered-jump fixture is falling before jump input.");
        motor.RequestJump();
        float firstLanding = actor.transform.position.y;
        bool reachedFloor = false;
        bool bufferedAscent = false;
        bool landedAfterBufferedJump = false;
        float jumpDeadline = Time.time + 2f;
        while (Time.time < jumpDeadline)
        {
            float height = actor.transform.position.y;
            firstLanding = Mathf.Min(firstLanding, height);
            if (height < 1.15f)
                reachedFloor = true;
            if (reachedFloor && height > firstLanding + 0.4f)
            {
                bufferedAscent = true;
            }
            if (bufferedAscent && motor.IsGrounded)
            {
                landedAfterBufferedJump = true;
                break;
            }
            yield return null;
        }
        Require(reachedFloor && bufferedAscent && landedAfterBufferedJump,
            "A falling jump request survives landing, produces a second ascent, and lands again.");
        buffer.floatValue = originalBuffer;
        motorData.ApplyModifiedPropertiesWithoutUndo();

        motor.Teleport(new Vector3(30f, 1.04f, 0.5f), Quaternion.identity);
        cameraRig.ResetView();
        yield return Wait(0.2f);
        Require(motor.IsGrounded, "The coyote-time fixture settles onto its platform before walking.");
        driver.Input = Vector2.up;
        float edgeDeadline = Time.time + 1.5f;
        while (motor.IsGrounded && Time.time < edgeDeadline)
            yield return null;
        Require(!motor.IsGrounded, "The coyote-time fixture walks beyond a platform edge.");
        motor.RequestJump();
        float coyoteApex = actor.transform.position.y;
        float coyoteDeadline = Time.time + 0.8f;
        while (Time.time < coyoteDeadline)
        {
            coyoteApex = Mathf.Max(coyoteApex, actor.transform.position.y);
            yield return null;
        }
        Require(coyoteApex > 1.7f,
            "A jump just after leaving a platform uses coyote time.");

        motor.Teleport(new Vector3(6f, 1.04f, 0f), Quaternion.identity);
        cameraRig.ResetView();
        driver.Input = Vector2.up;
        yield return Wait(0.3f);
        motor.AddImpulse(Vector3.right * 3f + Vector3.up * 2f);
        start = actor.transform.position;
        yield return Wait(0.4f);
        Require(actor.transform.position.x - start.x > 0.2f,
            "External knockback works without a Rigidbody.");

        motor.Teleport(new Vector3(-10f, 1.04f, -1.4f), Quaternion.identity);
        cameraRig.ResetView();
        driver.Input = Vector2.up;
        yield return Wait(0.9f);
        Require(actor.transform.position.z > -0.2f && actor.transform.position.y > 1.1f && motor.IsGrounded,
            "The motor climbs a step within its configured step height.");

        motor.Teleport(new Vector3(20f, 1.04f, -4.5f), Quaternion.identity);
        cameraRig.ResetView();
        float rampStartHeight = actor.transform.position.y;
        driver.Input = Vector2.up;
        yield return Wait(3f);
        Require(actor.transform.position.z > -2f && actor.transform.position.y > rampStartHeight + 0.35f,
            "The motor climbs a walkable slope.");

        Debug.Log("[Player Locomotion Play] Exercise completed.");
        Object.Destroy(fixture);
        Object.Destroy(floor);
        Object.Destroy(wall);
        Object.Destroy(step);
        Object.Destroy(ramp);
        Object.Destroy(ledge);
    }

    private static GameObject Cube(string name, Vector3 position, Vector3 scale, int layer)
    {
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.layer = layer;
        cube.transform.position = position;
        cube.transform.localScale = scale;
        return cube;
    }

    private static void ConfigureCapsule(CharacterController capsule)
    {
        capsule.height = 2f;
        capsule.radius = 0.35f;
        capsule.center = Vector3.zero;
        capsule.skinWidth = 0.035f;
        capsule.stepOffset = 0.3f;
        capsule.slopeLimit = 50f;
        capsule.minMoveDistance = 0f;
    }

    private static IEnumerator Wait(float duration)
    {
        float until = Time.time + duration;
        while (Time.time < until)
            yield return null;
    }

    private static IEnumerator CheckPoseDrift(GameObject actor, Animator animator,
        LocomotionCheckInput driver, bool sprint, string gait)
    {
        var hips = animator.GetBoneTransform(HumanBodyBones.Hips);
        Require(hips != null, "Humanoid hips are available for the " + gait + " pose drift check.");
        driver.Input = Vector2.up;
        driver.Sprint = sprint;
        yield return Wait(0.4f);

        float cycleStart = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
        Vector3 hipsAtStart = actor.transform.InverseTransformPoint(hips.position);
        float cycleDeadline = Time.time + 5f;
        while (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < cycleStart + 3f &&
            Time.time < cycleDeadline)
            yield return null;

        Vector3 hipsAfterCycles = actor.transform.InverseTransformPoint(hips.position);
        Require(Mathf.Abs(hipsAfterCycles.z - hipsAtStart.z) < 0.15f,
            gait + " animation does not accumulate forward pose drift across gait cycles.");
    }

    private static float PlanarDistance(Vector3 first, Vector3 second)
    {
        Vector3 difference = second - first;
        difference.y = 0f;
        return difference.magnitude;
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
    public CharacterMotor Motor;
    public PlayerCameraRig CameraRig;
    public Vector2 Input;
    public bool Sprint;
    public bool AllowsMovement = true;
    public bool UseCameraMovement;

    public void Configure(CharacterMotor motor, PlayerCameraRig cameraRig, bool useCameraMovement)
    {
        Motor = motor;
        CameraRig = cameraRig;
        UseCameraMovement = useCameraMovement;
    }

    private void Update()
    {
        Vector3? worldMovement = UseCameraMovement && CameraRig != null
            ? CameraRig.GetMovementDirection(Input)
            : null;
        Motor.Tick(Input, Sprint, AllowsMovement, Time.deltaTime, worldMovement);
    }
}
