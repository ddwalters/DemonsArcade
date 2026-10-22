using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace NodeTree.Editor
{
    [NodeFor(typeof(BoolConditionNodeData))]
    public class BoolConditionNode : BaseNode
    {
        private TextField _keyField;
        public string Key { get; set; } = string.Empty;

        public BoolConditionNode()
        {
            title = "Bool Condition";

            inputContainer.Add(NodeVisualElementFactory.CreateInputPort(Port.Capacity.Multi));
            outputContainer.Add(NodeVisualElementFactory.CreateOutputPort(Port.Capacity.Single, "True"));
            outputContainer.Add(NodeVisualElementFactory.CreateOutputPort(Port.Capacity.Single, "False"));

            _keyField = new TextField("Key");
            _keyField.style.marginTop = 4;
            _keyField.RegisterValueChangedCallback(evt => Key = evt.newValue);
            mainContainer.Add(_keyField);
        }

        protected override NodeData CreateData() => new BoolConditionNodeData { Key = Key };

        public override void LoadNode(NodeData data)
        {
            base.LoadNode(data);
            if (data is not BoolConditionNodeData d) return;

            Key = d.Key ?? string.Empty;
            _keyField?.SetValueWithoutNotify(Key);
        }
    }
}
