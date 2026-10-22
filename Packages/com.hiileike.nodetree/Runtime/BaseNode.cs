using System.Linq;

namespace NodeTree
{
    public abstract class RuntimeBaseNode
    {
        protected NodeData baseData;

        public RuntimeBaseNode(NodeData data)
        {
            baseData = data;
        }

        public abstract void Execute(DialogRunner uiManager, NodeTreeGraphSO graph);

        protected bool ConditionsPassed()
        {
            if (baseData.Conditions == null || baseData.Conditions.Count == 0)
                return true;

            foreach (var condition in baseData.Conditions)
                if (condition != null && !condition.Evaluate()) return false;

            return true;
        }

        protected void HandleConditionFail(DialogRunner uiManager, NodeTreeGraphSO graph, int conditionFailPortIndex)
        {
            switch (baseData.ConditionFailBehavior)
            {
                case ConditionFailBehavior.Stop:
                    uiManager.StopDialog();
                    break;

                case ConditionFailBehavior.Skip:
                    var skipEdge = graph.Edges.FirstOrDefault(e => e.OutputNodeGuid == baseData.Guid && e.OutputPortIndex == 0);
                    if (skipEdge != null) uiManager.GoToNode(skipEdge.InputNodeGuid);
                    else uiManager.StopDialog();
                    break;

                case ConditionFailBehavior.Jump:
                    var jumpEdge = graph.Edges.FirstOrDefault(e => e.OutputNodeGuid == baseData.Guid && e.OutputPortIndex == conditionFailPortIndex);
                    if (jumpEdge != null) uiManager.GoToNode(jumpEdge.InputNodeGuid);
                    else uiManager.StopDialog();
                    break;
            }
        }
    }
}
