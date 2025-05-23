using Backup_Service.Models;
using Backup_Service.Forms;

namespace Backup_Service.Services;

/// <summary>
/// Service for managing file synchronization between source and target directories
/// </summary>
public class SyncService
{
    private const string ICON_PATH = "resources/icon.png";
    private const string ALL_FILES_PATTERN = "*.*";

    private readonly Config config;
    private System.Threading.Timer? syncTimer;
    private bool wasTargetAvailable = false;
    private Icon? appIcon;
    private static bool isSyncWindowOpen = false;

    /// <summary>
    /// Initializes a new instance of the SyncService class
    /// </summary>
    /// <param name="config">The configuration containing source and target paths</param>
    public SyncService(Config config)
    {
        this.config = config;
        LoadAppIcon();
    }

    /// <summary>
    /// Loads the application icon from the resource file
    /// </summary>
    private void LoadAppIcon()
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
            // Error handling for icon loading
            Logger.Log(LogLevel.Error, $"Error loading icon: {ex.Message}");
            appIcon = SystemIcons.Application;
        }
    }

    /// <summary>
    /// Loads an icon from a file path
    /// </summary>
    /// <param name="iconPath">The path to the icon file</param>
    /// <returns>The loaded icon</returns>
    private Icon LoadIconFromFile(string iconPath)
    {
        using var stream = new FileStream(iconPath, FileMode.Open, FileAccess.Read);
        var bitmap = new Bitmap(stream);
        return Icon.FromHandle(bitmap.GetHicon());
    }

    /// <summary>
    /// Starts the synchronization service with regular checks
    /// </summary>
    public void Start()
    {
        StopExistingTimer();
        StartNewTimer();
    }

    /// <summary>
    /// Stops and disposes the existing timer if it exists
    /// </summary>
    private void StopExistingTimer()
    {
        syncTimer?.Dispose();
    }

    /// <summary>
    /// Creates and starts a new timer for synchronization checks
    /// </summary>
    private void StartNewTimer()
    {
        syncTimer = new System.Threading.Timer(
            _ => Task.Run(CheckAndSync),
            null,
            TimeSpan.Zero,
            TimeSpan.FromSeconds(config.PollingInS));
    }

    /// <summary>
    /// Stops the synchronization service
    /// </summary>
    public void Stop()
    {
        StopExistingTimer();
    }

    /// <summary>
    /// Checks and synchronizes files between source and target directories
    /// </summary>
    private async Task CheckAndSync()
    {
        try
        {
            if (!ValidateSourceDirectory()) return;
            if (!ValidateTargetAvailability()) return;
            await EnsureTargetDirectoryExists();

            var differences = await FindSynchronizationDifferences();
            if (differences.Any() && !isSyncWindowOpen)
            {
                await ShowSyncDialogAndProcess(differences);
            }
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, $"Error during synchronization check: {ex.Message}");
        }
    }

    /// <summary>
    /// Validates if the source directory exists
    /// </summary>
    /// <returns>True if the source directory exists, false otherwise</returns>
    private bool ValidateSourceDirectory()
    {
        if (Directory.Exists(config.Source)) return true;

        Logger.Log(LogLevel.Error, "Source directory does not exist");
        return false;
    }

    /// <summary>
    /// Validates if the target drive is available
    /// </summary>
    /// <returns>True if the target drive is available, false otherwise</returns>
    private bool ValidateTargetAvailability()
    {
        var targetDrive = Path.GetPathRoot(config.Target);
        var isTargetAvailable = Directory.Exists(targetDrive);

        if (!isTargetAvailable && wasTargetAvailable)
        {
            Logger.Log(LogLevel.Warning, "Target directory not available (possibly external drive not connected)");
            wasTargetAvailable = false;
            return false;
        }

        if (!wasTargetAvailable && isTargetAvailable)
        {
            Logger.Log(LogLevel.Information, "Target directory available again");
            wasTargetAvailable = true;
        }

        return isTargetAvailable;
    }

    /// <summary>
    /// Ensures the target directory exists, creates it if necessary
    /// </summary>
    private async Task EnsureTargetDirectoryExists()
    {
        if (Directory.Exists(config.Target)) return;

        try
        {
            await Task.Run(() => Directory.CreateDirectory(config.Target));
            Logger.Log(LogLevel.Information, $"Target directory created: {config.Target}");
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, $"Error creating target directory: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Finds differences between source and target directories
    /// </summary>
    /// <returns>A list of differences found between the directories</returns>
    private async Task<List<dynamic>> FindSynchronizationDifferences()
    {
        var sourceFiles = await GetFilesAsync(config.Source);
        var targetFiles = await GetFilesAsync(config.Target);

        var sourceToTarget = await FindSourceToTargetDifferences(sourceFiles);
        var targetToSource = await FindTargetToSourceDifferences(targetFiles);

        return sourceToTarget.Concat(targetToSource).ToList();
    }

    /// <summary>
    /// Gets all files from a directory recursively
    /// </summary>
    /// <param name="directory">The directory to search</param>
    /// <returns>An array of file paths</returns>
    private async Task<string[]> GetFilesAsync(string directory)
    {
        return await Task.Run(() =>
            Directory.GetFiles(directory, ALL_FILES_PATTERN, SearchOption.AllDirectories));
    }

    /// <summary>
    /// Finds differences from source to target
    /// </summary>
    /// <param name="sourceFiles">List of source files</param>
    /// <returns>A list of differences from source to target</returns>
    private async Task<List<dynamic>> FindSourceToTargetDifferences(string[] sourceFiles)
    {
        return await Task.Run(() => sourceFiles
            .Select(sourceFile => CheckFileDifference(sourceFile, config.Source, config.Target, true))
            .Where(x => x != null)
            .Select(x => x!)
            .ToList());
    }

    /// <summary>
    /// Finds differences from target to source
    /// </summary>
    /// <param name="targetFiles">List of target files</param>
    /// <returns>A list of differences from target to source</returns>
    private async Task<List<dynamic>> FindTargetToSourceDifferences(string[] targetFiles)
    {
        return await Task.Run(() => targetFiles
            .Select(targetFile => CheckFileDifference(targetFile, config.Target, config.Source, false))
            .Where(x => x != null)
            .Select(x => x!)
            .ToList());
    }

    /// <summary>
    /// Checks for differences between a source and target file
    /// </summary>
    /// <param name="sourceFile">The source file path</param>
    /// <param name="sourceRoot">The source root directory</param>
    /// <param name="targetRoot">The target root directory</param>
    /// <param name="isSourceToTarget">Direction of synchronization</param>
    /// <returns>A difference object if differences are found, null otherwise</returns>
    private dynamic? CheckFileDifference(string sourceFile, string sourceRoot, string targetRoot, bool isSourceToTarget)
    {
        var relativePath = Path.GetRelativePath(sourceRoot, sourceFile);
        var targetFile = Path.Combine(targetRoot, relativePath);

        if (!File.Exists(targetFile))
        {
            return CreateDifferenceObject(sourceFile, targetFile, "New", isSourceToTarget);
        }

        var sourceInfo = new FileInfo(sourceFile);
        var targetInfo = new FileInfo(targetFile);

        if (sourceInfo.LastWriteTime > targetInfo.LastWriteTime)
        {
            return CreateDifferenceObject(sourceFile, targetFile, "Update", isSourceToTarget);
        }

        return null;
    }

    /// <summary>
    /// Creates a difference object for synchronization
    /// </summary>
    /// <param name="source">The source file path</param>
    /// <param name="target">The target file path</param>
    /// <param name="action">The action to perform</param>
    /// <param name="isSourceToTarget">Direction of synchronization</param>
    /// <returns>A difference object</returns>
    private dynamic CreateDifferenceObject(string source, string target, string action, bool isSourceToTarget)
    {
        var direction = isSourceToTarget ? "Source -> Target" : "Target -> Source";
        return new { Source = source, Target = target, Action = $"{action} ({direction})" };
    }

    /// <summary>
    /// Shows the synchronization dialog and processes selected items
    /// </summary>
    /// <param name="differences">List of differences to show</param>
    private async Task ShowSyncDialogAndProcess(List<dynamic> differences)
    {
        isSyncWindowOpen = true;
        try
        {
            using var form = new SyncForm(differences, config.Source, config.Target);
            form.Icon = appIcon;

            if (form.ShowDialog() == DialogResult.OK)
            {
                await ProcessSelectedItems(form.GetSelectedItems());
            }
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, $"Error showing sync dialog: {ex.Message}");
        }
        finally
        {
            isSyncWindowOpen = false;
        }
    }

    /// <summary>
    /// Processes the selected items for synchronization
    /// </summary>
    /// <param name="selectedItems">List of selected items to process</param>
    private async Task ProcessSelectedItems(IEnumerable<dynamic> selectedItems)
    {
        foreach (var item in selectedItems)
        {
            try
            {
                await EnsureTargetDirectoryExists(item.Target);
                await CopyFileAsync(item.Source, item.Target);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, $"Error processing item: {ex.Message}");
                // Continue with next item
            }
        }
    }

    /// <summary>
    /// Ensures the target directory exists for a file
    /// </summary>
    /// <param name="targetPath">The target file path</param>
    private async Task EnsureTargetDirectoryExists(string targetPath)
    {
        try
        {
            var targetDir = Path.GetDirectoryName(targetPath);
            if (targetDir != null && !Directory.Exists(targetDir))
            {
                await Task.Run(() => Directory.CreateDirectory(targetDir));
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            Logger.Log(LogLevel.Error, $"Access denied when creating directory: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, $"Error creating directory: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Copies a file from source to target
    /// </summary>
    /// <param name="source">The source file path</param>
    /// <param name="target">The target file path</param>
    private async Task CopyFileAsync(string source, string target)
    {
        try
        {
            var targetDir = Path.GetDirectoryName(target);
            if (targetDir != null && !Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            await Task.Run(() => File.Copy(source, target, true));
            Logger.Log(LogLevel.Execution, $"File synchronized: {source} -> {target}");
        }
        catch (UnauthorizedAccessException ex)
        {
            Logger.Log(LogLevel.Error, $"Access denied when copying file: {ex.Message}");
            throw;
        }
        catch (IOException ex)
        {
            Logger.Log(LogLevel.Error, $"IO error when copying file: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, $"Error copying file: {ex.Message}");
            throw;
        }
    }
}