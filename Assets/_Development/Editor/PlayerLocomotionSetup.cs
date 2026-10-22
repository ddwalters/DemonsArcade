using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

// Explicit setup command. Batch preparation updates the player assets without rebuilding scenes.
public static class PlayerLocomotionSetup
{
    private const string Model = "Assets/_Game/Features/Player/Male_yBot_Testing.fbx";
    private const string Clips = "Assets/_Game/Shared/Animations/Humanoid/Clips/Locomotion/";
    private const string ControllerPath = "Assets/_Game/Features/Player/Animations/Controllers/PlayerLocomotion.controller";
    private const string PrefabPath = "Assets/_Game/Features/Player/Prefabs/player.prefab";
    private const string LegacyRootMotionRelayGuid = "258a7c5aaa007a944b04f268a8e34630";

    private static readonly string[] LoopingClips =
    {
        "CalmIdle",
        "Walking",
        "Walking Backwards",
        "Left Strafe Walking",
        "Right Strafe Walking",
        "Running",
        "Falling Idle"
    };

    private static readonly string[] OneShotClips =
    {
        "Falling To Landing"
    };

    private static readonly string[] JumpAnimationClips =
    {
        "Jumping",
        "Running Jump"
    };

    [MenuItem("Tools/Player/Rebuild Locomotion Setup")]
    public static void Run()
    {
        PrepareAssets();
        Debug.Log("[Player Locomotion] Configured in-place Humanoid clips, speed blend, and player prefab.");
    }

    // Use with Unity -batchmode -executeMethod PlayerLocomotionSetup.PrepareBatch.
    [MenuItem("Tools/Player/Prepare Locomotion for Batch Validation")]
    public static void PrepareBatch()
    {
        PrepareAssets();
        Debug.Log("[Player Locomotion] Batch preparation complete; no scene was rebuilt.");
    }

    // Use this method as the single Unity batch entry when preparing and validating the player.
    public static void PrepareAndValidateBatch()
    {
        PrepareAssets();
        PlayerLocomotionChecks.RunBatch();
    }

    private static void PrepareAssets()
    {
        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Exit Play Mode before configuring the player.");

        ConfigureImport(Model, false);
        float? walkingSpeed = MeasureAuthoredGaitSpeed("Walking");
        float? runningSpeed = MeasureAuthoredGaitSpeed("Running");
        foreach (string clip in LoopingClips.Where(clip => clip != "Walking" && clip != "Running"))
            ConfigureImport(Clips + clip + ".fbx", true, true, true);
        foreach (string clip in OneShotClips)
            ConfigureImport(Clips + clip + ".fbx", true, true, false);
        foreach (string clip in JumpAnimationClips)
            ConfigureImport(Clips + clip + ".fbx", true, false, false, true);

        var controller = BuildController(walkingSpeed, runningSpeed);
        ConfigurePrefab(controller);
        PlayerCameraSetup.ConfigurePrefab();
        AssetDatabase.SaveAssets();
    }

    private static float? MeasureAuthoredGaitSpeed(string name)
    {
        string path = Clips + name + ".fbx";
        ConfigureImport(path, true, false);
        float speed;
        try
        {
            speed = Clip(name).averageSpeed.magnitude;
        }
        finally
        {
            ConfigureImport(path, true, true);
        }

        if (speed <= 0.05f)
        {
            Debug.LogWarning($"[Gait Timing] Could not measure {name} root speed; keeping the conservative 1.3 playback scale. Visual stride calibration remains unmeasured.");
            return null;
        }

        Debug.Log($"[Gait Timing] {name}: measured authored root speed {speed:F2} m/s before baking root translation.");
        return speed;
    }

    private static void ConfigureImport(string path, bool animation, bool inPlace = true, bool loop = true,
        bool extractJumpRootTranslation = false)
    {
        var importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null)
            throw new InvalidOperationException("Missing model importer: " + path);

        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importCameras = false;
        importer.importLights = false;
        importer.optimizeGameObjects = false;

