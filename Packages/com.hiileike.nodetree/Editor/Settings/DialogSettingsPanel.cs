using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace NodeTree.Editor
{
    public class DialogSettingsPanel : VisualElement
    {
        private readonly SettingsFieldBuilder _treeBuilder = new SettingsFieldBuilder(new TreeSettingsTarget());
        private readonly SettingsFieldBuilder _globalBuilder = new SettingsFieldBuilder(new GlobalSettingsTarget());
        private readonly VisualElement _treeContent = new VisualElement();
        private readonly VisualElement _globalContent = new VisualElement();
        private readonly Label _noTreeLabel;
        private readonly Button _treeButton;
        private readonly Button _globalButton;
        private bool _showGlobal;

        public DialogSettingsPanel()
        {
            var modeRow = new VisualElement();
            modeRow.style.flexDirection = FlexDirection.Row;
            modeRow.style.marginBottom = 4;

            _treeButton = new Button(() => SetMode(false)) { text = "This Tree", tooltip = "Overrides for the open tree. Unchecked rows use the global value." };
            _globalButton = new Button(() => SetMode(true)) { text = "Global", tooltip = "Defaults for every tree." };
            _treeButton.style.flexGrow = 1;
            _globalButton.style.flexGrow = 1;
            modeRow.Add(_treeButton);
            modeRow.Add(_globalButton);
            Add(modeRow);

            _noTreeLabel = new Label("Open a tree to edit its settings.");
            _noTreeLabel.style.opacity = 0.6f;
            _treeContent.Add(_noTreeLabel);
            BuildContent(_treeContent, _treeBuilder);
            BuildContent(_globalContent, _globalBuilder);

            var selectAsset = new Button(() =>
            {
                var asset = GlobalDialogDefaultsUtility.GetOrCreate();
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
            }) { text = "Select Global Defaults Asset" };
            selectAsset.style.marginTop = 4;
            _globalContent.Add(selectAsset);

            Add(_treeContent);
            Add(_globalContent);

            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                DialogSettingsContext.Changed += Refresh;
                Undo.undoRedoPerformed += Refresh;
                Refresh();
            });
            RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                DialogSettingsContext.Changed -= Refresh;
                Undo.undoRedoPerformed -= Refresh;
            });

            SetMode(false);
        }

        private static void BuildContent(VisualElement container, SettingsFieldBuilder builder)
        {
            builder.AddDefaultsToggle(container);
            builder.AddGeneralRows(container);

            var styleFoldout = new Foldout { text = "Default Text Style", value = false };
            builder.AddTextStyleRows(styleFoldout);
            container.Add(styleFoldout);
        }

        private void SetMode(bool showGlobal)
        {
            _showGlobal = showGlobal;
            _treeContent.style.display = showGlobal ? DisplayStyle.None : DisplayStyle.Flex;
            _globalContent.style.display = showGlobal ? DisplayStyle.Flex : DisplayStyle.None;
            _treeButton.SetEnabled(showGlobal);
            _globalButton.SetEnabled(!showGlobal);
            Refresh();
        }

        public void Refresh()
        {
            bool hasTree = DialogSettingsContext.CurrentGraph != null;
            _noTreeLabel.style.display = hasTree ? DisplayStyle.None : DisplayStyle.Flex;

            if (_showGlobal)
                _globalBuilder.Refresh();
            else
                _treeBuilder.Refresh();

            foreach (var child in _treeContent.Children())
                if (child != _noTreeLabel)
                    child.SetEnabled(hasTree);
        }
    }
}
