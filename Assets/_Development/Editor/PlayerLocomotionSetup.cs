using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Object = UnityEngine.Object;

// Explicit setup command, never an automatic importer. Existing gameplay references stay on the player root.
public static class PlayerLocomotionSetup
{
    private const string Model = "Assets/_Game/Features/Player/Male_yBot_Testing.fbx";
    private const string Clips = "Assets/_Game/Shared/Animations/Humanoid/Clips/Locomotion/";
    private const string ControllerPath = "Assets/_Game/Features/Player/Animations/Controllers/PlayerLocomotion.controller";
    private const string PrefabPath = "Assets/_Game/Features/Player/Prefabs/player.prefab";

    [MenuItem("Tools/Player/Rebuild Locomotion Setup")]
    public static void Run()
    {
        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Exit Play Mode before configuring the player.");

        ConfigureImport(Model, false, false);
        foreach (string clip in new[] { "CalmIdle", "Walking", "Walking Backwards", "Left Strafe Walking", "Right Strafe Walking", "Running", "Falling Idle" })
            ConfigureImport(Clips + clip + ".fbx", true, clip == "CalmIdle" || clip == "Falling Idle");

        var controller = BuildController();
        ConfigurePrefab(controller);
        RefineBlending();
        PlayerCameraSetup.ConfigurePrefab();
        AssetDatabase.SaveAssets();
        Debug.Log("[Player Locomotion] Configured yBot, Humanoid locomotion clips, controller and player prefab.");
    }

    private static void ConfigureImport(string path, bool animation, bool inPlace)
    {
        var importer = (ModelImporter)AssetImporter.GetAtPath(path);
        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importCameras = false;
        importer.importLights = false;
        importer.optimizeGameObjects = false;
        importer.SaveAndReimport();

        if (!animation)
            return;

        var clips = importer.defaultClipAnimations;
        foreach (var clip in clips)
        {
            clip.loopTime = true;
            clip.loopPose = true;
            clip.lockRootRotation = true;
            clip.keepOriginalOrientation = true;
            clip.lockRootHeightY = true;
            clip.keepOriginalPositionY = true;
            clip.lockRootPositionXZ = inPlace;
            clip.keepOriginalPositionXZ = true;
        }

        importer.clipAnimations = clips;
        importer.SaveAndReimport();
        var avatar = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();
        if (avatar == null || !avatar.isValid || !avatar.isHuman)
            throw new InvalidOperationException("Humanoid avatar could not be generated: " + path);
    }

    private static AnimationClip Clip(string name)
    {
        var clip = AssetDatabase.LoadAllAssetsAtPath(Clips + name + ".fbx").OfType<AnimationClip>()
            .First(value => !value.name.StartsWith("__preview__"));
        Debug.Log($"[Player Locomotion] {name}: {clip.length:F2}s, average root velocity {clip.averageSpeed}");
        return clip;
    }

    private static AnimatorController BuildController()
    {
        // Rebuilding is explicit. Reuse the controller asset so prefab GUID references stay valid.
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        else
        {
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(ControllerPath))
                if (asset != controller)
                    Object.DestroyImmediate(asset, true);
        }

        controller.parameters = Array.Empty<AnimatorControllerParameter>();
        controller.layers = Array.Empty<AnimatorControllerLayer>();
        controller.AddParameter("MoveX", AnimatorControllerParameterType.Float);
        controller.AddParameter("MoveY", AnimatorControllerParameterType.Float);
        controller.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
        controller.AddLayer("Locomotion");
        var machine = controller.layers[0].stateMachine;
        var ground = machine.AddState("Grounded");
        machine.defaultState = ground;
        var blend = new BlendTree
        {
            name = "Directional Root Motion",
            blendType = BlendTreeType.FreeformDirectional2D,
            blendParameter = "MoveX",
            blendParameterY = "MoveY",
            useAutomaticThresholds = false
        };
        AssetDatabase.AddObjectToAsset(blend, controller);
        blend.AddChild(Clip("CalmIdle"), Vector2.zero);
        blend.AddChild(Clip("Walking"), Vector2.up);
        blend.AddChild(Clip("Walking Backwards"), Vector2.down);
        blend.AddChild(Clip("Left Strafe Walking"), Vector2.left);
        blend.AddChild(Clip("Right Strafe Walking"), Vector2.right);
        blend.AddChild(Clip("Running"), Vector2.up * 2f);
        ground.motion = blend;

