using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Stable asset GUIDs are baked into the catalog, so player builds never need AssetDatabase.
public class SaveCatalogBuilder : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report) => Refresh();

    [MenuItem("Tools/Save System/Refresh Item Catalog")]
    public static void Refresh()
    {
        var catalog = Resources.Load<ItemCatalog>("ItemCatalog");
        if (catalog == null)
            throw new BuildFailedException("The save item catalog is missing.");

        var definitions = new List<ItemData>();
        foreach (string guid in AssetDatabase.FindAssets("t:ItemData", new[] { "Assets/_Game" }))
        {
            var definition = AssetDatabase.LoadAssetAtPath<ItemData>(AssetDatabase.GUIDToAssetPath(guid));
            if (definition.SaveId != guid)
            {
                var serialized = new SerializedObject(definition);
                serialized.FindProperty("saveId").stringValue = guid;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            definitions.Add(definition);
        }

        if (!catalog.definitions.SequenceEqual(definitions))
        {
            catalog.definitions = definitions;
            EditorUtility.SetDirty(catalog);
        }

        AssetDatabase.SaveAssets();
    }
}
