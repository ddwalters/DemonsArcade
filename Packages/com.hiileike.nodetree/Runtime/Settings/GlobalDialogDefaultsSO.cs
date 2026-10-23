using UnityEngine;

namespace NodeTree
{
    public class GlobalDialogDefaultsSO : ScriptableObject
    {
        public const string ResourcePath = "NodeTree/GlobalDialogDefaults";
        public const string AssetPath = "Assets/Resources/NodeTree/GlobalDialogDefaults.asset";

        public DialogSettings Settings = new DialogSettings();

        private static GlobalDialogDefaultsSO _fallback;

        public static GlobalDialogDefaultsSO Load()
        {
            return Resources.Load<GlobalDialogDefaultsSO>(ResourcePath);
        }

        public static GlobalDialogDefaultsSO Current
        {
            get
            {
                var loaded = Load();
                if (loaded != null)
                    return loaded;

                if (_fallback == null)
                {
                    _fallback = CreateInstance<GlobalDialogDefaultsSO>();
                    _fallback.hideFlags = HideFlags.HideAndDontSave;
                }

                return _fallback;
            }
        }
    }
}
