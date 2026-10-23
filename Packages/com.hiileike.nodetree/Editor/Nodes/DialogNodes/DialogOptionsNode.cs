using System.Collections.Generic;
using TMPro;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace NodeTree.Editor
{
    [NodeFor(typeof(DialogOptionsNodeData))]
    public class DialogOptionsNode : BaseNode, IOwnerSpeakerNode
    {
        private TextField _dialogTextField;
        private OwnerPickerElement _ownerPicker;
        private VisualElement _conditionListContainer;
        private EnumField _failBehaviorField;

        public string OwnerId => _ownerPicker?.OwnerId ?? string.Empty;
        public PortraitSide Side => _ownerPicker?.Side ?? PortraitSide.Left;

        public string DialogText { get; set; }
        public List<string> ResponseOptions { get; set; } = new List<string>();

        private List<ConditionBase> _conditions = new List<ConditionBase>();
        private ConditionFailBehavior _conditionFailBehavior;

        public DialogOptionsNode()
        {
            title = "Dialog Options";

            inputContainer.Add(NodeVisualElementFactory.CreateInputPort(Port.Capacity.Multi));

            var richText = NodeVisualElementFactory.CreateRichTextField(DialogText, newValue => { DialogText = newValue; });
            _dialogTextField = richText.textField;
            mainContainer.Add(richText.container);

            _ownerPicker = new OwnerPickerElement(id => OwnerPickerElement.FindSideInGraph(this, id), lockLeft: true);
            mainContainer.Add(_ownerPicker);

            var addButton = new Button(() =>
            {
                ResponseOptions.Add(string.Empty);
                RefreshOutputPorts();
            });
            addButton.text = "Add";
            addButton.tooltip = "Adds a dialog option, requires a paired node to save.";
            titleButtonContainer.Add(addButton);

            var general = BuildGeneralFoldout(offByDefault: true);
            mainContainer.Add(BuildTextStyleFoldout());
            mainContainer.Add(general);
            BuildConditionSection();
            RefreshOutputPorts();
        }

        protected override NodeData CreateData()
        {
            return new DialogOptionsNodeData
            {
                DialogText = DialogText,
                ResponseOptions = new List<string>(ResponseOptions),
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
            if (data is not DialogOptionsNodeData d) return;

            DialogText = d.DialogText ?? string.Empty;
            ResponseOptions = new List<string>(d.ResponseOptions ?? new List<string>());
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

            var addConditionButton = new Button(() =>
            {
                _conditions.Add(typeDropdown.value == "Int" ? (ConditionBase)new IntCondition() : new BoolCondition());
                RefreshConditionList();
            }) { text = "Add" };

            addRow.Add(typeDropdown);
            addRow.Add(addConditionButton);
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

            for (int i = 0; i < ResponseOptions.Count; i++)
            {
                var optionIndex = i;
                var outputPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(BaseNode));
                outputPort.portName = string.Empty;

                var optionTextField = new TextField { value = ResponseOptions[optionIndex] };
                optionTextField.maxLength = 200;
                optionTextField.multiline = true;
                optionTextField.style.flexGrow = 1;
                optionTextField.style.flexShrink = 1;
                optionTextField.style.minWidth = 0;
                optionTextField.style.maxWidth = 260;
                optionTextField.style.flexWrap = Wrap.Wrap;
                optionTextField.style.whiteSpace = WhiteSpace.Normal;
                optionTextField.verticalScrollerVisibility = ScrollerVisibility.Auto;
                optionTextField.RegisterValueChangedCallback(evt => ResponseOptions[optionIndex] = evt.newValue);

                outputPort.contentContainer.style.flexGrow = 1;
                outputPort.style.maxWidth = 300;
                outputPort.contentContainer.Add(optionTextField);
                outputContainer.Add(outputPort);
            }

            if (_conditionFailBehavior == ConditionFailBehavior.Jump)
                outputContainer.Add(NodeVisualElementFactory.CreateOutputPort(Port.Capacity.Single, "Condition Fail"));

            RefreshPorts();
            RefreshExpandedState();
        }
    }
}
