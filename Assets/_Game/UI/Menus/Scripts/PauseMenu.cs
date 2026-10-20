using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Audio;
using TMPro;

public class PauseMenu : MonoBehaviour
{
    [Header("OPTION MENU")]
    public GameObject GameManager;
    private PlayerModeController mode;
    public GameObject optionsMenu;
    public Button backButton;

    [Header("Resolution")]
    public TMP_Dropdown resolutionDropdown;
    Resolution[] resolutions;

    [Space]
    [Header("Volume")]
    public AudioMixer audioMixer;
    public Slider volume;
    public int Volume;
    public TMP_Text volumeText;

    [Header("Sensitivity")]
    public Slider sensitivity;
    public int Sensitivity;
    public TMP_Text sensitivityText;

    private bool isOptionsMenuActive = false;

    private void Start()
    {
        mode = PlayerModeController.ForPlayer(FindAnyObjectByType<PlayerController>());
        if (mode != null)
            mode.ModeChanged += ApplyMode;

        GameManager = FindAnyObjectByType<GameManager>().gameObject;

        ApplyMode(mode != null ? mode.Mode : PlayerMode.Gameplay);

        resolutions = Screen.resolutions;

        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();

        int currentResolutionIndex = 0;
        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + "x" + resolutions[i].height;
            options.Add(option);

            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();
    }

    private void Update()
    {
        if (isOptionsMenuActive)
        {
            Volume = (int)volume.value;
            volumeText.text = Volume + "%";

            // Map the 0-100 slider value to -80 to 0 for the audio mixer
            float mappedVolume = Map(Volume, 0, 100, -80, 0);
            audioMixer.SetFloat("volume", mappedVolume);

            Sensitivity = (int)sensitivity.value;
            sensitivityText.text = Sensitivity.ToString();
        }
    }

    private void OnDestroy()
    {
        if (mode != null)
            mode.ModeChanged -= ApplyMode;
    }

    private void ApplyMode(PlayerMode next)
    {
        isOptionsMenuActive = next == PlayerMode.Paused;
        optionsMenu.SetActive(isOptionsMenuActive);
    }

    public void options() => mode?.SetMode(PlayerMode.Paused);

    public void resume()
    {
        mode?.SetMode(PlayerMode.Gameplay);
        EventSystem.current?.SetSelectedGameObject(null);
    }

    public void exit()
    {
        Application.Quit();
    }

    private float Map(float value, float from1, float to1, float from2, float to2)
    {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }

    public void SetQuality(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }

    public void SetResolution(int resolutionIndex)
    {
        Resolution resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
    }
}