        var air = machine.AddState("Airborne");
        air.motion = Clip("Falling Idle");
        var takeoff = ground.AddTransition(air);
        takeoff.hasExitTime = false;
        takeoff.duration = 0.1f;
        takeoff.AddCondition(AnimatorConditionMode.IfNot, 0f, "Grounded");
        var landing = air.AddTransition(ground);
        landing.hasExitTime = false;
        landing.duration = 0.12f;
        landing.AddCondition(AnimatorConditionMode.If, 0f, "Grounded");
        EditorUtility.SetDirty(controller);
        return controller;
    }

    [MenuItem("Tools/Player/Refine Locomotion Blending")]
    public static void RefineBlending()
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        var tree = AssetDatabase.LoadAllAssetsAtPath(ControllerPath).OfType<BlendTree>().Single();
        tree.blendType = BlendTreeType.FreeformDirectional2D;
        var motions = tree.children;
        var scene = EditorSceneManager.NewPreviewScene();
        var actor = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), scene);
        try
        {
            foreach (var script in actor.GetComponentsInChildren<MonoBehaviour>(true))
                script.enabled = false;
            Object.DestroyImmediate(actor.GetComponentInChildren<CharacterRootMotion>());
            var animator = actor.GetComponent<CharacterMotor>().Animator;
            animator.applyRootMotion = false;
            float[] reference = SampleFootPhase(animator, Clip("Walking"));
            for (int motion = 1; motion < motions.Length; motion++)
            {
                float[] signal = SampleFootPhase(animator, (AnimationClip)motions[motion].motion);
                int bestOffset = 0;
                float bestError = float.PositiveInfinity;
                for (int offset = 0; offset < signal.Length; offset++)
                {
                    float error = 0f;
                    for (int sample = 0; sample < signal.Length; sample++)
                        error += Mathf.Pow(reference[sample] - signal[(sample + offset) % signal.Length], 2f);
                    if (error < bestError)
                    {
                        bestError = error;
                        bestOffset = offset;
                    }
                }
                motions[motion].cycleOffset = (float)bestOffset / signal.Length;
                Debug.Log($"[Gait Phase] {AssetDatabase.GetAssetPath(motions[motion].motion)} offset {motions[motion].cycleOffset:F3}");
            }
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(scene);
        }
        tree.children = motions;
        EditorUtility.SetDirty(tree);
        EditorUtility.SetDirty(controller);
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var data = new SerializedObject(root.GetComponent<CharacterMotor>());
            data.FindProperty("movementBlendTime").floatValue = 0.08f;
            data.FindProperty("stoppingBlendTime").floatValue = 0.08f;
            data.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
        AssetDatabase.SaveAssets();
    }

    private static float[] SampleFootPhase(Animator animator, AnimationClip clip)
    {
        var graph = PlayableGraph.Create("Gait phase sampling");
        try
        {
            var playable = AnimationClipPlayable.Create(graph, clip);
            playable.SetApplyFootIK(false);
            var output = AnimationPlayableOutput.Create(graph, "Body", animator);
            output.SetSourcePlayable(playable);
            graph.Play();
            float[] signal = new float[32];
            for (int sample = 0; sample < signal.Length; sample++)
            {
                playable.SetTime(clip.length * sample / signal.Length);
                graph.Evaluate(0f);
                signal[sample] = animator.GetBoneTransform(HumanBodyBones.LeftFoot).position.y -
                    animator.GetBoneTransform(HumanBodyBones.RightFoot).position.y;
            }
            float mean = signal.Average();
            float magnitude = Mathf.Sqrt(signal.Sum(value => (value - mean) * (value - mean)) / signal.Length);
            for (int sample = 0; sample < signal.Length; sample++)
                signal[sample] = (signal[sample] - mean) / Mathf.Max(magnitude, 0.001f);
            return signal;
        }
        finally
        {
            graph.Destroy();
        }
    }

    private static void ConfigurePrefab(AnimatorController controller)
    {
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var oldBody = root.transform.Find("BodyModel");
            if (oldBody != null)
                Object.DestroyImmediate(oldBody.gameObject);
            Object.DestroyImmediate(root.GetComponent<Rigidbody>());
            Object.DestroyImmediate(root.GetComponent<CapsuleCollider>());
            Object.DestroyImmediate(root.GetComponent<MeshRenderer>());
            Object.DestroyImmediate(root.GetComponent<MeshFilter>());

            var capsule = root.GetComponent<CharacterController>() ?? root.AddComponent<CharacterController>();
            capsule.height = 2f;
            capsule.radius = 0.35f;
            capsule.center = Vector3.zero;
            capsule.skinWidth = 0.035f;
            capsule.stepOffset = 0.3f;
            capsule.slopeLimit = 50f;
            capsule.minMoveDistance = 0f;
            var motor = root.GetComponent<CharacterMotor>() ?? root.AddComponent<CharacterMotor>();

            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Model), root.transform);
            model.name = "BodyModel";
            model.transform.localPosition = Vector3.down;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one;
            foreach (var part in model.GetComponentsInChildren<Transform>(true))
                part.gameObject.layer = root.layer;

            var animator = model.GetComponent<Animator>();
            if (animator == null || animator.avatar == null || !animator.avatar.isValid || !animator.avatar.isHuman)
                throw new InvalidOperationException("Player body needs a valid Humanoid Avatar.");

            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = true;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.Normal;
            var relay = model.AddComponent<CharacterRootMotion>();
            var motorData = new SerializedObject(motor);
            motorData.FindProperty("animator").objectReferenceValue = animator;
            motorData.ApplyModifiedPropertiesWithoutUndo();
            var relayData = new SerializedObject(relay);
            relayData.FindProperty("motor").objectReferenceValue = motor;
            relayData.ApplyModifiedPropertiesWithoutUndo();

            // Keep the gameplay camera independent from the new animated body.
            var camera = root.GetComponentInChildren<Camera>();
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

    private static void ApplyMaterials(GameObject model)
    {
        const string directory = "Assets/_Game/Features/Player/Materials";
        if (!AssetDatabase.IsValidFolder(directory))
            AssetDatabase.CreateFolder("Assets/_Game/Features/Player", "Materials");

        var body = Material(directory + "/TestBody.mat", new Color(0.65f, 0.48f, 0.16f));
        var joints = Material(directory + "/TestJoints.mat", new Color(0.12f, 0.15f, 0.18f));
        foreach (var renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>())
            renderer.sharedMaterials = renderer.sharedMaterials.Select(material =>
                material != null && material.name.ToLowerInvariant().Contains("joint") ? joints : body).ToArray();
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
