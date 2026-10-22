using System;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace NodeTree.Editor
{
    public static class NodeVisualElementFactory
    {
        public static Port CreateInputPort(Port.Capacity portCapacity)
        {
            var port = Port.Create<Edge>(Orientation.Horizontal, Direction.Input, portCapacity, typeof(BaseNode));
            port.portName = "Input";

            return port;
        }

        public static Port CreateOutputPort(Port.Capacity portCapacity, string portName = null)
        {
            var port = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, portCapacity, typeof(BaseNode));
            port.portName = portName ?? "Output";

            return port;
        }

        public static (VisualElement container, TextField textField) CreateRichTextField(string initialValue, Action<string> onValueChanged)
        {
            int savedCursor = 0;
            int savedSelect = 0;

            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Column;
            container.style.flexGrow = 0;
            container.style.marginBottom = 4;

            var label = new Label("Text:");
            label.style.marginBottom = 2;
            container.Add(label);

            var textField = new TextField();
            textField.multiline = true;
            textField.style.flexGrow = 0;
            textField.style.height = 80;
            textField.style.flexWrap = Wrap.Wrap;
            textField.style.maxWidth = 250;
            textField.style.whiteSpace = WhiteSpace.Normal;
            textField.verticalScrollerVisibility = ScrollerVisibility.Auto;
            textField.SetValueWithoutNotify(initialValue ?? string.Empty);
            textField.RegisterValueChangedCallback(evt => onValueChanged?.Invoke(evt.newValue));

            textField.RegisterCallback<FocusOutEvent>(_ =>
            {
                savedCursor = textField.textSelection.cursorIndex;
                savedSelect = textField.textSelection.selectIndex;
            });

            void WrapSelection(string open, string close)
            {
                var text = textField.value;
                int start = Mathf.Min(savedCursor, savedSelect);
                int end   = Mathf.Max(savedCursor, savedSelect);
                start = Mathf.Clamp(start, 0, text.Length);
                end   = Mathf.Clamp(end,   0, text.Length);

                textField.value = start == end
                    ? text.Insert(start, open + close)
                    : text[..start] + open + text[start..end] + close + text[end..];
            }

            // Toolbar
            var toolbar = new VisualElement();
            toolbar.style.flexDirection = FlexDirection.Row;
            toolbar.style.alignItems = Align.Center;
            toolbar.style.flexWrap = Wrap.Wrap;
            toolbar.style.marginBottom = 2;

            var boldBtn = new Button(() => WrapSelection("<b>", "</b>")) { text = "B" };
            boldBtn.style.width = 22;
            boldBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
            toolbar.Add(boldBtn);

            var italicBtn = new Button(() => WrapSelection("<i>", "</i>")) { text = "I" };
            italicBtn.style.width = 22;
            italicBtn.style.unityFontStyleAndWeight = FontStyle.Italic;
            toolbar.Add(italicBtn);

            var sep = new VisualElement();
            sep.style.width = 1;
            sep.style.height = 14;
            sep.style.marginLeft = 3;
            sep.style.marginRight = 3;
            sep.style.backgroundColor = new Color(0.4f, 0.4f, 0.4f);
            toolbar.Add(sep);

            var colorField = new ColorField { value = Color.white, showAlpha = false };
            colorField.style.width = 38;
            colorField.style.marginRight = 2;
            toolbar.Add(colorField);

            var colorBtn = new Button(() =>
            {
                var hex = ColorUtility.ToHtmlStringRGB(colorField.value);
                WrapSelection($"<color=#{hex}>", "</color>");
            }) { text = "Color" };
            toolbar.Add(colorBtn);

            var sep2 = new VisualElement();
            sep2.style.width = 1;
            sep2.style.height = 14;
            sep2.style.marginLeft = 3;
            sep2.style.marginRight = 3;
            sep2.style.backgroundColor = new Color(0.4f, 0.4f, 0.4f);
            toolbar.Add(sep2);

            var sizeField = new IntegerField { value = 36 };
            sizeField.style.width = 34;
            sizeField.style.marginRight = 2;
            toolbar.Add(sizeField);

            var sizeBtn = new Button(() => WrapSelection($"<size={sizeField.value}>", "</size>")) { text = "Size" };
            toolbar.Add(sizeBtn);

            container.Add(toolbar);
            container.Add(textField);

            return (container, textField);
        }

        public static VisualElement CreateTextField(string initialValue, Action<string> onValueChanged)
        {
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Column;
            container.style.flexGrow = 0;
            container.style.marginBottom = 4;

            var label = new Label("Text:");
            label.style.marginBottom = 2;
            container.Add(label);

            var textField = new TextField();
            textField.multiline = true;
            textField.style.flexGrow = 0;
            textField.style.height = 80;
            textField.style.flexWrap = Wrap.Wrap;
            textField.style.maxWidth = 250;
            textField.style.whiteSpace = WhiteSpace.Normal;
            textField.verticalScrollerVisibility = ScrollerVisibility.Auto;
            textField.SetValueWithoutNotify(initialValue ?? string.Empty);
            textField.RegisterValueChangedCallback(evt => onValueChanged?.Invoke(evt.newValue));
            container.Add(textField);

            return container;
        }

        public static VisualElement CreateLabelWithTooltip(string text, string tooltip)
        {
            var labelContainer = new VisualElement();
            labelContainer.style.flexDirection = FlexDirection.Row;
            labelContainer.style.alignItems = Align.Center;
            labelContainer.style.marginTop = 3;

            var label = new Label();
            label.text = text;
            label.tooltip = tooltip;
            label.style.marginLeft = 3;
            labelContainer.Add(label);

            var helpIcon = new Image();
            helpIcon.image = EditorGUIUtility.IconContent("_Help").image;
            helpIcon.tooltip = tooltip;
            helpIcon.style.marginLeft = 3;
            labelContainer.Add(helpIcon);

            return labelContainer;
        }

        public static Toggle CreateToggle(string label, bool initialValue, Action<bool> onValueChanged)
        {
            var repeatToggle = new Toggle(label);
            repeatToggle.SetValueWithoutNotify(initialValue);
            repeatToggle.RegisterValueChangedCallback(evt => onValueChanged?.Invoke(evt.newValue));

            return repeatToggle;
        }

        public static ObjectField CreateObjectField(Type objectType, UnityEngine.Object initialValue, Action<UnityEngine.Object> onValueChanged)
        {
            var objectField = new ObjectField();
            objectField.style.marginTop = 4;
            objectField.objectType = objectType;
            objectField.allowSceneObjects = true;
            objectField.SetValueWithoutNotify(initialValue);
            objectField.RegisterValueChangedCallback(evt => { onValueChanged?.Invoke(evt.newValue); });

            return objectField;
        }
    }
}
