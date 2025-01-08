using System;
using System.Collections;
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
    private const int WS_POPUP = unchecked((int)0x80000000);

    private const float WINDOWED_BORDERLESS_DURATION = 0.25f;

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
        switch (index)
        {
            case 0: // Windowed
                ApplyStandardMode(FullScreenMode.Windowed);
                break;
            case 1: // Fullscreen
                ApplyStandardMode(FullScreenMode.ExclusiveFullScreen);
                break;
            case 2: // Borderless
                ApplyStandardMode(FullScreenMode.FullScreenWindow);
                break;
            case 3: // Windowed Borderless
                StartCoroutine(ToggleWindowedBorderless());
                break;
        }

        // Save display mode
        PlayerPrefs.SetInt(DISPLAY_MODE_PREF_KEY, index);
        PlayerPrefs.Save();

        Debug.Log($"Display mode set to: {index}");
    }

    IEnumerator ToggleWindowedBorderless()
    {
        Resolution resolution = resolutions[resolutionDropdown.value];

        // First switch to Windowed Borderless
        ApplyWindowedBorderlessMode();
        yield return new WaitForSeconds(WINDOWED_BORDERLESS_DURATION);

        // Temporarily switch to Windowed mode
        ApplyStandardMode(FullScreenMode.Windowed);
        yield return new WaitForSeconds(WINDOWED_BORDERLESS_DURATION);

        // Switch back to Windowed Borderless
        ApplyWindowedBorderlessMode();
    }

    void ApplyStandardMode(FullScreenMode mode)
    {
        IntPtr hWnd = GetActiveWindow();

        // Restore the WS_OVERLAPPEDWINDOW style (for Windowed mode)
        int style = GetWindowLong(hWnd, GWL_STYLE);
        style |= WS_OVERLAPPEDWINDOW; // Add back the title bar and borders
        style &= ~WS_POPUP;           // Remove the borderless style
        SetWindowLong(hWnd, GWL_STYLE, style);

        // Apply the standard mode using Unity's Screen API
        Screen.fullScreenMode = mode;

        // Trigger a redraw to ensure the window style is updated
        SetWindowPos(hWnd, IntPtr.Zero, 0, 0, 0, 0, SWP_FRAMECHANGED | SWP_NOMOVE | SWP_NOSIZE);

        Debug.Log("Switched to standard mode with title bar restored.");
    }

    void ApplyWindowedBorderlessMode()
    {
        Resolution resolution = resolutions[resolutionDropdown.value];

        // Manually reduce width and height
        int targetWidth = resolution.width - 16;  // Subtract 16 pixels
        int targetHeight = resolution.height - 39; // Subtract 39 pixels

        // Ensure the screen is in Windowed mode first
        // Screen.SetResolution(resolution.width, resolution.height, FullScreenMode.Windowed);
        Screen.SetResolution(targetWidth, targetHeight, FullScreenMode.Windowed);

        IntPtr hWnd = GetActiveWindow();

        // Set window style to popup (borderless)
        int style = GetWindowLong(hWnd, GWL_STYLE);
        style &= ~WS_OVERLAPPEDWINDOW; // Remove the standard Windowed style
        style |= WS_POPUP;             // Add the borderless style
        SetWindowLong(hWnd, GWL_STYLE, style);

        // Resize and reposition the window with manual adjustments
        SetWindowPos(hWnd, IntPtr.Zero, 0, 0, targetWidth, targetHeight, SWP_NOMOVE | SWP_FRAMECHANGED);

        Debug.Log($"Switched to Windowed Borderless mode with adjusted size: {targetWidth}x{targetHeight}");
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
            StartCoroutine(ToggleWindowedBorderless());
        }
        else
        {
            ApplyStandardMode((FullScreenMode)savedDisplayMode);
        }
    }
}
