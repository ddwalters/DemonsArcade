using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;


namespace NodeTree
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class RuntimeNodeForAttribute : Attribute
    {
        public Type DataType { get; }

        public RuntimeNodeForAttribute(Type dataType)
        {
            DataType = dataType;
        }
    }

    public static class NodeRuntimeFactory
    {
        // This simply maps my nodes to their types on initialization. I can add attributes instead of adding to an if anytime I want to add a new type.
        private static Dictionary<Type, Type> _nodeTypeMap;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeFactory()
        {
            if (_nodeTypeMap != null) return;

            _nodeTypeMap = new Dictionary<Type, Type>();

            var runtimeNodeTypes = Assembly.GetExecutingAssembly().GetTypes()
                .Where(x => x.IsSubclassOf(typeof(RuntimeBaseNode)) && !x.IsAbstract);

            foreach (var type in runtimeNodeTypes)
            {
                var attribute = type.GetCustomAttribute<RuntimeNodeForAttribute>();
                if (attribute != null)
                    _nodeTypeMap.Add(attribute.DataType, type);
            }
        }

        public static RuntimeBaseNode CreateRuntimeNode(NodeData nodeData)
        {
            if (nodeData == null)
            {
                Debug.LogError("Tried to create a node from null data!");
                return null;
            }

            if (_nodeTypeMap == null)
                InitializeFactory();

            var dataType = nodeData.GetType();
            if (_nodeTypeMap.TryGetValue(dataType, out var handlerType))
                return (RuntimeBaseNode)Activator.CreateInstance(handlerType, nodeData);

            Debug.LogError($"NodeFactory doesn't know how to create a runtime node for {dataType}");
            return null;
        }
    }
}
