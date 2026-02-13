using System;
using System.Collections.Generic;
using System.Linq;
using BehaviorTreeMainProject.Log;

namespace BehaviorTreeMainProject.Log.Services
{
    public class ExecutionSummaryLogger : BaseLogger
    {
        private static ExecutionSummaryLogger instance;
        private static readonly object lockObject = new object();
        
        private DateTime treeStartTime;
        private DateTime treeEndTime;
        private TimeSpan totalExecutionTime;
        
        private Dictionary<string, PlanningServiceMetrics> planningServiceMetrics = new Dictionary<string, PlanningServiceMetrics>();
        private Dictionary<string, FlowNodeMetrics> flowNodeMetrics = new Dictionary<string, FlowNodeMetrics>();
        private Dictionary<string, int> nodeExecutionCounts = new Dictionary<string, int>();
        private Dictionary<string, int> nodeFailureCounts = new Dictionary<string, int>();
        private Dictionary<string, int> nodeCreationCounts = new Dictionary<string, int>();
        private Dictionary<string, int> nodeFinalCounts = new Dictionary<string, int>();
        private Dictionary<string, long> memorySnapshots = new Dictionary<string, long>();
        
        // New tracking fields for service and decorator failures
        private Dictionary<string, int> serviceFailureCounts = new Dictionary<string, int>();
        private Dictionary<string, int> decoratorFailureCounts = new Dictionary<string, int>();
        private Dictionary<string, int> decoratorBlockCounts = new Dictionary<string, int>();
        
        private DateTime? planningPhaseStart;
        private DateTime? planningPhaseEnd;
        private DateTime? executionPhaseStart;
        private DateTime? executionPhaseEnd;

