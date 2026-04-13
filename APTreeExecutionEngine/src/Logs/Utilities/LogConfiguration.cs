using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace BehaviorTreeMainProject.Log
{
    /// <summary>
    /// Log severity levels, ordered from most verbose to most critical.
    /// </summary>
    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Success = 2,
        Warning = 3,
        Error = 4,
        None = 5
    }

    /// <summary>
    /// Centralized configuration for all logging systems
    /// </summary>
    public static class LogConfiguration
    {
        public static string LogsDirectory { get; set; } = "WrittenLogs";
        public static bool EnableConsole { get; set; } = true;
        public static bool EnableFile { get; set; } = true;

        /// <summary>
        /// Messages below this level are suppressed on the console.
        /// File logging is unaffected (always writes everything).
        /// Default: Warning — only warnings, errors, and section headers appear in the terminal.
        /// Set to LogLevel.Debug to restore the previous (verbose) behaviour.
        /// </summary>
        public static LogLevel MinimumConsoleLogLevel { get; set; } = LogLevel.Warning;
        public static string TimestampFormat { get; set; } = "HH:mm:ss.fff";
        public static string DateFormat { get; set; } = "yyyy-MM-dd_HH-mm-ss";
        public static string DateFormatCompact { get; set; } = "yyyyMMdd_HHmmss";
        public static bool AutoFlush { get; set; } = true;
        public static int MaxLogFileSizeMB { get; set; } = 100;
        public static bool EnableColors { get; set; } = true;

        /// <summary>
        /// Maximum number of log files to keep per type (e.g. "ActionExecution.log", "TickTiming.csv").
        /// Older files beyond this limit are automatically deleted on startup.
        /// </summary>
        public static int MaxLogFilesPerType { get; set; } = 3;

        static LogConfiguration()
        {
            // Ensure logs directory exists
            if (!Directory.Exists(LogsDirectory))
            {
                try
                {
                    Directory.CreateDirectory(LogsDirectory);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Could not create logs directory: {ex.Message}");
                }
            }

            // Prune old log files on startup
            PruneOldLogFiles();
        }

        /// <summary>
        /// Groups all files in the logs directory by their type prefix + extension,
        /// then deletes the oldest files so that only <see cref="MaxLogFilesPerType"/>
        /// remain per group.
        ///
        /// Type prefix is derived by stripping the trailing timestamp portion from each
        /// filename (e.g. "ActionExecution_2026-02-17_10-09-59.log" → type "ActionExecution.log").
        /// </summary>
        public static void PruneOldLogFiles()
        {
            try
            {
                if (!Directory.Exists(LogsDirectory)) return;

                var files = Directory.GetFiles(LogsDirectory);
                if (files.Length == 0) return;

                // Regex matches the trailing timestamp(s) before the extension:
                //   _YYYY-MM-DD_HH-mm-ss  or  _YYYYMMDD_HHmmss_fff  (and combinations)
                var timestampPattern = new Regex(@"_\d{4}-?\d{2}-?\d{2}[_T-]\d{2}-?\d{2}-?\d{2}.*$");

                var groups = new Dictionary<string, List<FileInfo>>();

                foreach (var filePath in files)
                {
                    var fi = new FileInfo(filePath);
                    var nameWithoutExt = Path.GetFileNameWithoutExtension(fi.Name);
                    var ext = fi.Extension; // e.g. ".log", ".csv"

                    // Strip timestamp portion to get the type prefix
                    var typePrefix = timestampPattern.Replace(nameWithoutExt, "");
                    var groupKey = $"{typePrefix}{ext}";

                    if (!groups.ContainsKey(groupKey))
                        groups[groupKey] = new List<FileInfo>();
                    groups[groupKey].Add(fi);
                }

                int totalDeleted = 0;
                foreach (var kvp in groups)
                {
                    var sorted = kvp.Value.OrderByDescending(f => f.LastWriteTimeUtc).ToList();
                    if (sorted.Count <= MaxLogFilesPerType) continue;

                    // Delete the oldest files beyond the limit
                    foreach (var old in sorted.Skip(MaxLogFilesPerType))
                    {
                        try
                        {
                            old.Delete();
                            totalDeleted++;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Warning: Could not delete old log file {old.Name}: {ex.Message}");
                        }
                    }
                }

                if (totalDeleted > 0)
                {
                    Console.WriteLine($"🧹 LogConfiguration: Pruned {totalDeleted} old log file(s) from {LogsDirectory}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Log file pruning failed: {ex.Message}");
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
