using System;
using UnityEngine.Scripting;


namespace NodeTree
{
    [Serializable]
    public class EndNodeData : NodeData
    {
    }

    [Preserve]
    [RuntimeNodeFor(typeof(EndNodeData))]
    public class EndNodeHandler : RuntimeBaseNode
    {
        private EndNodeData _data;

        public EndNodeHandler(EndNodeData data)
            : base(data)
        {
            _data = data;
        }

        public override void Execute(DialogRunner uiManager, NodeTreeGraphSO graph)
        {
            uiManager.StopDialog();
        }
    }
}
