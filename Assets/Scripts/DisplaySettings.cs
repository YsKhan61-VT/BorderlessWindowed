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
    private static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll")]
    private static extern bool GetClientRect(IntPtr hWnd, out Rect lpRect);

    [DllImport("user32.dll")]
    private static extern bool AdjustWindowRectEx(ref Rect lpRect, uint dwStyle, bool bMenu, uint dwExStyle);

    private const int SM_CYCAPTION = 4; // Height of the window title bar
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_FRAMECHANGED = 0x0020;

    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

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

        // Center the window on the screen
        CenterWindow(hWnd);

        // Trigger a redraw to ensure the window style is updated
        SetWindowPos(hWnd, IntPtr.Zero, 0, 0, 0, 0, SWP_FRAMECHANGED | SWP_NOMOVE | SWP_NOSIZE);

        Debug.Log("Switched to standard mode with title bar restored and window centered.");
    }

    void CenterWindow(IntPtr hWnd)
    {
        // Get the monitor's screen dimensions
        int screenWidth = Screen.currentResolution.width;
        int screenHeight = Screen.currentResolution.height;

        // Get the window's resolution
        int windowWidth = Screen.width;
        int windowHeight = Screen.height;

        // Get the height of the title bar
        int titleBarHeight = GetSystemMetrics(SM_CYCAPTION);

        // Calculate the top-left corner to center the window
        int x = (screenWidth - windowWidth) / 2;
        int y = (screenHeight - windowHeight) / 2;

        // Adjust Y to account for the title bar
        y = Math.Max(0, y - titleBarHeight / 2); // Prevent moving off the screen

        // Position the window
        SetWindowPos(hWnd, IntPtr.Zero, x, y, 0, 0, SWP_NOSIZE | SWP_NOZORDER);
        Debug.Log($"Window centered at: ({x}, {y}) with title bar height adjustment.");
    }

    void DebugClientArea(IntPtr hWnd)
    {
        GetClientRect(hWnd, out Rect clientRect);
        int clientWidth = clientRect.Right - clientRect.Left;
        int clientHeight = clientRect.Bottom - clientRect.Top;

        Debug.Log($"Actual client area: {clientWidth}x{clientHeight}");
    }

    void ApplyWindowedBorderlessMode()
    {
        Resolution resolution = resolutions[resolutionDropdown.value];

        // Ensure the screen is in Windowed mode first
        Screen.SetResolution(resolution.width, resolution.height, FullScreenMode.Windowed);

        IntPtr hWnd = GetActiveWindow();

        // Set the window style to popup (borderless)
        int style = GetWindowLong(hWnd, GWL_STYLE);
        style &= ~WS_OVERLAPPEDWINDOW; // Remove standard window style
        style |= WS_POPUP;             // Add borderless popup style
        SetWindowLong(hWnd, GWL_STYLE, style);

        // Calculate the initial required window size for the target resolution
        Rect rect = new Rect
        {
            Left = 0,
            Top = 0,
            Right = resolution.width,
            Bottom = resolution.height
        };

        AdjustWindowRectEx(ref rect, (uint)style, false, 0);

        int adjustedWidth = rect.Right - rect.Left;
        int adjustedHeight = rect.Bottom - rect.Top;

        Debug.Log($"Initial adjusted window size: {adjustedWidth}x{adjustedHeight}");

        // Resize the window
        SetWindowPos(hWnd, IntPtr.Zero, 0, 0, adjustedWidth, adjustedHeight, SWP_NOMOVE | SWP_FRAMECHANGED);

        // Verify and iteratively correct the client area size
        for (int i = 0; i < 5; i++)
        {
            GetClientRect(hWnd, out Rect clientRect);
            int clientWidth = clientRect.Right - clientRect.Left;
            int clientHeight = clientRect.Bottom - clientRect.Top;

            if (clientWidth == resolution.width && clientHeight == resolution.height)
            {
                Debug.Log($"Client area matches target resolution: {clientWidth}x{clientHeight}");
                break;
            }

            // Adjust the window size based on the discrepancy
            int widthDifference = resolution.width - clientWidth;
            int heightDifference = resolution.height - clientHeight;

            Debug.Log($"Adjusting window size by: Width={widthDifference}, Height={heightDifference}");

            adjustedWidth += widthDifference;
            adjustedHeight += heightDifference;

            SetWindowPos(hWnd, IntPtr.Zero, 0, 0, adjustedWidth, adjustedHeight, SWP_NOMOVE | SWP_FRAMECHANGED);
        }

        Debug.Log($"Final window size: {adjustedWidth}x{adjustedHeight}");
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
