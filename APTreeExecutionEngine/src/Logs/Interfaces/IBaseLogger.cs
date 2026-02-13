using System;

namespace BehaviorTreeMainProject.Log
{
    /// <summary>
    /// Base interface for all logging systems
    /// </summary>
    public interface IBaseLogger
    {
        void WriteLog(string message);
        void WriteLog(string prefix, string message, ConsoleColor color = ConsoleColor.White);
        void WriteLog(LogEntry entry);
        void Flush();
        void Close();
        string GetLogFilePath();
        bool IsInitialized { get; }
    }
}
