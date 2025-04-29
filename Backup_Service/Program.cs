using Backup_Service.Forms;
using Backup_Service.Models;
using Backup_Service.Services;
using System.Text.Json;

namespace Backup_Service;

/// <summary>
/// Main application class
/// </summary>
static class Program
{
    private const string COMPANY_NAME = "AZDev";
    private const string APP_NAME = "Backup_Service";
    private const string CONFIG_FILE = "config.json";
    private const string ICON_PATH = "resources/icon.ico";

    private static NotifyIcon trayIcon = new();
    private static string configPath = string.Empty;
    private static Config? config;
    private static SyncService? syncService;
    private static Icon? appIcon;

    [STAThread]
    static void Main()
    {
        try
        {
            InitializeApplication();
            RunApplication();
        }
        catch (Exception ex)
        {
            HandleCriticalError(ex);
        }
    }

    private static void InitializeApplication()
    {
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        InitializeAppData();
        LoadAppIcon();
        InitializeTrayIcon();
        LoadConfig();
    }

    private static void RunApplication()
    {
        Application.Run();
    }

    private static void HandleCriticalError(Exception ex)
    {
        Logger.Log(LogLevel.Error, $"Critical error: {ex.Message}");
        MessageBox.Show(
            $"A critical error occurred: {ex.Message}\nThe application will be terminated.",
            "Critical Error",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
        Environment.Exit(1);
    }

    private static void LoadAppIcon()
    {
        try
        {
            var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ICON_PATH);
            appIcon = File.Exists(iconPath)
                ? LoadIconFromFile(iconPath)
                : SystemIcons.Application;
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, $"Error loading icon: {ex.Message}");
            appIcon = SystemIcons.Application;
        }
    }

    private static Icon LoadIconFromFile(string iconPath)
    {
        using var stream = new FileStream(iconPath, FileMode.Open, FileAccess.Read);
        var bitmap = new Bitmap(stream);
        return Icon.FromHandle(bitmap.GetHicon());
    }

    private static void InitializeAppData()
    {
        var appDataPath = GetAppDataPath();
        EnsureDirectoryExists(appDataPath);
        configPath = Path.Combine(appDataPath, CONFIG_FILE);
    }

    private static string GetAppDataPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            COMPANY_NAME,
            APP_NAME);
    }

    private static void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            Logger.Log(LogLevel.Information, "AppData directory created");
        }
    }

    private static void InitializeTrayIcon()
    {
        try
        {
            trayIcon.Icon = appIcon;
            trayIcon.Visible = true;
            trayIcon.Text = APP_NAME;
            InitializeContextMenu();
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, $"Error initializing tray icon: {ex.Message}");
            trayIcon.Icon = SystemIcons.Application;
        }
    }

    private static void InitializeContextMenu()
    {
        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("Configuration", null, ShowConfig);
        contextMenu.Items.Add("Open Log", null, ShowLog);
        contextMenu.Items.Add("Exit", null, ExitApplication);
        trayIcon.ContextMenuStrip = contextMenu;
    }

    private static void ShowLog(object? sender, EventArgs e)
    {
        try
        {
            var logPath = GetLogFilePath();
            if (File.Exists(logPath))
            {
                OpenLogFile(logPath);
            }
            else
            {
                ShowNoLogFileMessage();
            }
        }
        catch (Exception ex)
        {
            HandleLogOpenError(ex);
        }
    }

    private static string GetLogFilePath()
    {
        return Path.Combine(
            GetAppDataPath(),
            $"Service_Log_{DateTime.Now:ddMMyyyy}.log");
    }

    private static void OpenLogFile(string logPath)
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = logPath,
            UseShellExecute = true
        });
    }

    private static void ShowNoLogFileMessage()
    {
        MessageBox.Show(
            "No log file found for today.",
            "Information",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private static void HandleLogOpenError(Exception ex)
    {
        Logger.Log(LogLevel.Error, $"Error opening log: {ex.Message}");
        MessageBox.Show(
            $"Error opening log: {ex.Message}",
            "Error",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }

    private static void LoadConfig()
    {
        try
        {
            if (File.Exists(configPath))
            {
                LoadExistingConfig();
            }
            else
            {
                ShowConfig(null, EventArgs.Empty);
            }
        }
        catch (Exception ex)
        {
            HandleConfigLoadError(ex);
        }
    }

    private static void LoadExistingConfig()
    {
        var json = File.ReadAllText(configPath);
        config = JsonSerializer.Deserialize<Config>(json);
        Logger.Log(LogLevel.Information, "Configuration loaded");

        if (config != null && config.AutoStart != StartupManager.StartWithWindows)
        {
            StartupManager.StartWithWindows = config.AutoStart;
        }

        if (config?.Validate() == true)
        {
            StartSyncService();
        }
        else
        {
            ShowConfig(null, EventArgs.Empty);
        }
    }

    private static void HandleConfigLoadError(Exception ex)
    {
        Logger.Log(LogLevel.Error, $"Error loading configuration: {ex.Message}");
        ShowConfig(null, EventArgs.Empty);
    }

    private static void StartSyncService()
    {
        if (config == null)
        {
            Logger.Log(LogLevel.Error, "Cannot start sync service: Configuration is null");
            return;
        }

        StopExistingSyncService();
        CreateAndStartNewSyncService();
    }

    private static void StopExistingSyncService()
    {
        if (syncService != null)
        {
            syncService.Stop();
            syncService = null;
        }
    }

    private static void CreateAndStartNewSyncService()
    {
        if (config == null)
        {
            Logger.Log(LogLevel.Error, "Cannot create sync service: Configuration is null");
            return;
        }

        syncService = new SyncService(config);
        syncService.Start();
        Logger.Log(LogLevel.Information, "Sync service started");
    }

    private static void ShowConfig(object? sender, EventArgs e)
    {
        try
        {
            using var configForm = new ConfigForm(configPath);
            if (configForm.ShowDialog() == DialogResult.OK)
            {
                LoadConfig();
                StartSyncService();
            }
        }
        catch (Exception ex)
        {
            HandleConfigFormError(ex);
        }
    }

    private static void HandleConfigFormError(Exception ex)
    {
        Logger.Log(LogLevel.Error, $"Error in configuration form: {ex.Message}");
        MessageBox.Show(
            $"Error in configuration form: {ex.Message}",
            "Error",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }

    private static void SaveConfig()
    {
        try
        {
            if (config == null) return;

            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(configPath, json);
            Logger.Log(LogLevel.Information, "Configuration saved");

            if (config.AutoStart != StartupManager.StartWithWindows)
            {
                StartupManager.StartWithWindows = config.AutoStart;
            }
        }
        catch (Exception ex)
        {
            HandleConfigSaveError(ex);
        }
    }

    private static void HandleConfigSaveError(Exception ex)
    {
        Logger.Log(LogLevel.Error, $"Error saving configuration: {ex.Message}");
        MessageBox.Show(
            $"Error saving configuration: {ex.Message}",
            "Error",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }

    private static void ExitApplication(object? sender, EventArgs e)
    {
        try
        {
            StopExistingSyncService();
            trayIcon.Visible = false;
            Application.Exit();
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, $"Error during application exit: {ex.Message}");
            Environment.Exit(1);
        }
    }
}