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

    [DllImport("user32.dll")]
    private static extern bool GetClientRect(IntPtr hWnd, out Rect lpRect);

    [StructLayout(LayoutKind.Sequential)]
    public struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_FRAMECHANGED = 0x0020;

    private int selectedResolutionIndex;
    private int selectedDisplayMode;

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
        windowModeDropdown.options.Add(new TMP_Dropdown.OptionData("Windowed Borderless"));

        // Load saved settings or default to fullscreen at current resolution
        LoadSettings();
    }

    public void OnApplyButtonClicked()
    {
        // Read the selected values from the dropdowns
        selectedResolutionIndex = resolutionDropdown.value;
        selectedDisplayMode = windowModeDropdown.value;

        // Apply the settings
        SetResolution(selectedResolutionIndex);
        SetWindowMode(selectedDisplayMode);
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
            case 2: // Windowed Borderless
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

        if (mode == FullScreenMode.Windowed)
        {
            // Get current resolution
            Resolution resolution = resolutions[selectedResolutionIndex];

            // Add 16 pixels to width and 39 pixels to height
            int adjustedWidth = resolution.width + 16;
            int adjustedHeight = resolution.height + 39;

            // Center the window
            CenterWindow(hWnd, adjustedWidth, adjustedHeight);

            // Apply the resized window
            SetWindowPos(hWnd, IntPtr.Zero, 0, 0, adjustedWidth, adjustedHeight, SWP_FRAMECHANGED | SWP_NOMOVE);

            Debug.Log($"Switched to Windowed mode with adjusted size: {adjustedWidth}x{adjustedHeight}");
        }
        else
        {
            // For non-windowed modes, just update the style
            SetWindowPos(hWnd, IntPtr.Zero, 0, 0, 0, 0, SWP_FRAMECHANGED | SWP_NOMOVE | SWP_NOSIZE);
            Debug.Log("Switched to non-Windowed mode (Fullscreen or Borderless).");
        }
    }

    void ApplyWindowedBorderlessMode()
    {
        Resolution resolution = resolutions[selectedResolutionIndex];

        // Ensure the screen is in Windowed mode first
        Screen.SetResolution(resolution.width, resolution.height, FullScreenMode.Windowed);

        IntPtr hWnd = GetActiveWindow();

        // Set window style to popup (borderless)
        int style = GetWindowLong(hWnd, GWL_STYLE);
        style &= ~WS_OVERLAPPEDWINDOW; // Remove the standard Windowed style
        style |= WS_POPUP;             // Add the borderless style
        SetWindowLong(hWnd, GWL_STYLE, style);

        // Force the window size to match the target resolution
        int targetWidth = resolution.width + 1;
        int targetHeight = resolution.height + 1;

        // Center the window
        CenterWindow(hWnd, targetWidth, targetHeight);

        // Resize the window to match the resolution exactly
        SetWindowPos(hWnd, IntPtr.Zero, 0, 0, targetWidth, targetHeight, SWP_NOMOVE | SWP_FRAMECHANGED);

        Debug.Log($"Switched to Windowed Borderless mode with enforced size: {targetWidth}x{targetHeight}");
    }

    void CenterWindow(IntPtr hWnd, int width, int height)
    {
        // Get screen resolution
        int screenWidth = Screen.currentResolution.width;
        int screenHeight = Screen.currentResolution.height;

        // Calculate top-left corner position to center the window
        int x = (screenWidth - width) / 2;
        int y = (screenHeight - height) / 2;

        // Set the window's position
        SetWindowPos(hWnd, IntPtr.Zero, x, y, 0, 0, SWP_NOSIZE | SWP_FRAMECHANGED);

        Debug.Log($"Window centered at: ({x}, {y}) with size: {width}x{height}");
    }

    void LoadSettings()
    {
        // Check if saved settings exist
        if (PlayerPrefs.HasKey(RESOLUTION_PREF_KEY) && PlayerPrefs.HasKey(DISPLAY_MODE_PREF_KEY))
        {
            // Load saved preferences
            selectedResolutionIndex = PlayerPrefs.GetInt(RESOLUTION_PREF_KEY, 0);
            selectedDisplayMode = PlayerPrefs.GetInt(DISPLAY_MODE_PREF_KEY, 1); // Default to Fullscreen
        }
        else
        {
            // Default to fullscreen and current resolution
            selectedResolutionIndex = Array.FindIndex(resolutions, r =>
                r.width == Screen.currentResolution.width && r.height == Screen.currentResolution.height);
            if (selectedResolutionIndex == -1) selectedResolutionIndex = 0;
            selectedDisplayMode = 1; // Fullscreen
        }

        resolutionDropdown.value = selectedResolutionIndex;
        resolutionDropdown.RefreshShownValue();
        windowModeDropdown.value = selectedDisplayMode;
        windowModeDropdown.RefreshShownValue();
    }
}
