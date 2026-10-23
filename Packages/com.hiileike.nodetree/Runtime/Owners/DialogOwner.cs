using System;
using System.Collections.Generic;
using UnityEngine;


namespace NodeTree
{
    public enum PortraitSide
    {
        Left,
        Right,
    }

    [Serializable]
    public class OwnerPortrait
    {
        [Tooltip("Mood / scenario this sprite represents, e.g. Happy, Angry, Injured.")]
        public string Mood = "Default";
        public Sprite Sprite;
    }

    [Serializable]
    public class DialogOwner
    {
        // Stable id nodes reference. Never changes when the display name does.
        public string Id = Guid.NewGuid().ToString("N");
        public string DisplayName = "New Character";

        [Tooltip("Generic portrait. Used in lists and whenever a requested mood has no sprite.")]
        public Sprite DefaultPortrait;
        public List<OwnerPortrait> Portraits = new List<OwnerPortrait>();

        public IEnumerable<string> Moods
        {
            get
            {
                foreach (var portrait in Portraits)
                    if (!string.IsNullOrWhiteSpace(portrait.Mood))
                        yield return portrait.Mood;
            }
        }

        // Empty / unknown mood falls back to the default portrait, then the first available sprite.
        public Sprite GetPortrait(string mood)
        {
            if (!string.IsNullOrWhiteSpace(mood))
            {
                foreach (var portrait in Portraits)
                {
                    if (portrait.Sprite != null && string.Equals(portrait.Mood, mood, StringComparison.OrdinalIgnoreCase))
                        return portrait.Sprite;
                }
            }

            if (DefaultPortrait != null)
                return DefaultPortrait;

            foreach (var portrait in Portraits)
                if (portrait.Sprite != null)
                    return portrait.Sprite;

            return null;
        }
    }
}
