using System;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;

public class DisplaySettings : MonoBehaviour
{
    public TMP_Dropdown resolutionDropdown;
    public TMP_Dropdown windowModeDropdown;

    private Resolution[] resolutions;
    private const string RESOLUTION_PREF_KEY = "ResolutionIndex";
    private const string DISPLAY_MODE_PREF_KEY = "DisplayMode";

    private const int GWL_STYLE = -16;
    private const int WS_OVERLAPPEDWINDOW = 0x00CF0000;
    private const int WS_POPUP = unchecked((int)0x80000000); // Convert to int using unchecked

    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_FRAMECHANGED = 0x0020;

    void Start()
    {
        // Populate resolution dropdown
        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();
        foreach (var res in resolutions)
        {
            resolutionDropdown.options.Add(new TMP_Dropdown.OptionData(res.width + "x" + res.height));
        }

        // Populate window mode dropdown
        windowModeDropdown.ClearOptions();
        windowModeDropdown.options.Add(new TMP_Dropdown.OptionData("Windowed"));
        windowModeDropdown.options.Add(new TMP_Dropdown.OptionData("Fullscreen"));
        windowModeDropdown.options.Add(new TMP_Dropdown.OptionData("Borderless"));
        windowModeDropdown.options.Add(new TMP_Dropdown.OptionData("Windowed Borderless"));

        // Load saved settings
        LoadSettings();

        // Bind UI events
        resolutionDropdown.onValueChanged.AddListener(SetResolution);
        windowModeDropdown.onValueChanged.AddListener(SetWindowMode);
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

    void SetWindowMode(int index)
    {
        FullScreenMode mode = FullScreenMode.Windowed;

        switch (index)
        {
            case 0: // Windowed
                mode = FullScreenMode.Windowed;
                ApplyStandardMode(mode);
                break;
            case 1: // Fullscreen
                mode = FullScreenMode.ExclusiveFullScreen;
                ApplyStandardMode(mode);
                break;
            case 2: // Borderless
                mode = FullScreenMode.FullScreenWindow;
                ApplyStandardMode(mode);
                break;
            case 3: // Windowed Borderless
                ApplyWindowedBorderlessMode();
                break;
        }

        // Save display mode
        PlayerPrefs.SetInt(DISPLAY_MODE_PREF_KEY, index);
        PlayerPrefs.Save();

        Debug.Log($"Display mode set to: {mode}");
    }

    void ApplyStandardMode(FullScreenMode mode)
    {
        Screen.fullScreenMode = mode;
    }

    void ApplyWindowedBorderlessMode()
    {
        Resolution resolution = resolutions[resolutionDropdown.value];
        Screen.SetResolution(resolution.width, resolution.height, FullScreenMode.Windowed);

        IntPtr hWnd = GetActiveWindow();

        // Set window style to popup (borderless)
        int style = GetWindowLong(hWnd, GWL_STYLE);
        style &= ~WS_OVERLAPPEDWINDOW;
        style |= WS_POPUP; // WS_POPUP is now an int
        SetWindowLong(hWnd, GWL_STYLE, style);

        // Resize and reposition window to exclude the title bar
        SetWindowPos(hWnd, IntPtr.Zero, 0, 0, resolution.width, resolution.height, SWP_NOMOVE | SWP_FRAMECHANGED);
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
        int savedDisplayMode = PlayerPrefs.GetInt(DISPLAY_MODE_PREF_KEY, 0); // Default to Windowed
        windowModeDropdown.value = savedDisplayMode;
        windowModeDropdown.RefreshShownValue();

        if (savedDisplayMode == 3) // Windowed Borderless
        {
            ApplyWindowedBorderlessMode();
        }
        else
        {
            FullScreenMode mode = FullScreenMode.Windowed;
            switch (savedDisplayMode)
            {
                case 0: mode = FullScreenMode.Windowed; break;
                case 1: mode = FullScreenMode.ExclusiveFullScreen; break;
                case 2: mode = FullScreenMode.FullScreenWindow; break;
            }

            ApplyStandardMode(mode);
        }
    }
}