        public static ExecutionSummaryLogger Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (lockObject)
                    {
                        if (instance == null)
                        {
                            instance = new ExecutionSummaryLogger();
                        }
                    }
                }
                return instance;
            }
        }

        private ExecutionSummaryLogger()
        {
            // Initialize tracking
            Initialize("execution_summary");
        }

        // Public static methods for external calls
        public static void StartTreeExecution()
        {
            Instance.StartTreeExecutionInternal();
        }

        public static void EndTreeExecution()
        {
            Instance.EndTreeExecutionInternal();
        }

        public static void TrackPlanningService(string serviceName, string plannerType, DateTime startTime, bool success, int actionsGenerated, DateTime? plannerEndTime = null, DateTime? serviceEndTime = null)
        {
            Instance.TrackPlanningServiceInternal(serviceName, plannerType, startTime, success, actionsGenerated, plannerEndTime, serviceEndTime);
        }

        public static void TrackFlowNode(string nodeName, string nodeType, bool success)
        {
            Instance.TrackFlowNodeInternal(nodeName, nodeType, success);
        }

        public static void TrackNodeExecution(string nodeName, string nodeType, bool success)
        {
            Instance.TrackNodeExecutionInternal(nodeName, nodeType, success);
        }

        public static void TrackNodeCreation(string nodeType)
        {
            Instance.TrackNodeCreationInternal(nodeType);
        }

        public static void TrackNodeFinalCount(string nodeType, int count)
        {
            Instance.TrackNodeFinalCountInternal(nodeType, count);
        }

        public static void TrackMemoryUsage(string checkpoint, long memoryBytes)
        {
            Instance.TrackMemoryUsageInternal(checkpoint, memoryBytes);
        }

        public static void TrackPlanningPhaseTransition(bool isPlanningPhase)
        {
            Instance.TrackPlanningPhaseTransitionInternal(isPlanningPhase);
        }

        // New tracking methods for service and decorator failures
        public static void TrackServiceFailure(string serviceName)
        {
            Instance.TrackServiceFailureInternal(serviceName);
        }

        public static void TrackDecoratorFailure(string decoratorName)
        {
            Instance.TrackDecoratorFailureInternal(decoratorName);
        }

        public static void TrackDecoratorBlock(string decoratorName)
        {
            Instance.TrackDecoratorBlockInternal(decoratorName);
        }

        public static void GenerateSummary()
        {
            Instance.GenerateSummaryInternal();
        }

        public new static void Close()
        {
            Instance.CloseInternal();
        }

        // Private implementation methods
        private void StartTreeExecutionInternal()
        {
            treeStartTime = DateTime.Now;
            planningPhaseStart = DateTime.Now;
            WriteLog("Tree execution started");
        }

        private void EndTreeExecutionInternal()
        {
            treeEndTime = DateTime.Now;
            totalExecutionTime = treeEndTime - treeStartTime;
            WriteLog("Tree execution ended");
        }

        private void TrackPlanningServiceInternal(string serviceName, string plannerType, DateTime startTime, bool success, int actionsGenerated, DateTime? plannerEndTime = null, DateTime? serviceEndTime = null)
        {
            if (!planningServiceMetrics.ContainsKey(serviceName))
            {
                planningServiceMetrics[serviceName] = new PlanningServiceMetrics
                {
                    ServiceName = serviceName,
                    PlannerType = plannerType,
                    TotalCalls = 0,
                    SuccessfulCalls = 0,
                    TotalPlannerTime = TimeSpan.Zero,
                    TotalServiceTime = TimeSpan.Zero,
                    TotalActionsGenerated = 0
                };
            }

            var metrics = planningServiceMetrics[serviceName];
            metrics.TotalCalls++;
            
            if (plannerEndTime.HasValue)
            {
                var plannerDuration = plannerEndTime.Value - startTime;
                metrics.TotalPlannerTime += plannerDuration;
            }
            
            if (serviceEndTime.HasValue)
            {
                var serviceDuration = serviceEndTime.Value - startTime;
                metrics.TotalServiceTime += serviceDuration;
                metrics.TotalActionsGenerated += actionsGenerated;
                
                if (success)
                {
                    metrics.SuccessfulCalls++;
                }
            }
        }

        private void TrackFlowNodeInternal(string nodeName, string nodeType, bool success)
        {
            if (!flowNodeMetrics.ContainsKey(nodeName))
            {
                flowNodeMetrics[nodeName] = new FlowNodeMetrics
                {
                    NodeName = nodeName,
                    NodeType = nodeType,
                    TotalCalls = 0,
                    SuccessfulCalls = 0,
                    TotalPlanningTime = TimeSpan.Zero
                };
            }

            var metrics = flowNodeMetrics[nodeName];
            metrics.TotalCalls++;
            if (success)
            {
                metrics.SuccessfulCalls++;
            }
        }

        private void TrackNodeExecutionInternal(string nodeName, string nodeType, bool success)
        {
            var key = $"{nodeType}:{nodeName}";
            
            if (!nodeExecutionCounts.ContainsKey(key))
            {
                nodeExecutionCounts[key] = 0;
            }
            nodeExecutionCounts[key]++;

            if (!success)
            {
                if (!nodeFailureCounts.ContainsKey(key))
                {
                    nodeFailureCounts[key] = 0;
                }
                nodeFailureCounts[key]++;
            }
        }

        private void TrackNodeCreationInternal(string nodeType)
        {
            if (!nodeCreationCounts.ContainsKey(nodeType))
            {
                nodeCreationCounts[nodeType] = 0;
            }
            nodeCreationCounts[nodeType]++;
        }

        private void TrackNodeFinalCountInternal(string nodeType, int count)
        {
            nodeFinalCounts[nodeType] = count;
        }

        private void TrackMemoryUsageInternal(string checkpoint, long memoryBytes)
        {
            memorySnapshots[checkpoint] = memoryBytes;
        }

        private void TrackPlanningPhaseTransitionInternal(bool isPlanningPhase)
        {
            if (isPlanningPhase)
            {
                planningPhaseStart = DateTime.Now;
                if (executionPhaseEnd.HasValue)
                {
                    var executionDuration = planningPhaseStart.Value - executionPhaseEnd.Value;
                    WriteLog($"Planning phase started after {executionDuration.TotalMilliseconds:F2}ms execution");
                }
            }
            else
            {
                planningPhaseEnd = DateTime.Now;
                executionPhaseStart = DateTime.Now;
                if (planningPhaseStart.HasValue)
                {
                    var planningDuration = planningPhaseEnd.Value - planningPhaseStart.Value;
                    WriteLog($"Planning phase completed in {planningDuration.TotalMilliseconds:F2}ms");
                }
            }
        }

        // New internal tracking methods for service and decorator failures
        private void TrackServiceFailureInternal(string serviceName)
        {
            if (!serviceFailureCounts.ContainsKey(serviceName))
            {
                serviceFailureCounts[serviceName] = 0;
            }
            serviceFailureCounts[serviceName]++;
        }

        private void TrackDecoratorFailureInternal(string decoratorName)
        {
            if (!decoratorFailureCounts.ContainsKey(decoratorName))
            {
                decoratorFailureCounts[decoratorName] = 0;
            }
            decoratorFailureCounts[decoratorName]++;
        }

        private void TrackDecoratorBlockInternal(string decoratorName)
        {
            if (!decoratorBlockCounts.ContainsKey(decoratorName))
            {
                decoratorBlockCounts[decoratorName] = 0;
            }
            decoratorBlockCounts[decoratorName]++;
        }

        private void GenerateSummaryInternal()
        {
            WriteLog("=== EXECUTION SUMMARY REPORT ===");
            WriteLog("");

            // Total execution time
            WriteLog($"Total Tree Execution Time: {totalExecutionTime.TotalMilliseconds:F2}ms");
            WriteLog("");

            // Planning service metrics
            WriteLog("=== PLANNING SERVICE METRICS ===");
            foreach (var kvp in planningServiceMetrics)
            {
                var metrics = kvp.Value;
                var successRate = metrics.TotalCalls > 0 ? (double)metrics.SuccessfulCalls / metrics.TotalCalls * 100 : 0;
                WriteLog($"Service: {metrics.ServiceName}");
                WriteLog($"  Planner Type: {metrics.PlannerType}");
                WriteLog($"  Total Calls: {metrics.TotalCalls}");
                WriteLog($"  Successful Calls: {metrics.SuccessfulCalls} ({successRate:F1}%)");
                WriteLog($"  Total Planner Time: {metrics.TotalPlannerTime.TotalMilliseconds:F2}ms");
                WriteLog($"  Total Service Time: {metrics.TotalServiceTime.TotalMilliseconds:F2}ms");
                WriteLog($"  Average Planner Time: {(metrics.TotalCalls > 0 ? metrics.TotalPlannerTime.TotalMilliseconds / metrics.TotalCalls : 0):F2}ms");
                WriteLog($"  Average Service Time: {(metrics.TotalCalls > 0 ? metrics.TotalServiceTime.TotalMilliseconds / metrics.TotalCalls : 0):F2}ms");
                WriteLog($"  Total Actions Generated: {metrics.TotalActionsGenerated}");
                WriteLog("");
            }

            // Flow node metrics
            WriteLog("=== FLOW NODE METRICS ===");
            foreach (var kvp in flowNodeMetrics)
            {
                var metrics = kvp.Value;
                var successRate = metrics.TotalCalls > 0 ? (double)metrics.SuccessfulCalls / metrics.TotalCalls * 100 : 0;
                WriteLog($"Flow Node: {metrics.NodeName} ({metrics.NodeType})");
                WriteLog($"  Total Calls: {metrics.TotalCalls}");
                WriteLog($"  Successful Calls: {metrics.SuccessfulCalls} ({successRate:F1}%)");
                WriteLog($"  Total Planning Time: {metrics.TotalPlanningTime.TotalMilliseconds:F2}ms");
                WriteLog("");
            }

            // Node execution counts
            WriteLog("=== NODE EXECUTION COUNTS ===");
            var nodeTypeGroups = nodeExecutionCounts
                .GroupBy(kvp => kvp.Key.Split(':')[0])
                .OrderBy(g => g.Key);

            foreach (var group in nodeTypeGroups)
            {
                WriteLog($"Node Type: {group.Key}");
                foreach (var kvp in group.OrderBy(x => x.Key))
                {
                    var nodeName = kvp.Key.Split(':')[1];
                    var count = kvp.Value;
                    var failureCount = nodeFailureCounts.ContainsKey(kvp.Key) ? nodeFailureCounts[kvp.Key] : 0;
                    var successCount = count - failureCount;
                    WriteLog($"  {nodeName}: {count} calls ({successCount} success, {failureCount} failures)");
                }
                WriteLog("");
            }

            // Service failure counts
            WriteLog("=== SERVICE FAILURE COUNTS ===");
            foreach (var kvp in serviceFailureCounts)
            {
                var serviceName = kvp.Key;
                var failureCount = kvp.Value;
                var totalTicks = planningServiceMetrics.ContainsKey(serviceName) ? 
                    planningServiceMetrics[serviceName].TotalCalls : 0;
                var failureRate = totalTicks > 0 ? (double)failureCount / totalTicks * 100 : 0;
                WriteLog($"{serviceName}: {totalTicks} ticks, {failureCount} failures ({failureRate:F1}% failure rate)");
            }
            WriteLog("");

            // Decorator failure and block counts
            WriteLog("=== DECORATOR FAILURE COUNTS ===");
            foreach (var kvp in decoratorFailureCounts)
            {
                var decoratorName = kvp.Key;
                var failureCount = kvp.Value;
                var blockCount = decoratorBlockCounts.ContainsKey(decoratorName) ? 
                    decoratorBlockCounts[decoratorName] : 0;
                var totalTicks = failureCount + blockCount;
                var failureRate = totalTicks > 0 ? (double)failureCount / totalTicks * 100 : 0;
                var blockRate = totalTicks > 0 ? (double)blockCount / totalTicks * 100 : 0;
                WriteLog($"{decoratorName}: {totalTicks} ticks, {failureCount} failures ({failureRate:F1}%), {blockCount} blocks ({blockRate:F1}%)");
            }
            WriteLog("");

            // Node creation vs final counts
            WriteLog("=== NODE CREATION VS FINAL COUNTS ===");
            foreach (var kvp in nodeCreationCounts)
            {
                var nodeType = kvp.Key;
                var created = kvp.Value;
                var final = nodeFinalCounts.ContainsKey(nodeType) ? nodeFinalCounts[nodeType] : 0;
                WriteLog($"{nodeType}: Created {created}, Final Count {final}");
            }
            WriteLog("");

            // Memory usage
            WriteLog("=== MEMORY USAGE ===");
            foreach (var kvp in memorySnapshots)
            {
                var checkpoint = kvp.Key;
                var memoryMB = kvp.Value / (1024.0 * 1024.0);
                WriteLog($"{checkpoint}: {memoryMB:F2} MB");
            }
            WriteLog("");

            // Phase transition timing
            WriteLog("=== PHASE TRANSITION TIMING ===");
            if (planningPhaseStart.HasValue && planningPhaseEnd.HasValue)
            {
                var planningDuration = planningPhaseEnd.Value - planningPhaseStart.Value;
                WriteLog($"Planning Phase Duration: {planningDuration.TotalMilliseconds:F2}ms");
            }
            if (executionPhaseStart.HasValue && treeEndTime != DateTime.MinValue)
            {
                var executionDuration = treeEndTime - executionPhaseStart.Value;
                WriteLog($"Execution Phase Duration: {executionDuration.TotalMilliseconds:F2}ms");
            }
            WriteLog("");

            WriteLog("=== END OF EXECUTION SUMMARY ===");
        }

        private void CloseInternal()
        {
            // Generate final summary before closing
            GenerateSummaryInternal();
            base.Close();
        }

        // Helper classes for tracking metrics
        private class PlanningServiceMetrics
        {
            public string ServiceName { get; set; }
            public string PlannerType { get; set; }
            public int TotalCalls { get; set; }
            public int SuccessfulCalls { get; set; }
            public TimeSpan TotalPlannerTime { get; set; } // External planner time only
            public TimeSpan TotalServiceTime { get; set; } // Total service time including NodeGraph generation
            public int TotalActionsGenerated { get; set; }
        }

        private class FlowNodeMetrics
        {
            public string NodeName { get; set; }
            public string NodeType { get; set; }
            public int TotalCalls { get; set; }
            public int SuccessfulCalls { get; set; }
            public TimeSpan TotalPlanningTime { get; set; }
        }
    }
}
