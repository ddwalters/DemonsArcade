using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SaveEntryView : MonoBehaviour
{
    public TMP_Text label;
    public Button button;
    private SaveData save;

    public void Bind(SaveData entry, Action<SaveData> select)
    {
        save = entry;
        label.text = entry.playthrough.characterName + "   |   " +
            DateTime.Parse(entry.savedAtUtc).ToLocalTime().ToString("g") + "   |   Town   |   Level " + entry.stats.lvl;
        button.onClick.AddListener(() => select(save));
    }

    public void SetSelected(bool selected)
    {
        button.image.color = selected ? new Color(0.28f, 0.42f, 0.48f) : new Color(0.13f, 0.16f, 0.19f);
    }
}
