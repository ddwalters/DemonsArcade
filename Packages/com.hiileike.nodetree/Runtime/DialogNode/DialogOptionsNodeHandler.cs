using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Scripting;


namespace NodeTree
{
    [Serializable]
    public class DialogOptionsNodeData : NodeData
    {
        public string DialogText;
        public List<string> ResponseOptions = new List<string>();
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
    [RuntimeNodeFor(typeof(DialogOptionsNodeData))]
    public class DialogOptionsNodeHandler : RuntimeBaseNode
    {
        private DialogOptionsNodeData _data;

        public DialogOptionsNodeHandler(DialogOptionsNodeData data)
            : base(data)
        {
            _data = data;
        }

        public override void Execute(DialogRunner uiManager, NodeTreeGraphSO graph)
        {
            if (!ConditionsPassed())
            {
                HandleConditionFail(uiManager, graph, conditionFailPortIndex: _data.ResponseOptions.Count);
                return;
            }

            uiManager.ClearOptions();
            uiManager.ShowSpeaker(_data.OwnerId, _data.Mood, PortraitSide.Left);
            var settings = DialogSettingsResolver.Resolve(_data, graph);
            uiManager.ApplyTextStyle(settings.FontAsset, settings.FontSize, settings.FontColor, settings.FontStyle);
            uiManager.DisplayText(_data.DialogText, settings.TextSpeed, settings.TypingTickClip);

            for (int i = 0; i < _data.ResponseOptions.Count; i++)
            {
                int portIndex = i;
                string buttonText = _data.ResponseOptions[i];

                var connectedEdge = graph.Edges.FirstOrDefault(e =>
                    e.OutputNodeGuid == _data.Guid &&
                    e.OutputPortIndex == portIndex);

                if (connectedEdge != null)
                {
                    uiManager.AddResponseOption(buttonText, () =>
                    {
                        uiManager.GoToNode(connectedEdge.InputNodeGuid);
                    });
                }
            }
        }
    }
}
