namespace Backup_Service.Services;

/// <summary>
/// Defines the different log levels for the application
/// </summary>
public enum LogLevel
{
    /// <summary>Informational messages</summary>
    Information,
    /// <summary>Questions or decisions</summary>
    Question,
    /// <summary>Executed actions</summary>
    Execution,
    /// <summary>Warnings</summary>
    Warning,
    /// <summary>Errors</summary>
    Error
}

/// <summary>
/// Static class for logging application events
/// </summary>
public static class Logger
{
    private const string COMPANY_NAME = "AZDev";
    private const string APP_NAME = "Backup_Service";
    private const string LOG_FILE_PREFIX = "Service_Log_";
    private const string LOG_FILE_EXTENSION = ".log";
    private const string LOG_DATE_FORMAT = "ddMMyyyy";
    private const string LOG_TIMESTAMP_FORMAT = "yyyy-MM-dd HH:mm:ss";

    private static readonly string LogDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        COMPANY_NAME,
        APP_NAME);

    private static string LogFile => Path.Combine(
        LogDirectory,
        $"{LOG_FILE_PREFIX}{DateTime.Now:LOG_DATE_FORMAT}{LOG_FILE_EXTENSION}");

    /// <summary>
    /// Writes a message to the log
    /// </summary>
    /// <param name="level">The log level of the message</param>
    /// <param name="message">The message to log</param>
    public static void Log(LogLevel level, string message)
    {
        try
        {
            EnsureLogDirectoryExists();
            WriteLogMessage(level, message);
        }
        catch (Exception ex)
        {
            HandleLoggingError(ex);
        }
    }

    /// <summary>
    /// Ensures the log directory exists
    /// </summary>
    private static void EnsureLogDirectoryExists()
    {
        if (!Directory.Exists(LogDirectory))
        {
            Directory.CreateDirectory(LogDirectory);
        }
    }

    /// <summary>
    /// Writes a log message to the log file
    /// </summary>
    /// <param name="level">The log level</param>
    /// <param name="message">The message to write</param>
    private static void WriteLogMessage(LogLevel level, string message)
    {
        var logMessage = FormatLogMessage(level, message);
        File.AppendAllText(LogFile, logMessage + Environment.NewLine);
    }

    /// <summary>
    /// Formats a log message with timestamp and log level
    /// </summary>
    /// <param name="level">The log level</param>
    /// <param name="message">The message to format</param>
    /// <returns>Formatted log message</returns>
    private static string FormatLogMessage(LogLevel level, string message)
    {
        return $"[{DateTime.Now:LOG_TIMESTAMP_FORMAT}] [{level}] {message}";
    }

    /// <summary>
    /// Handles errors that occur during logging
    /// </summary>
    /// <param name="ex">The exception that occurred</param>
    private static void HandleLoggingError(Exception ex)
    {
        // If logging fails, try to write to console
        Console.WriteLine($"Error during logging: {ex.Message}");

        // Additionally write to Windows Event Log
        try
        {
            if (!System.Diagnostics.EventLog.SourceExists(APP_NAME))
            {
                System.Diagnostics.EventLog.CreateEventSource(APP_NAME, "Application");
            }

            using var eventLog = new System.Diagnostics.EventLog("Application")
            {
                Source = APP_NAME
            };
            eventLog.WriteEntry($"Error during logging: {ex.Message}",
                System.Diagnostics.EventLogEntryType.Error);
        }
        catch (Exception eventLogEx)
        {
            // If this also fails, we can't do anything more
            Console.WriteLine($"Error writing to Event Log: {eventLogEx.Message}");
        }
    }
}