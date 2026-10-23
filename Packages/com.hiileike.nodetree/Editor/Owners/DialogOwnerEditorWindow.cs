using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace NodeTree.Editor
{
    public class DialogOwnerEditorWindow : EditorWindow
    {
        private ListView _ownerList;
        private ScrollView _detailPane;
        private DialogOwnerDatabaseSO _database;
        private string _selectedId;

        // Open on an existing owner, or on a brand new blank one when createNew is true.
        public static void Open(string ownerId = null, bool createNew = false)
        {
            var wnd = GetWindow<DialogOwnerEditorWindow>("Dialog Owners");
            wnd.minSize = new Vector2(560, 300);
            wnd.Show();

            var db = DialogOwnerDatabaseUtility.GetOrCreate();
            wnd._database = db;

            if (createNew)
                ownerId = DialogOwnerDatabaseUtility.AddOwner(db).Id;

            wnd._selectedId = ownerId ?? db.Owners.FirstOrDefault()?.Id;
            wnd.RefreshList();
        }

        private void OnEnable()
        {
            DialogOwnerDatabaseUtility.Changed += OnDatabaseChanged;
            Undo.undoRedoPerformed += OnDatabaseChanged;
        }

        private void OnDisable()
        {
            DialogOwnerDatabaseUtility.Changed -= OnDatabaseChanged;
            Undo.undoRedoPerformed -= OnDatabaseChanged;
        }

        public void CreateGUI()
        {
            _database = DialogOwnerDatabaseUtility.Find();

            var root = rootVisualElement;
            root.style.flexDirection = FlexDirection.Row;

            var left = new VisualElement();
            left.style.width = 220;
            left.style.flexShrink = 0;
            left.style.borderRightWidth = 1;
            left.style.borderRightColor = new Color(0.1f, 0.1f, 0.1f);
            root.Add(left);

            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;
            header.style.paddingLeft = 6;
            header.style.paddingTop = 4;
            header.style.paddingBottom = 4;
            var title = new Label("Characters");
            title.style.flexGrow = 1;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.Add(title);
            header.Add(new Button(() => Open(createNew: true)) { text = "+", tooltip = "New character" });
            left.Add(header);

            _ownerList = new ListView
            {
                fixedItemHeight = 40,
                selectionType = SelectionType.Single,
                makeItem = MakeOwnerRow,
                bindItem = BindOwnerRow,
            };
            _ownerList.style.flexGrow = 1;
            _ownerList.selectionChanged += _ =>
            {
                if (_ownerList.selectedItem is DialogOwner owner && owner.Id != _selectedId)
                {
                    _selectedId = owner.Id;
                    RebuildDetail();
                }
            };
            left.Add(_ownerList);

            _detailPane = new ScrollView();
            _detailPane.style.flexGrow = 1;
            _detailPane.style.paddingLeft = 10;
            _detailPane.style.paddingRight = 10;
            _detailPane.style.paddingTop = 6;
            root.Add(_detailPane);

            RefreshList();
        }

        private static VisualElement MakeOwnerRow()
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.paddingLeft = 4;

            var pfp = new Image { name = "pfp", scaleMode = ScaleMode.ScaleToFit };
            pfp.style.width = 32;
            pfp.style.height = 32;
            pfp.style.marginRight = 6;
            pfp.style.backgroundColor = new Color(0f, 0f, 0f, 0.25f);
            row.Add(pfp);

            row.Add(new Label { name = "owner-name" });
            return row;
        }

        private void BindOwnerRow(VisualElement element, int index)
        {
            var owner = _database.Owners[index];
            element.Q<Image>("pfp").sprite = owner.GetPortrait(null);
            element.Q<Label>("owner-name").text = owner.DisplayName;
        }

        private bool _selfEdit;

        // Tells other windows (node tree owner list, node pickers) about an in-place edit without rebuilding this window.
        private void NotifyOthers()
        {
            _selfEdit = true;
            DialogOwnerDatabaseUtility.NotifyChanged();
            _selfEdit = false;
        }

        private void OnDatabaseChanged()
        {
            if (_ownerList == null || _selfEdit) return;
            RefreshList();
        }

        private void RefreshList()
        {
            if (_ownerList == null) return;

            _database ??= DialogOwnerDatabaseUtility.Find();
            _ownerList.itemsSource = _database != null ? _database.Owners : new List<DialogOwner>();
            _ownerList.Rebuild();

            if (_database != null)
            {
                int index = _database.Owners.FindIndex(o => o.Id == _selectedId);
                if (index >= 0)
                    _ownerList.SetSelectionWithoutNotify(new[] { index });
                else
                    _ownerList.ClearSelection();
            }

            RebuildDetail();
        }

        private void RebuildDetail()
        {
            _detailPane.Clear();

            var owner = _database?.Owners.FirstOrDefault(o => o.Id == _selectedId);
            if (owner == null)
            {
                _detailPane.Add(new Label("Select a character, or press + to create one."));
                return;
            }

            // Edits to text/sprite fields commit without a full rebuild so typing keeps focus.
            // Only the list is refreshed, which is cheap.
            var nameField = new TextField("Name") { value = owner.DisplayName };
            nameField.RegisterValueChangedCallback(evt =>
            {
                Undo.RecordObject(_database, "Rename Dialog Owner");
                owner.DisplayName = evt.newValue;
                EditorUtility.SetDirty(_database);
                _ownerList.RefreshItems();
                NotifyOthers();
            });
            _detailPane.Add(nameField);

            var idField = new TextField("ID") { value = owner.Id };
            idField.SetEnabled(false);
            _detailPane.Add(idField);

            var defaultField = new ObjectField("Default Portrait") { objectType = typeof(Sprite), allowSceneObjects = false, value = owner.DefaultPortrait };
            defaultField.RegisterValueChangedCallback(evt =>
            {
                Undo.RecordObject(_database, "Set Default Portrait");
                owner.DefaultPortrait = evt.newValue as Sprite;
                EditorUtility.SetDirty(_database);
                _ownerList.RefreshItems();
                NotifyOthers();
            });
            _detailPane.Add(defaultField);

            var portraitsLabel = new Label("Portraits by mood");
            portraitsLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            portraitsLabel.style.marginTop = 10;
            portraitsLabel.style.marginBottom = 2;
            _detailPane.Add(portraitsLabel);

            foreach (var portrait in owner.Portraits)
            {
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.alignItems = Align.Center;
                row.style.marginBottom = 2;

                var moodField = new TextField { value = portrait.Mood, tooltip = "Mood / scenario name" };
                moodField.style.width = 130;
                moodField.RegisterValueChangedCallback(evt =>
                {
                    Undo.RecordObject(_database, "Edit Portrait Mood");
                    portrait.Mood = evt.newValue;
                    EditorUtility.SetDirty(_database);
                    NotifyOthers();
                });
                row.Add(moodField);

                var spriteField = new ObjectField { objectType = typeof(Sprite), allowSceneObjects = false, value = portrait.Sprite };
                spriteField.style.flexGrow = 1;
                spriteField.RegisterValueChangedCallback(evt =>
                {
                    Undo.RecordObject(_database, "Edit Portrait Sprite");
                    portrait.Sprite = evt.newValue as Sprite;
                    EditorUtility.SetDirty(_database);
                    _ownerList.RefreshItems();
                    NotifyOthers();
                });
                row.Add(spriteField);

                var removedPortrait = portrait;
                row.Add(new Button(() =>
                {
                    Undo.RecordObject(_database, "Remove Portrait");
                    owner.Portraits.Remove(removedPortrait);
                    DialogOwnerDatabaseUtility.Commit(_database);
                }) { text = "X" });

                _detailPane.Add(row);
            }

            _detailPane.Add(new Button(() =>
            {
                Undo.RecordObject(_database, "Add Portrait");
                owner.Portraits.Add(new OwnerPortrait { Mood = "Mood" });
                DialogOwnerDatabaseUtility.Commit(_database);
            }) { text = "Add Portrait" });

            var captureButton = new Button(() => PortraitCaptureWindow.Open(owner.Id)) { text = "Capture Portrait From Model..." };
            captureButton.tooltip = "Pose a model and save the frame as a mood portrait for this character.";
            _detailPane.Add(captureButton);

            var deleteButton = new Button(() =>
            {
                if (!EditorUtility.DisplayDialog("Delete character", "Delete '" + owner.DisplayName + "'? Nodes using it will lose their speaker.", "Delete", "Cancel"))
                    return;

                _selectedId = null;
                DialogOwnerDatabaseUtility.RemoveOwner(_database, owner);
            }) { text = "Delete Character" };
            deleteButton.style.marginTop = 16;
            _detailPane.Add(deleteButton);
        }
    }
}
