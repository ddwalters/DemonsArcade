using System;
using UnityEngine.Scripting;


namespace NodeTree
{
    [Serializable]
    public class StartNewTreeNodeData : NodeData
    {
        public NodeTreeGraphSO NodeTreeGraphSO;
    }

    [Preserve]
    [RuntimeNodeFor(typeof(StartNewTreeNodeData))]
    public class StartNewTreeNodeHandler : RuntimeBaseNode
    {
        private StartNewTreeNodeData _data;

        public StartNewTreeNodeHandler(StartNewTreeNodeData data)
            : base(data)
        {
            _data = data;
        }

        public override void Execute(DialogRunner uiManager, NodeTreeGraphSO graph)
        {
            if (_data.NodeTreeGraphSO != null)
                uiManager.StartDialog(_data.NodeTreeGraphSO);
        }
    }
}
