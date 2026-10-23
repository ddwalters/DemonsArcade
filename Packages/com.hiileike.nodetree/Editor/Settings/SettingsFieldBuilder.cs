using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace NodeTree.Editor
{
    public static class DialogSettingsContext
    {
        public static NodeTreeGraphSO CurrentGraph { get; set; }

        public static event Action Changed;

        public static void NotifyChanged() => Changed?.Invoke();
    }

    public static class GlobalDialogDefaultsUtility
    {
        public static GlobalDialogDefaultsSO Find()
        {
            var atPath = AssetDatabase.LoadAssetAtPath<GlobalDialogDefaultsSO>(GlobalDialogDefaultsSO.AssetPath);
            if (atPath != null)
                return atPath;

            var guids = AssetDatabase.FindAssets("t:GlobalDialogDefaultsSO");
            if (guids.Length == 0)
                return null;

            if (guids.Length > 1)
                Debug.LogWarning("More than one GlobalDialogDefaultsSO exists. Only the first is used; delete the extras.");

            return AssetDatabase.LoadAssetAtPath<GlobalDialogDefaultsSO>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        public static GlobalDialogDefaultsSO GetOrCreate()
        {
            var defaults = Find();
            if (defaults != null)
                return defaults;

            Directory.CreateDirectory(Path.GetDirectoryName(GlobalDialogDefaultsSO.AssetPath));
            defaults = ScriptableObject.CreateInstance<GlobalDialogDefaultsSO>();
            AssetDatabase.CreateAsset(defaults, GlobalDialogDefaultsSO.AssetPath);
            AssetDatabase.SaveAssets();
            return defaults;
        }
    }

    public interface ISettingsTarget
    {
        string UseDefaultsLabel { get; }
        bool UsesDefaults { get; }
        DialogSettings Effective();
        void SetUsesDefaults(bool useDefaults);
        void Edit(Action<DialogSettings> assign);
    }

    public class GlobalSettingsTarget : ISettingsTarget
    {
        public string UseDefaultsLabel => null;

        public bool UsesDefaults => false;

        public DialogSettings Effective() => GlobalDialogDefaultsSO.Current.Settings;

        public void SetUsesDefaults(bool useDefaults) { }

        public void Edit(Action<DialogSettings> assign)
        {
            var defaults = GlobalDialogDefaultsUtility.GetOrCreate();
            Undo.RecordObject(defaults, "Edit Global Dialog Defaults");
            assign(defaults.Settings);
            EditorUtility.SetDirty(defaults);
            DialogSettingsContext.NotifyChanged();
        }
    }

    public class TreeSettingsTarget : ISettingsTarget
    {
        public string UseDefaultsLabel => "Use Global Defaults";

        public bool UsesDefaults
        {
            get
            {
                var graph = DialogSettingsContext.CurrentGraph;
                return graph == null || graph.UseGlobalDefaults;
            }
        }

        public DialogSettings Effective() => DialogSettingsResolver.ResolveTree(DialogSettingsContext.CurrentGraph);

        public void SetUsesDefaults(bool useDefaults)
        {
            var graph = DialogSettingsContext.CurrentGraph;
            if (graph == null)
                return;

            Undo.RecordObject(graph, "Edit Tree Dialog Settings");
            if (!useDefaults)
            {
                graph.Settings = Effective().Clone();
                graph.defaultTextSpeed = DialogSettings.DefaultTextSpeed;
            }

            graph.UseGlobalDefaults = useDefaults;
            EditorUtility.SetDirty(graph);
            DialogSettingsContext.NotifyChanged();
        }

        public void Edit(Action<DialogSettings> assign)
        {
            var graph = DialogSettingsContext.CurrentGraph;
            if (graph == null)
                return;

            Undo.RecordObject(graph, "Edit Tree Dialog Settings");
            assign(graph.Settings);
            EditorUtility.SetDirty(graph);
            DialogSettingsContext.NotifyChanged();
        }
    }

    public class NodeSettingsTarget : ISettingsTarget
    {
        private readonly BaseNode _node;

        public NodeSettingsTarget(BaseNode node)
        {
            _node = node;
        }

        public string UseDefaultsLabel => "Use Tree Defaults";

        public bool UsesDefaults => _node.UseTreeDefaults;

        public DialogSettings Effective()
        {
            return _node.UseTreeDefaults
                ? DialogSettingsResolver.ResolveTree(DialogSettingsContext.CurrentGraph)
                : _node.NodeSettings;
        }

        public void SetUsesDefaults(bool useDefaults)
        {
            if (!useDefaults)
                _node.NodeSettings = Effective().Clone();

            _node.UseTreeDefaults = useDefaults;
        }

        public void Edit(Action<DialogSettings> assign)
        {
            assign(_node.NodeSettings);
        }
    }

    public class SettingsFieldBuilder
    {
        private readonly ISettingsTarget _target;
        private readonly List<Action> _refreshers = new List<Action>();
        private bool _refreshing;

        public SettingsFieldBuilder(ISettingsTarget target)
        {
            _target = target;
        }

        public void AddDefaultsToggle(VisualElement parent)
        {
            if (_target.UseDefaultsLabel == null)
                return;

            var toggle = new Toggle(_target.UseDefaultsLabel);
            toggle.RegisterValueChangedCallback(evt =>
            {
                if (_refreshing)
                    return;

                _target.SetUsesDefaults(evt.newValue);
                Refresh();
            });
            parent.Add(toggle);

            _refreshers.Add(() => toggle.SetValueWithoutNotify(_target.UsesDefaults));
        }

        public void AddGeneralRows(VisualElement parent)
        {
            AddRow(parent,
                new Toggle("Is Repeatable") { tooltip = "Whether this can happen again in later conversations. Not repeatable: it runs once, then is skipped." },
                s => s.IsRepeatable, (s, v) => s.IsRepeatable = v);

            AddRow(parent,
                new Toggle("Can Rewind Dialog") { tooltip = "Whether the player can step back to this line. Only affects generic Dialog nodes." },
                s => s.CanRewind, (s, v) => s.CanRewind = v);
        }

        public void AddTextStyleRows(VisualElement parent)
        {
            AddRow(parent,
                new FloatField("Text Speed") { tooltip = "Seconds per character." },
                s => s.TextSpeed, (s, v) => s.TextSpeed = Mathf.Max(0f, v));

            AddRow<UnityEngine.Object>(parent,
                new ObjectField("Tick Sound") { objectType = typeof(AudioClip), tooltip = "Per-character typing sound. Empty = the default on DialogRunner." },
                s => s.TypingTickClip, (s, v) => s.TypingTickClip = v as AudioClip);

            AddRow<UnityEngine.Object>(parent,
                new ObjectField("Font") { objectType = typeof(TMP_FontAsset) },
                s => s.FontAsset, (s, v) => s.FontAsset = v as TMP_FontAsset);

            AddRow(parent,
                new FloatField("Size"),
                s => s.FontSize, (s, v) => s.FontSize = v);

            AddRow(parent,
                new ColorField("Color") { showAlpha = false },
                s => s.FontColor, (s, v) => s.FontColor = v);

            AddRow<Enum>(parent,
                new EnumFlagsField("Style", FontStyles.Normal),
                s => s.FontStyle, (s, v) => s.FontStyle = (FontStyles)v);
        }

        public void Refresh()
        {
            _refreshing = true;
            foreach (var refresh in _refreshers)
                refresh();
            _refreshing = false;
        }

        private void AddRow<T>(VisualElement parent, BaseField<T> input, Func<DialogSettings, T> get, Action<DialogSettings, T> set)
        {
            input.RegisterValueChangedCallback(evt =>
            {
                if (_refreshing)
                    return;

                _target.Edit(s => set(s, evt.newValue));
            });
            parent.Add(input);

            _refreshers.Add(() =>
            {
                input.SetValueWithoutNotify(get(_target.Effective()));
                input.SetEnabled(!_target.UsesDefaults);
            });
        }
    }
}
