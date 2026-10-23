using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;


namespace NodeTree.Editor
{
    public class AssetHandler
    {
        [OnOpenAsset]
        public static bool OnOpenAsset(EntityId entityId, int line)
        {
            var asset = EditorUtility.EntityIdToObject(entityId);

            if (asset is NodeTreeGraphSO dialogueGraph)
            {
                NodeTree.OpenWindow(dialogueGraph);
                return true;
            }

            if (asset is DialogOwnerDatabaseSO)
            {
                DialogOwnerEditorWindow.Open();
                return true;
            }

            return false;
        }
    }
}
