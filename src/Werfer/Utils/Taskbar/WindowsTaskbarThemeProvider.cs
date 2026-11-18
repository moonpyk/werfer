using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Avalonia.Controls;
using Avalonia.Platform;
using Microsoft.Win32;

namespace Werfer.Utils.Taskbar;

/// <inheritdoc />
[SupportedOSPlatform("windows")]
public class WindowsTaskbarThemeProvider : ITaskbarThemeProvider
{
    private const uint WmSettingChange = 0x001A;

    private WindowsTaskbarThemeProvider()
    {
    }

    /// <summary>
    ///     The only instance: it holds no state, registered as the taskbar theme provider on Windows
    /// </summary>
    public static ITaskbarThemeProvider Instance { get; } = new WindowsTaskbarThemeProvider();

    /// <inheritdoc />
    /// <remarks>
    ///     It follows the Windows mode, not the apps mode: both are set apart in Settings > Personalization > Colors
    /// </remarks>
    public PlatformThemeVariant Theme
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"
            );

            // Missing before Windows 10 1903, whose taskbar was always dark
            return key?.GetValue("SystemUsesLightTheme") is int value && value != 0
                ? PlatformThemeVariant.Light
                : PlatformThemeVariant.Dark;
        }
    }

    /// <inheritdoc />
    /// <remarks>
    ///     Called for a Windows or apps theme switch. The window gets the broadcast even while hidden in the tray
    /// </remarks>
    public void OnThemeChanged(TopLevel window, Action changed)
    {
        Win32Properties.AddWndProcHookCallback(window, (_, msg, _, lParam, ref _) =>
        {
            if (msg == WmSettingChange
                && lParam != IntPtr.Zero
                && Marshal.PtrToStringUni(lParam) == "ImmersiveColorSet")
            {
                changed();
            }

            return IntPtr.Zero;
        });
    }
}
