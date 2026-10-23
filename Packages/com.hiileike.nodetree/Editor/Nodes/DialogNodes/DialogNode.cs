using System.Collections.Generic;
using TMPro;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace NodeTree.Editor
{
    [NodeFor(typeof(DialogNodeData))]
    public class DialogNode : BaseNode, IOwnerSpeakerNode
    {
        private TextField _dialogTextField;
        private OwnerPickerElement _ownerPicker;
        private VisualElement _conditionListContainer;
        private EnumField _failBehaviorField;

        public string OwnerId => _ownerPicker?.OwnerId ?? string.Empty;
        public PortraitSide Side => _ownerPicker?.Side ?? PortraitSide.Left;

        public string DialogText { get; set; }

        private List<ConditionBase> _conditions = new List<ConditionBase>();
        private ConditionFailBehavior _conditionFailBehavior;

        public DialogNode()
        {
            title = "Dialog";

            inputContainer.Add(NodeVisualElementFactory.CreateInputPort(Port.Capacity.Multi));

            var richText = NodeVisualElementFactory.CreateRichTextField(DialogText, newValue => { DialogText = newValue; });
            _dialogTextField = richText.textField;
            mainContainer.Add(richText.container);

            _ownerPicker = new OwnerPickerElement(id => OwnerPickerElement.FindSideInGraph(this, id));
            mainContainer.Add(_ownerPicker);

            var general = BuildGeneralFoldout(offByDefault: false);
            mainContainer.Add(BuildTextStyleFoldout());
            mainContainer.Add(general);
            BuildConditionSection();
            RefreshOutputPorts();
        }

        protected override NodeData CreateData()
        {
            return new DialogNodeData
            {
                DialogText = DialogText,
                OwnerId = _ownerPicker.OwnerId,
                Mood = _ownerPicker.Mood,
                Side = _ownerPicker.Side,
                Conditions = new List<ConditionBase>(_conditions),
                ConditionFailBehavior = _conditionFailBehavior,
            };
        }

        public override void LoadNode(NodeData data)
        {
            base.LoadNode(data);
            if (data is not DialogNodeData d) return;

            DialogText = d.DialogText ?? string.Empty;
            _conditions = new List<ConditionBase>(d.Conditions ?? new List<ConditionBase>());
            _conditionFailBehavior = d.ConditionFailBehavior;
            _ownerPicker.SetValues(d.OwnerId, d.Mood, d.Side);

            _dialogTextField?.SetValueWithoutNotify(DialogText);
            _failBehaviorField?.SetValueWithoutNotify(_conditionFailBehavior);

            RefreshConditionList();
            RefreshOutputPorts();
        }

        private void BuildConditionSection()
        {
            var foldout = new Foldout { text = "Conditions", value = false };

            _conditionListContainer = new VisualElement();
            foldout.Add(_conditionListContainer);

            var addRow = new VisualElement();
            addRow.style.flexDirection = FlexDirection.Row;
            addRow.style.marginTop = 4;

            var typeDropdown = new DropdownField(new List<string> { "Bool", "Int" }, 0);
            typeDropdown.style.flexGrow = 1;

            var addButton = new Button(() =>
            {
                _conditions.Add(typeDropdown.value == "Int" ? (ConditionBase)new IntCondition() : new BoolCondition());
                RefreshConditionList();
            }) { text = "Add" };

            addRow.Add(typeDropdown);
            addRow.Add(addButton);
            foldout.Add(addRow);

            _failBehaviorField = new EnumField("On Fail", ConditionFailBehavior.Stop);
            _failBehaviorField.style.marginTop = 4;
            _failBehaviorField.RegisterValueChangedCallback(evt =>
            {
                _conditionFailBehavior = (ConditionFailBehavior)evt.newValue;
                RefreshOutputPorts();
            });
            foldout.Add(_failBehaviorField);

            mainContainer.Add(foldout);
        }

        private void RefreshConditionList()
        {
            _conditionListContainer.Clear();

            for (int i = 0; i < _conditions.Count; i++)
            {
                int idx = i;
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Column;
                row.style.marginBottom = 4;
                row.style.borderBottomWidth = 1;
                row.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);

                if (_conditions[idx] is BoolCondition bc)
                {
                    var keyField = new TextField("Key") { value = bc.Key ?? string.Empty };
                    keyField.RegisterValueChangedCallback(evt => bc.Key = evt.newValue);
                    var expectedToggle = new Toggle("Expected") { value = bc.ExpectedValue };
                    expectedToggle.RegisterValueChangedCallback(evt => bc.ExpectedValue = evt.newValue);
                    row.Add(keyField);
                    row.Add(expectedToggle);
                }
                else if (_conditions[idx] is IntCondition ic)
                {
                    var keyField = new TextField("Key") { value = ic.Key ?? string.Empty };
                    keyField.RegisterValueChangedCallback(evt => ic.Key = evt.newValue);
                    var opField = new EnumField("Op", ic.Operator);
                    opField.RegisterValueChangedCallback(evt => ic.Operator = (ComparisonOperator)evt.newValue);
                    var valField = new IntegerField("Value") { value = ic.Value };
                    valField.RegisterValueChangedCallback(evt => ic.Value = evt.newValue);
                    row.Add(keyField);
                    row.Add(opField);
                    row.Add(valField);
                }

                var removeButton = new Button(() => { _conditions.RemoveAt(idx); RefreshConditionList(); }) { text = "Remove" };
                removeButton.style.marginTop = 2;
                row.Add(removeButton);
                _conditionListContainer.Add(row);
            }
        }

        private void RefreshOutputPorts()
        {
            outputContainer.Clear();
            outputContainer.Add(NodeVisualElementFactory.CreateOutputPort(Port.Capacity.Single));

            if (_conditionFailBehavior == ConditionFailBehavior.Jump)
                outputContainer.Add(NodeVisualElementFactory.CreateOutputPort(Port.Capacity.Single, "Condition Fail"));

            RefreshPorts();
            RefreshExpandedState();
        }
    }
}
