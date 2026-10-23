using System;
using System.Collections.Generic;
using UnityEngine;


namespace NodeTree
{
    [Serializable]
    public class EdgeData
    {
        public string OutputNodeGuid;
        public string InputNodeGuid;
        public int OutputPortIndex;
    }

    [Serializable]
    public abstract class NodeData
    {
        public string Guid;
        public Vector2 Position;
        public bool HasSeen;

        public bool HasSettings;
        public bool UseTreeDefaults = true;
        public DialogSettings Settings = new DialogSettings();

        public virtual bool SupportsRepeatSetting => false;

        public virtual bool ApplyLegacySettings(DialogSettings target) => false;

        [SerializeReference]
        public List<ConditionBase> Conditions = new List<ConditionBase>();
        public ConditionFailBehavior ConditionFailBehavior;
    }

    [CreateAssetMenu(fileName = "NewNodeTree", menuName = "Node Tree/Graph")]
    public class NodeTreeGraphSO : ScriptableObject, ISerializationCallbackReceiver
    {
        public void OnBeforeSerialize() { }

        public void OnAfterDeserialize()
        {
            // [SerializeReference] writes null when a type has moved assemblies.
            // Remove those entries so the editor doesn't crash on open.
            Nodes?.RemoveAll(x => x == null);

            if (Nodes != null)
                foreach (var node in Nodes)
                    node?.Conditions?.RemoveAll(c => c == null);
        }

        // Needed for showing in the inspector as NodeData is abstract
        [SerializeReference]
        public List<NodeData> Nodes = new List<NodeData>();

        public List<EdgeData> Edges = new List<EdgeData>();

        [Header("Tree Settings")]
        [Tooltip("Edit these from the Dialog Settings panel in the Node Tree window.")]
        public bool UseGlobalDefaults = true;
        public DialogSettings Settings = new DialogSettings();

        [HideInInspector]
        public float defaultTextSpeed = DialogSettings.DefaultTextSpeed;

        [Header("Decorum")]
        public Sprite IndicatorIcon;
    }
}
