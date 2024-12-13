using System.Linq;
using UnityEditor;

[CustomEditor(typeof(Portal))]
public class PortalEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        Portal portal = (Portal)target;

        // Fetch all scenes
        var scenes = EditorBuildSettings.scenes;
        string[] sceneNames = scenes.Select(scene => System.IO.Path.GetFileNameWithoutExtension(scene.path)).ToArray();

        // Create a dropdown
        int selectedIndex = EditorGUILayout.Popup("Select Scene", portal.GetSceneIndex(), sceneNames);

        // Update the selected scene when the user changes the dropdown
        if (selectedIndex != portal.GetSceneIndex())
        {
            portal.SetSceneIndex(selectedIndex);
            EditorUtility.SetDirty(target); // Mark the object as dirty to enable saving
        }
    }
}