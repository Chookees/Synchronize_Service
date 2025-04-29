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
            using var key = Registry.CurrentUser.OpenSubKey(REGISTRY_RUN_KEY, false);
            if (key == null)
            {
                Logger.Log(LogLevel.Warning, "Registry key not found");
                return false;
            }

            var value = key.GetValue(APP_NAME);
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
            if (key == null)
            {
                Logger.Log(LogLevel.Error, "Failed to open registry key for writing");
                return;
            }

            if (enable)
            {
                var exePath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    $"{APP_NAME}.exe");

                if (!File.Exists(exePath))
                {
                    Logger.Log(LogLevel.Error, $"Executable not found at path: {exePath}");
                    return;
                }

                key.SetValue(APP_NAME, exePath);
                Logger.Log(LogLevel.Information, "Application set to start with Windows");
            }
            else
            {
                if (key.GetValue(APP_NAME) != null)
                {
                    key.DeleteValue(APP_NAME, false);
                    Logger.Log(LogLevel.Information, "Application removed from Windows startup");
                }
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            Logger.Log(LogLevel.Error, $"Access denied when modifying registry: {ex.Message}");
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, $"Error setting startup status: {ex.Message}");
        }
    }
}