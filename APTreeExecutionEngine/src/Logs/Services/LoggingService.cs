using System;
using System.Collections.Generic;
using System.Linq;
using BehaviorTreeMainProject.Log;

namespace BehaviorTreeMainProject.Log.Services
{
    public class LoggingService : BaseLogger
    {
        private static LoggingService? instance;
        private static readonly object lockObject = new object();
        
        // Node tracking statistics
        private Dictionary<string, NodeExecutionInfo> nodeExecutionStats = new Dictionary<string, NodeExecutionInfo>();
        private Dictionary<string, PlanningServiceInfo> planningServiceStats = new Dictionary<string, PlanningServiceInfo>();
        private int totalNodes = 0;
        private int flowNodeCount = 0;
        private int actionNodeCount = 0;

        public static LoggingService Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (lockObject)
                    {
                        if (instance == null)
                        {
                            instance = new LoggingService();
                        }
                    }
                }
                return instance;
            }
        }

        private LoggingService() { }

        public new static void Initialize(string serviceName, bool enableConsole = true, bool enableFile = true)
        {
            var logger = Instance;
            logger.InitializeInternal(serviceName, enableConsole, enableFile);
        }

        private void InitializeInternal(string serviceName, bool enableConsole, bool enableFile)
        {
            base.Initialize(serviceName, enableConsole, enableFile);
        }

        public static void LogDebug(string message)
        {
            Instance.WriteLog("🔍 DEBUG", message, ConsoleColor.Gray);
        }

        public static void LogInfo(string message)
        {
            Instance.WriteLog("ℹ️ INFO", message, ConsoleColor.White);
        }

        public static void LogSuccess(string message)
        {
            Instance.WriteLog("✅ SUCCESS", message, ConsoleColor.Green);
        }

        public static void LogWarning(string message)
        {
            Instance.WriteLog("⚠️ WARNING", message, ConsoleColor.Yellow);
        }

        public static void LogError(string message)
        {
            Instance.WriteLog("❌ ERROR", message, ConsoleColor.Red);
        }

        public static void LogSection(string message)
        {
            Instance.WriteSectionHeader(message);
        }

        public static void LogSubsection(string message)
        {
            Instance.WriteSubsectionHeader(message);
        }



        // Node tracking methods
        public static void TrackNodeStart(string nodeName, string nodeType, DateTime startTime)
        {
            Instance.TrackNodeStartInternal(nodeName, nodeType, startTime);
        }

        private void TrackNodeStartInternal(string nodeName, string nodeType, DateTime startTime)
        {
            if (!nodeExecutionStats.ContainsKey(nodeName))
            {
                nodeExecutionStats[nodeName] = new NodeExecutionInfo(nodeName, nodeType, startTime);

                totalNodes++;
                if (nodeType.Contains("FlowNode"))
                {
                    flowNodeCount++;
                }
                else if (nodeType.Contains("GenericBTAction"))
                {
                    actionNodeCount++;
                }
            }
        }

        public static void TrackNodeCompletion(string nodeName, DateTime endTime, bool success)
        {
            Instance.TrackNodeCompletionInternal(nodeName, endTime, success);
        }

        private void TrackNodeCompletionInternal(string nodeName, DateTime endTime, bool success)
        {
            if (nodeExecutionStats.ContainsKey(nodeName))
            {
                var nodeInfo = nodeExecutionStats[nodeName];
                nodeInfo.Complete(endTime, success);
            }
        }

        public static void TrackPlanningService(string serviceName, string plannerType, DateTime startTime, bool success, int actionsGenerated, DateTime? endTime = null)
        {
            Instance.TrackPlanningServiceInternal(serviceName, plannerType, startTime, success, actionsGenerated, endTime);
        }

        private void TrackPlanningServiceInternal(string serviceName, string plannerType, DateTime startTime, bool success, int actionsGenerated, DateTime? endTime)
        {
            var planningInfo = new PlanningServiceInfo(serviceName, plannerType, startTime, actionsGenerated);

            if (endTime.HasValue)
            {
                planningInfo.Complete(endTime.Value, success);
            }
            else
            {
                planningInfo.Success = success;
            }

            planningServiceStats[serviceName] = planningInfo;
        }

        public static void GenerateSummaryTable()
        {
            Instance.GenerateSummaryTableInternal();
        }

        private void GenerateSummaryTableInternal()
        {
            WriteSectionHeader("📊 EXECUTION SUMMARY REPORT");
            
            // Node Statistics
            WriteSubsectionHeader("NODE STATISTICS");
            WriteLog($"Total Nodes: {totalNodes}");
            WriteLog($"Flow Nodes: {flowNodeCount}");
            WriteLog($"Action Nodes: {actionNodeCount}");
            WriteLog("");

            // Node Execution Details
            WriteSubsectionHeader("NODE EXECUTION DETAILS");
            WriteTableHeader("Node Name", "Type", "Duration", "Status");
            WriteTableSeparator(4);
            
            foreach (var nodeInfo in nodeExecutionStats.Values)
            {
                string duration = nodeInfo.Completed ? 
                    LogFormatter.FormatDuration(nodeInfo.CompletionTime) : "N/A";
                string status = nodeInfo.Completed ? 
                    (nodeInfo.Success ? "✅ Success" : "❌ Failed") : "⏳ Running";
                
                WriteTableRow(nodeInfo.NodeName, nodeInfo.NodeType, duration, status);
            }
            WriteLog("");

            // Planning Service Details
            WriteSubsectionHeader("PLANNING SERVICE DETAILS");
            WriteTableHeader("Service Name", "Planner Type", "Duration", "Success", "Actions Generated");
            WriteTableSeparator(5);
            
            foreach (var planningInfo in planningServiceStats.Values)
            {
                string duration = planningInfo.Completed ? 
                    LogFormatter.FormatDuration(planningInfo.PlanningTime) : "N/A";
                string success = planningInfo.Success ? "✅ Yes" : "❌ No";
                
                WriteTableRow(planningInfo.ServiceName, planningInfo.PlannerType, duration, success, planningInfo.ActionsGenerated.ToString());
            }
            WriteLog("");

            // Summary Statistics
            WriteSubsectionHeader("SUMMARY STATISTICS");
            
            int completedNodes = nodeExecutionStats.Values.Count(n => n.Completed);
            int successfulNodes = nodeExecutionStats.Values.Count(n => n.Completed && n.Success);
            int failedNodes = nodeExecutionStats.Values.Count(n => n.Completed && !n.Success);
            
            var totalExecutionTime = nodeExecutionStats.Values
                .Where(n => n.Completed)
                .Sum(n => n.CompletionTime.TotalMilliseconds);
            
            var totalPlanningTime = planningServiceStats.Values
                .Where(p => p.Completed)
                .Sum(p => p.PlanningTime.TotalMilliseconds);
            
            int totalActionsGenerated = planningServiceStats.Values.Sum(p => p.ActionsGenerated);
            int successfulPlanners = planningServiceStats.Values.Count(p => p.Success);
            int totalPlanners = planningServiceStats.Count;

            WriteLog($"Node Completion Rate: {LogFormatter.FormatCounter(completedNodes, totalNodes)}");
            WriteLog($"Node Success Rate: {LogFormatter.FormatCounter(successfulNodes, completedNodes)}");
            WriteLog($"Total Execution Time: {LogFormatter.FormatDuration(TimeSpan.FromMilliseconds(totalExecutionTime))}");
            WriteLog($"Total Planning Time: {LogFormatter.FormatDuration(TimeSpan.FromMilliseconds(totalPlanningTime))}");
            WriteLog($"Total Actions Generated: {totalActionsGenerated}");
            WriteLog($"Planning Success Rate: {LogFormatter.FormatCounter(successfulPlanners, totalPlanners)}");
            
            WriteSectionHeader("END OF SUMMARY REPORT");
        }

        public new static void Close()
        {
            Instance.CloseInternal();
        }

        private void CloseInternal()
        {
            base.Close();
        }
    }
}
