using UnityEditor;
using UnityEngine;

namespace NodeTree.Editor
{
    [CustomEditor(typeof(DialogOwnerDatabaseSO))]
    public class DialogOwnerDatabaseInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("Edit characters in the Dialog Owners window.", MessageType.Info);
            if (GUILayout.Button("Open Character Editor", GUILayout.Height(30)))
                DialogOwnerEditorWindow.Open();
        }
    }
}
