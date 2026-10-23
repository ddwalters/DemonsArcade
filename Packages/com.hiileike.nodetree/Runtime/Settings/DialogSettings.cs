using System;
using TMPro;
using UnityEngine;

namespace NodeTree
{
    [Serializable]
    public class DialogSettings
    {
        public const float DefaultTextSpeed = 0.05f;
        public const float DefaultFontSize = 36f;

        public bool IsRepeatable = true;
        public bool CanRewind = true;

        [Tooltip("Seconds per character.")]
        public float TextSpeed = DefaultTextSpeed;
        public AudioClip TypingTickClip;
        public TMP_FontAsset FontAsset;
        public float FontSize = DefaultFontSize;
        public Color FontColor = Color.white;
        public FontStyles FontStyle = FontStyles.Normal;

        public DialogSettings Clone()
        {
            return (DialogSettings)MemberwiseClone();
        }

        public bool ApplyLegacyTextStyle(float textSpeed, AudioClip tickClip, TMP_FontAsset fontAsset, float fontSize, Color fontColor, FontStyles fontStyle)
        {
            bool changed = false;

            if (textSpeed > 0f)
            {
                TextSpeed = textSpeed;
                changed = true;
            }

            if (tickClip != null)
            {
                TypingTickClip = tickClip;
                changed = true;
            }

            if (fontAsset != null)
            {
                FontAsset = fontAsset;
                changed = true;
            }

            if (fontSize > 0f && !Mathf.Approximately(fontSize, DefaultFontSize))
            {
                FontSize = fontSize;
                changed = true;
            }

            if (fontColor != Color.white)
            {
                FontColor = fontColor;
                changed = true;
            }

            if (fontStyle != FontStyles.Normal)
            {
                FontStyle = fontStyle;
                changed = true;
            }

            return changed;
        }
    }

    public static class DialogSettingsResolver
    {
        public static DialogSettings ResolveTree(NodeTreeGraphSO tree)
        {
            if (tree != null && !tree.UseGlobalDefaults)
                return tree.Settings.Clone();

            var result = GlobalDialogDefaultsSO.Current.Settings.Clone();
            if (tree != null && !Mathf.Approximately(tree.defaultTextSpeed, DialogSettings.DefaultTextSpeed))
                result.TextSpeed = tree.defaultTextSpeed;

            return result;
        }

        public static DialogSettings Resolve(NodeData node, NodeTreeGraphSO tree)
        {
            if (node != null && node.HasSettings && !node.UseTreeDefaults)
                return node.Settings.Clone();

            var result = ResolveTree(tree);
            if (node != null && !node.HasSettings)
                node.ApplyLegacySettings(result);

            return result;
        }
    }
}
