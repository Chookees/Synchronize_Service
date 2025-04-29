using Microsoft.Win32;

namespace Backup_Service.Services;

/// <summary>
/// Manages the application's startup behavior with Windows
/// </summary>
public static class StartupManager
{
    private const string COMPANY_NAME = "AZDev";
    private const string APP_NAME = "Backup_Service";
    private const string REGISTRY_RUN_KEY = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";

    /// <summary>
    /// Gets or sets whether the application should start with Windows
    /// </summary>
    public static bool StartWithWindows
    {
        get => IsStartupEnabled();
        set => SetStartup(value);
    }

    /// <summary>
    /// Checks if the application is configured to start with Windows
    /// </summary>
    /// <returns>True if the application is set to start with Windows</returns>
    private static bool IsStartupEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(REGISTRY_RUN_KEY);
            var value = key?.GetValue(APP_NAME);
            return value != null;
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, $"Error checking startup status: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Sets whether the application should start with Windows
    /// </summary>
    /// <param name="enable">True to enable startup with Windows, false to disable</param>
    private static void SetStartup(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(REGISTRY_RUN_KEY, true);
            if (key == null) return;

            if (enable)
            {
                var exePath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    $"{APP_NAME}.exe");

                key.SetValue(APP_NAME, exePath);
                Logger.Log(LogLevel.Information, "Application set to start with Windows");
            }
            else
            {
                key.DeleteValue(APP_NAME, false);
                Logger.Log(LogLevel.Information, "Application removed from Windows startup");
            }
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, $"Error setting startup status: {ex.Message}");
        }
    }
}