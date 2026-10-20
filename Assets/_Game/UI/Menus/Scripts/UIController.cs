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

    private void Awake()
    {
        Current = this;
        menuGroup = transform.Find("MenuCanvas").GetComponent<CanvasGroup>();
        if (startScreen != null)
            startGroup = startScreen.GetComponent<CanvasGroup>() ?? startScreen.AddComponent<CanvasGroup>();
    }

    private void Start()
    {
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
        bool loading = SaveManager.Instance.IsLoading;
        loadingScreen.SetActive(loading);
        menuGroup.interactable = !confirmation.IsOpen && !loading;
        if (startGroup != null)
            startGroup.interactable = !confirmation.IsOpen && !loading;
        if (isStartMenu && Input.GetKeyDown(KeyCode.Escape))
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
        HidePages();
        settingsScreen.gameObject.SetActive(true);
        SelectFirst(settingsScreen.gameObject);
    }

    public void ShowSave() => ShowSaves(true);
    public void ShowLoad() => ShowSaves(false);

    private void ShowSaves(bool saving)
    {
        HidePages();
        saveLoadScreen.Show(saving);
        SelectFirst(saveLoadScreen.gameObject);
    }

    public bool HandleEscape()
    {
        if (SaveManager.Instance.IsLoading)
            return true;

        if (confirmation.IsOpen || settingsScreen.gameObject.activeSelf || saveLoadScreen.gameObject.activeSelf)
        {
            Back();
            return true;
        }

        return false;
    }

    public void Back()
    {
        if (confirmation.IsOpen)
        {
            confirmation.Cancel();
            return;
        }

        ShowRoot();
    }

    public void Resume() => mode?.SetMode(PlayerMode.Gameplay);
    public void NewGame() => SaveManager.Instance.NewGame();
    public void MainMenu() => confirmation.Show("Return to the main menu?\nProgress since your last save will be lost.", SaveManager.Instance.MainMenu);
    public void Quit() => Application.Quit();

    private void ApplyMode(PlayerMode next)
    {
        confirmation.Cancel();
        HidePages();
        deathScreen.gameObject.SetActive(next == PlayerMode.Dead);
        if (next == PlayerMode.Paused)
            ShowRoot();
    }

    private void ShowRoot()
    {
        HidePages();
        GameObject root = isStartMenu ? startScreen : pauseScreen;
        root.SetActive(true);
        SelectFirst(root);
    }

    private void HidePages()
    {
        if (pauseScreen != null)
            pauseScreen.SetActive(false);
        if (startScreen != null)
            startScreen.SetActive(false);

        settingsScreen.gameObject.SetActive(false);
        saveLoadScreen.gameObject.SetActive(false);
    }

    private static void SelectFirst(GameObject screen)
    {
        var first = screen.GetComponentInChildren<UnityEngine.UI.Selectable>();
        EventSystem.current?.SetSelectedGameObject(first != null ? first.gameObject : null);
    }
}
