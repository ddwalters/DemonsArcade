using System;
using System.Linq;
using UnityEngine.Scripting;

namespace NodeTree
{
    [Serializable]
    public class ActionNodeData : NodeData
    {
        public string EventKey;
    }

    [Preserve]
    [RuntimeNodeFor(typeof(ActionNodeData))]
    public class ActionNodeHandler : RuntimeBaseNode
    {
        private ActionNodeData _data;

        public ActionNodeHandler(ActionNodeData data) : base(data)
        {
            _data = data;
        }

        public override void Execute(DialogRunner uiManager, NodeTreeGraphSO graph)
        {
            if (!ConditionsPassed())
            {
                HandleConditionFail(uiManager, graph, conditionFailPortIndex: 1);
                return;
            }

            NodeTreeEvents.Fire(_data.EventKey);

            var edge = graph.Edges.FirstOrDefault(e => e.OutputNodeGuid == _data.Guid && e.OutputPortIndex == 0);
            if (edge != null)
                uiManager.GoToNode(edge.InputNodeGuid);
            else
                uiManager.StopDialog();
        }
    }
}
