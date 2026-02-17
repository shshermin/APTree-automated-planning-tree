using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BehaviorTreeMainProject.Log;

namespace BehaviorTreeMainProject.Log.Services
{
    /// <summary>
    /// Logger for tracking behavior tree component statistics and generating CSV summaries
    /// Simplified to track only: tick count, success count, and failure count for flow nodes
    /// </summary>
    public class BehaviorTreeComponentLogger : BaseLogger
    {
        private static BehaviorTreeComponentLogger? instance;
        private static readonly object lockObject = new object();
        
        // Simplified flow node tracking - only tick, success, and failure counts
        private readonly Dictionary<string, FlowNodeStats> flowNodeStats = new Dictionary<string, FlowNodeStats>();
        
        // Other component tracking (kept for compatibility)
        private readonly Dictionary<string, ComponentStats> componentStats = new Dictionary<string, ComponentStats>();
        private readonly Dictionary<string, int> failureCounts = new Dictionary<string, int>();
        private readonly Dictionary<string, int> callCounts = new Dictionary<string, int>();
        private readonly Dictionary<string, int> actionDeletions = new Dictionary<string, int>();
        private readonly Dictionary<string, int> nodeGraphResets = new Dictionary<string, int>();
        private int totalSubtreesCleared = 0;
        private int finalActionsRemaining = 0;

