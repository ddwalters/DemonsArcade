using System;
using System.Linq;
using UnityEngine.Scripting;

namespace NodeTree
{
    [Serializable]
    public class BoolConditionNodeData : NodeData
    {
        public string Key;
        // Port 0 = true branch, port 1 = false branch
    }

    [Preserve]
    [RuntimeNodeFor(typeof(BoolConditionNodeData))]
    public class BoolConditionNodeHandler : RuntimeBaseNode
    {
        private BoolConditionNodeData _data;

        public BoolConditionNodeHandler(BoolConditionNodeData data) : base(data)
        {
            _data = data;
        }

        public override void Execute(DialogRunner uiManager, NodeTreeGraphSO graph)
        {
            int portIndex = ConditionContext.GetBool(_data.Key) ? 0 : 1;
            var edge = graph.Edges.FirstOrDefault(e => e.OutputNodeGuid == _data.Guid && e.OutputPortIndex == portIndex);

            if (edge != null)
                uiManager.GoToNode(edge.InputNodeGuid);
            else
                uiManager.StopDialog();
        }
    }
}
