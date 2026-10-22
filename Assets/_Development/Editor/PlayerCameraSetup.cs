using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public static class PlayerCameraSetup
{
    private const string PrefabPath = "Assets/_Game/Features/Player/Prefabs/player.prefab";

    [MenuItem("Tools/Player/Configure Third Person Camera")]
    public static void Run()
    {
        if (EditorApplication.isPlaying)
            throw new System.InvalidOperationException("Exit Play Mode before configuring the cameras.");

        ConfigurePrefab();
        ConfigureSettings();
        // Refresh the reusable scene, preserving its showcase and external-camera setup.
        var scene = EditorSceneManager.OpenScene(PlayerAnimationSceneSetup.ScenePath);
        var demo = Object.FindAnyObjectByType<PlayerAnimationTest>();
        if (demo.sceneCamera.GetComponent<AudioListener>() == null)
            demo.sceneCamera.gameObject.AddComponent<AudioListener>();
        demo.player.GetComponent<PlayerCameraRig>().ViewCamera.GetComponent<AudioListener>().enabled = false;
        demo.details.text = "WASD / Shift / Space / Mouse  |  Third-person camera | Camera button: player / overview";
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
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

    }

    private static void ConfigureSettings()
    {
        foreach (string path in new[] { "Assets/_Game/UI/Menus/Prefabs/FrontEndUI.prefab", "Assets/_Game/UI/Menus/Prefabs/GameUI.prefab" })
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var settings = root.GetComponentsInChildren<SettingsScreen>(true)
                    .Single(screen => screen.GetType() == typeof(SettingsScreen));
                if (settings.thirdPersonFov == null)
                {
                    foreach (RectTransform child in settings.transform)
                    {
                        float y = child.anchoredPosition.y;
                        float nextY = y;
                        if (Mathf.Approximately(y, 180f))
                            nextY = 240f;
                        else if (Mathf.Approximately(y, 90f))
                            nextY = 165f;
                        else if (Mathf.Approximately(y, 0f))
                            nextY = 90f;
                        else if (Mathf.Approximately(y, -90f))
                            nextY = -135f;
                        else if (Mathf.Approximately(y, -180f))
                            nextY = -210f;
                        else if (Mathf.Approximately(y, -270f))
                            nextY = -285f;
                        child.anchoredPosition = new Vector2(child.anchoredPosition.x, nextY);
                    }
                    settings.thirdPersonFov = FovSlider(settings, "ThirdPersonFov", "Camera FOV", -60f, out settings.thirdPersonFovText);
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }

    private static Slider FovSlider(SettingsScreen settings, string name, string text, float y, out TMP_Text valueText)
    {
        var slider = Object.Instantiate(settings.sensitivity, settings.transform);
        slider.name = name;
        slider.onValueChanged = new Slider.SliderEvent();
        slider.minValue = PlayerCameraRig.MinFov;
        slider.maxValue = PlayerCameraRig.MaxFov;
        slider.wholeNumbers = true;
        slider.SetValueWithoutNotify(PlayerCameraRig.DefaultFov);
        ((RectTransform)slider.transform).anchoredPosition = new Vector2(90f, y);
        var label = Object.Instantiate(settings.GetComponentsInChildren<TMP_Text>(true)
            .Single(label => label.transform.parent == settings.transform && label.text == "Sensitivity"), settings.transform);
        label.name = name + "Label";
        label.text = text;
        label.rectTransform.anchoredPosition = new Vector2(-450f, y);
        valueText = Object.Instantiate(settings.sensitivityText, settings.transform);
        valueText.name = name + "Value";
        valueText.text = PlayerCameraRig.DefaultFov.ToString("0") + "°";
        valueText.rectTransform.anchoredPosition = new Vector2(500f, y);
        return slider;
    }
}
