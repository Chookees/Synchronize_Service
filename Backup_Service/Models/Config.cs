using System.Text.Json.Serialization;
using Backup_Service.Services;

namespace Backup_Service.Models;

/// <summary>
/// Represents the configuration for the backup service
/// </summary>
public class Config
{
    /// <summary>
    /// Gets or sets the source directory path
    /// </summary>
    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target directory path
    /// </summary>
    [JsonPropertyName("target")]
    public string Target { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the polling interval in seconds
    /// </summary>
    [JsonPropertyName("pollingInS")]
    public int PollingInS { get; set; } = 60;

    /// <summary>
    /// Gets or sets whether the application should start with Windows
    /// </summary>
    [JsonPropertyName("autoStart")]
    public bool AutoStart { get; set; } = false;

    /// <summary>
    /// Validates the configuration
    /// </summary>
    /// <returns>True if the configuration is valid, false otherwise</returns>
    public bool Validate()
    {
        if (string.IsNullOrWhiteSpace(Source))
        {
            Logger.Log(LogLevel.Error, "Source path is empty");
            return false;
        }

        if (string.IsNullOrWhiteSpace(Target))
        {
            Logger.Log(LogLevel.Error, "Target path is empty");
            return false;
        }

        if (PollingInS < 1)
        {
            Logger.Log(LogLevel.Error, "Polling interval must be at least 1 second");
            return false;
        }

        return true;
    }
}