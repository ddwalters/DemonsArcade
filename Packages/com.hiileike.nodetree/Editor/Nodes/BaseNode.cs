using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace NodeTree.Editor
{
    public abstract class BaseNode : Node
    {
        public string GUID { get; set; }

        public BaseNode()
        {
            GUID = Guid.NewGuid().ToString();
            inputContainer.style.height = new StyleLength(20);
        }

        protected abstract NodeData CreateData();

        public NodeData SaveNode()
        {
            NodeData data = CreateData();

            data.Guid = GUID;
            data.Position = GetPosition().position;

            return data;
        }

        public virtual void LoadNode(NodeData data)
        {
            GUID = data.Guid;
            SetPosition(new Rect(data.Position, Vector2.zero));
        }

        public HashSet<BaseNode> GetNodeOutputConnections()
        {
            var connectedNodes = new HashSet<BaseNode>();
            List<Port> outputPorts = outputContainer.Query<Port>().ToList();

            foreach (var port in outputPorts)
            {
                foreach (var edge in port.connections)
                {
                    var connectedNode = edge.input.node as BaseNode;

                    if (connectedNode != null)
                        connectedNodes.Add(connectedNode);
                }
            }

            return connectedNodes;
        }
    }
}
