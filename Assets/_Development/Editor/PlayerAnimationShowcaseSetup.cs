using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class PlayerAnimationShowcaseSetup
{
    private const string Controller = "Assets/_Development/Prototypes/AnimationShowcase.controller";
    private static readonly string[] Actions =
    {
        "Jumping", "Running Jump", "Jump Backward", "Jumping Down", "Falling Idle", "Falling To Landing",
        "Start Walking", "Stop Walking", "Walking Turn 180", "Withdrawing Sword", "CombatIdle",
        "Run With Sword", "Standing Melee Run Jump Attack", "Stable Sword Outward Slash", "Sheathing Sword",
        "Falling Back Death", "Falling Forward Death", "Flying Back Death"
    };

    [MenuItem("Tools/Player/Configure Animation Showcase")]
    public static void Run()
    {
        foreach (string name in Actions.Where(name => name != "Falling Idle"))
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(PathFor(name));
            var clips = importer.defaultClipAnimations;
            foreach (var clip in clips)
            {
                clip.loopTime = name == "CombatIdle" || name == "Run With Sword";
                clip.loopPose = clip.loopTime;
                clip.lockRootRotation = !name.Contains("Turn");
                clip.keepOriginalOrientation = true;
                clip.lockRootHeightY = true;
                clip.keepOriginalPositionY = true;
                clip.lockRootPositionXZ = false;
                clip.keepOriginalPositionXZ = true;
            }
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        // A separate controller keeps this choreography out of the gameplay prefab.
        var source = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/_Game/Features/Player/Animations/Controllers/PlayerLocomotion.controller");
        // CopyAsset correctly copies the state machine/tree subassets as well.
        AssetDatabase.DeleteAsset(Controller);
        AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(source), Controller);
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(Controller);
        foreach (string name in Actions)
        {
            var state = controller.layers[0].stateMachine.AddState(name);
            state.motion = AssetDatabase.LoadAllAssetsAtPath(PathFor(name)).OfType<AnimationClip>()
                .First(clip => !clip.name.StartsWith("__preview__"));
            Debug.Log($"[Action Setup] {name}: {state.motion.averageDuration:F3}s");
        }
        EditorUtility.SetDirty(controller);
        var scene = EditorSceneManager.OpenScene(PlayerAnimationSceneSetup.ScenePath);
        var demo = Object.FindAnyObjectByType<PlayerAnimationTest>();
        var showcase = demo.GetComponent<PlayerAnimationShowcase>() ?? demo.gameObject.AddComponent<PlayerAnimationShowcase>();
        showcase.demo = demo;
        showcase.controller = controller;
        demo.showcase = showcase;
        var floor = GameObject.Find("Floor");
        floor.transform.localScale = new Vector3(100f, 1f, 100f);
        var existingOverlay = demo.caption.canvas.transform.Find("Scenario fade");
        if (existingOverlay != null)
            Object.DestroyImmediate(existingOverlay.gameObject);
        var overlay = new GameObject("Scenario fade", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        overlay.transform.SetParent(demo.caption.canvas.transform, false);
        var rect = (RectTransform)overlay.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        overlay.GetComponent<Image>().raycastTarget = false;
        overlay.GetComponent<Image>().color = Color.clear;
        showcase.fadeOverlay = overlay.GetComponent<Image>();
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    private static string PathFor(string name) => AssetDatabase.GUIDToAssetPath(
        AssetDatabase.FindAssets(name + " t:Model").First(guid =>
            System.IO.Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(guid)) == name));
}
