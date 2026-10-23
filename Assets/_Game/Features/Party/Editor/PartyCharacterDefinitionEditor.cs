using System.Collections.Generic;
using System.Linq;
using NodeTree;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PartyCharacterDefinition))]
public sealed class PartyCharacterDefinitionEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawPropertiesExcluding(serializedObject, "m_Script", "dialogueOwnerId");
        var ownerId = serializedObject.FindProperty("dialogueOwnerId");
        var database = DialogOwnerDatabaseSO.Load();
        var ids = new List<string> { "" };
        var names = new List<string> { "None" };
        if (database != null)
        {
            foreach (var owner in database.Owners)
            {
                if (owner == null) continue;
                ids.Add(owner.Id);
                names.Add(owner.DisplayName);
            }
        }
        int index = ids.IndexOf(ownerId.stringValue);
        if (index < 0)
        {
            ids.Add(ownerId.stringValue);
            names.Add("Missing character: " + ownerId.stringValue);
            index = ids.Count - 1;
        }
        int chosen = EditorGUILayout.Popup("Node Tree Character", index, names.ToArray());
        string selectedId = ids[chosen];
        bool alreadyUsed = !string.IsNullOrEmpty(selectedId) && GameCharacterAuthoring.Definitions()
            .Any(other => other != target && other.dialogueOwnerId == selectedId);
        if (alreadyUsed && selectedId != ownerId.stringValue)
            EditorGUILayout.HelpBox("That Node Tree character already has a game definition. Open that definition instead.", MessageType.Error);
        else
            ownerId.stringValue = selectedId;
        serializedObject.ApplyModifiedProperties();
        var character = (PartyCharacterDefinition)target;
        using (new EditorGUI.DisabledScope(true)) EditorGUILayout.TextField("Gameplay ID", character.CharacterId);
        if (character.ValidationError != null) EditorGUILayout.HelpBox(character.ValidationError, MessageType.Error);
        if (!GameCharacterAuthoring.IsRuntimeDefinition(character))
            EditorGUILayout.HelpBox("Move this definition into a Resources/Party folder so runtime and saves can resolve it.", MessageType.Error);
        foreach (var other in GameCharacterAuthoring.Definitions())
        {
            if (other == character) continue;
            if (other.CharacterId == character.CharacterId ||
                (!string.IsNullOrEmpty(character.dialogueOwnerId) && other.dialogueOwnerId == character.dialogueOwnerId))
                EditorGUILayout.HelpBox("Duplicate character link or gameplay ID: " + AssetDatabase.GetAssetPath(other), MessageType.Error);
        }
        if (GUILayout.Button("Audit Character Links")) GameCharacterAuditWindow.Open();
        if (character.DialogueOwner != null)
            EditorGUILayout.ObjectField("Default Portrait", character.DialogueOwner.GetPortrait(null), typeof(Sprite), false);
    }
}
