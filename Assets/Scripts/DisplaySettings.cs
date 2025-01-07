using UnityEngine;
using UnityEngine.UI;

public class DisplaySetting : MonoBehaviour
{
    public Dropdown resolutionDropdown;
    public Button windowedButton;
    public Button borderlessButton;
    public Button fullscreenButton;

    private Resolution[] resolutions;
    private const string RESOLUTION_PREF_KEY = "ResolutionIndex";
    private const string DISPLAY_MODE_PREF_KEY = "DisplayMode";

    void Start()
    {
        // Populate resolution dropdown
        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();
        foreach (var res in resolutions)
        {
            resolutionDropdown.options.Add(new Dropdown.OptionData(res.width + "x" + res.height));
        }

        // Load saved settings
        LoadSettings();

        // Bind UI events
        resolutionDropdown.onValueChanged.AddListener(SetResolution);
        windowedButton.onClick.AddListener(() => SetDisplayMode(FullScreenMode.Windowed));
        borderlessButton.onClick.AddListener(() => SetDisplayMode(FullScreenMode.FullScreenWindow));
        fullscreenButton.onClick.AddListener(() => SetDisplayMode(FullScreenMode.ExclusiveFullScreen));
    }

    void SetResolution(int index)
    {
        Resolution selectedResolution = resolutions[index];
        Screen.SetResolution(selectedResolution.width, selectedResolution.height, Screen.fullScreenMode);

        // Save resolution index
        PlayerPrefs.SetInt(RESOLUTION_PREF_KEY, index);
        PlayerPrefs.Save();

        Debug.Log($"Resolution set to {selectedResolution.width}x{selectedResolution.height}");
    }

    void SetDisplayMode(FullScreenMode mode)
    {
        Screen.fullScreenMode = mode;

        // Save display mode
        PlayerPrefs.SetInt(DISPLAY_MODE_PREF_KEY, (int)mode);
        PlayerPrefs.Save();

        Debug.Log($"Display mode set to: {mode}");
    }

    void LoadSettings()
    {
        // Load resolution index
        int savedResolutionIndex = PlayerPrefs.GetInt(RESOLUTION_PREF_KEY, 0); // Default to first resolution
        resolutionDropdown.value = savedResolutionIndex;
        resolutionDropdown.RefreshShownValue();

        // Apply saved resolution
        if (resolutions.Length > savedResolutionIndex)
        {
            Resolution savedResolution = resolutions[savedResolutionIndex];
            Screen.SetResolution(savedResolution.width, savedResolution.height, Screen.fullScreenMode);
        }

        // Load and apply display mode
        int savedDisplayMode = PlayerPrefs.GetInt(DISPLAY_MODE_PREF_KEY, (int)FullScreenMode.Windowed); // Default to Windowed
        Screen.fullScreenMode = (FullScreenMode)savedDisplayMode;

        Debug.Log($"Loaded settings: ResolutionIndex={savedResolutionIndex}, DisplayMode={(FullScreenMode)savedDisplayMode}");
    }
}
