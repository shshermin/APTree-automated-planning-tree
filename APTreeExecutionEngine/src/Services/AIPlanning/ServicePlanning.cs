using System;
using System.Threading.Tasks;

using AIPlanning;
using System.Collections.Generic;
using BehaviorTreeMainProject.Services;
using BehaviorTreeMainProject.Log;
using BehaviorTreeMainProject.Log.Services;

/// <summary>
/// Base class for all planning services with enhanced tracking capabilities.
/// 
/// Usage Examples:
/// 
/// // Check if planning was successful and plan was generated
/// if (planner.HasCompleted)
/// {
///     if (planner.WasSuccessful && planner.HasPlanGenerated)
///     {
///         Console.WriteLine($"✅ Planning successful! Generated {planner.GetGeneratedNodeGraph().GetAllActionNodes().Count} actions");
///     }
///     else
///     {
///         Console.WriteLine($"❌ Planning failed: {planner.LastError}");
///     }
/// }
/// 
/// // Get planning status summary
/// string status = planner.GetPlanningStatusSummary();
/// Console.WriteLine($"Planning Status: {status}");
/// 
/// // Get detailed statistics
/// var stats = planner.GetPlanningStatistics();
/// foreach (var kvp in stats)
/// {
///     Console.WriteLine($"{kvp.Key}: {kvp.Value}");
/// }
/// </summary>
public abstract class ServicePlanning : Service
{
    // Store the generated NodeGraph
    protected NodeGraph generatedNodeGraph;
    
    // Property to access the generated NodeGraph
    public NodeGraph GeneratedNodeGraph => generatedNodeGraph;

    // The communicator for external planners
    protected IPlannerCommunicator plannerCommunicator;
    public IPlanningRequest planningRequest { get; }

    // The current planner used by this planning service
    public Planner CurrentPlanner { get; set; }
    
    // Execution tracking
    public DateTime StartTime { get; private set; }
    public DateTime PlannerEndTime { get; private set; } // Time when external planner finishes
    public DateTime EndTime { get; private set; } // Time when entire service finishes
    public bool IsExecuting { get; private set; } = false;
    public bool HasCompleted { get; private set; } = false;
    public bool WasSuccessful { get; private set; } = false; // True if planning succeeded and plan was generated
    public bool HasPlanGenerated { get; private set; } = false; // True if NodeGraph was successfully created
    public string LastError { get; private set; } = null; // Last error message if planning failed
    public TimeSpan PlannerExecutionDuration => HasCompleted ? PlannerEndTime - StartTime : TimeSpan.Zero; // External planner time only
    public TimeSpan TotalExecutionDuration => HasCompleted ? EndTime - StartTime : TimeSpan.Zero; // Total service time
    public string PlannerName => GetType().Name;



    protected ServicePlanning(IBehaviorTree InOwningTree, IPlannerCommunicator communicator, IPlanningRequest InPlanningRequest)
        : base(InOwningTree)
    {
        generatedNodeGraph = null;
        plannerCommunicator = communicator ?? throw new ArgumentNullException(nameof(communicator));
        planningRequest = InPlanningRequest ?? throw new ArgumentNullException(nameof(InPlanningRequest));
    }
    
    // Reference to the flow node that owns this planning service
    protected FlowNode OwningFlowNode { get; set; }
    
    /// <summary>
    /// Set the owning flow node for this planning service
    /// </summary>
    /// <param name="flowNode">The flow node that owns this planning service</param>
    public void SetOwningFlowNode(FlowNode flowNode)
    {
        LoggingService.LogInfo($"🔧 ServicePlanning: SetOwningFlowNode called - {GetType().Name} ↔ {flowNode.DebugDisplayName}");
        OwningFlowNode = flowNode;
        LoggingService.LogInfo($"🔧 ServicePlanning: Bidirectional reference established - {GetType().Name} ↔ {flowNode.DebugDisplayName}");
    }
   

