using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NodeTree;
using NodeTree.Editor;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.UIElements;

[InitializeOnLoad]
public static class GameCharacterAuthoring
{
    static GameCharacterAuthoring()
    {
        DialogOwnerEditorWindow.ExtendCharacterDetails += DrawSection;
        DialogOwnerEditorWindow.CharacterDeletionWarning += DeletionWarning;
        EditorApplication.projectChanged += DialogOwnerDatabaseUtility.NotifyChanged;
    }

    public static PartyCharacterDefinition[] Definitions() => AssetDatabase.FindAssets("t:PartyCharacterDefinition")
        .Select(guid => AssetDatabase.LoadAssetAtPath<PartyCharacterDefinition>(AssetDatabase.GUIDToAssetPath(guid)))
        .Where(definition => definition != null).ToArray();

    public static bool IsRuntimeDefinition(PartyCharacterDefinition definition)
    {
        string path = AssetDatabase.GetAssetPath(definition);
        return path.Contains("/Resources/Party/");
    }

    public static List<string> Issues()
    {
        var result = new List<string>();
        var definitions = Definitions();
        var database = DialogOwnerDatabaseSO.Load();
        if (database == null)
        {
            result.Add("Node Tree character database is missing.");
            return result;
        }
        var ownerIds = new HashSet<string>();
        foreach (var owner in database.Owners)
        {
            if (owner == null || string.IsNullOrWhiteSpace(owner.Id) || !ownerIds.Add(owner.Id))
            {
                result.Add("Node Tree contains a missing or duplicate character ID.");
                continue;
            }
            if (!definitions.Any(definition => definition.dialogueOwnerId == owner.Id))
                result.Add("No game definition: " + owner.DisplayName);
        }
        var gameplayIds = new HashSet<string>();
        var links = new HashSet<string>();
        foreach (var definition in definitions)
        {
            string path = AssetDatabase.GetAssetPath(definition);
            if (definition.ValidationError != null) result.Add(path + ": " + definition.ValidationError);
            if (!IsRuntimeDefinition(definition)) result.Add(path + ": Move this definition into a Resources/Party folder.");
            if (!gameplayIds.Add(definition.CharacterId)) result.Add(path + ": Duplicate gameplay ID.");
            if (!links.Add(definition.dialogueOwnerId)) result.Add(path + ": Node Tree character is linked more than once.");
        }
        if (definitions.Count(definition => definition.isPlayer) != 1)
            result.Add("Exactly one local-player definition is required.");
        return result;
    }

    public static PartyCharacterDefinition CreateDefinition(DialogOwner owner)
    {
        if (owner == null || string.IsNullOrWhiteSpace(owner.Id)) throw new ArgumentException("Select a valid Node Tree character.");
        var existing = Definitions().Where(definition => definition.dialogueOwnerId == owner.Id).ToArray();
        if (existing.Length > 1) throw new InvalidOperationException("Resolve duplicate character links before creating another definition.");
        if (existing.Length == 1) return existing[0];

        const string folder = "Assets/Resources/Party";
        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Resources", "Party");
        var definition = ScriptableObject.CreateInstance<PartyCharacterDefinition>();
        definition.dialogueOwnerId = owner.Id;
        var serialized = new SerializedObject(definition);
        serialized.FindProperty("characterId").stringValue = Guid.NewGuid().ToString("N");
        serialized.ApplyModifiedPropertiesWithoutUndo();
        string fileName = new string(owner.DisplayName.Select(character => Path.GetInvalidFileNameChars().Contains(character) ? '_' : character).ToArray());
        if (string.IsNullOrWhiteSpace(fileName)) fileName = "Character";
        AssetDatabase.CreateAsset(definition, AssetDatabase.GenerateUniqueAssetPath(folder + "/" + fileName + ".asset"));
        Undo.RegisterCreatedObjectUndo(definition, "Create Game Character Definition");
        AssetDatabase.SaveAssets();
        return definition;
    }

    private static void DrawSection(DialogOwner owner, VisualElement parent)
    {
        var section = new VisualElement();
        section.style.marginTop = 12;
        section.Add(new Label("Game Character Definition"));
        var matches = Definitions().Where(definition => definition.dialogueOwnerId == owner.Id).ToArray();
        if (matches.Length == 0)
        {
            section.Add(new HelpBox("This character has no game definition. Create one before using it in the game.", HelpBoxMessageType.Warning));
            section.Add(new Button(() => { Selection.activeObject = CreateDefinition(owner); DialogOwnerDatabaseUtility.NotifyChanged(); }) { text = "Create Game Definition" });
        }
        else
        {
            foreach (var definition in matches)
            {
                section.Add(new Button(() => Selection.activeObject = definition) { text = "Open " + definition.name });
                if (definition.ValidationError != null) section.Add(new HelpBox(definition.ValidationError, HelpBoxMessageType.Error));
            }
            if (matches.Length > 1) section.Add(new HelpBox("Duplicate links: only one game definition may select this character.", HelpBoxMessageType.Error));
        }
        section.Add(new Button(GameCharacterAuditWindow.Open) { text = "Audit Character Links" });
        parent.Add(section);
    }

