using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class JumpMotionReview
{
    private const string PrefabPath = "Assets/_Game/Features/Player/Prefabs/Player.prefab";
    private const string ModelPath = "Assets/Review/Male_yBot_Testing.fbx";
    private const string ControllerPath = "Assets/Review/PlayerLocomotion.controller";
    private const string OutputRoot = "Temp/JumpReview";
    private const int FrameRate = 60;
    private const int DurationSeconds = 3;
    private const int ImageWidth = 768;
    private const int ImageHeight = 576;
    private const int ImageSampleInterval = 6;

    private static bool captureRunning;
    private static bool captureBatchRequested;
    private static bool captureSucceeded;

    private sealed class Scenario
    {
        public string Name;
        public Vector3 StartPosition;
        public bool Walk;
        public bool Sprint;
        public bool LedgeFall;
        public bool RepeatedJump;
        public int JumpFrame = -1;
    }

    private sealed class ScenarioSummary
    {
        public string Name;
        public float PeakHeight;
        public float PeakSpeed;
        public float PeakActualSpeed;
        public float PeakSkinVertexAverageDelta;
        public float PeakSkinVertexMaximumDelta;
        public int GroundedFrames;
        public int JumpRequests;
        public readonly HashSet<string> AnimationClips = new HashSet<string>();
    }

    private sealed class BakedSkin
    {
        public SkinnedMeshRenderer Source;
        public Mesh Mesh;
        public MeshFilter Filter;
        public MeshRenderer Renderer;
    }

    [MenuItem("Tools/Player/Capture Jump Review")]
    public static void Capture()
    {
        if (captureRunning)
        {
            Debug.LogWarning("[Jump Review] A capture is already running.");
            return;
        }

        if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[Jump Review] Exit Play Mode before capturing. The user's open scene is kept in edit mode.");
            return;
        }

        captureRunning = true;
        captureSucceeded = false;
        string runDirectory = Path.Combine(OutputRoot, "UnityRuntime_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        Scene originalActiveScene = SceneManager.GetActiveScene();
        Scene reviewScene = default;
        Camera captureCamera = null;
        RenderTexture renderTexture = null;
        Material floorMaterial = null;
        Material gridMaterial = null;
        Material edgeMaterial = null;
        BakedSkin[] bakedSkins = null;
        var summaries = new List<ScenarioSummary>();

        try
        {
            Directory.CreateDirectory(runDirectory);
            NewSceneMode sceneMode = captureBatchRequested ? NewSceneMode.Single : NewSceneMode.Additive;
            reviewScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, sceneMode);
            if (!reviewScene.IsValid() || !reviewScene.isLoaded)
                throw new InvalidOperationException("Unity did not create the temporary review scene.");

            SceneManager.SetActiveScene(reviewScene);
            BuildRenderEnvironment(reviewScene, out captureCamera, out floorMaterial, out gridMaterial, out edgeMaterial);
            renderTexture = new RenderTexture(ImageWidth, ImageHeight, 24, RenderTextureFormat.ARGB32)
            {
                name = "Jump Review Capture",
                antiAliasing = 1,
                useMipMap = false,
                autoGenerateMips = false
            };
            renderTexture.Create();
            captureCamera.targetTexture = renderTexture;

            GameObject actor = CreatePlayerClone(reviewScene);
            CharacterMotor motor = actor.GetComponent<CharacterMotor>();
            Animator animator = actor.GetComponentInChildren<Animator>(true);
            if (motor == null || animator == null)
                throw new InvalidOperationException("The player prefab must contain CharacterMotor and Animator components.");

            bakedSkins = PrepareBakedSkins(actor);

            var stateNames = BuildAnimatorStateNames(animator);
            var scenarios = CreateScenarios();
            for (int scenarioIndex = 0; scenarioIndex < scenarios.Count; scenarioIndex++)
            {
                Scenario scenario = scenarios[scenarioIndex];
                BuildGround(reviewScene, scenario, floorMaterial, gridMaterial, edgeMaterial);
                Physics.SyncTransforms();
                PreparePlayer(actor, motor, animator, scenario.StartPosition);
                summaries.Add(CaptureScenario(actor, motor, animator, captureCamera, renderTexture,
                    bakedSkins, scenario, stateNames, runDirectory, scenarioIndex, scenarios.Count));
            }

            WriteSummary(runDirectory, summaries);
            File.WriteAllText(Path.Combine(OutputRoot, "latest.txt"), Path.GetFileName(runDirectory));
            captureSucceeded = true;
            Debug.Log($"[Jump Review] Captured five scenarios at {FrameRate} Hz. Output: {Path.GetFullPath(runDirectory)}");
            foreach (ScenarioSummary summary in summaries)
            {
                Debug.Log($"[Jump Review] {summary.Name}: peak root rise {summary.PeakHeight:F2} m, " +
                    $"peak motor speed {summary.PeakSpeed:F2} m/s, peak actual speed {summary.PeakActualSpeed:F2} m/s, " +
                    $"grounded frames {summary.GroundedFrames}, baked skin vertex delta avg/max " +
                    $"{summary.PeakSkinVertexAverageDelta:F3}/{summary.PeakSkinVertexMaximumDelta:F3} m, " +
                    $"clips {string.Join(" | ", summary.AnimationClips)}");
            }
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            if (captureCamera != null)
                captureCamera.targetTexture = null;
            if (renderTexture != null)
            {
                renderTexture.Release();
                UnityEngine.Object.DestroyImmediate(renderTexture);
            }
            DestroyTemporaryMaterial(floorMaterial);
            DestroyTemporaryMaterial(gridMaterial);
            DestroyTemporaryMaterial(edgeMaterial);
            CleanupBakedSkins(bakedSkins);

            if (originalActiveScene.IsValid() && originalActiveScene.isLoaded)
                SceneManager.SetActiveScene(originalActiveScene);
            if (!captureBatchRequested && reviewScene.IsValid() && reviewScene.isLoaded)
            {
                EditorSceneManager.CloseScene(reviewScene, true);
            }
            if (originalActiveScene.IsValid() && originalActiveScene.isLoaded)
                SceneManager.SetActiveScene(originalActiveScene);
            captureRunning = false;
        }
    }

    public static void CaptureBatch()
    {
        if (!Application.isBatchMode)
            throw new InvalidOperationException("Use the capture menu in an interactive editor; batch capture requires a separate batch process.");

        captureBatchRequested = true;
        try
        {
            Capture();
            if (!captureSucceeded)
                throw new InvalidOperationException("Jump review capture failed; inspect the preceding exception in the editor log.");
        }
        finally
        {
            captureBatchRequested = false;
        }
    }

    [MenuItem("Tools/Player/Capture Jump Review", true)]
    private static bool CanCapture() => !captureRunning && !EditorApplication.isPlayingOrWillChangePlaymode;

    private static void BuildRenderEnvironment(Scene scene, out Camera camera,
        out Material floorMaterial, out Material gridMaterial, out Material edgeMaterial)
    {
        floorMaterial = CreateMaterial("Jump Review Floor", new Color(0.075f, 0.095f, 0.12f));
        gridMaterial = CreateMaterial("Jump Review Grid", new Color(0.14f, 0.17f, 0.2f));
        edgeMaterial = CreateMaterial("Jump Review Ledge Edge", new Color(0.95f, 0.42f, 0.12f));

        var lightObject = new GameObject("Jump Review Key Light");
        SceneManager.MoveGameObjectToScene(lightObject, scene);
        Light keyLight = lightObject.AddComponent<Light>();
        keyLight.type = LightType.Directional;
        keyLight.intensity = 1.7f;
        keyLight.shadows = LightShadows.Soft;
        lightObject.transform.rotation = Quaternion.Euler(42f, -32f, 0f);

        var cameraObject = new GameObject("Jump Review Camera");
        SceneManager.MoveGameObjectToScene(cameraObject, scene);
        camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.055f, 0.07f, 0.09f);
        camera.orthographic = true;
        camera.orthographicSize = 2.2f;
        camera.nearClipPlane = 0.05f;
        camera.farClipPlane = 80f;
        camera.cullingMask = ~0;
        camera.useOcclusionCulling = false;
        camera.allowHDR = false;
        camera.allowMSAA = false;
        if (GraphicsSettings.currentRenderPipeline != null)
        {
            Type additionalCameraData = Type.GetType(
                "UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
            if (additionalCameraData != null)
                cameraObject.AddComponent(additionalCameraData);
        }
    }

    private static GameObject CreatePlayerClone(Scene scene)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        var staging = new GameObject("Jump Review Inactive Staging");
        SceneManager.MoveGameObjectToScene(staging, scene);
        staging.SetActive(false);

        GameObject actor;
        if (prefab != null)
        {
            actor = UnityEngine.Object.Instantiate(prefab, staging.transform, false);
        }
        else
        {
            GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            RuntimeAnimatorController controllerAsset = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
            if (modelAsset == null || controllerAsset == null)
                throw new FileNotFoundException("Neither the player prefab nor the standalone review model/controller could be loaded.");

            actor = new GameObject("Jump Review Player");
            actor.transform.SetParent(staging.transform, false);
            GameObject body = UnityEngine.Object.Instantiate(modelAsset, actor.transform, false);
            body.name = "BodyModel";
            body.transform.localPosition = Vector3.down;
            body.transform.localRotation = Quaternion.identity;
            body.transform.localScale = Vector3.one;
            Animator bodyAnimator = body.GetComponent<Animator>();
            if (bodyAnimator == null)
                bodyAnimator = body.AddComponent<Animator>();
            bodyAnimator.runtimeAnimatorController = controllerAsset;

            CharacterController capsule = actor.AddComponent<CharacterController>();
            capsule.height = 2f;
            capsule.radius = 0.35f;
            capsule.center = Vector3.zero;
            capsule.skinWidth = 0.035f;
            capsule.stepOffset = 0.3f;
            capsule.slopeLimit = 50f;
            capsule.minMoveDistance = 0f;
            actor.AddComponent<CharacterMotor>();
        }
        actor.name = "Jump Review Player";
        actor.SetActive(false);

        CharacterMotor motor = actor.GetComponent<CharacterMotor>();
        if (motor == null)
            throw new InvalidOperationException("The player prefab has no CharacterMotor component.");

        foreach (MonoBehaviour behaviour in actor.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (behaviour != null && behaviour != motor)
                UnityEngine.Object.DestroyImmediate(behaviour);
        }
        foreach (Camera gameplayCamera in actor.GetComponentsInChildren<Camera>(true))
            gameplayCamera.enabled = false;
        foreach (AudioListener listener in actor.GetComponentsInChildren<AudioListener>(true))
            listener.enabled = false;

        actor.transform.SetParent(null, false);
        SceneManager.MoveGameObjectToScene(actor, scene);
        UnityEngine.Object.DestroyImmediate(staging);
        actor.SetActive(true);
        return actor;
    }

    private static List<Scenario> CreateScenarios()
    {
        const float firstX = 5000f;
        const float firstZ = 5000f;
        const float spacing = 32f;
        return new List<Scenario>
        {
            new Scenario { Name = "IdleJump", StartPosition = new Vector3(firstX, 1.04f, firstZ), JumpFrame = 24 },
            new Scenario { Name = "WalkJump", StartPosition = new Vector3(firstX + spacing, 1.04f, firstZ), Walk = true, JumpFrame = 36 },
            new Scenario { Name = "SprintJump", StartPosition = new Vector3(firstX + spacing * 2f, 1.04f, firstZ), Walk = true, Sprint = true, JumpFrame = 36 },
            new Scenario { Name = "LedgeFall", StartPosition = new Vector3(firstX + spacing * 3f, 1.04f, firstZ - 1.1f), Walk = true, LedgeFall = true },
            new Scenario { Name = "RepeatedJump", StartPosition = new Vector3(firstX + spacing * 4f, 1.04f, firstZ), RepeatedJump = true, JumpFrame = 24 }
        };
    }

    private static void BuildGround(Scene scene, Scenario scenario, Material floorMaterial,
        Material gridMaterial, Material edgeMaterial)
    {
        Vector3 start = scenario.StartPosition;
        if (scenario.LedgeFall)
        {
            CreateCube(scene, "Ledge", new Vector3(start.x, -0.15f, start.z + 1.1f),
                new Vector3(8f, 0.3f, 3.2f), floorMaterial, true);
            CreateCube(scene, "Lower Landing", new Vector3(start.x, -2.05f, start.z + 4f),
                new Vector3(16f, 0.1f, 24f), floorMaterial, true);
            CreateCube(scene, "Ledge Marker", new Vector3(start.x, 0.012f, start.z + 2.7f),
                new Vector3(8f, 0.02f, 0.035f), edgeMaterial, false);
            AddGrid(scene, start.x, start.z + 1.1f, gridMaterial, 6f, 10f);
            return;
        }

        CreateCube(scene, "Ground", new Vector3(start.x, -0.05f, start.z + 35f),
            new Vector3(12f, 0.1f, 80f), floorMaterial, true);
        AddGrid(scene, start.x, start.z + 2f, gridMaterial, 6f, 10f);
    }

    private static void AddGrid(Scene scene, float centerX, float centerZ, Material material,
        float halfWidth, float halfDepth)
    {
        const float y = 0.004f;
        for (int x = -5; x <= 5; x++)
        {
            CreateCube(scene, "Grid Line", new Vector3(centerX + x, y, centerZ),
                new Vector3(0.012f, 0.008f, halfDepth * 2f), material, false);
        }
        for (int z = -5; z <= 5; z++)
        {
            CreateCube(scene, "Grid Line", new Vector3(centerX, y, centerZ + z),
                new Vector3(halfWidth * 2f, 0.008f, 0.012f), material, false);
        }
    }

    private static GameObject CreateCube(Scene scene, string name, Vector3 position, Vector3 scale,
        Material material, bool hasCollider)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        SceneManager.MoveGameObjectToScene(cube, scene);
        cube.name = name;
        cube.transform.position = position;
        cube.transform.localScale = scale;
        Renderer renderer = cube.GetComponent<Renderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        if (!hasCollider)
            UnityEngine.Object.DestroyImmediate(cube.GetComponent<Collider>());
        return cube;
    }

    private static void PreparePlayer(GameObject actor, CharacterMotor motor, Animator animator, Vector3 position)
    {
        actor.transform.SetPositionAndRotation(position, Quaternion.identity);
        actor.SetActive(true);
        animator.enabled = true;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        animator.updateMode = AnimatorUpdateMode.Normal;
        animator.speed = 1f;

        MethodInfo awake = typeof(CharacterMotor).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
        if (awake == null)
            throw new MissingMethodException(typeof(CharacterMotor).FullName, "Awake");
        awake.Invoke(motor, null);

        animator.Rebind();
        motor.Teleport(position, Quaternion.identity);
        animator.Update(0f);
        Physics.SyncTransforms();

        // Let the Animator evaluate its grounded entry transition before frame zero is captured.
        // Teleport intentionally resets the motor to airborne until its first physics tick, so
        // capturing immediately would record a false takeoff/landing transition at spawn.
        const int warmupFrames = 30;
        const float warmupDeltaTime = 1f / FrameRate;
        for (int frame = 0; frame < warmupFrames; frame++)
        {
            motor.Tick(Vector2.zero, false, true, warmupDeltaTime, Vector3.forward);
            animator.Update(warmupDeltaTime);
            Physics.SyncTransforms();
        }

        if (!motor.IsGrounded)
            throw new InvalidOperationException("The review actor did not settle onto the test ground during warm-up.");
    }

    private static ScenarioSummary CaptureScenario(GameObject actor, CharacterMotor motor, Animator animator,
        Camera camera, RenderTexture renderTexture, BakedSkin[] bakedSkins,
        Scenario scenario, Dictionary<int, string> stateNames,
        string runDirectory, int scenarioIndex, int scenarioCount)
    {
        const float deltaTime = 1f / FrameRate;
        int totalFrames = FrameRate * DurationSeconds;
        string scenarioDirectory = Path.Combine(runDirectory, scenario.Name);
        string framesDirectory = Path.Combine(scenarioDirectory, "frames");
        Directory.CreateDirectory(framesDirectory);

        ConfigureCamera(camera, scenario.StartPosition, scenario.LedgeFall);
        var summary = new ScenarioSummary { Name = scenario.Name };
        var imagePaths = new List<string>();
        Vector3[][] baselineSkinVertices = new Vector3[bakedSkins.Length][];
        bool wasAirborne = false;
        int launches = 0;
        bool primaryTimeline = scenario.Name == "IdleJump" ||
            scenario.Name == "WalkJump" || scenario.Name == "SprintJump";
        int scenarioImageSampleInterval = primaryTimeline ? ImageSampleInterval : 30;

        using (var writer = new StreamWriter(Path.Combine(scenarioDirectory, "telemetry.csv"), false, new UTF8Encoding(false)))
        {
            writer.WriteLine("scenario,frame,time_s,input_x,input_y,sprinting,motor_grounded,root_y,root_rise,planar_speed,actual_planar_speed,move_y,animator_grounded,animator_jumping,animator_sprint_jump,vertical_velocity,state,state_time,in_transition,next_state,next_state_time,clips,hips_local_y,hips_world_y,left_foot_local_y,left_foot_world_y,right_foot_local_y,right_foot_world_y,skinned_vertex_avg_delta,skinned_vertex_max_delta");

            for (int frame = 0; frame <= totalFrames; frame++)
            {
                float time = frame * deltaTime;
                Vector2 input = scenario.Walk ? Vector2.up : Vector2.zero;
                bool sprinting = scenario.Sprint && input.sqrMagnitude > 0f;

                if (frame == scenario.JumpFrame)
                {
                    motor.RequestJump();
                    summary.JumpRequests++;
                    launches++;
                }
                else if (scenario.RepeatedJump && wasAirborne && motor.IsGrounded && launches < 3)
                {
                    motor.RequestJump();
                    summary.JumpRequests++;
                    launches++;
                }

                bool groundedBeforeTick = motor.IsGrounded;
                motor.Tick(input, sprinting, true, deltaTime, Vector3.forward);
                animator.Update(deltaTime);
                Physics.SyncTransforms();
                GetBakedSkinDisplacement(bakedSkins, baselineSkinVertices,
                    out float skinAverageDelta, out float skinMaximumDelta);
                summary.PeakSkinVertexAverageDelta = Mathf.Max(summary.PeakSkinVertexAverageDelta, skinAverageDelta);
                summary.PeakSkinVertexMaximumDelta = Mathf.Max(summary.PeakSkinVertexMaximumDelta, skinMaximumDelta);

                float rootRise = actor.transform.position.y - scenario.StartPosition.y;
                summary.PeakHeight = Mathf.Max(summary.PeakHeight, rootRise);
                summary.PeakSpeed = Mathf.Max(summary.PeakSpeed, motor.PlanarSpeed);
                summary.PeakActualSpeed = Mathf.Max(summary.PeakActualSpeed, motor.ActualPlanarSpeed);
                if (motor.IsGrounded)
                    summary.GroundedFrames++;
                if (!motor.IsGrounded)
                    wasAirborne = true;

                AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);
                AnimatorStateInfo nextState = animator.GetNextAnimatorStateInfo(0);
                string stateName = LookupStateName(stateNames, currentState.fullPathHash, currentState.shortNameHash);
                string nextStateName = animator.IsInTransition(0)
                    ? LookupStateName(stateNames, nextState.fullPathHash, nextState.shortNameHash)
                    : string.Empty;
                string clips = DescribeClips(animator, 0, false);
                if (animator.IsInTransition(0))
                    clips += " -> " + DescribeClips(animator, 0, true);
                CollectClipNames(summary.AnimationClips, clips);

                Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
                Transform leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
                Transform rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
                writer.WriteLine(string.Join(",", new[]
                {
                    Csv(scenario.Name), frame.ToString(), F(time),
                    F(input.x), F(input.y), sprinting ? "1" : "0", motor.IsGrounded ? "1" : "0",
                    F(actor.transform.position.y), F(rootRise), F(motor.PlanarSpeed), F(motor.ActualPlanarSpeed),
                    F(motor.AnimationInput.y), animator.GetBool("Grounded") ? "1" : "0",
                    animator.GetBool("Jumping") ? "1" : "0", animator.GetBool("SprintJump") ? "1" : "0",
                    F(animator.GetFloat("VerticalVelocity")), Csv(stateName), F(currentState.normalizedTime),
                    animator.IsInTransition(0) ? "1" : "0", Csv(nextStateName),
                    animator.IsInTransition(0) ? F(nextState.normalizedTime) : string.Empty, Csv(clips),
                    F(LocalY(hips)), F(WorldY(hips)), F(LocalY(leftFoot)), F(WorldY(leftFoot)),
                    F(LocalY(rightFoot)), F(WorldY(rightFoot)), F(skinAverageDelta), F(skinMaximumDelta)
                }));

                bool becameAirborne = groundedBeforeTick && !motor.IsGrounded;
                bool landedThisFrame = !groundedBeforeTick && motor.IsGrounded;
                bool keyPose = becameAirborne || landedThisFrame || frame == scenario.JumpFrame;
                if (frame % scenarioImageSampleInterval == 0 || frame == totalFrames || (!primaryTimeline && keyPose))
                {
                    FollowCamera(camera, actor.transform.position, scenario.StartPosition, scenario.LedgeFall);
                    string imagePath = Path.Combine(framesDirectory, $"frame_{frame:D3}_{time:F2}s.png");
                    SetBakedSkinsVisible(bakedSkins, true);
                    try
                    {
                        RenderFrame(camera, renderTexture, imagePath);
                    }
                    finally
                    {
                        SetBakedSkinsVisible(bakedSkins, false);
                    }
                    imagePaths.Add(imagePath);
                }

                if (frame % 12 == 0)
                {
                    float overallProgress = (scenarioIndex + frame / (float)totalFrames) / scenarioCount;
                    EditorUtility.DisplayProgressBar("Jump motion review",
                        $"{scenario.Name}: {frame}/{totalFrames} frames at {FrameRate} Hz", overallProgress);
                }
            }
        }

        CreateContactSheet(imagePaths, Path.Combine(scenarioDirectory, "contact_sheet.png"));
        return summary;
    }

    private static void ConfigureCamera(Camera camera, Vector3 start, bool ledgeFall)
    {
        camera.orthographicSize = ledgeFall ? 2.1f : 1.95f;
        FollowCamera(camera, start, start, ledgeFall);
    }

    private static BakedSkin[] PrepareBakedSkins(GameObject actor)
    {
        SkinnedMeshRenderer[] sources = actor.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        var bakedSkins = new BakedSkin[sources.Length];
        for (int index = 0; index < sources.Length; index++)
        {
            SkinnedMeshRenderer source = sources[index];
            source.updateWhenOffscreen = true;
            var mesh = new Mesh { name = source.name + " Jump Review Bake" };
            mesh.MarkDynamic();
            MeshFilter filter = source.gameObject.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            MeshRenderer renderer = source.gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = source.sharedMaterials;
            renderer.shadowCastingMode = source.shadowCastingMode;
            renderer.receiveShadows = source.receiveShadows;
            renderer.lightProbeUsage = source.lightProbeUsage;
            renderer.reflectionProbeUsage = source.reflectionProbeUsage;
            renderer.sortingLayerID = source.sortingLayerID;
            renderer.sortingOrder = source.sortingOrder;
            renderer.enabled = false;
            bakedSkins[index] = new BakedSkin
            {
                Source = source,
                Mesh = mesh,
                Filter = filter,
                Renderer = renderer
            };
        }
        return bakedSkins;
    }

    private static void GetBakedSkinDisplacement(BakedSkin[] bakedSkins, Vector3[][] baselineVertices,
        out float averageDelta, out float maximumDelta)
    {
        double totalDistance = 0d;
        int totalVertices = 0;
        maximumDelta = 0f;
        for (int skinIndex = 0; skinIndex < bakedSkins.Length; skinIndex++)
        {
            BakedSkin bakedSkin = bakedSkins[skinIndex];
            bakedSkin.Source.BakeMesh(bakedSkin.Mesh, false);
            Vector3[] vertices = bakedSkin.Mesh.vertices;
            if (baselineVertices[skinIndex] == null || baselineVertices[skinIndex].Length != vertices.Length)
            {
                baselineVertices[skinIndex] = (Vector3[])vertices.Clone();
            }

            Vector3[] baseline = baselineVertices[skinIndex];
            for (int vertexIndex = 0; vertexIndex < vertices.Length; vertexIndex++)
            {
                float distance = Vector3.Distance(vertices[vertexIndex], baseline[vertexIndex]);
                totalDistance += distance;
                totalVertices++;
                maximumDelta = Mathf.Max(maximumDelta, distance);
            }
        }
        averageDelta = totalVertices > 0 ? (float)(totalDistance / totalVertices) : 0f;
    }

    private static void SetBakedSkinsVisible(BakedSkin[] bakedSkins, bool visible)
    {
        foreach (BakedSkin bakedSkin in bakedSkins)
        {
            bakedSkin.Source.enabled = !visible;
            bakedSkin.Renderer.enabled = visible;
        }
    }

    private static void CleanupBakedSkins(BakedSkin[] bakedSkins)
    {
        if (bakedSkins == null)
            return;
        foreach (BakedSkin bakedSkin in bakedSkins)
        {
            if (bakedSkin.Source != null)
                bakedSkin.Source.enabled = true;
            if (bakedSkin.Renderer != null)
                UnityEngine.Object.DestroyImmediate(bakedSkin.Renderer);
            if (bakedSkin.Filter != null)
                UnityEngine.Object.DestroyImmediate(bakedSkin.Filter);
            if (bakedSkin.Mesh != null)
                UnityEngine.Object.DestroyImmediate(bakedSkin.Mesh);
        }
    }

    private static void FollowCamera(Camera camera, Vector3 actorPosition, Vector3 start, bool ledgeFall)
    {
        float targetY = ledgeFall ? start.y - 0.3f : start.y + 0.55f;
        float cameraY = ledgeFall ? start.y + 0.55f : start.y + 1.4f;
        camera.transform.position = new Vector3(actorPosition.x + 4.4f, cameraY, actorPosition.z - 6.4f);
        camera.transform.LookAt(new Vector3(actorPosition.x, targetY, actorPosition.z + 0.5f));
    }

    private static void RenderFrame(Camera camera, RenderTexture renderTexture, string path)
    {
        RenderTexture previousTarget = RenderTexture.active;
        var frame = new Texture2D(ImageWidth, ImageHeight, TextureFormat.RGB24, false, false);
        try
        {
            if (GraphicsSettings.currentRenderPipeline != null)
            {
                var request = new RenderPipeline.StandardRequest
                {
                    destination = renderTexture
                };
                if (!RenderPipeline.SupportsRenderRequest(camera, request))
                    throw new InvalidOperationException("The active render pipeline does not support standard camera capture requests.");
                RenderPipeline.SubmitRenderRequest(camera, request);
            }
            else
            {
                camera.Render();
            }

            RenderTexture.active = renderTexture;
            frame.ReadPixels(new Rect(0f, 0f, ImageWidth, ImageHeight), 0, 0, false);
            frame.Apply(false, false);
            File.WriteAllBytes(path, frame.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previousTarget;
            UnityEngine.Object.DestroyImmediate(frame);
        }
    }

    private static void CreateContactSheet(List<string> imagePaths, string outputPath)
    {
        const int columns = 4;
        List<string> representativePaths = SelectRepresentativeFrames(imagePaths, 12);
        int rows = Mathf.CeilToInt(representativePaths.Count / (float)columns);
        var sheet = new Texture2D(columns * ImageWidth, rows * ImageHeight, TextureFormat.RGB24, false, false);
        try
        {
            var blank = new Color[ImageWidth * ImageHeight];
            for (int index = 0; index < blank.Length; index++)
                blank[index] = new Color(18f / 255f, 22f / 255f, 28f / 255f, 1f);
            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                    sheet.SetPixels(column * ImageWidth, row * ImageHeight, ImageWidth, ImageHeight, blank);
            }

            for (int index = 0; index < representativePaths.Count; index++)
            {
                var source = new Texture2D(2, 2, TextureFormat.RGB24, false, false);
                try
                {
                    source.LoadImage(File.ReadAllBytes(representativePaths[index]), false);
                    int column = index % columns;
                    int rowFromTop = index / columns;
                    int rowFromBottom = rows - rowFromTop - 1;
                    sheet.SetPixels(column * ImageWidth, rowFromBottom * ImageHeight,
                        ImageWidth, ImageHeight, source.GetPixels());
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(source);
                }
            }

            sheet.Apply(false, false);
            File.WriteAllBytes(outputPath, sheet.EncodeToPNG());
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(sheet);
        }
    }

    private static List<string> SelectRepresentativeFrames(List<string> imagePaths, int maximumCount)
    {
        if (imagePaths.Count <= maximumCount)
            return imagePaths;

        var selected = new List<string>(maximumCount);
        for (int index = 0; index < maximumCount; index++)
        {
            int sourceIndex = Mathf.RoundToInt(index * (imagePaths.Count - 1f) / (maximumCount - 1f));
            selected.Add(imagePaths[sourceIndex]);
        }
        return selected;
    }

    private static Dictionary<int, string> BuildAnimatorStateNames(Animator animator)
    {
        var stateNames = new Dictionary<int, string>();
        var controller = animator.runtimeAnimatorController as AnimatorController;
        if (controller == null)
            return stateNames;

        foreach (AnimatorControllerLayer layer in controller.layers)
            AddStateNames(layer.stateMachine, layer.name, stateNames);
        return stateNames;
    }

    private static void AddStateNames(AnimatorStateMachine machine, string path, Dictionary<int, string> stateNames)
    {
        foreach (ChildAnimatorState child in machine.states)
        {
            string statePath = path + "." + child.state.name;
            stateNames[Animator.StringToHash(statePath)] = statePath;
            stateNames[Animator.StringToHash(child.state.name)] = child.state.name;
        }
        foreach (ChildAnimatorStateMachine childMachine in machine.stateMachines)
            AddStateNames(childMachine.stateMachine, path + "." + childMachine.stateMachine.name, stateNames);
    }

    private static string LookupStateName(Dictionary<int, string> stateNames, int fullPathHash, int shortNameHash)
    {
        if (stateNames.TryGetValue(fullPathHash, out string fullName))
            return fullName;
        if (stateNames.TryGetValue(shortNameHash, out string shortName))
            return shortName;
        return $"hash_{fullPathHash:X8}";
    }

    private static string DescribeClips(Animator animator, int layerIndex, bool next)
    {
        AnimatorClipInfo[] clips = next
            ? animator.GetNextAnimatorClipInfo(layerIndex)
            : animator.GetCurrentAnimatorClipInfo(layerIndex);
        var description = new StringBuilder();
        for (int index = 0; index < clips.Length; index++)
        {
            if (index > 0)
                description.Append('|');
            description.Append(clips[index].clip != null ? clips[index].clip.name : "<null>");
            description.Append('@');
            description.Append(clips[index].weight.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
        }
        return description.ToString();
    }

    private static void CollectClipNames(HashSet<string> clipNames, string description)
    {
        string[] segments = description.Split(new[] { '|', ' ', '>' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string segment in segments)
        {
            int separator = segment.IndexOf('@');
            if (separator > 0)
                clipNames.Add(segment.Substring(0, separator));
        }
    }

    private static void WriteSummary(string runDirectory, List<ScenarioSummary> summaries)
    {
        var lines = new List<string>
        {
            "scenario,peak_root_rise_m,peak_motor_speed_mps,peak_actual_speed_mps,grounded_frames,jump_requests,peak_skin_vertex_average_delta_m,peak_skin_vertex_maximum_delta_m,clips"
        };
        foreach (ScenarioSummary summary in summaries)
        {
            lines.Add(string.Join(",", Csv(summary.Name), F(summary.PeakHeight), F(summary.PeakSpeed),
                F(summary.PeakActualSpeed), summary.GroundedFrames.ToString(), summary.JumpRequests.ToString(),
                F(summary.PeakSkinVertexAverageDelta), F(summary.PeakSkinVertexMaximumDelta),
                Csv(string.Join("|", summary.AnimationClips))));
        }
        File.WriteAllLines(Path.Combine(runDirectory, "summary.csv"), lines, new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(runDirectory, "README.txt"),
            "Jump motion review captured in edit mode from the current Player prefab, CharacterMotor, and Animator Controller.\r\n" +
            "Each telemetry.csv contains 60 Hz motor, grounded, Animator state/clip, hips, foot, and baked-skin vertex displacement measurements.\r\n" +
            "The primary jump scenarios retain all sampled PNG frames for video and contact_sheet.png selects up to 12 evenly spaced frames.\r\n" +
            "The temporary preview scene was removed after capture; the user's open scene was restored as active.\r\n");
    }

    private static Material CreateMaterial(string name, Color color)
    {
        Shader shader = GraphicsSettings.currentRenderPipeline != null
            ? Shader.Find("Universal Render Pipeline/Lit")
            : Shader.Find("Standard");
        shader ??= Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit");
        var material = new Material(shader) { name = name };
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", 0.12f);
        return material;
    }

    private static void DestroyTemporaryMaterial(Material material)
    {
        if (material != null)
            UnityEngine.Object.DestroyImmediate(material);
    }

    private static float LocalY(Transform bone) => bone != null ? bone.localPosition.y : float.NaN;
    private static float WorldY(Transform bone) => bone != null ? bone.position.y : float.NaN;
    private static string F(float value) => value.ToString("F4", System.Globalization.CultureInfo.InvariantCulture);
    private static string Csv(string value) => "\"" + (value ?? string.Empty).Replace("\"", "\"\"") + "\"";
}