        public static BehaviorTreeComponentLogger Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (lockObject)
                    {
                        if (instance == null)
                        {
                            instance = new BehaviorTreeComponentLogger();
                        }
                    }
                }
                return instance;
            }
        }

        private BehaviorTreeComponentLogger()
        {
            base.Initialize("BehaviorTreeComponent", true, true);
            
            // Initialize component tracking
            InitializeComponentTracking();
            
            WriteSectionHeader("🌳 BEHAVIOR TREE COMPONENT LOGGER INITIALIZED");
            WriteLog("Ready to track behavior tree component statistics");
        }

        private void InitializeComponentTracking()
        {
            // Initialize simplified flow node tracking
            flowNodeStats["BTFlowNode_Dynamic"] = new FlowNodeStats();
            flowNodeStats["BTFlowNode_Composite"] = new FlowNodeStats();
            
            // Initialize decorator tracking
            flowNodeStats["BTDecorator_PlanningComplete"] = new FlowNodeStats();
            flowNodeStats["BTDecorator_DynamicPlanningComplete"] = new FlowNodeStats();
            flowNodeStats["BTDecorator_LowestCostExecution"] = new FlowNodeStats();
            
            // Initialize service tracking - use actual concrete service class names
            flowNodeStats["PDDLPlanningService"] = new FlowNodeStats();
            flowNodeStats["CallGOAPPlanner"] = new FlowNodeStats();
            flowNodeStats["CallSCPlanner"] = new FlowNodeStats();
            flowNodeStats["BTService_PlanningPhaseManager"] = new FlowNodeStats();
            flowNodeStats["SubtreeInjectionService"] = new FlowNodeStats();
            
            // Initialize action tracking
            flowNodeStats["GenericBTAction"] = new FlowNodeStats();
            
            // Keep other components for compatibility
            var components = new[] { 
                "GenericBTAction", 
                "SubtreesInjected", 
                "DecoratorBTDecorator_DynamicPlanningComplete", 
                "DecoratorBTDecorator_LowestCostExecution", 
                "DecoratorBTDecorator_PlanningComplete", 
                "ServicePDDLPlanningService", 
                "ServiceBTService_PlanningPhaseManager", 
                "ServiceSubtreeInjectionService",
                "PDDLPlanningService"
            };
            
            foreach (var component in components)
            {
                componentStats[component] = new ComponentStats();
                failureCounts[component] = 0;
                callCounts[component] = 0;
            }
        }

        /// <summary>
        /// Track flow node tick - called every time Tick() is called on a flow node
        /// </summary>
        public static void TrackFlowNodeTick(string flowNodeType)
        {
            Instance.TrackFlowNodeTickInternal(flowNodeType);
        }

        /// <summary>
        /// Track flow node success - called when Tick() returns success
        /// </summary>
        public static void TrackFlowNodeSuccess(string flowNodeType)
        {
            Instance.TrackFlowNodeSuccessInternal(flowNodeType);
        }

        /// <summary>
        /// Track flow node failure - called when Tick() returns failure
        /// </summary>
        public static void TrackFlowNodeFailure(string flowNodeType)
        {
            Instance.TrackFlowNodeFailureInternal(flowNodeType);
        }

        /// <summary>
        /// Track decorator tick - called every time Tick() is called on a decorator
        /// </summary>
        public static void TrackDecoratorTick(string decoratorType)
        {
            Instance.TrackDecoratorTickInternal(decoratorType);
        }

        /// <summary>
        /// Track decorator success - called when Tick() returns success
        /// </summary>
        public static void TrackDecoratorSuccess(string decoratorType)
        {
            Instance.TrackDecoratorSuccessInternal(decoratorType);
        }

        /// <summary>
        /// Track decorator failure - called when Tick() returns failure
        /// </summary>
        public static void TrackDecoratorFailure(string decoratorType)
        {
            Instance.TrackDecoratorFailureInternal(decoratorType);
        }

        /// <summary>
        /// Track service tick - called every time Tick() is called on a service
        /// </summary>
        public static void TrackServiceTick(string serviceType)
        {
            Instance.TrackServiceTickInternal(serviceType);
        }

        /// <summary>
        /// Track service success - called when Tick() returns success
        /// </summary>
        public static void TrackServiceSuccess(string serviceType)
        {
            Instance.TrackServiceSuccessInternal(serviceType);
        }

        /// <summary>
        /// Track service failure - called when Tick() returns failure
        /// </summary>
        public static void TrackServiceFailure(string serviceType)
        {
            Instance.TrackServiceFailureInternal(serviceType);
        }

        /// <summary>
        /// Track flow node addition - called when a new flow node is added
        /// </summary>
        public static void TrackFlowNodeAddition(string flowNodeType)
        {
            Instance.TrackFlowNodeAdditionInternal(flowNodeType);
        }

        /// <summary>
        /// Track flow node initialization - called when a new flow node is created/initialized
        /// </summary>
        public static void TrackFlowNodeInitialization(string flowNodeType)
        {
            Instance.TrackFlowNodeInitializationInternal(flowNodeType);
        }

        /// <summary>
        /// Track service addition - called when a new service is added
        /// </summary>
        public static void TrackServiceAddition(string serviceType)
        {
            Instance.TrackServiceAdditionInternal(serviceType);
        }

        /// <summary>
        /// Track decorator addition - called when a new decorator is added
        /// </summary>
        public static void TrackDecoratorAddition(string decoratorType)
        {
            Instance.TrackDecoratorAdditionInternal(decoratorType);
        }

        /// <summary>
        /// Track action tick - called every time Tick() is called on an action
        /// </summary>
        public static void TrackActionTick(string actionType)
        {
            Instance.TrackActionTickInternal(actionType);
        }

        /// <summary>
        /// Track action success - called when Tick() returns success
        /// </summary>
        public static void TrackActionSuccess(string actionType)
        {
            Instance.TrackActionSuccessInternal(actionType);
        }

        /// <summary>
        /// Track action failure - called when Tick() returns failure
        /// </summary>
        public static void TrackActionFailure(string actionType)
        {
            Instance.TrackActionFailureInternal(actionType);
        }

        /// <summary>
        /// Track action addition - called when a new action is added
        /// </summary>
        public static void TrackActionAddition(string actionType)
        {
            Instance.TrackActionAdditionInternal(actionType);
        }

        /// <summary>
        /// Track a component execution (kept for compatibility)
        /// </summary>
        public static void TrackComponentExecution(string componentType, string instanceName, bool success, int childCount = 0)
        {
            Instance.TrackComponentExecutionInternal(componentType, instanceName, success, childCount);
        }

        private void TrackFlowNodeTickInternal(string flowNodeType)
        {
            lock (lockObject)
            {
                if (flowNodeStats.ContainsKey(flowNodeType))
                {
                    flowNodeStats[flowNodeType].TickCount++;
                    WriteLog($"📊 Flow Node Tick: {flowNodeType} - Total ticks: {flowNodeStats[flowNodeType].TickCount}");
                }
            }
        }

        private void TrackFlowNodeSuccessInternal(string flowNodeType)
        {
            lock (lockObject)
            {
                if (flowNodeStats.ContainsKey(flowNodeType))
                {
                    flowNodeStats[flowNodeType].SuccessCount++;
                    WriteLog($"✅ Flow Node Success: {flowNodeType} - Total successes: {flowNodeStats[flowNodeType].SuccessCount}");
                }
            }
        }

        private void TrackFlowNodeFailureInternal(string flowNodeType)
        {
            lock (lockObject)
            {
                if (flowNodeStats.ContainsKey(flowNodeType))
                {
                    flowNodeStats[flowNodeType].FailureCount++;
                    WriteLog($"❌ Flow Node Failure: {flowNodeType} - Total failures: {flowNodeStats[flowNodeType].FailureCount}");
                }
            }
        }

        private void TrackDecoratorTickInternal(string decoratorType)
        {
            lock (lockObject)
            {
                if (flowNodeStats.ContainsKey(decoratorType))
                {
                    flowNodeStats[decoratorType].TickCount++;
                    WriteLog($"📊 Decorator Tick: {decoratorType} - Total ticks: {flowNodeStats[decoratorType].TickCount}");
                }
            }
        }

        private void TrackDecoratorSuccessInternal(string decoratorType)
        {
            lock (lockObject)
            {
                if (flowNodeStats.ContainsKey(decoratorType))
                {
                    flowNodeStats[decoratorType].SuccessCount++;
                    WriteLog($"✅ Decorator Success: {decoratorType} - Total successes: {flowNodeStats[decoratorType].SuccessCount}");
                }
            }
        }

        private void TrackDecoratorFailureInternal(string decoratorType)
        {
            lock (lockObject)
            {
                if (flowNodeStats.ContainsKey(decoratorType))
                {
                    flowNodeStats[decoratorType].FailureCount++;
                    WriteLog($"❌ Decorator Failure: {decoratorType} - Total failures: {flowNodeStats[decoratorType].FailureCount}");
                }
            }
        }

        private void TrackServiceTickInternal(string serviceType)
        {
            lock (lockObject)
            {
                if (flowNodeStats.ContainsKey(serviceType))
                {
                    flowNodeStats[serviceType].TickCount++;
                    WriteLog($"📊 Service Tick: {serviceType} - Total ticks: {flowNodeStats[serviceType].TickCount}");
                }
            }
        }

        private void TrackServiceSuccessInternal(string serviceType)
        {
            lock (lockObject)
            {
                if (flowNodeStats.ContainsKey(serviceType))
                {
                    flowNodeStats[serviceType].SuccessCount++;
                    WriteLog($"✅ Service Success: {serviceType} - Total successes: {flowNodeStats[serviceType].SuccessCount}");
                }
            }
        }

        private void TrackServiceFailureInternal(string serviceType)
        {
            lock (lockObject)
            {
                if (flowNodeStats.ContainsKey(serviceType))
                {
                    flowNodeStats[serviceType].FailureCount++;
                    WriteLog($"❌ Service Failure: {serviceType} - Total failures: {flowNodeStats[serviceType].FailureCount}");
                }
            }
        }

        private void TrackFlowNodeAdditionInternal(string flowNodeType)
        {
            lock (lockObject)
            {
                if (flowNodeStats.ContainsKey(flowNodeType))
                {
                    flowNodeStats[flowNodeType].AdditionCount++;
                    WriteLog($"➕ Flow Node Addition: {flowNodeType} - Total additions: {flowNodeStats[flowNodeType].AdditionCount}");
                }
            }
        }

        private void TrackFlowNodeInitializationInternal(string flowNodeType)
        {
            lock (lockObject)
            {
                if (flowNodeStats.ContainsKey(flowNodeType))
                {
                    flowNodeStats[flowNodeType].AdditionCount++;
                    WriteLog($"🏗️ Flow Node Initialization: {flowNodeType} - Total initializations: {flowNodeStats[flowNodeType].AdditionCount}");
                }
            }
        }

        private void TrackServiceAdditionInternal(string serviceType)
        {
            lock (lockObject)
            {
                if (flowNodeStats.ContainsKey(serviceType))
                {
                    flowNodeStats[serviceType].AdditionCount++;
                    WriteLog($"➕ Service Addition: {serviceType} - Total additions: {flowNodeStats[serviceType].AdditionCount}");
                }
            }
        }

        private void TrackDecoratorAdditionInternal(string decoratorType)
        {
            lock (lockObject)
            {
                if (flowNodeStats.ContainsKey(decoratorType))
                {
                    flowNodeStats[decoratorType].AdditionCount++;
                    WriteLog($"➕ Decorator Addition: {decoratorType} - Total additions: {flowNodeStats[decoratorType].AdditionCount}");
                }
            }
        }

        private void TrackActionTickInternal(string actionType)
        {
            lock (lockObject)
            {
                if (flowNodeStats.ContainsKey(actionType))
                {
                    flowNodeStats[actionType].TickCount++;
                    WriteLog($"📊 Action Tick: {actionType} - Total ticks: {flowNodeStats[actionType].TickCount}");
                }
            }
        }

        private void TrackActionSuccessInternal(string actionType)
        {
            lock (lockObject)
            {
                if (flowNodeStats.ContainsKey(actionType))
                {
                    flowNodeStats[actionType].SuccessCount++;
                    WriteLog($"✅ Action Success: {actionType} - Total successes: {flowNodeStats[actionType].SuccessCount}");
                }
            }
        }

        private void TrackActionFailureInternal(string actionType)
        {
            lock (lockObject)
            {
                if (flowNodeStats.ContainsKey(actionType))
                {
                    flowNodeStats[actionType].FailureCount++;
                    WriteLog($"❌ Action Failure: {actionType} - Total failures: {flowNodeStats[actionType].FailureCount}");
                }
            }
        }

        private void TrackActionAdditionInternal(string actionType)
        {
            lock (lockObject)
            {
                if (flowNodeStats.ContainsKey(actionType))
                {
                    flowNodeStats[actionType].AdditionCount++;
                    WriteLog($"➕ Action Addition: {actionType} - Total additions: {flowNodeStats[actionType].AdditionCount}");
                }
            }
        }

        private void TrackComponentExecutionInternal(string componentType, string instanceName, bool success, int childCount)
        {
            lock (lockObject)
            {
                // Increment call count
                if (callCounts.ContainsKey(componentType))
                {
                    callCounts[componentType]++;
                }
                
                // Track failure
                if (!success && failureCounts.ContainsKey(componentType))
                {
                    failureCounts[componentType]++;
                }
                
                // Update component stats
                if (componentStats.ContainsKey(componentType))
                {
                    componentStats[componentType].TotalCalls++;
                    if (success)
                    {
                        componentStats[componentType].Successes++;
                    }
                    else
                    {
                        componentStats[componentType].Failures++;
                    }
                }
            }
        }

        /// <summary>
        /// Track subtree injection
        /// </summary>
        public static void TrackSubtreeInjection(string subtreeName)
        {
            Instance.TrackSubtreeInjectionInternal(subtreeName);
        }

        private void TrackSubtreeInjectionInternal(string subtreeName)
        {
            lock (lockObject)
            {
                if (!callCounts.ContainsKey("SubtreesInjected"))
                {
                    callCounts["SubtreesInjected"] = 0;
                }
                callCounts["SubtreesInjected"]++;
                
                if (!componentStats.ContainsKey("SubtreesInjected"))
                {
                    componentStats["SubtreesInjected"] = new ComponentStats();
                }
                componentStats["SubtreesInjected"].TotalCalls++;
                
                WriteLog($"📊 Subtree Injection: {subtreeName} injected (Total: {componentStats["SubtreesInjected"].TotalCalls})");
            }
        }

        /// <summary>
        /// Track decorator evaluation
        /// </summary>
        public static void TrackDecoratorEvaluation(string decoratorType, bool result)
        {
            Instance.TrackDecoratorEvaluationInternal(decoratorType, result);
        }

        private void TrackDecoratorEvaluationInternal(string decoratorType, bool result)
        {
            lock (lockObject)
            {
                if (callCounts.ContainsKey(decoratorType))
                {
                    callCounts[decoratorType]++;
                }
                
                if (!result && failureCounts.ContainsKey(decoratorType))
                {
                    failureCounts[decoratorType]++;
                }
                
                if (componentStats.ContainsKey(decoratorType))
                {
                    componentStats[decoratorType].TotalCalls++;
                    if (!result)
                    {
                        componentStats[decoratorType].Failures++;
                    }
                }
            }
        }

        /// <summary>
        /// Track service execution
        /// </summary>
        public static void TrackServiceExecution(string serviceType, bool success)
        {
            Instance.TrackServiceExecutionInternal(serviceType, success);
        }

        // Removed complex TrackServiceAddition method - simplified to basic service addition tracking only

        // Removed complex real-time tracking methods - simplified to basic flow node tracking only

        // Removed complex TrackDecoratorAddition method - simplified to basic decorator addition tracking only

        // Removed complex TrackFlowNodeAddition method - simplified to basic flow node addition tracking only

        /// <summary>
        /// Track when a node fails
        /// </summary>
        public static void TrackNodeFailure(string nodeType, string nodeName)
        {
            Instance.TrackNodeFailureInternal(nodeType, nodeName);
        }

        // Removed complex detailed statistics tracking methods - simplified to basic flow node tracking only

        /// <summary>
        /// Track when actions are deleted or removed from the system
        /// </summary>
        public static void TrackActionDeletion(string deletionType, int actionCount, string reason = "")
        {
            Instance.TrackActionDeletionInternal(deletionType, actionCount, reason);
        }

        /// <summary>
        /// Track when NodeGraphs are reset or cleared
        /// </summary>
        public static void TrackNodeGraphReset(string resetType, int actionCount, string reason = "")
        {
            Instance.TrackNodeGraphResetInternal(resetType, actionCount, reason);
        }

        /// <summary>
        /// Track when subtrees are cleared from blackboard
        /// </summary>
        public static void TrackSubtreeClearing(int subtreeCount, string reason = "")
        {
            Instance.TrackSubtreeClearingInternal(subtreeCount, reason);
        }

        /// <summary>
        /// Track final actions that remain in the tree after all operations
        /// </summary>
        public static void TrackFinalActionsRemaining(int actionCount, string source = "")
        {
            Instance.TrackFinalActionsRemainingInternal(actionCount, source);
        }

        // Removed TrackFlowNodeChildCount - simplified tracking doesn't need child count tracking

        private void TrackServiceExecutionInternal(string serviceType, bool success)
        {
            lock (lockObject)
            {
                if (callCounts.ContainsKey(serviceType))
                {
                    callCounts[serviceType]++;
                }
                
                if (!success && failureCounts.ContainsKey(serviceType))
                {
                    failureCounts[serviceType]++;
                }
                
                if (componentStats.ContainsKey(serviceType))
                {
                    componentStats[serviceType].TotalCalls++;
                    if (!success)
                    {
                        componentStats[serviceType].Failures++;
                    }
                }
            }
        }

        // Removed complex TrackServiceAdditionInternal method - simplified to basic service addition tracking only

        // Removed complex internal tracking methods - simplified to basic flow node tracking only

        // Removed complex TrackDecoratorAdditionInternal method - simplified to basic decorator addition tracking only

        // Removed complex TrackFlowNodeAdditionInternal method - simplified to basic flow node addition tracking only

        private void TrackNodeFailureInternal(string nodeType, string nodeName)
        {
            lock (lockObject)
            {
                // Track failures for the specific node type
                if (!failureCounts.ContainsKey(nodeType))
                {
                    failureCounts[nodeType] = 0;
                }
                failureCounts[nodeType]++;
                
                // Also track in componentStats if it exists
                if (componentStats.ContainsKey(nodeType))
                {
                    componentStats[nodeType].Failures++;
                }
                
                WriteLog($"❌ Node Failure: {nodeType} '{nodeName}' failed (Total failures: {failureCounts[nodeType]})");
            }
        }

        // Removed complex internal tracking methods - simplified to basic flow node tracking only

        // Removed TrackFlowNodeChildCountInternal - simplified tracking doesn't need child count tracking

        private void TrackActionDeletionInternal(string deletionType, int actionCount, string reason)
        {
            lock (lockObject)
            {
                if (!actionDeletions.ContainsKey(deletionType))
                {
                    actionDeletions[deletionType] = 0;
                }
                actionDeletions[deletionType] += actionCount;
                
                WriteLog($"🗑️ Action Deletion: {deletionType} - {actionCount} actions deleted (Reason: {reason}) (Total: {actionDeletions[deletionType]})");
            }
        }

        private void TrackNodeGraphResetInternal(string resetType, int actionCount, string reason)
        {
            lock (lockObject)
            {
                if (!nodeGraphResets.ContainsKey(resetType))
                {
                    nodeGraphResets[resetType] = 0;
                }
                nodeGraphResets[resetType] += actionCount;
                
                WriteLog($"🔄 NodeGraph Reset: {resetType} - {actionCount} actions reset (Reason: {reason}) (Total: {nodeGraphResets[resetType]})");
            }
        }

        private void TrackSubtreeClearingInternal(int subtreeCount, string reason)
        {
            lock (lockObject)
            {
                totalSubtreesCleared += subtreeCount;
                WriteLog($"🧹 Subtree Clearing: {subtreeCount} subtrees cleared (Reason: {reason}) (Total cleared: {totalSubtreesCleared})");
            }
        }

        private void TrackFinalActionsRemainingInternal(int actionCount, string source)
        {
            lock (lockObject)
            {
                finalActionsRemaining = actionCount; // Update the final count
                WriteLog($"📊 Final Actions Remaining: {actionCount} actions remain in tree (Source: {source})");
            }
        }

        // Removed complex decorator and service internal tracking methods - simplified to basic flow node tracking only

        /// <summary>
        /// Generate and export the comprehensive CSV summary
        /// </summary>
        public static void GenerateCSVSummary(Blackboard<FastName> blackboard)
        {
            Instance.GenerateCSVSummaryInternal(blackboard);
        }

        /// <summary>
        /// Generate and display a simplified summary table of all tracked components
        /// </summary>
        public static void GenerateSimplifiedSummary(Blackboard<FastName> blackboard)
        {
            Instance.GenerateSimplifiedSummaryInternal(blackboard);
        }

        private void GenerateCSVSummaryInternal(Blackboard<FastName> blackboard)
        {
            lock (lockObject)
            {
                WriteSectionHeader("🌳 BEHAVIOR TREE COMPONENT CSV SUMMARY");
                
                // Collect current data from blackboard
                var currentData = CollectCurrentComponentData(blackboard);
                
                // Generate CSV content
                var csvContent = GenerateCSVContent(currentData, blackboard);
                
                // Write CSV to log
                WriteLog("CSV Summary:");
                WriteLog(csvContent);
                
                // Also write to a separate CSV file
                WriteCSVToFile(csvContent);
            }
        }

        private void GenerateSimplifiedSummaryInternal(Blackboard<FastName> blackboard)
        {
            lock (lockObject)
            {
                WriteSectionHeader("📊 SIMPLIFIED COMPONENT SUMMARY");
                
                // Collect current data from blackboard
                var currentData = CollectCurrentComponentData(blackboard);
                
                // Generate and display summary table
                var summaryTable = GenerateSummaryTable(currentData);
                WriteLog("Component Summary:");
                WriteLog(summaryTable);
                
                // Generate and display CSV
                var csvContent = GenerateCSVContent(currentData, blackboard);
                WriteLog("\nCSV Output:");
                WriteLog(csvContent);
                
                // Also write to a separate CSV file
                WriteCSVToFile(csvContent);
            }
        }

        private Dictionary<string, object> CollectCurrentComponentData(Blackboard<FastName> blackboard)
        {
            var data = new Dictionary<string, object>();
            
            try
            {
                // Get counts from blackboard (only for actions and subtrees)
                data["GenericBTAction"] = blackboard.GetAllActions().Count;
                data["SubtreesInjected"] = blackboard.GetAllInjectedSubtrees().Count;
                
                // Track final actions remaining
                var finalActionCount = blackboard.GetAllActions().Count;
                TrackFinalActionsRemaining(finalActionCount, "CSV generation - final count from blackboard");
                
                // Debug: Print all keys in flowNodeStats for all tracked components
                WriteLog("🔍 DEBUG: All tracked component keys in flowNodeStats:");
                foreach (var kvp in flowNodeStats)
                {
                    if (kvp.Key.StartsWith("BTFlowNode_") || kvp.Key.StartsWith("BTDecorator_") || kvp.Key.StartsWith("BTService") || kvp.Key.StartsWith("Call") || kvp.Key == "SubtreeInjectionService" || kvp.Key == "GenericBTAction")
                    {
                        WriteLog($"   Key: '{kvp.Key}' -> AdditionCount: {kvp.Value.AdditionCount}");
                    }
                }
                
                // Get all component counts from flowNodeStats (tracked by simplified addition tracking)
                // For flow nodes, decorators, services, and actions, instance count = addition count
                foreach (var kvp in flowNodeStats)
                {
                    var componentType = kvp.Key;
                    if (componentType.StartsWith("BTFlowNode_") || componentType.StartsWith("BTDecorator_") || componentType.StartsWith("BTService") || componentType.StartsWith("Call") || componentType == "SubtreeInjectionService" || componentType == "GenericBTAction")
                    {
                        data[componentType] = kvp.Value.AdditionCount;
                        WriteLog($"🔍 DEBUG: Added {componentType} = {kvp.Value.AdditionCount} to data (from AdditionCount)");
                    }
                }
                
            }
            catch (Exception ex)
            {
                WriteLog($"⚠️ Error collecting component data: {ex.Message}");
            }
            
            return data;
        }


        private string GenerateCSVContent(Dictionary<string, object> currentData, Blackboard<FastName> blackboard)
        {
            var csv = new StringBuilder();
            
            // Simplified CSV Header - focus on tick, success, failure, and addition counts
            csv.AppendLine("ComponentType,InstanceCount,TickCount,SuccessCount,FailureCount,AdditionCount");
            
            // Get all tracked components from flowNodeStats (includes flow nodes, decorators, and services)
            var allTrackedComponents = flowNodeStats.Keys.ToList();
            allTrackedComponents.Sort(); // Sort alphabetically for consistent output
            
            // Add tracked component rows with simplified stats
            foreach (var component in allTrackedComponents)
            {
                var instanceCount = currentData.ContainsKey(component) ? Convert.ToInt32(currentData[component]) : 0;
                var tickCount = flowNodeStats[component].TickCount;
                var successCount = flowNodeStats[component].SuccessCount;
                var failureCount = flowNodeStats[component].FailureCount;
                var additionCount = flowNodeStats[component].AdditionCount;
                
                csv.AppendLine($"{component},{instanceCount},{tickCount},{successCount},{failureCount},{additionCount}");
            }
            
            // Add summary totals
            var totalTicks = flowNodeStats.Values.Sum(x => x.TickCount);
            var totalSuccesses = flowNodeStats.Values.Sum(x => x.SuccessCount);
            var totalFailures = flowNodeStats.Values.Sum(x => x.FailureCount);
            var totalAdditions = flowNodeStats.Values.Sum(x => x.AdditionCount);
            var totalInstances = allTrackedComponents.Sum(component => currentData.ContainsKey(component) ? Convert.ToInt32(currentData[component]) : 0);
            
            csv.AppendLine($"TOTAL,{totalInstances},{totalTicks},{totalSuccesses},{totalFailures},{totalAdditions}");
            
            return csv.ToString();
        }

        private string GenerateSummaryTable(Dictionary<string, object> currentData)
        {
            var table = new StringBuilder();
            
            // Get all tracked components and sort them
            var allTrackedComponents = flowNodeStats.Keys.ToList();
            allTrackedComponents.Sort();
            
            // Calculate column widths
            var maxComponentWidth = Math.Max("Component Type".Length, allTrackedComponents.Max(x => x.Length));
            var maxInstanceWidth = Math.Max("Instances".Length, 8);
            var maxTickWidth = Math.Max("Ticks".Length, 8);
            var maxSuccessWidth = Math.Max("Successes".Length, 8);
            var maxFailureWidth = Math.Max("Failures".Length, 8);
            var maxAdditionWidth = Math.Max("Additions".Length, 8);
            
            // Create header
            table.AppendLine("┌" + new string('─', maxComponentWidth + 2) + "┬" + 
                           new string('─', maxInstanceWidth + 2) + "┬" + 
                           new string('─', maxTickWidth + 2) + "┬" + 
                           new string('─', maxSuccessWidth + 2) + "┬" + 
                           new string('─', maxFailureWidth + 2) + "┬" + 
                           new string('─', maxAdditionWidth + 2) + "┐");
            
            table.AppendLine("│ " + "Component Type".PadRight(maxComponentWidth) + " │ " + 
                           "Instances".PadRight(maxInstanceWidth) + " │ " + 
                           "Ticks".PadRight(maxTickWidth) + " │ " + 
                           "Successes".PadRight(maxSuccessWidth) + " │ " + 
                           "Failures".PadRight(maxFailureWidth) + " │ " + 
                           "Additions".PadRight(maxAdditionWidth) + " │");
            
            table.AppendLine("├" + new string('─', maxComponentWidth + 2) + "┼" + 
                           new string('─', maxInstanceWidth + 2) + "┼" + 
                           new string('─', maxTickWidth + 2) + "┼" + 
                           new string('─', maxSuccessWidth + 2) + "┼" + 
                           new string('─', maxFailureWidth + 2) + "┼" + 
                           new string('─', maxAdditionWidth + 2) + "┤");
            
            // Add data rows
            foreach (var component in allTrackedComponents)
            {
                var instanceCount = currentData.ContainsKey(component) ? Convert.ToInt32(currentData[component]) : 0;
                var tickCount = flowNodeStats[component].TickCount;
                var successCount = flowNodeStats[component].SuccessCount;
                var failureCount = flowNodeStats[component].FailureCount;
                var additionCount = flowNodeStats[component].AdditionCount;
                
                table.AppendLine("│ " + component.PadRight(maxComponentWidth) + " │ " + 
                               instanceCount.ToString().PadRight(maxInstanceWidth) + " │ " + 
                               tickCount.ToString().PadRight(maxTickWidth) + " │ " + 
                               successCount.ToString().PadRight(maxSuccessWidth) + " │ " + 
                               failureCount.ToString().PadRight(maxFailureWidth) + " │ " + 
                               additionCount.ToString().PadRight(maxAdditionWidth) + " │");
            }
            
            // Add totals row
            var totalInstances = allTrackedComponents.Sum(component => currentData.ContainsKey(component) ? Convert.ToInt32(currentData[component]) : 0);
            var totalTicks = flowNodeStats.Values.Sum(x => x.TickCount);
            var totalSuccesses = flowNodeStats.Values.Sum(x => x.SuccessCount);
            var totalFailures = flowNodeStats.Values.Sum(x => x.FailureCount);
            var totalAdditions = flowNodeStats.Values.Sum(x => x.AdditionCount);
            
            table.AppendLine("├" + new string('─', maxComponentWidth + 2) + "┼" + 
                           new string('─', maxInstanceWidth + 2) + "┼" + 
                           new string('─', maxTickWidth + 2) + "┼" + 
                           new string('─', maxSuccessWidth + 2) + "┼" + 
                           new string('─', maxFailureWidth + 2) + "┼" + 
                           new string('─', maxAdditionWidth + 2) + "┤");
            
            table.AppendLine("│ " + "TOTAL".PadRight(maxComponentWidth) + " │ " + 
                           totalInstances.ToString().PadRight(maxInstanceWidth) + " │ " + 
                           totalTicks.ToString().PadRight(maxTickWidth) + " │ " + 
                           totalSuccesses.ToString().PadRight(maxSuccessWidth) + " │ " + 
                           totalFailures.ToString().PadRight(maxFailureWidth) + " │ " + 
                           totalAdditions.ToString().PadRight(maxAdditionWidth) + " │");
            
            table.AppendLine("└" + new string('─', maxComponentWidth + 2) + "┴" + 
                           new string('─', maxInstanceWidth + 2) + "┴" + 
                           new string('─', maxTickWidth + 2) + "┴" + 
                           new string('─', maxSuccessWidth + 2) + "┴" + 
                           new string('─', maxFailureWidth + 2) + "┴" + 
                           new string('─', maxAdditionWidth + 2) + "┘");
            
            return table.ToString();
        }

        private void WriteCSVToFile(string csvContent)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                var csvFilePath = $"WrittenLogs/BehaviorTreeComponentSummary_{timestamp}.csv";
                
                System.IO.File.WriteAllText(csvFilePath, csvContent, Encoding.UTF8);
                WriteLog($"📄 CSV summary written to: {csvFilePath}");
            }
            catch (Exception ex)
            {
                WriteLog($"⚠️ Error writing CSV file: {ex.Message}");
            }
        }

        /// <summary>
        /// Close the logger
        /// </summary>
        public new static void Close()
        {
            Instance.CloseInternal();
        }

        private void CloseInternal()
        {
            WriteSectionHeader("🏁 BEHAVIOR TREE COMPONENT LOGGER CLOSED");
            base.Close();
        }

        /// <summary>
        /// Simplified flow node statistics tracking - tick, success, failure, and addition counts
        /// </summary>
        private class FlowNodeStats
        {
            public int TickCount { get; set; } = 0;
            public int SuccessCount { get; set; } = 0;
            public int FailureCount { get; set; } = 0;
            public int AdditionCount { get; set; } = 0; // Tracks how many times a component type is created/added
        }

        /// <summary>
        /// Component statistics tracking (kept for compatibility)
        /// </summary>
        private class ComponentStats
        {
            public int TotalCalls { get; set; } = 0;
            public int Failures { get; set; } = 0;
            public int Successes { get; set; } = 0;
            public double AverageBranchingFactor { get; set; } = 0.0;
        }
    }
}