        if (animation)
        {
            var clips = importer.defaultClipAnimations;
            foreach (var clip in clips)
            {
                clip.loopTime = loop;
                clip.loopPose = loop;
                clip.lockRootRotation = true;
                clip.keepOriginalOrientation = true;
                if (extractJumpRootTranslation)
                {
                    // Extract the authored jump root travel so it is not doubled with motor movement.
                    // Root motion remains disabled on the player Animator; only pose articulation is applied.
                    clip.lockRootHeightY = false;
                    clip.keepOriginalPositionY = false;
                    clip.heightFromFeet = true;
                    clip.lockRootPositionXZ = false;
                    clip.keepOriginalPositionXZ = false;
                }
                else
                {
                    clip.lockRootHeightY = true;
                    clip.keepOriginalPositionY = true;
                    clip.lockRootPositionXZ = inPlace;
                    clip.keepOriginalPositionXZ = true;
                }
            }

            importer.clipAnimations = clips;
        }

        importer.SaveAndReimport();

        if (!animation)
            return;

        var avatar = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();
        if (avatar == null || !avatar.isValid || !avatar.isHuman)
            throw new InvalidOperationException("Humanoid avatar could not be generated: " + path);
    }

    private static AnimationClip Clip(string name)
    {
        var clip = AssetDatabase.LoadAllAssetsAtPath(Clips + name + ".fbx").OfType<AnimationClip>()
            .First(value => !value.name.StartsWith("__preview__", StringComparison.Ordinal));
        Debug.Log($"[Player Locomotion] {name}: {clip.length:F2}s, authored average speed {clip.averageSpeed.magnitude:F2} m/s");
        return clip;
    }

    private static AnimatorController BuildController(float? walkingSpeed, float? runningSpeed)
    {
        // Reuse the controller and its first state machine so prefab GUID references stay valid.
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

        var layers = controller.layers;
        if (layers.Length == 0)
        {
            controller.AddLayer("Locomotion");
            layers = controller.layers;
        }

        var layer = layers[0];
        var machine = layer.stateMachine;
        var previousStates = machine.states.Select(child => child.state).ToArray();
        var ground = previousStates.FirstOrDefault(state => state.name == "Grounded") ?? machine.AddState("Grounded");
        var air = previousStates.FirstOrDefault(state => state.name == "Airborne") ?? machine.AddState("Airborne");
        var jump = previousStates.FirstOrDefault(state => state.name == "Jump") ?? machine.AddState("Jump");
        var sprintJump = previousStates.FirstOrDefault(state => state.name == "Sprint Jump") ?? machine.AddState("Sprint Jump");
        var landing = previousStates.FirstOrDefault(state => state.name == "Landing") ?? machine.AddState("Landing");
        var sprintLanding = previousStates.FirstOrDefault(state => state.name == "Sprint Landing") ?? machine.AddState("Sprint Landing");

        foreach (var state in previousStates)
        {
            if (state != ground && state != air && state != jump && state != sprintJump &&
                state != landing && state != sprintLanding)
                machine.RemoveState(state);
        }

        foreach (var transition in machine.anyStateTransitions.ToArray())
            machine.RemoveAnyStateTransition(transition);
        foreach (var transition in machine.entryTransitions.ToArray())
            machine.RemoveEntryTransition(transition);
        foreach (var transition in ground.transitions.ToArray())
            ground.RemoveTransition(transition);
        foreach (var transition in air.transitions.ToArray())
            air.RemoveTransition(transition);
        foreach (var transition in jump.transitions.ToArray())
            jump.RemoveTransition(transition);
        foreach (var transition in sprintJump.transitions.ToArray())
            sprintJump.RemoveTransition(transition);
        foreach (var transition in landing.transitions.ToArray())
            landing.RemoveTransition(transition);
        foreach (var transition in sprintLanding.transitions.ToArray())
            sprintLanding.RemoveTransition(transition);

        machine.defaultState = ground;
        var childStates = machine.states;
        for (int i = 0; i < childStates.Length; i++)
        {
            var childState = childStates[i];
            if (childState.state == ground)
                childState.position = new Vector3(200f, 0f, 0f);
            else if (childState.state == jump)
                childState.position = new Vector3(450f, -100f, 0f);
            else if (childState.state == sprintJump)
                childState.position = new Vector3(450f, 100f, 0f);
            else if (childState.state == air)
                childState.position = new Vector3(700f, 0f, 0f);
            else if (childState.state == landing)
                childState.position = new Vector3(950f, 0f, 0f);
            else if (childState.state == sprintLanding)
                childState.position = new Vector3(950f, 140f, 0f);

            childStates[i] = childState;
        }
        machine.states = childStates;
        layer.name = "Locomotion";
        layer.defaultWeight = 1f;
        controller.layers = new[] { layer };
        controller.parameters = Array.Empty<AnimatorControllerParameter>();
        controller.AddParameter("MoveX", AnimatorControllerParameterType.Float);
        controller.AddParameter("MoveY", AnimatorControllerParameterType.Float);
        controller.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Jumping", AnimatorControllerParameterType.Bool);
        controller.AddParameter("SprintJump", AnimatorControllerParameterType.Bool);
        controller.AddParameter("VerticalVelocity", AnimatorControllerParameterType.Float);
        controller.AddParameter("JumpTime", AnimatorControllerParameterType.Float);
        controller.AddParameter("LandingActive", AnimatorControllerParameterType.Bool);

        var blend = AssetDatabase.LoadAllAssetsAtPath(ControllerPath).OfType<BlendTree>().FirstOrDefault();
        if (blend == null)
        {
            blend = new BlendTree();
            AssetDatabase.AddObjectToAsset(blend, controller);
        }

        blend.name = "Ground Speed";
        blend.blendType = BlendTreeType.Simple1D;
        blend.blendParameter = "MoveY";
        blend.useAutomaticThresholds = false;
        blend.children = Array.Empty<ChildMotion>();
        blend.AddChild(Clip("CalmIdle"), 0f);
        blend.AddChild(Clip("Walking"), 1f);
        blend.AddChild(Clip("Running"), 2f);

        var children = blend.children;
        children[1].timeScale = GaitTimeScale((AnimationClip)children[1].motion, walkingSpeed, 2.2f);
        children[2].timeScale = GaitTimeScale((AnimationClip)children[2].motion, runningSpeed, 5f);
        blend.children = children;
        ground.motion = blend;
        air.motion = Clip("Falling Idle");
        jump.motion = Clip("Jumping");
        sprintJump.motion = Clip("Running Jump");
        landing.motion = Clip("Jumping");
        sprintLanding.motion = Clip("Running Jump");
        SetMotionTimeParameter(jump, "JumpTime");
        SetMotionTimeParameter(sprintJump, "JumpTime");
        SetMotionTimeParameter(landing, "JumpTime");
        SetMotionTimeParameter(sprintLanding, "JumpTime");

        AddJumpTransition(ground, jump, false);
        AddJumpTransition(ground, sprintJump, true);
        AddUnplannedFallTransition(ground, air);

        AddLandingTransitions(jump, landing, sprintLanding);
        AddLandingTransitions(sprintJump, landing, sprintLanding);
        AddGroundedFallbackTransition(jump, ground);
        AddGroundedFallbackTransition(sprintJump, ground);
        AddJumpTimeoutTransition(jump, air);
        AddJumpTimeoutTransition(sprintJump, air);
        AddJumpTransition(air, jump, false);
        AddJumpTransition(air, sprintJump, true);
        AddLandingTransitions(air, landing, sprintLanding);
        AddGroundedFallbackTransition(air, ground);
        AddJumpTransition(landing, jump, false);
        AddJumpTransition(landing, sprintJump, true);
        AddJumpTransition(sprintLanding, jump, false);
        AddJumpTransition(sprintLanding, sprintJump, true);
        AddUnplannedFallTransition(landing, air);
        AddUnplannedFallTransition(sprintLanding, air);
        AddLandingRecoveryTransition(landing, ground);
        AddLandingRecoveryTransition(sprintLanding, ground);

        EditorUtility.SetDirty(blend);
        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static void AddJumpTransition(AnimatorState source, AnimatorState destination, bool sprintJump)
    {
        var transition = source.AddTransition(destination);
        transition.hasExitTime = false;
        transition.duration = 0.06f;
        transition.interruptionSource = TransitionInterruptionSource.DestinationThenSource;
        transition.orderedInterruption = true;
        transition.AddCondition(AnimatorConditionMode.If, 0f, "Jumping");
        transition.AddCondition(sprintJump ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot,
            0f, "SprintJump");
    }

    private static void AddLandingTransitions(AnimatorState source, AnimatorState landing,
        AnimatorState sprintLanding)
    {
        AddGroundContactTransition(source, landing, false);
        AddGroundContactTransition(source, sprintLanding, true);
    }

    private static void AddGroundContactTransition(AnimatorState source, AnimatorState destination,
        bool sprintLanding)
    {
        var transition = source.AddTransition(destination);
        transition.hasExitTime = false;
        transition.duration = 0.02f;
        transition.interruptionSource = TransitionInterruptionSource.None;
        transition.AddCondition(AnimatorConditionMode.If, 0f, "Grounded");
        transition.AddCondition(AnimatorConditionMode.If, 0f, "LandingActive");
        transition.AddCondition(sprintLanding ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot,
            0f, "SprintJump");
    }

    private static void AddGroundedFallbackTransition(AnimatorState source, AnimatorState grounded)
    {
        var transition = source.AddTransition(grounded);
        transition.hasExitTime = false;
        transition.duration = 0.08f;
        transition.interruptionSource = TransitionInterruptionSource.None;
        transition.AddCondition(AnimatorConditionMode.If, 0f, "Grounded");
        transition.AddCondition(AnimatorConditionMode.IfNot, 0f, "LandingActive");
        transition.AddCondition(AnimatorConditionMode.IfNot, 0f, "Jumping");
    }

    private static void AddJumpTimeoutTransition(AnimatorState source, AnimatorState destination)
    {
        var transition = source.AddTransition(destination);
        transition.hasExitTime = false;
        transition.duration = 0.08f;
        transition.AddCondition(AnimatorConditionMode.IfNot, 0f, "Grounded");
        transition.AddCondition(AnimatorConditionMode.IfNot, 0f, "Jumping");
    }

    private static void AddUnplannedFallTransition(AnimatorState source, AnimatorState destination)
    {
        var transition = source.AddTransition(destination);
        transition.hasExitTime = false;
        transition.duration = 0.1f;
        transition.AddCondition(AnimatorConditionMode.IfNot, 0f, "Grounded");
        transition.AddCondition(AnimatorConditionMode.IfNot, 0f, "Jumping");
    }

    private static void AddLandingRecoveryTransition(AnimatorState source, AnimatorState destination)
    {
        var transition = source.AddTransition(destination);
        transition.hasExitTime = false;
        transition.duration = 0.08f;
        transition.offset = 0.05f;
        transition.interruptionSource = TransitionInterruptionSource.SourceThenDestination;
        transition.orderedInterruption = true;
        transition.AddCondition(AnimatorConditionMode.If, 0f, "Grounded");
        transition.AddCondition(AnimatorConditionMode.IfNot, 0f, "LandingActive");
    }

    private static void SetMotionTimeParameter(AnimatorState state, string parameterName)
    {
        state.timeParameterActive = true;
        state.timeParameter = parameterName;
    }

    private static float GaitTimeScale(AnimationClip clip, float? authoredSpeed, float targetSpeed)
    {
        if (!authoredSpeed.HasValue || authoredSpeed.Value <= 0.05f)
        {
            Debug.LogWarning($"[Gait Timing] {clip.name}: using an uncalibrated 1.3 playback scale for {targetSpeed:F1} m/s motor movement.");
            return 1.3f;
        }

        float scale = targetSpeed / authoredSpeed.Value;
        scale = Mathf.Clamp(scale, 0.5f, 2f);
        Debug.Log($"[Gait Timing] {clip.name}: {authoredSpeed.Value:F2} m/s authored, {scale:F2} playback scale for {targetSpeed:F1} m/s movement.");
        return scale;
    }

    private static void ConfigurePrefab(AnimatorController controller)
    {
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            RemoveLegacyRootMotionRelay(root);
            var oldBody = root.transform.Find("BodyModel");
            GameObject model = oldBody != null
                ? oldBody.gameObject
                : (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Model), root.transform);
            model.name = "BodyModel";
            model.transform.localPosition = Vector3.down;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one;
            foreach (var part in model.GetComponentsInChildren<Transform>(true))
                part.gameObject.layer = root.layer;

            var capsule = root.GetComponent<CharacterController>() ?? root.AddComponent<CharacterController>();
            capsule.height = 2f;
            capsule.radius = 0.35f;
            capsule.center = Vector3.zero;
            capsule.skinWidth = 0.035f;
            capsule.stepOffset = 0.3f;
            capsule.slopeLimit = 50f;
            capsule.minMoveDistance = 0f;

            Object.DestroyImmediate(root.GetComponent<Rigidbody>());
            Object.DestroyImmediate(root.GetComponent<CapsuleCollider>());
            Object.DestroyImmediate(root.GetComponent<MeshRenderer>());
            Object.DestroyImmediate(root.GetComponent<MeshFilter>());

            var motor = root.GetComponent<CharacterMotor>() ?? root.AddComponent<CharacterMotor>();
            var animator = model.GetComponent<Animator>();
            if (animator == null || animator.avatar == null || !animator.avatar.isValid || !animator.avatar.isHuman)
                throw new InvalidOperationException("Player body needs a valid Humanoid Avatar.");

            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.Normal;

            var motorData = new SerializedObject(motor);
            var animatorProperty = motorData.FindProperty("animator");
            if (animatorProperty != null)
                animatorProperty.objectReferenceValue = animator;
            SetFloat(motorData, "walkSpeed", 2.2f);
            SetFloat(motorData, "sprintSpeed", 5f);
            SetFloat(motorData, "acceleration", 30f);
            SetFloat(motorData, "braking", 40f);
            SetFloat(motorData, "turnSpeed", 720f);
            motorData.ApplyModifiedPropertiesWithoutUndo();

            var camera = root.GetComponentInChildren<Camera>(true);
            if (camera == null)
                throw new InvalidOperationException("Player prefab needs its gameplay camera.");
            camera.cullingMask &= ~(1 << root.layer);
            camera.nearClipPlane = 0.05f;

            ApplyMaterials(model);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void RemoveLegacyRootMotionRelay(GameObject root)
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string prefabFile = Path.Combine(projectRoot, PrefabPath.Replace('/', Path.DirectorySeparatorChar));
        string prefabYaml = File.ReadAllText(prefabFile);
        int relayReferences = prefabYaml.Split(new[] { LegacyRootMotionRelayGuid }, StringSplitOptions.None).Length - 1;
        if (relayReferences == 0)
            return;

        var missingScriptObjects = root.GetComponentsInChildren<Transform>(true)
            .Select(transform => transform.gameObject)
            .Select(gameObject => new
            {
                GameObject = gameObject,
                Count = gameObject.GetComponents<Component>().Count(component => component == null)
            })
            .Where(entry => entry.Count > 0)
            .ToArray();
        int missingScriptCount = missingScriptObjects.Sum(entry => entry.Count);
        if (relayReferences != 1 || missingScriptCount != 1 || missingScriptObjects.Length != 1)
        {
            throw new InvalidOperationException(
                "The player prefab has missing scripts beyond the known locomotion relay; refusing to remove unrelated components.");
        }

        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(missingScriptObjects[0].GameObject);
    }

    private static void SetFloat(SerializedObject serializedObject, string propertyName, float value)
    {
        var property = serializedObject.FindProperty(propertyName);
        if (property != null)
            property.floatValue = value;
    }

    private static void ApplyMaterials(GameObject model)
    {
        const string directory = "Assets/_Game/Features/Player/Materials";
        if (!AssetDatabase.IsValidFolder(directory))
            AssetDatabase.CreateFolder("Assets/_Game/Features/Player", "Materials");

        var body = Material(directory + "/TestBody.mat", new Color(0.65f, 0.48f, 0.16f));
        var joints = Material(directory + "/TestJoints.mat", new Color(0.12f, 0.15f, 0.18f));
        foreach (var renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            renderer.sharedMaterials = renderer.sharedMaterials.Select(material =>
                material != null && material.name.ToLowerInvariant().Contains("joint") ? joints : body).ToArray();
        }
    }

    private static Material Material(string path, Color color)
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0.3f);
            AssetDatabase.CreateAsset(material, path);
        }
        return material;
    }
}
