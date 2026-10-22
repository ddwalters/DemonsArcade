using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SettingsScreen : MonoBehaviour
{
    public TMP_Dropdown resolutionDropdown;
    public TMP_Dropdown qualityDropdown;
    public Toggle fullscreenToggle;
    public AudioMixer audioMixer;
    public Slider volume;
    public TMP_Text volumeText;
    public Slider sensitivity;
    public TMP_Text sensitivityText;
    private readonly List<Resolution> resolutions = new();

    public void ApplySavedSettings()
    {
        float savedVolume = PlayerPrefs.GetFloat("Settings.Volume", 100f);
        audioMixer?.SetFloat("volume", savedVolume <= 0f ? -80f : Mathf.Log10(savedVolume / 100f) * 20f);
        if (PlayerPrefs.HasKey("Settings.Quality"))
            QualitySettings.SetQualityLevel(PlayerPrefs.GetInt("Settings.Quality"));

        if (PlayerPrefs.HasKey("Settings.Width") && PlayerPrefs.HasKey("Settings.Height"))
            Screen.SetResolution(PlayerPrefs.GetInt("Settings.Width"), PlayerPrefs.GetInt("Settings.Height"),
                PlayerPrefs.GetInt("Settings.Fullscreen", Screen.fullScreen ? 1 : 0) == 1);
        else if (PlayerPrefs.HasKey("Settings.Fullscreen"))
            Screen.fullScreen = PlayerPrefs.GetInt("Settings.Fullscreen") == 1;
    }

    private void Start()
    {
        if (qualityDropdown != null)
            qualityDropdown.SetValueWithoutNotify(QualitySettings.GetQualityLevel());
        if (fullscreenToggle != null)
            fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);
        var options = new List<string>();
        int selected = 0;
        foreach (var resolution in Screen.resolutions)
        {
            string label = resolution.width + "x" + resolution.height;
            if (options.Contains(label))
                continue;

            if (resolution.width == Screen.width && resolution.height == Screen.height)
                selected = options.Count;

            resolutions.Add(resolution);
            options.Add(label);
        }

        string currentSize = Screen.width + "x" + Screen.height;
        if (!options.Contains(currentSize))
        {
            resolutions.Insert(0, new Resolution { width = Screen.width, height = Screen.height });
            options.Insert(0, currentSize);
            selected = 0;
        }

        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(options);
        resolutionDropdown.SetValueWithoutNotify(selected);
        resolutionDropdown.RefreshShownValue();
        volume.SetValueWithoutNotify(PlayerPrefs.GetFloat("Settings.Volume", 100f));
        float currentSensitivity = FindAnyObjectByType<PlayerController>()?.LookSensitivity ?? 10f;
        sensitivity.SetValueWithoutNotify(PlayerPrefs.GetFloat("Settings.Sensitivity", currentSensitivity));
        volume.onValueChanged.AddListener(SetVolume);
        sensitivity.onValueChanged.AddListener(SetSensitivity);
        SetVolume(volume.value);
        SetSensitivity(sensitivity.value);
    }

    private void OnDisable() => PlayerPrefs.Save();

    public void SetVolume(float value)
    {
        volumeText.text = Mathf.RoundToInt(value) + "%";
        audioMixer?.SetFloat("volume", value <= 0f ? -80f : Mathf.Log10(value / 100f) * 20f);
        PlayerPrefs.SetFloat("Settings.Volume", value);
    }

    public void SetSensitivity(float value)
    {
        sensitivityText.text = value.ToString("0.0");
        FindAnyObjectByType<PlayerController>()?.SetSensitivity(value);
        PlayerPrefs.SetFloat("Settings.Sensitivity", value);
    }

    public void SetQuality(int value)
    {
        QualitySettings.SetQualityLevel(value);
        PlayerPrefs.SetInt("Settings.Quality", value);
    }

    public void SetFullscreen(bool value)
    {
        Screen.fullScreen = value;
        PlayerPrefs.SetInt("Settings.Fullscreen", value ? 1 : 0);
    }

    public void SetResolution(int index)
    {
        if (index < 0 || index >= resolutions.Count)
            return;

        var resolution = resolutions[index];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
        PlayerPrefs.SetInt("Settings.Width", resolution.width);
        PlayerPrefs.SetInt("Settings.Height", resolution.height);
    }
}
