using System;
using System.Collections.Generic;

namespace BehaviorTreeMainProject.Log
{
    /// <summary>
    /// Base class that consolidates common logging functionality
    /// </summary>
    public abstract class BaseLogger : IBaseLogger
    {
        protected LogFileManager? fileManager;
        protected LogStatistics statistics;
        protected readonly object logLock = new object();
        protected bool enableConsole;
        protected bool enableFile;
        protected bool isInitialized = false;

        public bool IsInitialized => isInitialized;

        protected BaseLogger()
        {
            statistics = new LogStatistics();
        }

        protected virtual void Initialize(string serviceName, bool enableConsole = true, bool enableFile = true)
        {
            this.enableConsole = enableConsole;
            this.enableFile = enableFile;

            if (enableFile)
            {
                var logFilePath = LogConfiguration.GetLogFilePath(serviceName);
                fileManager = new LogFileManager(logFilePath);
            }

            isInitialized = true;
        }

        public virtual void WriteLog(string message)
        {
            if (!isInitialized) return;

            lock (logLock)
            {
                if (enableConsole)
                {
                    Console.WriteLine(message);
                }

                if (enableFile && fileManager != null)
                {
                    fileManager.WriteLine(message);
                }
            }
        }

        public virtual void WriteLog(string prefix, string message, ConsoleColor color = ConsoleColor.White)
        {
            if (!isInitialized) return;

            var formattedMessage = LogFormatter.FormatMessage(prefix, message);

            lock (logLock)
            {
                // Console output (with optional color)
                if (enableConsole)
                {
                    if (LogConfiguration.EnableColors)
                    {
                        var originalColor = Console.ForegroundColor;
                        Console.ForegroundColor = color;
                        Console.WriteLine(formattedMessage);
                        Console.ForegroundColor = originalColor;
                    }
                    else
                    {
                        Console.WriteLine(formattedMessage);
                    }
                }

                // File output (once)
                if (enableFile && fileManager != null)
                {
                    fileManager.WriteLine(formattedMessage);
                }
            }
        }

        public virtual void WriteLog(LogEntry entry)
        {
            if (!isInitialized) return;

            var formattedMessage = entry.ToString();

            lock (logLock)
            {
                // Console output (with optional color)
                if (enableConsole)
                {
                    if (LogConfiguration.EnableColors)
                    {
                        var originalColor = Console.ForegroundColor;
                        Console.ForegroundColor = entry.Color;
                        Console.WriteLine(formattedMessage);
                        Console.ForegroundColor = originalColor;
                    }
                    else
                    {
                        Console.WriteLine(formattedMessage);
                    }
                }

                // File output (once)
                if (enableFile && fileManager != null)
                {
                    fileManager.WriteLine(formattedMessage);
                }
            }
        }

        public virtual void Flush()
        {
            if (fileManager != null)
            {
                fileManager.Flush();
            }
        }

        public virtual void Close()
        {
            if (fileManager != null)
            {
                fileManager.Dispose();
            }
            fileManager = null;
            isInitialized = false;
        }

        public virtual void Clear()
        {
            if (fileManager != null)
            {
                fileManager.Clear();
            }
        }

        public virtual string GetLogFilePath()
        {
            return fileManager?.GetLogFilePath() ?? string.Empty;
        }

        protected virtual void TrackCounter(string key)
        {
            statistics.Increment(key);
        }

        protected virtual void StartTiming(string key)
        {
            statistics.StartTiming(key);
        }

        protected virtual void EndTiming(string key)
        {
            statistics.EndTiming(key);
        }

        protected virtual int GetCounter(string key)
        {
            return statistics.GetCount(key);
        }

        protected virtual TimeSpan GetTiming(string key)
        {
            return statistics.GetTiming(key);
        }

        protected virtual Dictionary<string, int> GetAllCounters()
        {
            return statistics.GetAllCounters();
        }

        protected virtual Dictionary<string, TimeSpan> GetAllTimings()
        {
            return statistics.GetAllTimings();
        }

        protected virtual void ClearStatistics()
        {
            statistics.Clear();
        }

        protected virtual void WriteSectionHeader(string header)
        {
            var formattedHeader = LogFormatter.FormatSectionHeader(header);
            WriteLog(formattedHeader);
        }

        protected virtual void WriteSubsectionHeader(string header)
        {
            var formattedHeader = LogFormatter.FormatSubsectionHeader(header);
            WriteLog(formattedHeader);
        }

        protected virtual void WriteSeparator(char separator = '-', int length = 80)
        {
            var formattedSeparator = LogFormatter.FormatSeparator(separator, length);
            WriteLog(formattedSeparator);
        }

        protected virtual void WriteTableRow(params string[] columns)
        {
            var formattedRow = LogFormatter.FormatTableRow(columns);
            WriteLog(formattedRow);
        }

        protected virtual void WriteTableHeader(params string[] columns)
        {
            var formattedHeader = LogFormatter.FormatTableHeader(columns);
            WriteLog(formattedHeader);
        }

        protected virtual void WriteTableSeparator(int columnCount, char separator = '-')
        {
            var formattedSeparator = LogFormatter.FormatTableSeparator(columnCount, separator);
            WriteLog(formattedSeparator);
        }
    }
}
