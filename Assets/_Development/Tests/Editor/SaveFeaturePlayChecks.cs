using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class SaveFeaturePlayChecks
{
    private static IEnumerator routine;
    private static int checks;
    private const string Running = "SaveFeaturePlayChecks.Running";

    static SaveFeaturePlayChecks()
    {
        EditorApplication.playModeStateChanged += Changed;
    }

    [MenuItem("Tools/Validation/Save and Menus (Play Mode)")]
    public static void Run()
    {
        if (EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        SessionState.SetBool(Running, true);
        SessionState.SetString("SaveFeaturePlayChecks.Directory", Path.GetFullPath("Temp/MenuPlayChecks/" + Guid.NewGuid().ToString("N")));
        EditorSceneManager.OpenScene("Assets/_Game/Scenes/Menus/_StartScreen.unity");
        EditorApplication.isPlaying = true;
    }

    public static void RunBatch()
    {
        SessionState.SetBool("SaveFeaturePlayChecks.Batch", true);
        Run();
    }

    private static void Changed(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Running, false))
            return;

        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            checks = 0;
            routine = Exercise();
            EditorApplication.update += Tick;
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.update -= Tick;
            SessionState.SetBool(Running, false);
            string directory = SessionState.GetString("SaveFeaturePlayChecks.Directory", "");
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);

            if (SessionState.GetBool("SaveFeaturePlayChecks.Batch", false))
            {
                SessionState.SetBool("SaveFeaturePlayChecks.Batch", false);
                EditorApplication.Exit(SessionState.GetBool("SaveFeaturePlayChecks.Passed", false) ? 0 : 1);
            }
        }
    }

    private static void Tick()
    {
        try
        {
            if (routine.MoveNext())
                return;

            Debug.Log($"[Save Play Checks] PASS: {checks} checks.");
            File.WriteAllText("Temp/SavePlayChecks.result", $"PASS: {checks}");
            Finish(true);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            File.WriteAllText("Temp/SavePlayChecks.result", exception.ToString());
            Finish(false);
        }
    }

    private static void Finish(bool passed)
    {
        EditorApplication.update -= Tick;
        SessionState.SetBool("SaveFeaturePlayChecks.Passed", passed);
        EditorApplication.isPlaying = false;
    }

    private static IEnumerator Exercise()
    {
        var saves = SaveManager.Instance;
        typeof(SaveManager).GetField("store", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(saves, new SaveStore(SessionState.GetString("SaveFeaturePlayChecks.Directory", "")));
        Require(UIController.Current.isStartMenu, "Start menu initializes.");
        Require(Camera.allCameras.Any(camera => camera.targetTexture == null), "Start menu has a display camera, avoiding the no-cameras overlay.");
        UIController.Current.ShowSettings();
        int startSettingsFrame = Time.frameCount + 2;
        while (Time.frameCount < startSettingsFrame)
            yield return null;
        Require(UIController.Current.settingsScreen != null, "Main-menu settings open without a player present.");
        UIController.Current.HandleEscape();
        saves.NewGame();
        double deadline = EditorApplication.timeSinceStartup + 120;
        while (saves.IsLoading)
        {
            RequireTime(deadline);
            yield return null;
        }

        Require(saves.ActivePlaythrough.characterName == "user_0" && saves.ListSaves().Count == 1, "New game creates a character and initial save.");
        Require(Object.FindObjectsByType<PlayerController>().Length == 1, "New game has exactly one player.");
        var player = Object.FindAnyObjectByType<PlayerController>();
        var stats = player.GetComponent<PlayerStatsManager>();
        var mode = PlayerModeController.ForPlayer(player);
        var view = player.GetComponent<PlayerCameraRig>();
        Require(view.ViewCamera.enabled && view.ViewCamera.targetTexture == null && view.IsThirdPerson,
            "Town uses the display camera with third-person view available.");
        mode.SetMode(PlayerMode.Inventory);
        Require(Object.FindObjectsByType<InventoryGrid>().Length > 0, "Inventory opens with bound grids.");
        mode.SetMode(PlayerMode.Paused);
        Require(Time.timeScale == 0 && UIController.Current.pauseScreen.activeSelf, "Pause shows the pause page and stops time.");
        UIController.Current.ShowSettings();
        Require(UIController.Current.settingsScreen.gameObject.activeSelf && !UIController.Current.pauseScreen.activeSelf, "Settings is a separate page.");
        int settingsFrame = Time.frameCount + 2;
        while (Time.frameCount < settingsFrame)
            yield return null;

        CheckSettings(UIController.Current.settingsScreen, player);
        UIController.Current.HandleEscape();
        Require(UIController.Current.pauseScreen.activeSelf && Time.timeScale == 0, "Escape returns to Pause without resuming.");
        UIController.Current.Resume();
        Require(mode.AllowsGameplay && Time.timeScale > 0, "Resume restores gameplay and time.");
        var inventoryView = Object.FindAnyObjectByType<Inventory>();
        var inventory = Object.FindAnyObjectByType<InventoryManager>();
        var sword = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Game/Features/Inventory/Data/Items/ShortSword.asset");
        Require(inventoryView.AddItem(0, sword), "An item can be collected before saving.");
        var instance = inventory.GetItems(0).list.First(item => item.Definition == sword);
        Require(inventory.TryMoveItem(0, instance, 6, Vector2Int.zero, 0, Vector2Int.one), "Equipment can be prepared for saving.");
        player.goldAmount = 321;
        var chest = Object.FindObjectsByType<WorldSaveObject>().FirstOrDefault(target => target.GetComponent<LootingInteractable>() != null);
        string chestId = chest != null ? chest.SaveId : null;
        if (chest != null)
            inventory.ReplaceItems(chest.GetComponent<LootingInteractable>().GridId, new System.Collections.Generic.List<ItemInstance>());
        var pickup = Object.FindObjectsByType<WorldSaveObject>().FirstOrDefault(target => target.GetComponent<ItemInteractable>() != null);
        string pickupId = pickup != null ? pickup.SaveId : null;
        if (pickup != null)
        {
            saves.RecordCollected(pickup);
            Object.Destroy(pickup.gameObject);
        }

        int nextFrame = Time.frameCount + 2;
        while (Time.frameCount < nextFrame)
            yield return null;

        Require(saves.Save(), "A complete Town snapshot can be saved: " + saves.LastError);
        var saved = saves.ListSaves().OrderByDescending(save => save.savedAtUtc).First();
        Require(saved.inventory.Single(container => container.slot == 6).items.Count == 1, "Equipment is in the snapshot.");
        player.goldAmount = 0;
        inventory.RemoveItem(6, instance);
        stats.DamagePlayer(20);
        Require(saves.Load(saved), "A save can be loaded.");
        deadline = EditorApplication.timeSinceStartup + 120;
        while (saves.IsLoading)
        {
            RequireTime(deadline);
            yield return null;
        }

        player = Object.FindAnyObjectByType<PlayerController>();
        stats = player.GetComponent<PlayerStatsManager>();
        inventory = Object.FindAnyObjectByType<InventoryManager>();
        Require(player.goldAmount == 321 && stats.GetPlayerStats().currentHealth == saved.stats.currentHealth, "Load replaces mutated player data.");
        Require(inventory.GetItems(6).list.Single().Definition == sword, "Load restores equipped item definitions.");
        Require(Object.FindObjectsByType<PlayerController>().Length == 1 &&
            Object.FindObjectsByType<InventoryManager>().Length == 1, "Load replaces persistent objects without duplicates.");
        Require(player.GetComponent<PlayerCameraRig>().IsThirdPerson &&
            player.GetComponent<PlayerCameraRig>().ViewCamera.targetTexture == null,
            "Save/load retains the local view preference and display camera.");
        if (pickupId != null)
            Require(!Object.FindObjectsByType<WorldSaveObject>().Any(target => target.SaveId == pickupId), "Collected pickups stay removed after loading.");

        if (chestId != null)
        {
            var restoredChest = Object.FindObjectsByType<WorldSaveObject>().Single(target => target.SaveId == chestId);
            Require(inventory.GetItems(restoredChest.GetComponent<LootingInteractable>().GridId).list.Count == 0, "Looted container contents stay empty after loading.");
        }

        var blocker = new GameObject("Save restriction check");
        var otherBlocker = new GameObject("Second save restriction check");
        saves.BlockSaving(blocker, "Combat");
        saves.BlockSaving(otherBlocker, "Transition");
        saves.AllowSaving(blocker);
        Require(!saves.CanSave, "Removing one restriction cannot clear another owner's restriction.");
        saves.AllowSaving(otherBlocker);
        Require(saves.CanSave, "Saving becomes available when restrictions clear.");
        Object.Destroy(blocker);
        Object.Destroy(otherBlocker);
        stats.DamagePlayer(100000);
        Require(PlayerModeController.ForPlayer(player).Mode == PlayerMode.Dead && !saves.CanSave, "Death blocks gameplay and saving.");
        var death = UIController.Current.deathScreen;
        Require(death.gameObject.activeSelf && !death.respawnGroup.interactable, "Respawn is initially hidden and unavailable.");
        double reveal = EditorApplication.timeSinceStartup + 1.5;
        while (EditorApplication.timeSinceStartup < reveal)
            yield return null;

        Require(death.respawnGroup.alpha == 1 && death.respawnGroup.interactable, "Respawn fades in after its delay.");
        death.respawnButton.onClick.Invoke();
        deadline = EditorApplication.timeSinceStartup + 120;
        while (saves.IsLoading)
        {
            RequireTime(deadline);
            yield return null;
        }

        player = Object.FindAnyObjectByType<PlayerController>();
        Require(player.goldAmount == 321 && PlayerModeController.ForPlayer(player).AllowsGameplay && player.GetComponent<PlayerStatsManager>().GetPlayerStats().currentHealth > 0,
            "Respawn restores the latest save and returns to gameplay.");
        PlayerModeController.ForPlayer(player).SetMode(PlayerMode.Paused);
        UIController.Current.ShowLoad();
        var browser = UIController.Current.saveLoadScreen;
        Require(!browser.saveButton.gameObject.activeSelf && browser.loadButton.gameObject.activeSelf, "Load mode exposes Load and hides Save.");
        var profileGroup = browser.content.GetComponentInChildren<SaveProfileGroup>();
        profileGroup.headerButton.onClick.Invoke();
        Require(!profileGroup.IsExpanded && !profileGroup.entries.gameObject.activeSelf, "A profile header collapses all its saves together.");
        profileGroup.headerButton.onClick.Invoke();
        Require(profileGroup.IsExpanded && profileGroup.entries.GetComponentsInChildren<SaveEntryView>().Length > 0,
            "Expanding a profile restores its save entries.");
        var entry = browser.content.GetComponentsInChildren<SaveEntryView>().First();
        entry.button.onClick.Invoke();
        int beforeDelete = saves.ListSaves().Count;
        browser.deleteButton.onClick.Invoke();
        Require(UIController.Current.confirmation.IsOpen && saves.ListSaves().Count == beforeDelete, "Delete first opens confirmation without removing a save.");
        UIController.Current.confirmation.cancelButton.onClick.Invoke();
        Require(saves.ListSaves().Count == beforeDelete, "Cancel preserves the save.");
        browser.deleteButton.onClick.Invoke();
        UIController.Current.confirmation.okButton.onClick.Invoke();
        Require(saves.ListSaves().Count == beforeDelete - 1, "OK deletes only the selected save.");
        UIController.Current.Back();
        UIController.Current.ShowSave();
        Require(browser.saveButton.gameObject.activeSelf && !browser.loadButton.gameObject.activeSelf, "Save mode exposes Save and hides Load.");
        saves.MainMenu();
        deadline = EditorApplication.timeSinceStartup + 120;
        while (saves.IsLoading)
        {
            RequireTime(deadline);
            yield return null;
        }

        Require(UIController.Current.isStartMenu && Object.FindAnyObjectByType<PlayerController>() == null && Time.timeScale == 1,
            "Main Menu clears the player session and resets time.");
    }

    private static void CheckSettings(SettingsScreen settings, PlayerController player)
    {
        float oldSensitivity = player.LookSensitivity;
        float oldVolume = settings.volume.value;
        int oldQuality = QualitySettings.GetQualityLevel();
        bool hadVolume = PlayerPrefs.HasKey("Settings.Volume");
        bool hadSensitivity = PlayerPrefs.HasKey("Settings.Sensitivity");
        bool hadQuality = PlayerPrefs.HasKey("Settings.Quality");
        float savedVolume = PlayerPrefs.GetFloat("Settings.Volume");
        float savedSensitivity = PlayerPrefs.GetFloat("Settings.Sensitivity");
        int savedQuality = PlayerPrefs.GetInt("Settings.Quality");
        try
        {
            settings.volume.value = 37f;
            Require(settings.volumeText.text == "37%" && settings.audioMixer.GetFloat("volume", out float decibels) &&
                Mathf.Abs(decibels - Mathf.Log10(0.37f) * 20f) < 0.01f, "Volume slider changes the mixer and label.");
            settings.sensitivity.value = 7f;
            Require(Mathf.Approximately(player.LookSensitivity, 7f), "Sensitivity slider changes the live player's look sensitivity.");
            int nextQuality = (oldQuality + 1) % QualitySettings.names.Length;
            settings.qualityDropdown.value = nextQuality;
            Require(QualitySettings.GetQualityLevel() == nextQuality, "Quality dropdown changes the engine quality level.");
            Require(settings.resolutionDropdown.onValueChanged.GetPersistentEventCount() > 0 &&
                settings.fullscreenToggle.onValueChanged.GetPersistentEventCount() > 0,
                "Resolution and fullscreen controls have runtime callbacks.");
        }
        finally
        {
            settings.volume.value = oldVolume;
            settings.sensitivity.value = oldSensitivity;
            if (settings.qualityDropdown != null)
                settings.qualityDropdown.value = oldQuality;
            if (hadVolume)
                PlayerPrefs.SetFloat("Settings.Volume", savedVolume);
            else
                PlayerPrefs.DeleteKey("Settings.Volume");
            if (hadSensitivity)
                PlayerPrefs.SetFloat("Settings.Sensitivity", savedSensitivity);
            else
                PlayerPrefs.DeleteKey("Settings.Sensitivity");
            if (hadQuality)
                PlayerPrefs.SetInt("Settings.Quality", savedQuality);
            else
                PlayerPrefs.DeleteKey("Settings.Quality");
        }
    }

    private static void RequireTime(double deadline)
    {
        if (EditorApplication.timeSinceStartup > deadline)
            throw new TimeoutException("Scene transition did not finish within two minutes.");
    }

    private static void Require(bool condition, string description)
    {
        if (!condition)
            throw new Exception(description);
        checks++;
    }
}
