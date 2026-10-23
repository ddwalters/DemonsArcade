using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Scripting;


namespace NodeTree
{
    [Serializable]
    public class DialogNodeData : NodeData
    {
        public string DialogText;
        public AudioClip TypingTickClip;

        public TMP_FontAsset FontAsset;
        public float FontSize = 36f;
        public Color FontColor = Color.white;
        public FontStyles FontStyle = FontStyles.Normal;

        public float TextSpeed = 0f;

        public string OwnerId;
        public string Mood;
        public PortraitSide Side;

        public override bool SupportsRepeatSetting => true;

        public override bool ApplyLegacySettings(DialogSettings target)
        {
            return target.ApplyLegacyTextStyle(TextSpeed, TypingTickClip, FontAsset, FontSize, FontColor, FontStyle);
        }
    }

    [Preserve]
    [RuntimeNodeFor(typeof(DialogNodeData))]
    public class DialogNodeHandler : RuntimeBaseNode
    {
        private DialogNodeData _data;

        public DialogNodeHandler(DialogNodeData data)
            : base(data)
        {
            _data = data;
        }

        public override void Execute(DialogRunner uiManager, NodeTreeGraphSO graph)
        {
            if (!ConditionsPassed())
            {
                HandleConditionFail(uiManager, graph, conditionFailPortIndex: 1);
                return;
            }

            if (_data.HasSeen) return;

            uiManager.ClearOptions();
            uiManager.ShowSpeaker(_data.OwnerId, _data.Mood, _data.Side);
            var settings = DialogSettingsResolver.Resolve(_data, graph);
            uiManager.ApplyTextStyle(settings.FontAsset, settings.FontSize, settings.FontColor, settings.FontStyle);

            var connectedEdge = graph.Edges.FirstOrDefault(x => x.OutputNodeGuid == _data.Guid && x.OutputPortIndex == 0);
            string nextGuid = connectedEdge?.InputNodeGuid;
            uiManager.DisplayText(_data.DialogText, settings.TextSpeed, settings.TypingTickClip, () => uiManager.GoToNode(nextGuid));

            _data.HasSeen = true;
        }
    }
}
