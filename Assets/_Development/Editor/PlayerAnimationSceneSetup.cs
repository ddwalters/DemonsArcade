using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class PlayerAnimationSceneSetup
{
    public const string ScenePath = "Assets/_Development/Scenes/PlayerLocomotionTest.unity";
    private const string ClipRoot = "Assets/_Game/Shared/Animations/Humanoid/Clips";
    private const string PreviewController = "Assets/_Development/Prototypes/AnimationPreview.controller";

    [MenuItem("Tools/Player/Build Animation Test Scene")]
    public static void Build()
    {
        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Exit Play Mode before building the test scene.");

        string[] paths = Directory.GetFiles(ClipRoot, "*.fbx", SearchOption.AllDirectories)
            .Select(path => path.Replace('\\', '/')).OrderBy(path => path, StringComparer.Ordinal).ToArray();
        foreach (string path in paths)
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            if (importer.animationType != ModelImporterAnimationType.Human)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.importCameras = false;
                importer.importLights = false;
                importer.SaveAndReimport();
            }
        }

        var previews = paths.Select(path => new PlayerAnimationTest.Preview
        {
            name = Path.GetFileNameWithoutExtension(path),
            clip = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                .First(clip => !clip.name.StartsWith("__preview__")),
            sword = path.Contains("Sword") || path.Contains("Melee") || path.Contains("Heavy Weapon") || path.Contains("CombatIdle")
        }).ToArray();
        AnimationClip idle = previews.First(preview => preview.name == "CalmIdle").clip;
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(PreviewController);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(PreviewController);
            var state = controller.layers[0].stateMachine.AddState("Preview");
            state.motion = idle;
            controller.layers[0].stateMachine.defaultState = state;
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.65f, 0.69f, 0.75f);
        RenderSettings.fog = false;
        var light = new GameObject("Key light").AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.5f;
        light.shadows = LightShadows.Soft;
        light.transform.rotation = Quaternion.Euler(45f, -35f, 0f);

        var floorMaterial = GetMaterial("TestFloor", new Color(0.15f, 0.20f, 0.25f));
        var obstacleMaterial = GetMaterial("TestObstacles", new Color(0.32f, 0.43f, 0.49f));
        var gridMaterial = GetMaterial("TestGrid", new Color(0.27f, 0.34f, 0.38f));
        Cube("Floor", new Vector3(0f, -0.5f, 0f), new Vector3(40f, 1f, 40f), floorMaterial);
        for (int line = -20; line <= 20; line++)
        {
            Cube("Grid", new Vector3(line, 0.003f, 0f), new Vector3(0.018f, 0.004f, 40f), gridMaterial, false);
            Cube("Grid", new Vector3(0f, 0.003f, line), new Vector3(40f, 0.004f, 0.018f), gridMaterial, false);
        }
        Cube("Collision wall", new Vector3(0f, 1.5f, 10f), new Vector3(6f, 3f, 0.35f), obstacleMaterial);
        Cube("Step platform", new Vector3(5f, 0.15f, 0f), new Vector3(3f, 0.3f, 3f), obstacleMaterial);
        var ramp = Cube("Walkable slope", new Vector3(-7f, 0.8f, 0f), new Vector3(3f, 0.2f, 6f), obstacleMaterial);
        ramp.transform.rotation = Quaternion.Euler(-20f, 0f, 0f);

        var playerObject = (GameObject)PrefabUtility.InstantiatePrefab(
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Features/Player/Prefabs/player.prefab"));
        playerObject.transform.SetPositionAndRotation(new Vector3(0f, 1.04f, -2f), Quaternion.identity);
        playerObject.GetComponentInChildren<Camera>().enabled = false;
        var camera = new GameObject("Test scene camera").AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.065f, 0.085f, 0.11f);
        camera.nearClipPlane = 0.05f;
        camera.farClipPlane = 100f;
        camera.fieldOfView = 45f;
        camera.transform.position = new Vector3(-3.6f, 2.74f, -6.2f);
        camera.transform.LookAt(playerObject.transform.position + Vector3.up * 0.1f);
        camera.gameObject.AddComponent<UniversalAdditionalCameraData>();

        var demo = new GameObject("Animation test controls").AddComponent<PlayerAnimationTest>();
        demo.player = playerObject.GetComponent<PlayerController>();
        demo.sceneCamera = camera;
        demo.previewController = controller;
        demo.previewBaseClip = idle;
        demo.previews = previews;
        demo.sword = CreateSword(demo.player.GetComponent<CharacterMotor>().Animator);
        var canvas = new GameObject("Test captions", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler)).GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = 1f;
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;
        demo.caption = Label(canvas.transform, "Title", new Vector2(24f, -20f), 30f);
        demo.caption.text = "PLAYER MOVEMENT TEST";
        demo.details = Label(canvas.transform, "Instructions", new Vector2(24f, -64f), 17f);
        demo.details.text = "WASD / Shift / Space / Mouse  |  Escape releases cursor";
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log($"[Animation Test Scene] Saved {ScenePath} with {previews.Length} animation previews.");
        PlayerCameraSetup.Run();
        PlayerAnimationShowcaseSetup.Run();
    }

    private static GameObject Cube(string name, Vector3 position, Vector3 scale, Material material, bool collision = true)
    {
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.layer = LayerMask.NameToLayer("Ground");
        cube.transform.position = position;
        cube.transform.localScale = scale;
        cube.GetComponent<Renderer>().sharedMaterial = material;
        if (!collision)
            Object.DestroyImmediate(cube.GetComponent<Collider>());
        return cube;
    }

    private static TMP_Text Label(Transform parent, string name, Vector2 position, float size)
    {
        var text = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
        text.transform.SetParent(parent, false);
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = size;
        text.color = Color.white;
        text.raycastTarget = false;
        var rect = text.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(1220f, 42f);
        return text;
    }

    private static Material GetMaterial(string name, Color color)
    {
        string path = "Assets/_Development/Prototypes/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0.1f);
            AssetDatabase.CreateAsset(material, path);
        }
        return material;
    }

    private static GameObject CreateSword(Animator animator)
    {
        var definition = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Game/Features/Inventory/Data/Items/ShortSword.asset");
        var sword = Object.Instantiate(definition.itemStats.GetPrefab());
        sword.name = "Sword pose preview (no damage)";
        foreach (var script in sword.GetComponentsInChildren<MonoBehaviour>(true))
            Object.DestroyImmediate(script);
        foreach (var collider in sword.GetComponentsInChildren<Collider>(true))
            Object.DestroyImmediate(collider);
        foreach (var body in sword.GetComponentsInChildren<Rigidbody>(true))
            Object.DestroyImmediate(body);
        sword.transform.SetParent(animator.GetBoneTransform(HumanBodyBones.RightHand), false);
        sword.transform.localPosition = Vector3.zero;
        sword.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        var renderers = sword.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            foreach (var renderer in renderers)
                bounds.Encapsulate(renderer.bounds);
            sword.transform.localScale *= 0.85f / Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        }
        sword.SetActive(false);
        return sword;
    }
}
