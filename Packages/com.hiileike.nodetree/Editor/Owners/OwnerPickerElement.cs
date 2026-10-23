using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace NodeTree.Editor
{
    // Implemented by nodes that show a speaker, so other nodes can reuse the side a character already sits on.
    public interface IOwnerSpeakerNode
    {
        string OwnerId { get; }
        PortraitSide Side { get; }
    }

    // Owner + mood + side picker shared by the dialog nodes.
    public class OwnerPickerElement : VisualElement
    {
        private const string DefaultMoodLabel = "Default";
        private const string NoneLabel = "(None)";

        private readonly DropdownField _ownerDropdown;
        private readonly DropdownField _moodDropdown;
        private readonly EnumField _sideField;
        private readonly Image _preview;
        private readonly Func<string, PortraitSide?> _findExistingSide;
        private readonly bool _lockLeft;
        private List<DialogOwner> _owners = new List<DialogOwner>();

        public string OwnerId { get; private set; } = string.Empty;
        public string Mood { get; private set; } = string.Empty;
        public PortraitSide Side { get; private set; } = PortraitSide.Left;

        // findExistingSide returns the side this owner already uses elsewhere in the tree, or null.
        public OwnerPickerElement(Func<string, PortraitSide?> findExistingSide, bool lockLeft = false)
        {
            _findExistingSide = findExistingSide;
            _lockLeft = lockLeft;

            var foldout = new Foldout { text = "Speaker", value = true };

            _ownerDropdown = new DropdownField("Owner", new List<string> { NoneLabel }, 0);
            _ownerDropdown.RegisterCallback<PointerDownEvent>(_ => RefreshOwners(), TrickleDown.TrickleDown);
            _ownerDropdown.RegisterValueChangedCallback(_ => OnOwnerPicked());
            foldout.Add(_ownerDropdown);

            _moodDropdown = new DropdownField("Mood", new List<string> { DefaultMoodLabel }, 0);
            _moodDropdown.RegisterValueChangedCallback(_ =>
            {
                Mood = _moodDropdown.index <= 0 ? string.Empty : _moodDropdown.value;
                RefreshPreview();
            });
            foldout.Add(_moodDropdown);

            _sideField = new EnumField("Side", PortraitSide.Left);
            _sideField.RegisterValueChangedCallback(evt => Side = (PortraitSide)evt.newValue);
            if (_lockLeft)
            {
                _sideField.SetEnabled(false);
                _sideField.tooltip = "Dialog Options always show the speaker on the left; the option buttons cover the right.";
            }
            foldout.Add(_sideField);

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginTop = 2;

            _preview = new Image { scaleMode = ScaleMode.ScaleToFit };
            _preview.style.width = 48;
            _preview.style.height = 48;
            _preview.style.backgroundColor = new Color(0f, 0f, 0f, 0.25f);
            row.Add(_preview);

            var editButton = new Button(() => DialogOwnerEditorWindow.Open(string.IsNullOrEmpty(OwnerId) ? null : OwnerId)) { text = "Edit Characters" };
            editButton.style.marginLeft = 6;
            row.Add(editButton);
            foldout.Add(row);

            Add(foldout);

            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                DialogOwnerDatabaseUtility.Changed += OnDatabaseChanged;
                OnDatabaseChanged();
            });
            RegisterCallback<DetachFromPanelEvent>(_ => DialogOwnerDatabaseUtility.Changed -= OnDatabaseChanged);

            RefreshOwners();
        }

        // Used when loading saved node data; never triggers side auto-population.
        public void SetValues(string ownerId, string mood, PortraitSide side)
        {
            OwnerId = ownerId ?? string.Empty;
            Mood = mood ?? string.Empty;
            Side = _lockLeft ? PortraitSide.Left : side;

            _sideField.SetValueWithoutNotify(Side);
            RefreshOwners();
        }

        // Finds the side an owner already uses on another node in the same graph.
        public static PortraitSide? FindSideInGraph(Node self, string ownerId)
        {
            var graph = self.GetFirstAncestorOfType<GraphView>();
            if (graph == null || string.IsNullOrEmpty(ownerId))
                return null;

            foreach (var node in graph.nodes.ToList())
            {
                if (node != self && node is IOwnerSpeakerNode other && other.OwnerId == ownerId)
                    return other.Side;
            }

            return null;
        }

        private void OnDatabaseChanged()
        {
            RefreshOwners();
        }

        private void OnOwnerPicked()
        {
            int index = _ownerDropdown.index;
            if (index < 0 || index > _owners.Count)
                return;

            var newId = index == 0 ? string.Empty : _owners[index - 1].Id;
            if (newId == OwnerId)
                return;

            OwnerId = newId;
            Mood = string.Empty;

            // Reuse the side this character already has in the tree; the user can still override it.
            var existing = _findExistingSide?.Invoke(OwnerId);
            if (existing.HasValue && !_lockLeft)
            {
                Side = existing.Value;
                _sideField.SetValueWithoutNotify(Side);
            }

            RefreshOwners();
        }

        private void RefreshOwners()
        {
            var db = DialogOwnerDatabaseUtility.Find();
            _owners = db != null ? db.Owners.ToList() : new List<DialogOwner>();

            var choices = new List<string> { NoneLabel };
            choices.AddRange(_owners.Select(o => o.DisplayName));

            int selected = 0;
            int found = _owners.FindIndex(o => o.Id == OwnerId);
            if (found >= 0)
            {
                selected = found + 1;
            }
            else if (!string.IsNullOrEmpty(OwnerId))
            {
                choices.Add("(missing)");
                selected = choices.Count - 1;
            }

            _ownerDropdown.choices = choices;
            _ownerDropdown.SetValueWithoutNotify(choices[selected]);

            RefreshMoods(found >= 0 ? _owners[found] : null);
            RefreshPreview();
        }

        private void RefreshMoods(DialogOwner owner)
        {
            var choices = new List<string> { DefaultMoodLabel };
            if (owner != null)
                choices.AddRange(owner.Moods.Distinct());

            // Keep a saved mood visible even if its sprite was since removed.
            if (!string.IsNullOrEmpty(Mood) && !choices.Contains(Mood))
                choices.Add(Mood);

            _moodDropdown.choices = choices;
            _moodDropdown.SetValueWithoutNotify(string.IsNullOrEmpty(Mood) ? DefaultMoodLabel : Mood);
            _moodDropdown.SetEnabled(owner != null);
        }

        private void RefreshPreview()
        {
            var owner = _owners.FirstOrDefault(o => o.Id == OwnerId);
            _preview.sprite = owner?.GetPortrait(Mood);
        }
    }
}
