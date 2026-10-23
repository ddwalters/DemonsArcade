using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace NodeTree.Editor
{
    public abstract class BaseNode : Node
    {
        private SettingsFieldBuilder _settingsBuilder;

        public string GUID { get; set; }
        public bool UseTreeDefaults { get; set; } = true;
        public DialogSettings NodeSettings { get; set; } = new DialogSettings();

        protected SettingsFieldBuilder SettingsBuilder => _settingsBuilder ??= new SettingsFieldBuilder(new NodeSettingsTarget(this));

        public BaseNode()
        {
            GUID = Guid.NewGuid().ToString();
            inputContainer.style.height = new StyleLength(20);

            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                DialogSettingsContext.Changed += RefreshSettings;
                RefreshSettings();
            });
            RegisterCallback<DetachFromPanelEvent>(_ => DialogSettingsContext.Changed -= RefreshSettings);
        }

        protected Foldout BuildGeneralFoldout(bool offByDefault = false)
        {
            if (offByDefault)
            {
                NodeSettings = DialogSettingsResolver.ResolveTree(DialogSettingsContext.CurrentGraph);
                NodeSettings.IsRepeatable = false;
                NodeSettings.CanRewind = false;
                UseTreeDefaults = false;
            }

            var foldout = new Foldout { text = "General", value = false };
            SettingsBuilder.AddDefaultsToggle(foldout);
            SettingsBuilder.AddGeneralRows(foldout);
            SettingsBuilder.Refresh();
            return foldout;
        }

        protected Foldout BuildTextStyleFoldout()
        {
            var foldout = new Foldout { text = "Text Style", value = false };
            SettingsBuilder.AddTextStyleRows(foldout);
            SettingsBuilder.Refresh();
            return foldout;
        }

        private void RefreshSettings()
        {
            _settingsBuilder?.Refresh();
        }

        protected abstract NodeData CreateData();

        public NodeData SaveNode()
        {
            NodeData data = CreateData();

            data.HasSettings = true;
            data.UseTreeDefaults = UseTreeDefaults;
            data.Settings = NodeSettings.Clone();
            data.Guid = GUID;
            data.Position = GetPosition().position;

            return data;
        }

        public virtual void LoadNode(NodeData data)
        {
            GUID = data.Guid;
            SetPosition(new Rect(data.Position, Vector2.zero));

            if (data.HasSettings && data.Settings != null)
            {
                UseTreeDefaults = data.UseTreeDefaults;
                NodeSettings = data.Settings.Clone();
            }
            else
            {
                var legacy = DialogSettingsResolver.ResolveTree(DialogSettingsContext.CurrentGraph);
                UseTreeDefaults = !data.ApplyLegacySettings(legacy);
                NodeSettings = legacy;
            }

            _settingsBuilder?.Refresh();
        }

        public HashSet<BaseNode> GetNodeOutputConnections()
        {
            var connectedNodes = new HashSet<BaseNode>();
            List<Port> outputPorts = outputContainer.Query<Port>().ToList();

            foreach (var port in outputPorts)
            {
                foreach (var edge in port.connections)
                {
                    var connectedNode = edge.input.node as BaseNode;

                    if (connectedNode != null)
                        connectedNodes.Add(connectedNode);
                }
            }

            return connectedNodes;
        }
    }
}
