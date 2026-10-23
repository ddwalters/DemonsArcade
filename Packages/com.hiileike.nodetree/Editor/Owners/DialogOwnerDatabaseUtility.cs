using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace NodeTree.Editor
{
    public static class DialogOwnerDatabaseUtility
    {
        // Raised after any edit so open windows can refresh their owner lists.
        public static event Action Changed;

        public static void NotifyChanged() => Changed?.Invoke();

        // Returns the single database without creating one.
        public static DialogOwnerDatabaseSO Find()
        {
            var atPath = AssetDatabase.LoadAssetAtPath<DialogOwnerDatabaseSO>(DialogOwnerDatabaseSO.AssetPath);
            if (atPath != null)
                return atPath;

            var guids = AssetDatabase.FindAssets("t:DialogOwnerDatabaseSO");
            if (guids.Length == 0)
                return null;

            if (guids.Length > 1)
                Debug.LogWarning("More than one DialogOwnerDatabaseSO exists. Only the first is used; delete the extras.");

            return AssetDatabase.LoadAssetAtPath<DialogOwnerDatabaseSO>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        public static DialogOwnerDatabaseSO GetOrCreate()
        {
            var db = Find();
            if (db != null)
                return db;

            Directory.CreateDirectory(Path.GetDirectoryName(DialogOwnerDatabaseSO.AssetPath));
            db = ScriptableObject.CreateInstance<DialogOwnerDatabaseSO>();
            AssetDatabase.CreateAsset(db, DialogOwnerDatabaseSO.AssetPath);
            AssetDatabase.SaveAssets();
            return db;
        }

        public static DialogOwner AddOwner(DialogOwnerDatabaseSO db)
        {
            Undo.RecordObject(db, "Add Dialog Owner");
            var owner = new DialogOwner();
            db.Owners.Add(owner);
            Commit(db);
            return owner;
        }

        public static void RemoveOwner(DialogOwnerDatabaseSO db, DialogOwner owner)
        {
            Undo.RecordObject(db, "Remove Dialog Owner");
            db.Owners.Remove(owner);
            Commit(db);
        }

        // Call after mutating an owner in place.
        public static void Commit(DialogOwnerDatabaseSO db)
        {
            EditorUtility.SetDirty(db);
            NotifyChanged();
        }
    }
}
