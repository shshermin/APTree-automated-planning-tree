using System;
using System.IO;
using System.Text;

namespace BehaviorTreeMainProject.Log
{
    /// <summary>
    /// Centralized file management for all logging systems
    /// </summary>
    public class LogFileManager : IDisposable
    {
        private StreamWriter fileWriter;
        private string logFilePath;
        private readonly object fileLock = new object();
        private bool isDisposed = false;

        public LogFileManager(string logFilePath)
        {
            this.logFilePath = logFilePath;
            InitializeFile();
        }

        private void InitializeFile()
        {
            try
            {
                // Ensure directory exists
                var directory = Path.GetDirectoryName(logFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Check if file is locked by another process
                if (File.Exists(logFilePath))
                {
                    try
                    {
                        // Try to open the file for writing to check if it's locked
                        using (var testStream = File.Open(logFilePath, FileMode.Append, FileAccess.Write, FileShare.Read))
                        {
                            // If we can open it, it's not locked
                        }
                    }
                    catch (IOException)
                    {
                        // File is locked, create a new filename with timestamp
                        var fileName = Path.GetFileNameWithoutExtension(logFilePath);
                        var extension = Path.GetExtension(logFilePath);
                        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
                        var newLogFilePath = Path.Combine(directory, $"{fileName}_{timestamp}{extension}");
                        
                        Console.WriteLine($"⚠️ Log file {logFilePath} is locked, creating new file: {newLogFilePath}");
                        logFilePath = newLogFilePath;
                    }
                }

                // Create or append to file
                fileWriter = new StreamWriter(logFilePath, true, Encoding.UTF8);
                fileWriter.AutoFlush = LogConfiguration.AutoFlush;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to create log file {logFilePath}: {ex.Message}");
                
                // Try to create a fallback log file in the same directory
                try
                {
                    var directory = Path.GetDirectoryName(logFilePath);
                    var fallbackPath = Path.Combine(directory, $"fallback_{DateTime.Now:yyyyMMdd_HHmmss_fff}.log");
                    Console.WriteLine($"🔄 Attempting to create fallback log file: {fallbackPath}");
                    
                    logFilePath = fallbackPath;
                    fileWriter = new StreamWriter(logFilePath, true, Encoding.UTF8);
                    fileWriter.AutoFlush = LogConfiguration.AutoFlush;
                    Console.WriteLine($"✅ Successfully created fallback log file: {fallbackPath}");
                }
                catch (Exception fallbackEx)
                {
                    Console.WriteLine($"❌ Failed to create fallback log file: {fallbackEx.Message}");
                    throw;
                }
            }
        }

        public void WriteLine(string message)
        {
            if (isDisposed || fileWriter == null) return;

            lock (fileLock)
            {
                try
                {
                    fileWriter.WriteLine(message);
                    
                    // Check file size and rotate if needed
                    CheckFileSize();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Failed to write to log file: {ex.Message}");
                }
            }
        }

        public void WriteHeader(string header)
        {
            WriteLine($"\n{header}");
        }

        public void WriteSeparator(char separator = '-', int length = 80)
        {
            WriteLine(new string(separator, length));
        }

        private void CheckFileSize()
        {
            try
            {
                if (fileWriter?.BaseStream != null)
                {
                    var fileSizeMB = fileWriter.BaseStream.Length / (1024 * 1024);
                    if (fileSizeMB > LogConfiguration.MaxLogFileSizeMB)
                    {
                        RotateLogFile();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not check log file size: {ex.Message}");
            }
        }

        private void RotateLogFile()
        {
            try
            {
                if (fileWriter != null)
                {
                    fileWriter.Close();
                    fileWriter.Dispose();
                }

                var backupPath = logFilePath.Replace(".log", $"_backup_{DateTime.Now:yyyyMMdd_HHmmss}.log");
                File.Move(logFilePath, backupPath);
                
                // Recreate file
                InitializeFile();
                WriteLine($"Log file rotated at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not rotate log file: {ex.Message}");
            }
        }

        public string GetLogFilePath()
        {
            return logFilePath;
        }

        public string CurrentLogFilePath => logFilePath;

        public void Flush()
        {
            if (fileWriter != null && !isDisposed)
            {
                lock (fileLock)
                {
                    fileWriter.Flush();
                }
            }
        }

        public void Clear()
        {
            if (isDisposed) return;

            lock (fileLock)
            {
                try
                {
                    if (fileWriter != null)
                    {
                        fileWriter.Close();
                        fileWriter.Dispose();
                    }

                    if (File.Exists(logFilePath))
                    {
                        File.Delete(logFilePath);
                    }

                    InitializeFile();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Could not clear log file: {ex.Message}");
                }
            }
        }

        public void Dispose()
        {
            if (!isDisposed)
            {
                lock (fileLock)
                {
                    try
                    {
                        if (fileWriter != null)
                        {
                            fileWriter.Flush();
                            fileWriter.Close();
                            fileWriter.Dispose();
                            fileWriter = null;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠️ Warning: Error during file disposal: {ex.Message}");
                    }
                    finally
                    {
                        isDisposed = true;
                    }
                }
            }
        }
    }
}
