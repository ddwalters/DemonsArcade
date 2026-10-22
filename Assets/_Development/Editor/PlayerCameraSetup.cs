using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PlayerCameraSetup
{
    private const string PrefabPath = "Assets/_Game/Features/Player/Prefabs/player.prefab";

    [MenuItem("Tools/Player/Configure Third Person Camera")]
    public static void Run()
    {
        if (EditorApplication.isPlaying)
            throw new System.InvalidOperationException("Exit Play Mode before configuring the cameras.");

        ConfigurePrefab();
        // Refresh the reusable scene, preserving its showcase and external-camera setup.
        var scene = EditorSceneManager.OpenScene(PlayerAnimationSceneSetup.ScenePath);
        var demo = Object.FindAnyObjectByType<PlayerAnimationTest>();
        if (demo.sceneCamera.GetComponent<AudioListener>() == null)
            demo.sceneCamera.gameObject.AddComponent<AudioListener>();
        demo.player.GetComponent<PlayerCameraRig>().ViewCamera.GetComponent<AudioListener>().enabled = false;
        demo.details.text = "WASD / Shift / Space / Mouse  |  Code-driven movement  |  Camera button: player / overview";
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[Player Camera Setup] Configured one gameplay camera and view pivot.");
    }

    public static void ConfigurePrefab()
    {
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var camera = root.GetComponentInChildren<Camera>(true);
            var pivot = root.transform.Find("ViewPivot");
            if (pivot == null)
            {
                pivot = new GameObject("ViewPivot").transform;
                pivot.SetParent(root.transform, false);
                pivot.localPosition = camera.transform.localPosition;
                camera.transform.SetParent(pivot, false);
                camera.transform.localPosition = Vector3.zero;
                camera.transform.localRotation = Quaternion.identity;
            }
            var rig = root.GetComponent<PlayerCameraRig>() ?? root.AddComponent<PlayerCameraRig>();
            var data = new SerializedObject(rig);
            data.FindProperty("viewCamera").objectReferenceValue = camera;
            data.FindProperty("viewPivot").objectReferenceValue = pivot;
            data.ApplyModifiedPropertiesWithoutUndo();
            rig.ApplyFieldOfView();
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

    }
}
