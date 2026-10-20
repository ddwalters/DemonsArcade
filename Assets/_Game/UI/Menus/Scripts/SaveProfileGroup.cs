using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SaveProfileGroup : MonoBehaviour
{
    public TMP_Text label;
    public Button headerButton;
    public RectTransform entries;
    public bool IsExpanded => entries.gameObject.activeSelf;
    private string characterName;
    private int saveCount;
    private Action<bool> expansionChanged;

    public void Bind(string name, int count, bool expanded, Action<bool> changed)
    {
        characterName = name;
        saveCount = count;
        expansionChanged = changed;
        headerButton.onClick.AddListener(() => SetExpanded(!IsExpanded));
        SetExpanded(expanded);
    }

    private void SetExpanded(bool expanded)
    {
        entries.gameObject.SetActive(expanded);
        label.text = (expanded ? "-  " : "+  ") + characterName + "  (" + saveCount + ")";
        expansionChanged?.Invoke(expanded);
    }
}