    public override bool OnEvaluate(float InDeltaTime)
    {
        // If planning has already completed (success or failure), don't run again
        if (HasCompleted)
        {
            if (WasSuccessful && HasPlanGenerated && generatedNodeGraph != null)
            {
                LoggingService.LogInfo($"⏭️ {GetType().Name}: Planning already completed successfully, preserving existing NodeGraph (HashCode: {generatedNodeGraph.GetHashCode()})");
                return true; // Return true to indicate success
            }
            else
            {
                LoggingService.LogInfo($"⏭️ {GetType().Name}: Planning already completed and failed, not retrying");
                return false; // Return false to indicate failure
            }
        }

        // If already executing, don't start again
        if (IsExecuting)
        {
            LoggingService.LogInfo($"⏳ {GetType().Name}: Planning already in progress, waiting...");
            return true; // Return true to indicate we're still working
        }

        // Start execution tracking
        StartTime = DateTime.Now;
        IsExecuting = true;

        // Get planner type for tracking (will be used at the end of execution)
        var plannerType = CurrentPlanner?.DefaultPlannerName ?? planningRequest.PlanningType;

        LoggingService.LogInfo($"🚀 {GetType().Name}: Starting planning process at {StartTime:HH:mm:ss.fff}");

        // Track this planner call
        var hlActionName = (OwningFlowNode?.ParentNode as PActionNode)?.InstanceName.ToString() ?? OwningFlowNode?.GetNodeName() ?? "Unknown";
        var problemFile = (planningRequest as PDDLPlanningRequest)?.ProblemFile ?? "Unknown";
        var plannerCallId = PlannerCallLogger.LogCallStart(plannerType, hlActionName, problemFile);

        try
        {
            // Step 2: Send to external planner via communicator
            var result = Task.Run(async () => await plannerCommunicator.SendPlanningRequestAsync(planningRequest)).Result;

            // Record when external planner finishes
            PlannerEndTime = DateTime.Now;
            LoggingService.LogInfo($"⏱️ {GetType().Name}: External planner finished at {PlannerEndTime:HH:mm:ss.fff} (Planner time: {PlannerEndTime - StartTime:hh\\:mm\\:ss\\.fff})");

            if (!result.Success)
            {
                EndTime = DateTime.Now;
                IsExecuting = false;
                HasCompleted = true; // Mark as completed even on failure to prevent infinite retries
                WasSuccessful = false;
                HasPlanGenerated = false;
                LastError = result.Error;

                LoggingService.LogError($"⚠️ {GetType().Name}: Planning failed at {EndTime:HH:mm:ss.fff} - {result.Error}");
                LoggingService.LogInfo($"⏱️ {GetType().Name}: Planner execution time: {PlannerEndTime - StartTime:hh\\:mm\\:ss\\.fff}");
                LoggingService.LogInfo($"⏱️ {GetType().Name}: Total service time: {EndTime - StartTime:hh\\:mm\\:ss\\.fff}");
                LoggingService.LogInfo($"📋 {GetType().Name}: Planning Status - Completed: {HasCompleted}, Successful: {WasSuccessful}, Plan Generated: {HasPlanGenerated}");
                LoggingService.LogWarning($"🔄 {GetType().Name}: Planning failed - this node will fail. No retries will be attempted.");
                PlannerCallLogger.LogCallFailed(plannerCallId, result.PlanningTimeSeconds, result.Error);
                return false;
            }

            // Step 3: Generate NodeGraph from planner result (implemented by each planner type)
            generatedNodeGraph = GenerateNodeGraphFromResult(result);

            if (generatedNodeGraph == null)
            {
                EndTime = DateTime.Now;
                IsExecuting = false;
                HasCompleted = true; // Mark as completed even on failure to prevent infinite retries
                WasSuccessful = false;
                HasPlanGenerated = false;
                LastError = "Failed to generate NodeGraph from planner result";

                LoggingService.LogError($"⚠️ {GetType().Name}: Failed to generate NodeGraph at {EndTime:HH:mm:ss.fff}");
                LoggingService.LogInfo($"⏱️ {GetType().Name}: Execution time: {EndTime - StartTime:hh\\:mm\\:ss\\.fff}");
                LoggingService.LogInfo($"📋 {GetType().Name}: Planning Status - Completed: {HasCompleted}, Successful: {WasSuccessful}, Plan Generated: {HasPlanGenerated}");
                LoggingService.LogWarning($"🔄 {GetType().Name}: NodeGraph generation failed - this node will fail. No retries will be attempted.");
                PlannerCallLogger.LogCallFailed(plannerCallId, result.PlanningTimeSeconds, "Failed to generate NodeGraph from planner result");
                return false;
            }

            // Step 4: Directly assign NodeGraph to owning flow node (if available)
            if (OwningFlowNode != null)
            {
                LoggingService.LogInfo($"🔧 ServicePlanning: Directly assigning NodeGraph to flow node {OwningFlowNode.DebugDisplayName}");
                LoggingService.LogInfo($"🔧 ServicePlanning: NodeGraph has {generatedNodeGraph.GetAllActionNodes().Count} actions");
                LoggingService.LogInfo($"🔧 ServicePlanning: Calling SetActionGraph on {OwningFlowNode.DebugDisplayName}");
                OwningFlowNode.SetActionGraph(generatedNodeGraph);

                // Set up services for all actions in the NodeGraph
                LoggingService.LogInfo($"🔧 ServicePlanning: Setting up services for all actions in NodeGraph...");
                var allActions = generatedNodeGraph.GetAllActionNodes();
                for (int i = 0; i < allActions.Count; i++)
                {
                    var action = allActions[i];
                    LoggingService.LogInfo($"🔧 ServicePlanning: Setting up services for action {i + 1}: {action.InstanceName.ToString()}");
                    OwningFlowNode.AddChild(action);
                }
                LoggingService.LogSuccess($"✅ ServicePlanning: Completed service setup for {allActions.Count} actions");


            }
            else
            {
                LoggingService.LogWarning($"⚠️ ServicePlanning: No owning flow node set, cannot directly assign NodeGraph");
                LoggingService.LogWarning($"⚠️ ServicePlanning: OwningFlowNode is null - this means the bidirectional reference was not set properly");
            }

            // Step 5: Store in blackboard (for backward compatibility and monitoring)
            StoreNodeGraphInBlackboard();

            // NEW: Add the subtree to the blackboard's injected subtrees after successful planning
            if (OwningFlowNode.ParentNode is PActionNode parentAction && parentAction.IsHighLevelAction)
            {
                AddSubtreeToBlackboardAfterSuccessfulPlanning();
            }
            else
            {
                LoggingService.LogWarning($"⚠️ ServicePlanning: OwningFlowNode is not a high-level action, cannot add subtree to blackboard");
            }

            // Complete execution tracking
            EndTime = DateTime.Now;
            IsExecuting = false;
            HasCompleted = true;
            WasSuccessful = true;
            HasPlanGenerated = true;
            LastError = null;

            
            // Track final actions remaining after successful planning
            var finalActionCount = linkedBlackboard.GetAllActions().Count;
            BehaviorTreeComponentLogger.TrackFinalActionsRemaining(finalActionCount, $"After successful {plannerType} planning");

            LoggingService.LogSuccess($"✅ {GetType().Name}: Planning process completed successfully at {EndTime:HH:mm:ss.fff}");
            LoggingService.LogInfo($"⏱️ {GetType().Name}: Planner execution time: {PlannerEndTime - StartTime:hh\\:mm\\:ss\\.fff}");
            LoggingService.LogInfo($"⏱️ {GetType().Name}: Total service time: {EndTime - StartTime:hh\\:mm\\:ss\\.fff}");
            LoggingService.LogInfo($"📊 {GetType().Name}: Generated {generatedNodeGraph.GetAllActionNodes().Count} actions");
            LoggingService.LogInfo($"📋 {GetType().Name}: Planning Status - Completed: {HasCompleted}, Successful: {WasSuccessful}, Plan Generated: {HasPlanGenerated}");

            PlannerCallLogger.LogCallEnd(plannerCallId, true, result.PlanningTimeSeconds, generatedNodeGraph.GetAllActionNodes().Count, result.PlanLength, result.PlannerUsed);

            return true;
        }
        catch (Exception ex)
        {
            EndTime = DateTime.Now;
            IsExecuting = false;
            HasCompleted = true; // Mark as completed even on failure to prevent infinite retries
            WasSuccessful = false;
            HasPlanGenerated = false;
            LastError = ex.Message;

            LoggingService.LogError($"❌ {GetType().Name}: Error during planning process at {EndTime:HH:mm:ss.fff}: {ex.Message}");
            LoggingService.LogInfo($"⏱️ {GetType().Name}: Execution time: {EndTime - StartTime:hh\\:mm\\:ss\\.fff}");
            LoggingService.LogInfo($"📋 {GetType().Name}: Planning Status - Completed: {HasCompleted}, Successful: {WasSuccessful}, Plan Generated: {HasPlanGenerated}");
            LoggingService.LogWarning($"🔄 {GetType().Name}: Planning exception occurred - this node will fail. No retries will be attempted.");
            PlannerCallLogger.LogCallFailed(plannerCallId, 0, ex.Message);
            return false;
        }
    }


    
    /// <summary>
    /// Generate NodeGraph from planner result (to be implemented by each planner type)
    /// </summary>
    /// <param name="result">Result from external planner</param>
    /// <returns>Generated NodeGraph</returns>
    protected abstract NodeGraph GenerateNodeGraphFromResult(PlanningResult result);
    

    
    /// <summary>
    /// Store the generated NodeGraph in the blackboard
    /// </summary>
    protected virtual void StoreNodeGraphInBlackboard()
    {
        if (generatedNodeGraph == null)
        {
            LoggingService.LogWarning("⚠️ ServicePlanning: No NodeGraph to store in blackboard");
            return;
        }
        
        try
        {
            // Generate a unique name for the NodeGraph
            string nodeGraphName = $"GeneratedPlan_{GetType().Name}_{DateTime.Now:yyyyMMdd_HHmmss}";
            var nodeGraphKey = new FastName(nodeGraphName);
            
            // Store in blackboard
            linkedBlackboard.SetNodeGraph(nodeGraphKey, generatedNodeGraph);
            
            LoggingService.LogSuccess($"✅ ServicePlanning: Stored NodeGraph '{nodeGraphName}' in blackboard");
            LoggingService.LogInfo($"   📊 NodeGraph contains {generatedNodeGraph.GetAllActionNodes().Count} actions");
        }
        catch (Exception ex)
        {
            LoggingService.LogError($"❌ ServicePlanning: Error storing NodeGraph in blackboard: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Get the generated NodeGraph (if planning has been completed)
    /// </summary>
    /// <returns>The generated NodeGraph or null if planning hasn't been completed</returns>
    public NodeGraph GetGeneratedNodeGraph()
    {
        return generatedNodeGraph;
    }

    
    
    /// <summary>
    /// Check if planning has been completed and NodeGraph is available
    /// </summary>
    /// <returns>True if NodeGraph is available, false otherwise</returns>
    public bool HasGeneratedNodeGraph()
    {
        return generatedNodeGraph != null && HasCompleted;
    }
    
    /// <summary>
    /// Check if planning has failed
    /// </summary>
    /// <returns>True if planning has completed and failed, false otherwise</returns>
    public bool HasPlanningFailed()
    {
        return HasCompleted && !WasSuccessful;
    }
    
    /// <summary>
    /// Check if planning has succeeded
    /// </summary>
    /// <returns>True if planning has completed and succeeded, false otherwise</returns>
    public bool HasPlanningSucceeded()
    {
        return HasCompleted && WasSuccessful && HasPlanGenerated;
    }
    
    /// <summary>
    /// Get a summary of the planning status
    /// </summary>
    /// <returns>String describing the current planning status</returns>
    public string GetPlanningStatusSummary()
    {
        if (!HasCompleted)
        {
            return IsExecuting ? "Planning in progress..." : "Planning not started";
        }
        
        if (WasSuccessful && HasPlanGenerated)
        {
            int actionCount = generatedNodeGraph?.GetAllActionNodes().Count ?? 0;
            return $"Planning successful - Generated plan with {actionCount} actions";
        }
        
        if (WasSuccessful && !HasPlanGenerated)
        {
            return "Planning succeeded but no plan was generated";
        }
        
        return $"Planning failed - {LastError ?? "Unknown error"}";
    }
    
    /// <summary>
    /// Get detailed planning statistics
    /// </summary>
    /// <returns>Dictionary with planning statistics</returns>
    public Dictionary<string, object> GetPlanningStatistics()
    {
        var stats = new Dictionary<string, object>
        {
            ["PlannerName"] = PlannerName,
            ["HasCompleted"] = HasCompleted,
            ["WasSuccessful"] = WasSuccessful,
            ["HasPlanGenerated"] = HasPlanGenerated,
            ["IsExecuting"] = IsExecuting,
            ["PlannerExecutionDuration"] = PlannerExecutionDuration,
            ["TotalExecutionDuration"] = TotalExecutionDuration,
            ["ActionCount"] = generatedNodeGraph?.GetAllActionNodes().Count ?? 0
        };
        
        if (!string.IsNullOrEmpty(LastError))
        {
            stats["LastError"] = LastError;
        }
        
        if (HasCompleted)
        {
            stats["StartTime"] = StartTime;
            stats["EndTime"] = EndTime;
        }
        
        return stats;
    }
    
 
    
    /// <summary>
    /// Check if the planning service has successfully completed and should be preserved
    /// </summary>
    /// <returns>True if planning completed successfully and should be preserved</returns>
    public bool ShouldPreservePlanningResult()
    {
        return HasCompleted && WasSuccessful && HasPlanGenerated && generatedNodeGraph != null;
    }
    
    /// <summary>
    /// Reset the planning service state (useful when tree is reset)
    /// </summary>
    public void ResetPlanningService()
    {
        // Clear the NodeGraph before setting to null to actually remove actions
        if (generatedNodeGraph != null)
        {
            var actionCount = generatedNodeGraph.GetAllActionNodes().Count;
            if (actionCount > 0)
            {
                BehaviorTreeComponentLogger.TrackNodeGraphReset("PlanningServiceReset", actionCount, "Planning service reset");
            }
            
            // Use Clear() to actually remove actions from the NodeGraph
            generatedNodeGraph.DestroyAllNodes();
        }
        
        generatedNodeGraph = null;
        IsExecuting = false;
        HasCompleted = false;
        WasSuccessful = false;
        HasPlanGenerated = false;
        LastError = null;
        LoggingService.LogWarning($"🔄 {GetType().Name}: Planning service reset");
    }

    /// <summary>
    /// Add the subtree to the blackboard's injected subtrees after successful planning
    /// </summary>
    protected virtual void AddSubtreeToBlackboardAfterSuccessfulPlanning()
    {
        if (OwningFlowNode == null)
        {
            LoggingService.LogWarning("⚠️ ServicePlanning: No owning flow node, cannot add subtree to blackboard");
            return;
        }
        
        try
        {
            // Generate a unique key for the subtree
            string subtreeKey = $"InjectedSubtree_{OwningFlowNode.DebugDisplayName}_{DateTime.Now:yyyyMMdd_HHmmss}";
            var fastNameKey = new FastName(subtreeKey);
            
            // Add the subtree to the blackboard's injected subtrees
            linkedBlackboard.SetInjectedSubtree(fastNameKey, OwningFlowNode as DynamicFlowNode);
            
            LoggingService.LogSuccess($"✅ ServicePlanning: Added subtree '{OwningFlowNode.DebugDisplayName}' to blackboard after successful planning");
            LoggingService.LogInfo($"   📝 Subtree key: {subtreeKey}");
            LoggingService.LogInfo($"   📊 NodeGraph contains {generatedNodeGraph?.GetAllActionNodes().Count ?? 0} actions");
        }
        catch (Exception ex)
        {
            LoggingService.LogError($"❌ ServicePlanning: Error adding subtree to blackboard: {ex.Message}");
        }
    }
}