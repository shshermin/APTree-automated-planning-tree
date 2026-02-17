using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BehaviorTreeMainProject.Log;

namespace BehaviorTreeMainProject.Log.Services
{
    /// <summary>
    /// Record of an action execution for CSV generation
    /// </summary>
    public class ActionExecutionRecord
    {
        public int Counter { get; set; }
        public DateTime Timestamp { get; set; }
        public string ActionName { get; set; } = "";
        public string InstanceName { get; set; } = "";
        public string Status { get; set; } = "";
        public string AdditionalInfo { get; set; } = "";
        public double TimeSinceStartMs { get; set; }
    }

    /// <summary>
    /// Service to track the order of ML action node execution in a separate log file
    /// </summary>
    public class ActionExecutionLogger : BaseLogger
    {
        private static ActionExecutionLogger? instance;
        private static readonly object lockObject = new object();
        private int executionCounter = 0;
        private readonly DateTime startTime;
        
        // CSV generation data
        private readonly List<ActionExecutionRecord> executionRecords = new List<ActionExecutionRecord>();

        public static ActionExecutionLogger Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (lockObject)
                    {
                        if (instance == null)
                        {
                            instance = new ActionExecutionLogger();
                        }
                    }
                }
                return instance;
            }
        }

        private ActionExecutionLogger()
        {
            startTime = DateTime.Now;
            
            // Initialize the base logger
            base.Initialize("ActionExecution", true, true);
            
            // Write header to log file
            WriteToLog("=== ML Action Execution Order Log ===");
            WriteToLog($"Started at: {startTime:yyyy-MM-dd HH:mm:ss.fff}");
            WriteToLog("Format: [Counter] [Timestamp] [ActionName] [InstanceName] [Status]");
            WriteToLog("=====================================");
        }

        /// <summary>
        /// Log the execution of an ML action node
        /// </summary>
        /// <param name="actionName">The name of the action class (e.g., "PickUpML")</param>
        /// <param name="instanceName">The instance name of the action</param>
        /// <param name="status">The execution status (Started, Completed, Failed)</param>
        /// <param name="additionalInfo">Optional additional information</param>
        public void LogActionExecution(string actionName, string instanceName, string status, string additionalInfo = "")
        {
            lock (lockObject)
            {
                executionCounter++;
                var timestamp = DateTime.Now;
                var timeSinceStart = timestamp - startTime;
                
                // Store record for CSV generation
                var record = new ActionExecutionRecord
                {
                    Counter = executionCounter,
                    Timestamp = timestamp,
                    ActionName = actionName,
                    InstanceName = instanceName,
                    Status = status,
                    AdditionalInfo = additionalInfo,
                    TimeSinceStartMs = timeSinceStart.TotalMilliseconds
                };
                executionRecords.Add(record);
                
                var timestampStr = timestamp.ToString("HH:mm:ss.fff");
                var logEntry = $"[{executionCounter:D4}] [{timestampStr}] [{actionName}] [{instanceName}] [{status}]";
                
                if (!string.IsNullOrEmpty(additionalInfo))
                {
                    logEntry += $" - {additionalInfo}";
                }
                
                logEntry += $" (+{timeSinceStart.TotalMilliseconds:F0}ms)";
                
                WriteToLog(logEntry);
            }
        }

        /// <summary>
        /// Log when an action starts executing
        /// </summary>
        public void LogActionStarted(string actionName, string instanceName, string additionalInfo = "")
        {
            LogActionExecution(actionName, instanceName, "STARTED", additionalInfo);
        }

        /// <summary>
        /// Log when an action completes successfully
        /// </summary>
        public void LogActionCompleted(string actionName, string instanceName, string additionalInfo = "")
        {
            LogActionExecution(actionName, instanceName, "COMPLETED", additionalInfo);
        }

        /// <summary>
        /// Log when an action fails
        /// </summary>
        public void LogActionFailed(string actionName, string instanceName, string additionalInfo = "")
        {
            LogActionExecution(actionName, instanceName, "FAILED", additionalInfo);
        }

        /// <summary>
        /// Log when an action is skipped or not executed
        /// </summary>
        public void LogActionSkipped(string actionName, string instanceName, string reason = "")
        {
            LogActionExecution(actionName, instanceName, "SKIPPED", reason);
        }

        /// <summary>
        /// Write a message to the log file
        /// </summary>
        private void WriteToLog(string message)
        {
            base.WriteLog(message);
        }

        /// <summary>
        /// Get the path to the current log file
        /// </summary>
        public new string GetLogFilePath()
        {
            return base.GetLogFilePath();
        }

        /// <summary>
        /// Get the total number of actions logged
        /// </summary>
        public int GetExecutionCount()
        {
            return executionCounter;
        }

        /// <summary>
        /// Clear the log file and reset counter (for testing purposes)
        /// </summary>
        public void ClearLog()
        {
            lock (lockObject)
            {
                executionCounter = 0;
                executionRecords.Clear();
                base.Clear();
                
                // Recreate header
                WriteToLog("=== ML Action Execution Order Log (CLEARED) ===");
                WriteToLog($"Cleared at: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                WriteToLog("Format: [Counter] [Timestamp] [ActionName] [InstanceName] [Status]");
                WriteToLog("===============================================");
            }
        }

        /// <summary>
        /// Generate and export CSV summary of action executions
        /// </summary>
        public static void GenerateCSVSummary()
        {
            Instance.GenerateCSVSummaryInternal();
        }

        private void GenerateCSVSummaryInternal()
        {
            lock (lockObject)
            {
                WriteSectionHeader("📊 ACTION EXECUTION CSV SUMMARY");
                
                if (executionRecords.Count == 0)
                {
                    WriteLog("⚠️ No action execution records found to generate CSV");
                    return;
                }

                try
                {
                    // Generate CSV content
                    var csvContent = GenerateCSVContent();
                    
                    // Write CSV to log
                    WriteLog("CSV Summary:");
                    WriteLog(csvContent);
                    
                    // Also write to a separate CSV file
                    WriteCSVToFile(csvContent);
                    
                    WriteLog($"✅ Action execution CSV summary generated successfully with {executionRecords.Count} records");
                }
                catch (Exception ex)
                {
                    WriteLog($"❌ Error generating action execution CSV summary: {ex.Message}");
                }
            }
        }

        private string GenerateCSVContent()
        {
            var csv = new StringBuilder();
            
            // CSV Header
            csv.AppendLine("Counter,Timestamp,ActionName,InstanceName,Status,AdditionalInfo,TimeSinceStartMs");
            
            // CSV Rows
            foreach (var record in executionRecords)
            {
                var additionalInfo = string.IsNullOrEmpty(record.AdditionalInfo) ? "" : record.AdditionalInfo.Replace(",", ";");
                csv.AppendLine($"{record.Counter},{record.Timestamp:yyyy-MM-dd HH:mm:ss.fff},{record.ActionName},{record.InstanceName},{record.Status},\"{additionalInfo}\",{record.TimeSinceStartMs:F2}");
            }
            
            return csv.ToString();
        }

        private void WriteCSVToFile(string csvContent)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                var csvFileName = $"ActionExecutionSummary_{timestamp}.csv";
                var csvFilePath = Path.Combine("WrittenLogs", csvFileName);
                
                // Ensure directory exists
                Directory.CreateDirectory("WrittenLogs");
                
                File.WriteAllText(csvFilePath, csvContent, Encoding.UTF8);
                WriteLog($"📄 CSV file written to: {csvFilePath}");
            }
            catch (Exception ex)
            {
                WriteLog($"❌ Error writing CSV file: {ex.Message}");
            }
        }

        /// <summary>
        /// Close the logger and generate final CSV summary
        /// </summary>
        public new void Close()
        {
            lock (lockObject)
            {
                // Generate final CSV summary before closing
                GenerateCSVSummaryInternal();
                
                // Call base close
                base.Close();
            }
        }
    }
}
