using System.Collections.Generic;
using UnityEngine;


namespace NodeTree
{
    // There should only ever be one of these. The editor creates it at Assets/Resources/NodeTree/DialogOwners.asset
    // so the runtime can find it with Resources.Load without any scene reference.
    public class DialogOwnerDatabaseSO : ScriptableObject
    {
        public const string ResourcePath = "NodeTree/DialogOwners";
        public const string AssetPath = "Assets/Resources/NodeTree/DialogOwners.asset";

        public List<DialogOwner> Owners = new List<DialogOwner>();

        public static DialogOwnerDatabaseSO Load()
        {
            return Resources.Load<DialogOwnerDatabaseSO>(ResourcePath);
        }

        public DialogOwner GetOwner(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            foreach (var owner in Owners)
                if (owner != null && owner.Id == id)
                    return owner;

            return null;
        }

        public Sprite GetPortrait(string id, string mood)
        {
            return GetOwner(id)?.GetPortrait(mood);
        }
    }
}
