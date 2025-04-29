using Backup_Service.Forms;
using Backup_Service.Models;
using Backup_Service.Services;
using System.Text.Json;

namespace Backup_Service;

/// <summary>
/// Hauptklasse der Anwendung
/// </summary>
static class Program
{
    private const string COMPANY_NAME = "AZDev";
    private const string APP_NAME = "Backup_Service";
    private const string CONFIG_FILE = "config.json";
    private const string ICON_PATH = "resources/icon.png";

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
        Logger.Log(LogLevel.Error, $"Kritischer Fehler: {ex.Message}");
        MessageBox.Show(
            $"Ein kritischer Fehler ist aufgetreten: {ex.Message}\nDie Anwendung wird beendet.",
            "Kritischer Fehler",
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
            Logger.Log(LogLevel.Error, $"Fehler beim Laden des Icons: {ex.Message}");
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
            Logger.Log(LogLevel.Information, "AppData-Verzeichnis erstellt");
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
            Logger.Log(LogLevel.Error, $"Fehler beim Initialisieren des Tray-Icons: {ex.Message}");
            trayIcon.Icon = SystemIcons.Application;
        }
    }

    private static void InitializeContextMenu()
    {
        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("Konfiguration", null, ShowConfig);
        contextMenu.Items.Add("Log öffnen", null, ShowLog);
        contextMenu.Items.Add("Beenden", null, ExitApplication);
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
            "Keine Log-Datei für heute gefunden.",
            "Information",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private static void HandleLogOpenError(Exception ex)
    {
        Logger.Log(LogLevel.Error, $"Fehler beim Öffnen des Logs: {ex.Message}");
        MessageBox.Show(
            $"Fehler beim Öffnen des Logs: {ex.Message}",
            "Fehler",
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
        Logger.Log(LogLevel.Information, "Konfiguration geladen");

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
        Logger.Log(LogLevel.Error, $"Fehler beim Laden der Konfiguration: {ex.Message}");
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
        syncService?.Stop();
    }

    private static void CreateAndStartNewSyncService()
    {
        if (config == null)
        {
            Logger.Log(LogLevel.Error, "Cannot start sync service: Configuration is null");
            return;
        }

        syncService = new SyncService(config);
        syncService.Start();
        Logger.Log(LogLevel.Information, "Synchronization service started");
    }

    private static void ShowConfig(object? sender, EventArgs e)
    {
        using var form = new ConfigForm(configPath);
        form.Icon = appIcon;
        if (form.ShowDialog() == DialogResult.OK)
        {
            LoadConfig();
        }
    }

    private static void ExitApplication(object? sender, EventArgs e)
    {
        StopExistingSyncService();
        trayIcon.Visible = false;
        Application.Exit();
    }
}