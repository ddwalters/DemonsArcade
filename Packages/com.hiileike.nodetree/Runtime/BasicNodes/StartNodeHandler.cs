using System;
using System.Linq;
using UnityEngine.Scripting;


namespace NodeTree
{
    [Serializable]
    public class StartNodeData : NodeData
    {
    }

    [Preserve]
    [RuntimeNodeFor(typeof(StartNodeData))]
    public class StartNodeHandler : RuntimeBaseNode
    {
        private StartNodeData _data;

        public StartNodeHandler(StartNodeData data)
            : base(data)
        {
            _data = data;
        }

        public override void Execute(DialogRunner uiManager, NodeTreeGraphSO graph)
        {
            var connectedEdge = graph.Edges.FirstOrDefault(x => x.OutputNodeGuid == _data.Guid);
            if (connectedEdge != null)
                uiManager.GoToNode(connectedEdge.InputNodeGuid);
        }
    }
}
