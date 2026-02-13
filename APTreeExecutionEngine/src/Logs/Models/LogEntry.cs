using System;

namespace BehaviorTreeMainProject.Log
{
    /// <summary>
    /// Standardized log entry model used across all loggers
    /// </summary>
    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Level { get; set; }
        public string Message { get; set; }
        public string Category { get; set; }
        public string Prefix { get; set; }
        public ConsoleColor Color { get; set; }
        public string AdditionalInfo { get; set; }

        public LogEntry(string level, string message, string category = "", string prefix = "", ConsoleColor color = ConsoleColor.White, string additionalInfo = "")
        {
            Timestamp = DateTime.Now;
            Level = level;
            Message = message;
            Category = category;
            Prefix = prefix;
            Color = color;
            AdditionalInfo = additionalInfo;
        }

        public override string ToString()
        {
            string timestamp = Timestamp.ToString("HH:mm:ss.fff");
            string result = $"[{timestamp}] {Prefix}: {Message}";
            
            if (!string.IsNullOrEmpty(AdditionalInfo))
            {
                result += $" - {AdditionalInfo}";
            }
            
            return result;
        }
    }
}
