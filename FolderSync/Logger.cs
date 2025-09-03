using System;
using System.IO;

namespace FolderSync
{
    /// <summary>
    /// Simple logger that outputs to both console and file
    /// </summary>
    public class Logger
    {
        private readonly string _logFilePath;
        private readonly object _lockObject = new object();

        /// <summary>
        /// Initialize logger with specified log file path
        /// </summary>
        /// <param name="logFilePath">Path to the log file</param>
        public Logger(string logFilePath)
        {
            _logFilePath = logFilePath;
            
            // Ensure log directory exists
            var logDir = Path.GetDirectoryName(Path.GetFullPath(logFilePath));
            if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }
        }

        /// <summary>
        /// Log an informational message
        /// </summary>
        /// <param name="message">Message to log</param>
        public void LogInfo(string message)
        {
            LogMessage("INFO", message);
        }

        /// <summary>
        /// Log an error message
        /// </summary>
        /// <param name="message">Error message to log</param>
        public void LogError(string message)
        {
            LogMessage("ERROR", message);
        }

        /// <summary>
        /// Log a warning message
        /// </summary>
        /// <param name="message">Warning message to log</param>
        public void LogWarning(string message)
        {
            LogMessage("WARNING", message);
        }

        /// <summary>
        /// Internal method to handle message logging with timestamp and level
        /// </summary>
        /// <param name="level">Log level (INFO, ERROR, WARNING)</param>
        /// <param name="message">Message to log</param>
        private void LogMessage(string level, string message)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var logEntry = $"[{timestamp}] {level}: {message}";

            lock (_lockObject)
            {
                // Write to console
                Console.WriteLine(logEntry);

                // Write to file
                try
                {
                    File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to write to log file: {ex.Message}");
                }
            }
        }
    }
}
