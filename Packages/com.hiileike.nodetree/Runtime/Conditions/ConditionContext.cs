using System.Collections.Generic;

namespace NodeTree
{
    public static class ConditionContext
    {
        private static readonly Dictionary<string, bool> _bools = new Dictionary<string, bool>();
        private static readonly Dictionary<string, int> _ints = new Dictionary<string, int>();

        public static void SetBool(string key, bool value) => _bools[key] = value;
        public static void SetInt(string key, int value) => _ints[key] = value;

        public static bool GetBool(string key) => _bools.TryGetValue(key, out var v) ? v : false;
        public static int GetInt(string key) => _ints.TryGetValue(key, out var v) ? v : 0;

        public static void Clear()
        {
            _bools.Clear();
            _ints.Clear();
        }
    }
}
