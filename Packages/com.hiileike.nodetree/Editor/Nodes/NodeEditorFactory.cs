using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;

namespace NodeTree.Editor
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class NodeForAttribute : Attribute
    {
        public Type DataType { get; }

        public NodeForAttribute(Type dataType)
        {
            DataType = dataType;
        }
    }

    public static class NodeEditorFactory
    {
        private static Dictionary<Type, Type> _dataToNodeMap;
        private static bool _isInitialized = false;

        private static void Initialize()
        {
            if (_isInitialized) return;

            _dataToNodeMap = new Dictionary<Type, Type>();

            var nodeTypes = TypeCache.GetTypesWithAttribute<NodeForAttribute>()
                .Where(x => typeof(BaseNode).IsAssignableFrom(x) && !x.IsAbstract);

            foreach (var nodeType in nodeTypes)
            {
                var nodeAttribute = nodeType.GetCustomAttribute<NodeForAttribute>();
                _dataToNodeMap.Add(nodeAttribute.DataType, nodeType);
            }

            _isInitialized = true;
        }

        public static BaseNode CreateNode(NodeData nodeData)
        {
            if (nodeData == null)
            {
                UnityEngine.Debug.LogWarning("NodeEditorFactory: skipping null NodeData entry (type was likely moved between assemblies — resave this graph).");
                return null;
            }

            Initialize();
            Type dataType = nodeData.GetType();

            if (_dataToNodeMap.TryGetValue(dataType, out Type nodeType))
                return (BaseNode)Activator.CreateInstance(nodeType);

            return null;
        }
    }
}
