using System;
using System.Collections.Generic;
using UnityEngine;

namespace NodeTree
{
    public static class NodeTreeEvents
    {
        private static readonly Dictionary<string, Action> _events = new Dictionary<string, Action>();

        public static void Subscribe(string key, Action callback)
        {
            if (string.IsNullOrEmpty(key) || callback == null) return;

            if (_events.ContainsKey(key))
                _events[key] += callback;
            else
                _events[key] = callback;
        }

        public static void Unsubscribe(string key, Action callback)
        {
            if (string.IsNullOrEmpty(key) || callback == null) return;

            if (_events.ContainsKey(key))
                _events[key] -= callback;
        }

        public static void Fire(string key)
        {
            if (string.IsNullOrEmpty(key)) return;

            if (_events.TryGetValue(key, out var action))
                action?.Invoke();
            else
                Debug.LogWarning($"[NodeTree] No subscriber found for event key: '{key}'");
        }

        public static void Clear() => _events.Clear();
    }
}
