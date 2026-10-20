using UnityEngine;

// Retains existing prefab button entry points; shared settings behavior lives in SettingsScreen.
public class PauseMenu : SettingsScreen
{
    public GameObject GameManager;
    public GameObject optionsMenu;
    public UnityEngine.UI.Button backButton;

    public void options() => UIController.Current.ShowSettings();
    public void back() => UIController.Current.Back();
    public void resume() => UIController.Current.Resume();
    public void exit() => Application.Quit();
}
