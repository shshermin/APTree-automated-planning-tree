using System;
using System.Collections.Generic;
using BehaviorTreeMainProject.Log;

namespace BehaviorTreeMainProject.Log.Services
{
    /// <summary>
    /// Logger for tracking blackboard-related events including new types, instances, and predicate negations
    /// </summary>
    public class BlackboardTrackingLogger : BaseLogger
    {
        private static BlackboardTrackingLogger? instance;
        private static readonly object lockObject = new object();
        
        // Tracking counters
        private int newTypeCounter = 0;
        private int newInstanceCounter = 0;
        private int predicateNegationCounter = 0;
        
        // Tracking collections
        private HashSet<string> trackedTypes = new HashSet<string>();
        private HashSet<string> trackedInstances = new HashSet<string>();
        private Dictionary<string, int> predicateNegationCounts = new Dictionary<string, int>();
        
        // Session tracking
        private readonly DateTime sessionStartTime;

        public static BlackboardTrackingLogger Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (lockObject)
                    {
                        if (instance == null)
                        {
                            instance = new BlackboardTrackingLogger();
                        }
                    }
                }
                return instance;
            }
        }

        private BlackboardTrackingLogger()
        {
            sessionStartTime = DateTime.Now;
            
            // Initialize the base logger
            base.Initialize("BlackboardTracking", true, true);
            
            // Write header to log file
            WriteToLog("=== BLACKBOARD TRACKING LOG ===");
            WriteToLog($"Started at: {sessionStartTime:yyyy-MM-dd HH:mm:ss.fff}");
            WriteToLog("Format: [Counter] [Timestamp] [EventType] [Details]");
            WriteToLog("=====================================");
        }

        /// <summary>
        /// Log when a new type is added to the blackboard
        /// </summary>
        /// <param name="typeName">Name of the new type</param>
        /// <param name="typeCategory">Category of the type (e.g., Action, Predicate, Parameter)</param>
        /// <param name="additionalInfo">Optional additional information</param>
        public static void LogNewType(string typeName, string typeCategory, string additionalInfo = "")
        {
            Instance.LogNewTypeInternal(typeName, typeCategory, additionalInfo);
        }

        private void LogNewTypeInternal(string typeName, string typeCategory, string additionalInfo)
        {
            lock (lockObject)
            {
                if (!trackedTypes.Contains(typeName))
                {
                    newTypeCounter++;
                    trackedTypes.Add(typeName);
                    
                    var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                    var timeSinceStart = DateTime.Now - sessionStartTime;
                    
                    var logEntry = $"[TYPE#{newTypeCounter:D4}] [{timestamp}] NEW_TYPE | {typeName} ({typeCategory})";
                    
                    if (!string.IsNullOrEmpty(additionalInfo))
                    {
                        logEntry += $" - {additionalInfo}";
                    }
                    
                    logEntry += $" (+{timeSinceStart.TotalMilliseconds:F0}ms)";
                    
                    WriteToLog(logEntry);
                }
            }
        }

        /// <summary>
        /// Log when a new instance is created
        /// </summary>
        /// <param name="instanceName">Name of the new instance</param>
        /// <param name="instanceType">Type of the instance</param>
        /// <param name="parentContext">Parent context or owner</param>
        /// <param name="additionalInfo">Optional additional information</param>
        public static void LogNewInstance(string instanceName, string instanceType, string parentContext = "", string additionalInfo = "")
        {
            Instance.LogNewInstanceInternal(instanceName, instanceType, parentContext, additionalInfo);
        }

        private void LogNewInstanceInternal(string instanceName, string instanceType, string parentContext, string additionalInfo)
        {
            lock (lockObject)
            {
                if (!trackedInstances.Contains(instanceName))
                {
                    newInstanceCounter++;
                    trackedInstances.Add(instanceName);
                    
                    var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                    var timeSinceStart = DateTime.Now - sessionStartTime;
                    
                    var logEntry = $"[INST#{newInstanceCounter:D4}] [{timestamp}] NEW_INSTANCE | {instanceName} ({instanceType})";
                    
                    if (!string.IsNullOrEmpty(parentContext))
                    {
                        logEntry += $" | Parent: {parentContext}";
                    }
                    
                    if (!string.IsNullOrEmpty(additionalInfo))
                    {
                        logEntry += $" - {additionalInfo}";
                    }
                    
                    logEntry += $" (+{timeSinceStart.TotalMilliseconds:F0}ms)";
                    
                    WriteToLog(logEntry);
                }
            }
        }

        /// <summary>
        /// Log when a predicate's isNegate property changes
        /// </summary>
        /// <param name="predicateName">Name of the predicate</param>
        /// <param name="oldValue">Previous negation value</param>
        /// <param name="newValue">New negation value</param>
        /// <param name="context">Context where the change occurred</param>
        /// <param name="additionalInfo">Optional additional information</param>
        public static void LogPredicateNegation(string predicateName, bool oldValue, bool newValue, string context = "", string additionalInfo = "")
        {
            Instance.LogPredicateNegationInternal(predicateName, oldValue, newValue, context, additionalInfo);
        }

        private void LogPredicateNegationInternal(string predicateName, bool oldValue, bool newValue, string context, string additionalInfo)
        {
            lock (lockObject)
            {
                predicateNegationCounter++;
                
                // Track count per predicate
                if (!predicateNegationCounts.ContainsKey(predicateName))
                {
                    predicateNegationCounts[predicateName] = 0;
                }
                predicateNegationCounts[predicateName]++;
                
                var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                var timeSinceStart = DateTime.Now - sessionStartTime;
                
                var changeDescription = oldValue != newValue ? $"CHANGED: {oldValue} → {newValue}" : $"SET: {newValue}";
                var logEntry = $"[NEG#{predicateNegationCounter:D4}] [{timestamp}] PREDICATE_NEGATION | {predicateName} | {changeDescription}";
                
                if (!string.IsNullOrEmpty(context))
                {
                    logEntry += $" | Context: {context}";
                }
                
                if (!string.IsNullOrEmpty(additionalInfo))
                {
                    logEntry += $" - {additionalInfo}";
                }
                
                logEntry += $" (+{timeSinceStart.TotalMilliseconds:F0}ms)";
                
                WriteToLog(logEntry);
            }
        }

        /// <summary>
        /// Generate and log blackboard tracking statistics
        /// </summary>
        public static void LogStatistics()
        {
            Instance.LogStatisticsInternal();
        }

        private void LogStatisticsInternal()
        {
            var sessionDuration = DateTime.Now - sessionStartTime;
            
            WriteSectionHeader("📊 BLACKBOARD TRACKING STATISTICS");
            WriteLog($"⏱️ Session Duration: {sessionDuration:hh\\:mm\\:ss\\.fff}");
            WriteLog($"🆕 Total New Types: {newTypeCounter}");
            WriteLog($"🆕 Total New Instances: {newInstanceCounter}");
            WriteLog($"🔄 Total Predicate Negations: {predicateNegationCounter}");
            
            WriteSubsectionHeader("📋 TRACKED TYPES:");
            foreach (var type in trackedTypes)
            {
                WriteLog($"   • {type}");
            }
            
            WriteSubsectionHeader("📋 TRACKED INSTANCES:");
            foreach (var instance in trackedInstances)
            {
                WriteLog($"   • {instance}");
            }
            
            WriteSubsectionHeader("🔄 PREDICATE NEGATION COUNTS:");
            foreach (var kvp in predicateNegationCounts.OrderByDescending(x => x.Value))
            {
                WriteLog($"   {kvp.Key}: {kvp.Value} negations");
            }
        }

        /// <summary>
        /// Close the logger and write final statistics
        /// </summary>
        public new static void Close()
        {
            Instance.CloseInternal();
        }

        private void CloseInternal()
        {
            LogStatisticsInternal();
            WriteSectionHeader("🏁 BLACKBOARD TRACKING LOGGER CLOSED");
            base.Close();
        }

        /// <summary>
        /// Get the log file path
        /// </summary>
        public new static string GetLogFilePath()
        {
            return Instance.GetLogFilePathInternal();
        }

        private string GetLogFilePathInternal()
        {
            return base.GetLogFilePath();
        }

        /// <summary>
        /// Get current statistics
        /// </summary>
        public static (int types, int instances, int negations) GetCurrentCounts()
        {
            return Instance.GetCurrentCountsInternal();
        }

        private (int types, int instances, int negations) GetCurrentCountsInternal()
        {
            return (newTypeCounter, newInstanceCounter, predicateNegationCounter);
        }

        /// <summary>
        /// Clear the log file and reset counters (for testing purposes)
        /// </summary>
        public static void ClearLog()
        {
            Instance.ClearLogInternal();
        }

        private void ClearLogInternal()
        {
            lock (lockObject)
            {
                newTypeCounter = 0;
                newInstanceCounter = 0;
                predicateNegationCounter = 0;
                trackedTypes.Clear();
                trackedInstances.Clear();
                predicateNegationCounts.Clear();
                
                base.Clear();
                
                // Recreate header
                WriteToLog("=== BLACKBOARD TRACKING LOG (CLEARED) ===");
                WriteToLog($"Cleared at: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                WriteToLog("Format: [Counter] [Timestamp] [EventType] [Details]");
                WriteToLog("===============================================");
            }
        }

        /// <summary>
        /// Write a message to the log file
        /// </summary>
        private void WriteToLog(string message)
        {
            base.WriteLog(message);
        }
    }
}
