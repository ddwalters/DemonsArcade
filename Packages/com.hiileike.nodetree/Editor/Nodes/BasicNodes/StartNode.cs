using UnityEditor.Experimental.GraphView;

namespace NodeTree.Editor
{
    [NodeFor(typeof(StartNodeData))]
    public class StartNode : BaseNode
    {
        public StartNode()
        {
            title = "Start";

            outputContainer.Add(NodeVisualElementFactory.CreateOutputPort(Port.Capacity.Single));
        }

        protected override NodeData CreateData()
        {
            return new StartNodeData { };
        }
    }
}
