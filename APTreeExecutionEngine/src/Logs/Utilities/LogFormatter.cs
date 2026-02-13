using System;

namespace BehaviorTreeMainProject.Log
{
    /// <summary>
    /// Centralized formatting utilities for all logging systems
    /// </summary>
    public static class LogFormatter
    {
        public static string FormatTimestamp(DateTime timestamp)
        {
            return timestamp.ToString(LogConfiguration.TimestampFormat);
        }

        public static string FormatMessage(string prefix, string message, string additionalInfo = "")
        {
            var timestamp = FormatTimestamp(DateTime.Now);
            var result = $"[{timestamp}] {prefix}: {message}";
            
            if (!string.IsNullOrEmpty(additionalInfo))
            {
                result += $" - {additionalInfo}";
            }
            
            return result;
        }

        public static string FormatSectionHeader(string header, char separator = '=')
        {
            return $"{separator}{separator}{separator}{separator}{separator}{separator}{separator}{separator} {header} {separator}{separator}{separator}{separator}{separator}{separator}{separator}{separator}";
        }

        public static string FormatSubsectionHeader(string header, char separator = '-')
        {
            return $"{separator}{separator}{separator}{separator}{separator}{separator}{separator}{separator} {header} {separator}{separator}{separator}{separator}{separator}{separator}{separator}{separator}";
        }

        public static string FormatSeparator(char separator = '-', int length = 80)
        {
            return new string(separator, length);
        }

        public static string FormatTableRow(params string[] columns)
        {
            return string.Join(" | ", columns);
        }

        public static string FormatTableHeader(params string[] columns)
        {
            return FormatTableRow(columns);
        }

        public static string FormatTableSeparator(int columnCount, char separator = '-')
        {
            var separators = new string[columnCount];
            for (int i = 0; i < columnCount; i++)
            {
                separators[i] = new string(separator, 10);
            }
            return FormatTableRow(separators);
        }

        public static string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalMilliseconds < 1000)
            {
                return $"{duration.TotalMilliseconds:F1}ms";
            }
            else if (duration.TotalSeconds < 60)
            {
                return $"{duration.TotalSeconds:F1}s";
            }
            else
            {
                return $"{duration:mm\\:ss\\.fff}";
            }
        }

        public static string FormatPercentage(double value, double total)
        {
            if (total == 0) return "0.0%";
            return $"{(value / total * 100):F1}%";
        }

        public static string FormatCounter(int count, int total)
        {
            return $"{count}/{total} ({FormatPercentage(count, total)})";
        }

        public static string FormatEmoji(string emoji, string text)
        {
            return $"{emoji} {text}";
        }

        public static string FormatPhaseTransition(string fromPhase, string toPhase)
        {
            return $"{fromPhase} → {toPhase}";
        }
    }
}
