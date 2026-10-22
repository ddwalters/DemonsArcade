using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Scripting;

namespace NodeTree
{
    [Serializable]
    public class IntConditionCase
    {
        public ComparisonOperator Operator;
        public int Value;
    }

    [Serializable]
    public class IntConditionNodeData : NodeData
    {
        public string Key;
        public List<IntConditionCase> Cases = new List<IntConditionCase>();
        // Ports 0..n-1 = cases in order, port n = default/else
    }

    [Preserve]
    [RuntimeNodeFor(typeof(IntConditionNodeData))]
    public class IntConditionNodeHandler : RuntimeBaseNode
    {
        private IntConditionNodeData _data;

        public IntConditionNodeHandler(IntConditionNodeData data) : base(data)
        {
            _data = data;
        }

        public override void Execute(DialogRunner uiManager, NodeTreeGraphSO graph)
        {
            int current = ConditionContext.GetInt(_data.Key);

            for (int i = 0; i < _data.Cases.Count; i++)
            {
                if (Evaluate(_data.Cases[i], current))
                {
                    var edge = graph.Edges.FirstOrDefault(e => e.OutputNodeGuid == _data.Guid && e.OutputPortIndex == i);
                    if (edge != null)
                    {
                        uiManager.GoToNode(edge.InputNodeGuid);
                        return;
                    }
                }
            }

            var defaultEdge = graph.Edges.FirstOrDefault(e => e.OutputNodeGuid == _data.Guid && e.OutputPortIndex == _data.Cases.Count);
            if (defaultEdge != null)
                uiManager.GoToNode(defaultEdge.InputNodeGuid);
            else
                uiManager.StopDialog();
        }

        private bool Evaluate(IntConditionCase c, int current)
        {
            return c.Operator switch
            {
                ComparisonOperator.Equal              => current == c.Value,
                ComparisonOperator.NotEqual           => current != c.Value,
                ComparisonOperator.LessThan           => current < c.Value,
                ComparisonOperator.LessThanOrEqual    => current <= c.Value,
                ComparisonOperator.GreaterThan        => current > c.Value,
                ComparisonOperator.GreaterThanOrEqual => current >= c.Value,
                _                                     => false
            };
        }
    }
}
