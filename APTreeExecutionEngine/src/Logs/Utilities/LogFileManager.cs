using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BehaviorTreeMainProject.Log
{
    /// <summary>
    /// Centralized file management for all logging systems.
    /// All writes are buffered in memory and flushed to disk only when
    /// Flush() or Dispose() is called — eliminating per-message I/O overhead.
    /// </summary>
    public class LogFileManager : IDisposable
    {
        private readonly List<string> _buffer = new List<string>();
        private string logFilePath;
        private readonly object fileLock = new object();
        private bool isDisposed = false;

        public LogFileManager(string logFilePath)
        {
            this.logFilePath = logFilePath;
            EnsureDirectory();
        }

        private void EnsureDirectory()
        {
            try
            {
                var directory = Path.GetDirectoryName(logFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to create log directory: {ex.Message}");
            }
        }

        public void WriteLine(string message)
        {
            if (isDisposed) return;
            lock (fileLock)
                _buffer.Add(message);
        }

        public void WriteHeader(string header) => WriteLine($"\n{header}");

        public void WriteSeparator(char separator = '-', int length = 80) => WriteLine(new string(separator, length));

        public string GetLogFilePath() => logFilePath;

        public string CurrentLogFilePath => logFilePath;

        public void Flush()
        {
            if (isDisposed) return;
            lock (fileLock)
            {
                if (_buffer.Count == 0) return;
                try
                {
                    EnsureDirectory();
                    File.AppendAllLines(logFilePath, _buffer, Encoding.UTF8);
                    _buffer.Clear();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Failed to flush log file {logFilePath}: {ex.Message}");
                }
            }
        }

        public void Clear()
        {
            if (isDisposed) return;
            lock (fileLock)
            {
                _buffer.Clear();
                try { if (File.Exists(logFilePath)) File.Delete(logFilePath); }
                catch (Exception ex) { Console.WriteLine($"Warning: Could not clear log file: {ex.Message}"); }
            }
        }

        public void Dispose()
        {
            if (!isDisposed)
            {
                Flush();
                isDisposed = true;
            }
        }
    }
}
