using UnityEditor.Experimental.GraphView;

namespace NodeTree.Editor
{
    [NodeFor(typeof(EndNodeData))]
    public class EndNode : BaseNode
    {
        public EndNode()
        {
            title = "Finished";

            inputContainer.Add(NodeVisualElementFactory.CreateInputPort(Port.Capacity.Multi));
        }

        protected override NodeData CreateData()
        {
            return new EndNodeData { };
        }
    }
}
