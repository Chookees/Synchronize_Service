using System.Text.Json.Serialization;

namespace Backup_Service.Models;

/// <summary>
/// Represents the configuration for the backup service
/// </summary>
public class Config
{
    /// <summary>
    /// The source path for files to be backed up
    /// </summary>
    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// The target path for backup copies
    /// </summary>
    [JsonPropertyName("target")]
    public string Target { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether the service should start automatically with the application
    /// </summary>
    [JsonPropertyName("autoStart")]
    public bool AutoStart { get; set; }

    /// <summary>
    /// The interval in seconds for checking changes
    /// </summary>
    [JsonPropertyName("pollingInS")]
    public int PollingInS { get; set; }

    /// <summary>
    /// Validates the configuration
    /// </summary>
    /// <returns>True if the configuration is valid</returns>
    public bool Validate()
    {
        return !string.IsNullOrWhiteSpace(Source) &&
               !string.IsNullOrWhiteSpace(Target) &&
               PollingInS > 0;
    }
}