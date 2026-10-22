using System.Collections.Generic;
using TMPro;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace NodeTree.Editor
{
    [NodeFor(typeof(DialogNodeData))]
    public class DialogNode : BaseNode
    {
        private TextField _dialogTextField;
        private VisualElement _conditionListContainer;
        private EnumField _failBehaviorField;

        // Style fields
        private FloatField _textSpeedField;
        private ObjectField _typingTickClipField;
        private ObjectField _fontAssetField;
        private FloatField _fontSizeField;
        private ColorField _fontColorField;
        private EnumFlagsField _fontStyleField;

        public string DialogText { get; set; }
        public AudioClip TypingTickClip { get; set; }
        public float TextSpeed { get; set; } = 0f;
        public TMP_FontAsset FontAsset { get; set; }
        public float FontSize { get; set; } = 36f;
        public Color FontColor { get; set; } = Color.white;
        public FontStyles FontStyle { get; set; } = FontStyles.Normal;

        private List<ConditionBase> _conditions = new List<ConditionBase>();
        private ConditionFailBehavior _conditionFailBehavior;

        public DialogNode()
        {
            title = "Dialog";

            inputContainer.Add(NodeVisualElementFactory.CreateInputPort(Port.Capacity.Multi));

            var richText = NodeVisualElementFactory.CreateRichTextField(DialogText, newValue => { DialogText = newValue; });
            _dialogTextField = richText.textField;
            mainContainer.Add(richText.container);

            BuildStyleSection();
            BuildConditionSection();
            RefreshOutputPorts();
        }

        protected override NodeData CreateData()
        {
            return new DialogNodeData
            {
                DialogText = DialogText,
                TypingTickClip = TypingTickClip,
                TextSpeed = TextSpeed,
                FontAsset = FontAsset,
                FontSize = FontSize,
                FontColor = FontColor,
                FontStyle = FontStyle,
                Conditions = new List<ConditionBase>(_conditions),
                ConditionFailBehavior = _conditionFailBehavior,
            };
        }

        public override void LoadNode(NodeData data)
        {
            base.LoadNode(data);
            if (data is not DialogNodeData d) return;

            DialogText = d.DialogText ?? string.Empty;
            TypingTickClip = d.TypingTickClip;
            TextSpeed = d.TextSpeed;
            FontAsset = d.FontAsset;
            FontSize = d.FontSize > 0 ? d.FontSize : 36f;
            FontColor = d.FontColor;
            FontStyle = d.FontStyle;
            _conditions = new List<ConditionBase>(d.Conditions ?? new List<ConditionBase>());
            _conditionFailBehavior = d.ConditionFailBehavior;

            _dialogTextField?.SetValueWithoutNotify(DialogText);
            _typingTickClipField?.SetValueWithoutNotify(TypingTickClip);
            _textSpeedField?.SetValueWithoutNotify(TextSpeed);
            _fontAssetField?.SetValueWithoutNotify(FontAsset);
            _fontSizeField?.SetValueWithoutNotify(FontSize);
            _fontColorField?.SetValueWithoutNotify(FontColor);
            _fontStyleField?.SetValueWithoutNotify(FontStyle);
            _failBehaviorField?.SetValueWithoutNotify(_conditionFailBehavior);

            RefreshConditionList();
            RefreshOutputPorts();
        }

        private void BuildStyleSection()
        {
            var foldout = new Foldout { text = "Text Style", value = false };

            _textSpeedField = new FloatField("Text Speed") { value = TextSpeed, tooltip = "Seconds per character. 0 = use graph default." };
            _textSpeedField.RegisterValueChangedCallback(evt => TextSpeed = Mathf.Max(0f, evt.newValue));
            foldout.Add(_textSpeedField);

            _typingTickClipField = new ObjectField("Tick Sound") { objectType = typeof(AudioClip), tooltip = "Per-character typing sound. Null = use global default on DialogRunner." };
            _typingTickClipField.RegisterValueChangedCallback(evt => TypingTickClip = evt.newValue as AudioClip);
            foldout.Add(_typingTickClipField);

            _fontAssetField = new ObjectField("Font") { objectType = typeof(TMP_FontAsset) };
            _fontAssetField.RegisterValueChangedCallback(evt => FontAsset = evt.newValue as TMP_FontAsset);
            foldout.Add(_fontAssetField);

            _fontSizeField = new FloatField("Size") { value = FontSize };
            _fontSizeField.RegisterValueChangedCallback(evt => FontSize = evt.newValue);
            foldout.Add(_fontSizeField);

            _fontColorField = new ColorField("Color") { value = FontColor, showAlpha = false };
            _fontColorField.RegisterValueChangedCallback(evt => FontColor = evt.newValue);
            foldout.Add(_fontColorField);

            _fontStyleField = new EnumFlagsField("Style", FontStyles.Normal);
            _fontStyleField.RegisterValueChangedCallback(evt => FontStyle = (FontStyles)evt.newValue);
            foldout.Add(_fontStyleField);

            mainContainer.Add(foldout);
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
