using System;
using System.Collections.Generic;
using System.Linq; // Added for OrderByDescending
using BehaviorTreeMainProject.Log;

namespace BehaviorTreeMainProject.Log.Services
{
    /// <summary>
    /// Dedicated logger for tracking execution flow of nodes, services, and decorators
    /// Provides a clean, focused view of what's being ticked during behavior tree execution
    /// </summary>
    public class ExecutionFlowLogger : BaseLogger
    {
        private static ExecutionFlowLogger? instance;
        private static readonly object lockObject = new object();
        private int tickCounter = 0;
        private DateTime sessionStartTime;

        // Statistics tracking
        private Dictionary<string, int> nodeTickCounts = new Dictionary<string, int>();
        private Dictionary<string, int> serviceTickCounts = new Dictionary<string, int>();
        private Dictionary<string, int> decoratorTickCounts = new Dictionary<string, int>();

        public static ExecutionFlowLogger Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (lockObject)
                    {
                        if (instance == null)
                        {
                            instance = new ExecutionFlowLogger();
                        }
                    }
                }
                return instance;
            }
        }

        private ExecutionFlowLogger() { }

        /// <summary>
        /// Initialize the execution flow logger
        /// </summary>
        /// <param name="serviceName">Name for the log file</param>
        /// <param name="enableConsole">Whether to output to console</param>
        /// <param name="enableFile">Whether to write to file</param>
        public new static void Initialize(string serviceName, bool enableConsole = true, bool enableFile = true)
        {
            var logger = Instance;
            logger.InitializeInternal(serviceName, enableConsole, enableFile);
        }

        private void InitializeInternal(string serviceName, bool enableConsole, bool enableFile)
        {
            sessionStartTime = DateTime.Now;
            this.enableConsole = enableConsole;
            this.enableFile = enableFile;

            if (enableFile)
            {
                // Use compact timestamp format for execution flow logs
                var logFilePath = LogConfiguration.GetCompactLogFilePath($"ExecutionFlow_{serviceName}");
                fileManager = new LogFileManager(logFilePath);
            }

            isInitialized = true;
            
            LogHeader($"🚀 EXECUTION FLOW LOGGER INITIALIZED - {serviceName}");
            LogHeader($"📅 Session started: {sessionStartTime:yyyy-MM-dd HH:mm:ss.fff}");
            LogHeader($"📁 Log file: {(enableFile ? base.GetLogFilePath() : "Console only")}");
            LogHeader("=".PadRight(80, '='));
        }

        /// <summary>
        /// Log a tick event for a node
        /// </summary>
        /// <param name="nodeName">Name of the node being ticked</param>
        /// <param name="nodeType">Type of the node</param>
        /// <param name="tickPhase">Current tick phase</param>
        /// <param name="status">Current status</param>
        public static void LogNodeTick(string nodeName, string nodeType, string tickPhase, string status)
        {
            Instance.LogNodeTickInternal(nodeName, nodeType, tickPhase, status);
        }

        private void LogNodeTickInternal(string nodeName, string nodeType, string tickPhase, string status)
        {
            tickCounter++;
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var message = $"🔄 TICK #{tickCounter:0000} | {timestamp} | NODE: {nodeName} ({nodeType}) | PHASE: {tickPhase} | STATUS: {status}";
            
            WriteLog(message);
            TrackNodeTick(nodeName);
        }

        /// <summary>
        /// Log a tick event for a service
        /// </summary>
        /// <param name="serviceName">Name of the service being ticked</param>
        /// <param name="serviceType">Type of the service</param>
        /// <param name="nodeName">Name of the node that owns the service</param>
        /// <param name="result">Service tick result</param>
        public static void LogServiceTick(string serviceName, string serviceType, string nodeName, string result)
        {
            Instance.LogServiceTickInternal(serviceName, serviceType, nodeName, result);
        }

        private void LogServiceTickInternal(string serviceName, string serviceType, string nodeName, string result)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var message = $"🔧 SERVICE | {timestamp} | {serviceName} ({serviceType}) | OWNER: {nodeName} | RESULT: {result}";
            
            WriteLog(message);
            TrackServiceTick(serviceName);
        }

        /// <summary>
        /// Log a tick event for a decorator
        /// </summary>
        /// <param name="decoratorName">Name of the decorator being ticked</param>
        /// <param name="decoratorType">Type of the decorator</param>
        /// <param name="nodeName">Name of the node that owns the decorator</param>
        /// <param name="result">Decorator evaluation result</param>
        public static void LogDecoratorTick(string decoratorName, string decoratorType, string nodeName, string result)
        {
            Instance.LogDecoratorTickInternal(decoratorName, decoratorType, nodeName, result);
        }

        private void LogDecoratorTickInternal(string decoratorName, string decoratorType, string nodeName, string result)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var message = $"🎭 DECORATOR | {timestamp} | {decoratorName} ({decoratorType}) | OWNER: {nodeName} | RESULT: {result}";
            
            WriteLog(message);
            TrackDecoratorTick(decoratorName);
        }

        /// <summary>
        /// Log a phase transition
        /// </summary>
        /// <param name="nodeName">Name of the node</param>
        /// <param name="fromPhase">Previous phase</param>
        /// <param name="toPhase">New phase</param>
        public static void LogPhaseTransition(string nodeName, string fromPhase, string toPhase)
        {
            Instance.LogPhaseTransitionInternal(nodeName, fromPhase, toPhase);
        }

        private void LogPhaseTransitionInternal(string nodeName, string fromPhase, string toPhase)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var message = $"🔄 PHASE TRANSITION | {timestamp} | {nodeName} | {fromPhase} → {toPhase}";
            
            WriteLog(message);
        }

        /// <summary>
        /// Log a planning phase event
        /// </summary>
        /// <param name="eventType">Type of planning event</param>
        /// <param name="details">Additional details</param>
        public static void LogPlanningEvent(string eventType, string details = "")
        {
            Instance.LogPlanningEventInternal(eventType, details);
        }

        private void LogPlanningEventInternal(string eventType, string details = "")
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var message = $"📋 PLANNING | {timestamp} | {eventType}";
            if (!string.IsNullOrEmpty(details))
            {
                message += $" | {details}";
            }
            
            WriteLog(message);
        }

        /// <summary>
        /// Log an execution phase event
        /// </summary>
        /// <param name="eventType">Type of execution event</param>
        /// <param name="details">Additional details</param>
        public static void LogExecutionEvent(string eventType, string details = "")
        {
            Instance.LogExecutionEventInternal(eventType, details);
        }

        private void LogExecutionEventInternal(string eventType, string details = "")
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var message = $"🚀 EXECUTION | {timestamp} | {eventType}";
            if (!string.IsNullOrEmpty(details))
            {
                message += $" | {details}";
            }
            
            WriteLog(message);
        }

        /// <summary>
        /// Log a separator line for better readability
        /// </summary>
        public static void LogSeparator()
        {
            Instance.LogSeparatorInternal();
        }

        private void LogSeparatorInternal()
        {
            WriteSeparator();
        }

        /// <summary>
        /// Log a section header
        /// </summary>
        /// <param name="header">Header text</param>
        public static void LogHeader(string header)
        {
            Instance.LogHeaderInternal(header);
        }

        private void LogHeaderInternal(string header)
        {
            WriteLog($"\n{header}");
        }

        /// <summary>
        /// Generate and log execution statistics
        /// </summary>
        public static void LogStatistics()
        {
            Instance.LogStatisticsInternal();
        }

        private void LogStatisticsInternal()
        {
            var sessionDuration = DateTime.Now - sessionStartTime;
            
            LogHeaderInternal("📊 EXECUTION FLOW STATISTICS");
            LogHeaderInternal($"⏱️ Session Duration: {sessionDuration:hh\\:mm\\:ss\\.fff}");
            LogHeaderInternal($"🔄 Total Ticks: {tickCounter}");
            
            LogHeaderInternal("📈 NODE TICK COUNTS:");
            foreach (var kvp in nodeTickCounts.OrderByDescending(x => x.Value))
            {
                WriteLog($"   {kvp.Key}: {kvp.Value} ticks");
            }
            
            LogHeaderInternal("🔧 SERVICE TICK COUNTS:");
            foreach (var kvp in serviceTickCounts.OrderByDescending(x => x.Value))
            {
                WriteLog($"   {kvp.Key}: {kvp.Value} ticks");
            }
            
            LogHeaderInternal("🎭 DECORATOR TICK COUNTS:");
            foreach (var kvp in decoratorTickCounts.OrderByDescending(x => x.Value))
            {
                WriteLog($"   {kvp.Key}: {kvp.Value} ticks");
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
            LogHeaderInternal("🏁 EXECUTION FLOW LOGGER CLOSED");
            base.Close();
        }

        /// <summary>
        /// Get the log file path
        /// </summary>
        /// <returns>Path to the log file</returns>
        public new static string GetLogFilePath()
        {
            return Instance.GetLogFilePathInternal();
        }

        private string GetLogFilePathInternal()
        {
            return base.GetLogFilePath();
        }

        /// <summary>
        /// Get the current tick counter
        /// </summary>
        /// <returns>Current tick count</returns>
        public static int GetTickCount()
        {
            return Instance.GetTickCountInternal();
        }

        private int GetTickCountInternal()
        {
            return tickCounter;
        }

        /// <summary>
        /// Clear the log file
        /// </summary>
        public static void ClearLog()
        {
            Instance.ClearLogInternal();
        }

        private void ClearLogInternal()
        {
            base.Clear();
            
            tickCounter = 0;
            nodeTickCounts.Clear();
            serviceTickCounts.Clear();
            decoratorTickCounts.Clear();
            sessionStartTime = DateTime.Now;
        }

        #region Private Methods

        private void TrackNodeTick(string nodeName)
        {
            if (!nodeTickCounts.ContainsKey(nodeName))
                nodeTickCounts[nodeName] = 0;
            nodeTickCounts[nodeName]++;
        }

        private void TrackServiceTick(string serviceName)
        {
            if (!serviceTickCounts.ContainsKey(serviceName))
                serviceTickCounts[serviceName] = 0;
            serviceTickCounts[serviceName]++;
        }

        private void TrackDecoratorTick(string decoratorName)
        {
            if (!decoratorTickCounts.ContainsKey(decoratorName))
                decoratorTickCounts[decoratorName] = 0;
            decoratorTickCounts[decoratorName]++;
        }

        #endregion
    }
}
