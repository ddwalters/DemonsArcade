using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIController : MonoBehaviour
{
    public static UIController Current { get; private set; }
    public bool isStartMenu;
    public GameObject pauseScreen;
    public GameObject startScreen;
    public SettingsScreen settingsScreen;
    public SaveLoadScreen saveLoadScreen;
    public DeathScreen deathScreen;
    public ConfirmationDialog confirmation;
    public GameObject loadingScreen;
    private PlayerModeController mode;
    private CanvasGroup menuGroup;
    private CanvasGroup startGroup;
    private bool missingSaveManagerReported;
    private readonly HashSet<string> reportedWarnings = new();

    private void Awake()
    {
        Current = this;
        ResolveMissingReferences();

        GameObject menuCanvas = FindChild("MenuCanvas");
        GameObject interactionRoot = menuCanvas != null ? menuCanvas : gameObject;
        menuGroup = GetCanvasGroup(interactionRoot);
        if (startScreen != null)
            startGroup = GetCanvasGroup(startScreen);
    }

    private void Start()
    {
        if (settingsScreen != null)
            settingsScreen.ApplySavedSettings();
        mode = FindAnyObjectByType<PlayerModeController>();
        if (mode != null)
        {
            mode.ModeChanged += ApplyMode;
            ApplyMode(mode.Mode);
        }
        else
            ShowRoot();
    }

    private void Update()
    {
        SaveManager saveManager = SaveManager.Instance;
        bool loading = saveManager == null || saveManager.IsLoading;
        if (saveManager == null && !missingSaveManagerReported)
        {
            Debug.LogError("UIController cannot enable menu actions because SaveManager is unavailable.", this);
            missingSaveManagerReported = true;
        }

        if (loadingScreen != null)
            loadingScreen.SetActive(loading);

        bool confirmationOpen = confirmation != null && confirmation.IsOpen;
        if (menuGroup != null)
            menuGroup.interactable = !confirmationOpen && !loading;
        if (startGroup != null)
            startGroup.interactable = !confirmationOpen && !loading;
        if (isStartMenu && MenuInput.PausePressed)
            Back();
    }

    private void OnDestroy()
    {
        if (mode != null)
            mode.ModeChanged -= ApplyMode;

        if (Current == this)
            Current = null;
    }

    public void ShowSettings()
    {
        if (settingsScreen == null)
        {
            WarnOnce("settings-screen", "Settings are unavailable because this UI has no SettingsScreen.");
            return;
        }

        HidePages();
        settingsScreen.gameObject.SetActive(true);
        SelectFirst(settingsScreen.gameObject);
    }

    public void ShowSave() => ShowSaves(true);
    public void ShowLoad() => ShowSaves(false);

    private void ShowSaves(bool saving)
    {
        if (saveLoadScreen == null)
        {
            WarnOnce("save-load-screen", "Save and load menus are unavailable because this UI has no SaveLoadScreen.");
            return;
        }

        HidePages();
        saveLoadScreen.Show(saving);
        SelectFirst(saveLoadScreen.gameObject);
    }

    public bool HandleEscape()
    {
        if (SaveManager.Instance == null || SaveManager.Instance.IsLoading)
            return true;

        if ((confirmation != null && confirmation.IsOpen) ||
            (settingsScreen != null && settingsScreen.gameObject.activeSelf) ||
            (saveLoadScreen != null && saveLoadScreen.gameObject.activeSelf))
        {
            Back();
            return true;
        }

        return false;
    }

    public void Back()
    {
        if (confirmation != null && confirmation.IsOpen)
        {
            confirmation.Cancel();
            return;
        }

        ShowRoot();
    }

    public void Resume()
    {
        if (mode != null)
            mode.SetMode(PlayerMode.Gameplay);
    }

    public void NewGame()
    {
        SaveManager saveManager = RequireSaveManager();
        if (saveManager != null)
            saveManager.NewGame();
    }

    public void MainMenu()
    {
        SaveManager saveManager = RequireSaveManager();
        if (saveManager == null)
            return;

        if (confirmation != null)
            confirmation.Show("Return to the main menu?\nProgress since your last save will be lost.", saveManager.MainMenu);
        else
        {
            WarnOnce("confirmation", "Confirmation dialog is missing; returning to the main menu without a prompt.");
            saveManager.MainMenu();
        }
    }

    public void Quit() => Application.Quit();

    private void ApplyMode(PlayerMode next)
    {
        if (confirmation != null)
            confirmation.Cancel();
        HidePages();
        if (deathScreen != null)
            deathScreen.gameObject.SetActive(next == PlayerMode.Dead);
        else if (next == PlayerMode.Dead)
            WarnOnce("death-screen", "Death UI is unavailable because this UI has no DeathScreen.");
        if (next == PlayerMode.Paused)
            ShowRoot();
    }

    private void ShowRoot()
    {
        HidePages();
        GameObject root = isStartMenu ? (startScreen != null ? startScreen : pauseScreen) : pauseScreen;
        if (root == null)
        {
            WarnOnce("root-screen", "The root menu is unavailable because this UI has no start or pause screen.");
            return;
        }

        root.SetActive(true);
        SelectFirst(root);
    }

    private void HidePages()
    {
        if (pauseScreen != null)
            pauseScreen.SetActive(false);
        if (startScreen != null)
            startScreen.SetActive(false);

        if (settingsScreen != null)
            settingsScreen.gameObject.SetActive(false);
        if (saveLoadScreen != null)
            saveLoadScreen.gameObject.SetActive(false);
    }

    private void ResolveMissingReferences()
    {
        if (pauseScreen == null)
            pauseScreen = FindChild("PauseScreen");
        if (startScreen == null)
            startScreen = FindChild("StartScreen");
        if (settingsScreen == null)
            settingsScreen = FindNamedComponent<SettingsScreen>("SettingsScreen");
        if (saveLoadScreen == null)
            saveLoadScreen = FindNamedComponent<SaveLoadScreen>("SaveLoadScreen");
        if (deathScreen == null)
            deathScreen = FindNamedComponent<DeathScreen>("DeathScreen");
        if (confirmation == null)
            confirmation = GetComponentInChildren<ConfirmationDialog>(true);
        if (loadingScreen == null)
            loadingScreen = FindChild("LoadingScreen");
    }

    private static CanvasGroup GetCanvasGroup(GameObject target)
    {
        CanvasGroup group = target.GetComponent<CanvasGroup>();
        return group;
    }

    private GameObject FindChild(string childName)
    {
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
            if (child != transform && child.name == childName)
                return child.gameObject;

        return null;
    }

    private T FindNamedComponent<T>(string childName) where T : Component
    {
        GameObject child = FindChild(childName);
        return child != null ? child.GetComponent<T>() : GetComponentInChildren<T>(true);
    }

    private SaveManager RequireSaveManager()
    {
        if (SaveManager.Instance != null)
            return SaveManager.Instance;

        ReportMissingSaveManager();
        return null;
    }

    private void WarnOnce(string key, string message)
    {
        if (reportedWarnings.Add(key))
            Debug.LogWarning(message, this);
    }

    private void ReportMissingSaveManager()
    {
        if (missingSaveManagerReported)
            return;

        Debug.LogError("UIController cannot enable save actions because SaveManager is unavailable.", this);
        missingSaveManagerReported = true;
    }

    private static void SelectFirst(GameObject screen)
    {
        var first = screen.GetComponentInChildren<UnityEngine.UI.Selectable>();
        EventSystem.current?.SetSelectedGameObject(first != null ? first.gameObject : null);
    }
}
