using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;

namespace NodeTree.Editor
{
    [NodeFor(typeof(StartNewTreeNodeData))]
    public class StartNewTreeNode : BaseNode
    {
        private ObjectField _nodeTreeField;

        public NodeTreeGraphSO NodeTreeGraphSO { get; set; }

        public StartNewTreeNode()
        {
            title = "Start New Tree";

            inputContainer.Add(NodeVisualElementFactory.CreateInputPort(Port.Capacity.Multi));

            _nodeTreeField = NodeVisualElementFactory.CreateObjectField(typeof(NodeTreeGraphSO), NodeTreeGraphSO, newValue => { NodeTreeGraphSO = newValue as NodeTreeGraphSO; });
            mainContainer.Add(_nodeTreeField);
        }

        protected override NodeData CreateData()
        {
            return new StartNewTreeNodeData
            {
                NodeTreeGraphSO = NodeTreeGraphSO
            };
        }

        public override void LoadNode(NodeData data)
        {
            base.LoadNode(data);

            if (data is not StartNewTreeNodeData d)
                return;

            NodeTreeGraphSO = d.NodeTreeGraphSO;
            _nodeTreeField?.SetValueWithoutNotify(NodeTreeGraphSO);
        }
    }
}
