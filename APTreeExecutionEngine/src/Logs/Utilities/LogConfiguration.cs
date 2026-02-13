using System;

namespace BehaviorTreeMainProject.Log
{
    /// <summary>
    /// Centralized configuration for all logging systems
    /// </summary>
    public static class LogConfiguration
    {
        public static string LogsDirectory { get; set; } = "WrittenLogs";
        public static bool EnableConsole { get; set; } = true;
        public static bool EnableFile { get; set; } = true;
        public static string TimestampFormat { get; set; } = "HH:mm:ss.fff";
        public static string DateFormat { get; set; } = "yyyy-MM-dd_HH-mm-ss";
        public static string DateFormatCompact { get; set; } = "yyyyMMdd_HHmmss";
        public static bool AutoFlush { get; set; } = true;
        public static int MaxLogFileSizeMB { get; set; } = 100;
        public static bool EnableColors { get; set; } = true;

        static LogConfiguration()
        {
            // Ensure logs directory exists
            if (!System.IO.Directory.Exists(LogsDirectory))
            {
                try
                {
                    System.IO.Directory.CreateDirectory(LogsDirectory);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Could not create logs directory: {ex.Message}");
                }
            }
        }

        public static string GetTimestamp()
        {
            return DateTime.Now.ToString(TimestampFormat);
        }

        public static string GetDateTimestamp()
        {
            return DateTime.Now.ToString(DateFormat);
        }

        public static string GetCompactDateTimestamp()
        {
            return DateTime.Now.ToString(DateFormatCompact);
        }

        public static string GetLogFilePath(string serviceName, string extension = "log")
        {
            var timestamp = GetDateTimestamp();
            return System.IO.Path.Combine(LogsDirectory, $"{serviceName}_{timestamp}.{extension}");
        }

        public static string GetCompactLogFilePath(string serviceName, string extension = "log")
        {
            var timestamp = GetCompactDateTimestamp();
            return System.IO.Path.Combine(LogsDirectory, $"{serviceName}_{timestamp}.{extension}");
        }
    }
}