    public static string[] AssetReferences(string assetPath)
    {
        string guid = AssetDatabase.AssetPathToGUID(assetPath);
        if (string.IsNullOrEmpty(guid)) return Array.Empty<string>();
        return AssetDatabase.GetAllAssetPaths().Where(path => path.StartsWith("Assets/") && path != assetPath &&
            (path.EndsWith(".asset") || path.EndsWith(".prefab") || path.EndsWith(".unity")))
            .Where(path => File.Exists(path) && File.ReadAllText(path).Contains(guid)).ToArray();
    }

    private static string DeletionWarning(DialogOwner owner)
    {
        var definitions = Definitions().Where(definition => definition.dialogueOwnerId == owner.Id).ToArray();
        if (definitions.Length == 0) return "No game definition is paired with this character.";
        var references = definitions.SelectMany(definition => new[] { AssetDatabase.GetAssetPath(definition) }
            .Concat(AssetReferences(AssetDatabase.GetAssetPath(definition)))).Distinct();
        return "This will break the Node Tree link in these game definitions/references:\n" + string.Join("\n", references) +
            "\nExisting saves keep their gameplay IDs, but registration is blocked until the definitions are relinked.";
    }
}

public sealed class GameCharacterAuditWindow : EditorWindow
{
    private List<string> issues = new();
    private Vector2 scroll;
    [MenuItem("Tools/Characters/Audit Character Links")]
    public static void Open()
    {
        var window = GetWindow<GameCharacterAuditWindow>("Character Links");
        window.Refresh();
        window.Show();
    }
    private void OnFocus() => Refresh();
    private void Refresh() => issues = GameCharacterAuthoring.Issues();
    private void OnGUI()
    {
        if (GUILayout.Button("Refresh Audit")) Refresh();
        EditorGUILayout.LabelField("Character identity and pairing", EditorStyles.boldLabel);
        if (issues.Count == 0) EditorGUILayout.HelpBox("Every Node Tree character has one valid game definition.", MessageType.Info);
        scroll = EditorGUILayout.BeginScrollView(scroll);
        foreach (string issue in issues) EditorGUILayout.HelpBox(issue, MessageType.Error);
        EditorGUILayout.EndScrollView();
    }
}

public sealed class GameCharacterBuildValidation : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;
    public void OnPreprocessBuild(BuildReport report)
    {
        var issues = GameCharacterAuthoring.Issues();
        if (issues.Count > 0) throw new BuildFailedException("Character links must be fixed before building:\n" + string.Join("\n", issues));
    }
}

public sealed class GameCharacterDeletionGuard : AssetModificationProcessor
{
    private static AssetDeleteResult OnWillDeleteAsset(string path, RemoveAssetOptions options)
    {
        var definition = AssetDatabase.LoadAssetAtPath<PartyCharacterDefinition>(path);
        var database = AssetDatabase.LoadAssetAtPath<DialogOwnerDatabaseSO>(path);
        if (definition == null && database == null)
        {
            if (!AssetDatabase.IsValidFolder(path)) return AssetDeleteResult.DidNotDelete;
            var descendants = GameCharacterAuthoring.Definitions()
                .Where(item => AssetDatabase.GetAssetPath(item).StartsWith(path + "/")).ToArray();
            bool containsDatabase = DialogOwnerDatabaseSO.AssetPath.StartsWith(path + "/");
            if (descendants.Length == 0 && !containsDatabase) return AssetDeleteResult.DidNotDelete;
            string folderWarning = "This folder contains character data. Deleting it can break existing saves and character links.\n\n" +
                string.Join("\n", descendants.SelectMany(item => new[] { AssetDatabase.GetAssetPath(item) }
                    .Concat(GameCharacterAuthoring.AssetReferences(AssetDatabase.GetAssetPath(item)))).Distinct());
            if (containsDatabase) folderWarning += "\n" + DialogOwnerDatabaseSO.AssetPath;
            return EditorUtility.DisplayDialog("Delete character data folder?", folderWarning, "Delete", "Cancel")
                ? AssetDeleteResult.DidNotDelete : AssetDeleteResult.FailedDelete;
        }
        string warning = definition != null
            ? "Deleting this game definition leaves its Node Tree character unpaired. Existing saves may reference gameplay ID " + definition.CharacterId + ".\n\nReferences:\n" + string.Join("\n", GameCharacterAuthoring.AssetReferences(path))
            : "Deleting the character database breaks dialogue and all linked game definitions.\n\n" + string.Join("\n", GameCharacterAuthoring.Definitions().Select(item => AssetDatabase.GetAssetPath(item)));
        return EditorUtility.DisplayDialog("Delete character data?", warning, "Delete", "Cancel")
            ? AssetDeleteResult.DidNotDelete : AssetDeleteResult.FailedDelete;
    }
}
