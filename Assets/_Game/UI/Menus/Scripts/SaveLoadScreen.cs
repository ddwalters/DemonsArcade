using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SaveLoadScreen : MonoBehaviour
{
    public TMP_Text title;
    public TMP_Text status;
    public RectTransform content;
    public SaveEntryView entryPrefab;
    public SaveProfileGroup groupPrefab;
    public Button saveButton;
    public Button loadButton;
    public Button deleteButton;
    public ConfirmationDialog confirmation;
    private readonly List<SaveEntryView> entries = new();
    private readonly List<SaveProfileGroup> groups = new();
    private readonly HashSet<string> collapsedProfiles = new();
    private SaveData selected;
    private bool saving;
    private string previousRestriction;

    private void Awake()
    {
        saveButton.onClick.AddListener(CreateSave);
        loadButton.onClick.AddListener(LoadSelected);
        deleteButton.onClick.AddListener(ConfirmDelete);
        ResolveConfirmation();
    }

    private void ResolveConfirmation()
    {
        if (confirmation != null)
            return;

        UIController controller = GetComponentInParent<UIController>(true);
        Transform searchRoot = controller != null ? controller.transform : transform.root;
        confirmation = searchRoot.GetComponentInChildren<ConfirmationDialog>(true);
    }

    private void OnEnable() => SaveManager.Instance.SavesChanged += Refresh;
    private void OnDisable()
    {
        if (SaveManager.Instance != null)
            SaveManager.Instance.SavesChanged -= Refresh;
    }

    public void Show(bool saveMode)
    {
        saving = saveMode;
        title.text = saving ? "Save Game" : "Load Game";
        saveButton.gameObject.SetActive(saving);
        loadButton.gameObject.SetActive(!saving);
        previousRestriction = null;
        gameObject.SetActive(true);
        Refresh();
    }

    private void Update()
    {
        if (!saving)
            return;

        string restriction = SaveManager.Instance.SaveBlockedReason;
        saveButton.interactable = restriction == null;
        if (restriction != previousRestriction)
        {
            status.text = restriction ?? "Save creates a new entry for " + SaveManager.Instance.ActivePlaythrough.characterName + ".";
            previousRestriction = restriction;
        }
    }

    private void Refresh()
    {
        foreach (var group in groups)
        {
            group.gameObject.SetActive(false);
            Destroy(group.gameObject);
        }

        groups.Clear();
        entries.Clear();
        selected = null;
        loadButton.interactable = false;
        deleteButton.interactable = false;
        foreach (var saves in SaveManager.Instance.ListSaves().GroupBy(save => save.playthrough.id))
        {
            string profileId = saves.Key;
            var group = Instantiate(groupPrefab, content);
            group.gameObject.SetActive(true);
            groups.Add(group);
            group.Bind(saves.First().playthrough.characterName, saves.Count(), !collapsedProfiles.Contains(profileId), expanded =>
            {
                if (expanded)
                    collapsedProfiles.Remove(profileId);
                else
                {
                    collapsedProfiles.Add(profileId);
                    if (selected?.playthrough.id == profileId)
                    {
                        selected = null;
                        loadButton.interactable = false;
                        deleteButton.interactable = false;
                        foreach (var entry in entries)
                            entry.SetSelected(false);
                    }
                }
            });

            foreach (var save in saves)
            {
                var entry = Instantiate(entryPrefab, group.entries);
                entry.gameObject.SetActive(true);
                entry.Bind(save, chosen => Select(chosen, entry));
                entries.Add(entry);
            }
        }

        status.text = entries.Count == 0 ? "No saves found." : "Select a save.";
        previousRestriction = "";
        saveButton.interactable = saving && SaveManager.Instance.CanSave;
    }

    private void Select(SaveData save, SaveEntryView selectedView)
    {
        selected = save;
        foreach (var entry in entries)
            entry.SetSelected(entry == selectedView);

        loadButton.interactable = true;
        deleteButton.interactable = true;
    }

    private void CreateSave()
    {
        status.text = SaveManager.Instance.Save() ? "Game saved." : SaveManager.Instance.LastError;
    }

    private void LoadSelected()
    {
        if (selected != null && !SaveManager.Instance.Load(selected))
            status.text = SaveManager.Instance.LastError;
    }

    private void ConfirmDelete()
    {
        if (selected == null)
            return;

        if (confirmation == null)
        {
            status.text = "Delete confirmation dialog is missing.";
            return;
        }

        SaveData target = selected;
        confirmation.Show("Delete this save for " + target.playthrough.characterName + "?\n" +
            DateTime.Parse(target.savedAtUtc).ToLocalTime().ToString("g") + "\nThis cannot be undone.", () =>
        {
            try
            {
                SaveManager.Instance.DeleteConfirmed(target);
            }
            catch (Exception exception)
            {
                status.text = "Could not delete save: " + exception.Message;
            }
        });
    }
}
