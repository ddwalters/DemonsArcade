using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace NodeTree.Editor
{
    [NodeFor(typeof(IntConditionNodeData))]
    public class IntConditionNode : BaseNode
    {
        private TextField _keyField;
        public string Key { get; set; } = string.Empty;
        public List<IntConditionCase> Cases { get; set; } = new List<IntConditionCase>();

        public IntConditionNode()
        {
            title = "Int Condition";

            inputContainer.Add(NodeVisualElementFactory.CreateInputPort(Port.Capacity.Multi));

            _keyField = new TextField("Key");
            _keyField.style.marginTop = 4;
            _keyField.RegisterValueChangedCallback(evt => Key = evt.newValue);
            mainContainer.Add(_keyField);

            var addButton = new Button(() => { Cases.Add(new IntConditionCase()); RefreshCasePorts(); });
            addButton.text = "Add Case";
            titleButtonContainer.Add(addButton);

            RefreshCasePorts();
        }

        protected override NodeData CreateData() => new IntConditionNodeData
        {
            Key = Key,
            Cases = new List<IntConditionCase>(Cases)
        };

        public override void LoadNode(NodeData data)
        {
            base.LoadNode(data);
            if (data is not IntConditionNodeData d) return;

            Key = d.Key ?? string.Empty;
            Cases = new List<IntConditionCase>(d.Cases ?? new List<IntConditionCase>());
            _keyField?.SetValueWithoutNotify(Key);
            RefreshCasePorts();
        }

        private void RefreshCasePorts()
        {
            outputContainer.Clear();

            for (int i = 0; i < Cases.Count; i++)
            {
                int idx = i;
                var port = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(BaseNode));
                port.portName = string.Empty;

                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.alignItems = Align.Center;

                var opField = new EnumField(Cases[idx].Operator);
                opField.style.minWidth = 100;
                opField.RegisterValueChangedCallback(evt => Cases[idx].Operator = (ComparisonOperator)evt.newValue);

                var valueField = new IntegerField { value = Cases[idx].Value };
                valueField.style.minWidth = 40;
                valueField.style.maxWidth = 60;
                valueField.RegisterValueChangedCallback(evt => Cases[idx].Value = evt.newValue);

                row.Add(opField);
                row.Add(valueField);
                port.contentContainer.Add(row);
                outputContainer.Add(port);
            }

            outputContainer.Add(NodeVisualElementFactory.CreateOutputPort(Port.Capacity.Single, "Default"));

            RefreshPorts();
            RefreshExpandedState();
        }
    }
}
