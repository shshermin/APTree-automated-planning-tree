using BehaviorTreeMainProject.Services;
using BehaviorTreeMainProject.Log.Services;

/// <summary>
/// Service that manages the transition from planning phase to execution phase.
/// This service runs during the planning phase and automatically switches to execution
/// when all planning services have completed successfully.
/// </summary>
public class ServicePlanningPhaseManager : Service
{
    public string DebugDisplayName { get; protected set; } = "PlanningPhaseManager";

    public ServicePlanningPhaseManager(IBehaviorTree InOwningTree, FlowNode InOwningFlowNode) : base(InOwningTree)
    {
        AttachedNode = InOwningFlowNode;
    }

    public override bool OnEvaluate(float inDeltaTime)
    {
        LoggingService.LogInfo($"🔧 ServicePlanningPhaseManager.Tick: Starting tick...");
        
        // This service runs even during planning phase
        LoggingService.LogInfo($"🔧 ServicePlanningPhaseManager.Tick: Calling CheckAndSwitchToExecutionPhase()...");
        CheckAndSwitchToExecutionPhase();
        
        LoggingService.LogInfo($"🔧 ServicePlanningPhaseManager.Tick: Tick completed");
        return true;
    }
    /// <summary>
    /// checks the highlevel planningservices and sets the flag
    /// </summary>
    private void CheckAndSwitchToExecutionPhase()
    {
        if (!linkedBlackboard.PlanningPhase)
        {
            return; // Already in execution phase
        }

        // Check if all planning services have completed
        bool allPlanningComplete = AreAllPlanningServicesComplete();
        
        if (allPlanningComplete)
        {
            LoggingService.LogSuccess("🎉 All planning completed! Switching to execution phase...");
            ExecutionFlowLogger.LogPlanningEvent("PHASE_COMPLETE", "All planning services finished");
            linkedBlackboard.PlanningPhase = false;
            

            
            // Track final actions remaining at the end of planning phase
            var finalActionCount = linkedBlackboard.GetAllActions().Count;
            BehaviorTreeComponentLogger.TrackFinalActionsRemaining(finalActionCount, "End of planning phase - all planning completed");
            
            LoggingService.LogWarning("🚨 WARNING: PlanningPhase has been set to FALSE - dynamic planning phase manager can now start checking!");
            ExecutionFlowLogger.LogExecutionEvent("PHASE_START", "ML actions can now execute");
            LoggingService.LogSuccess("✅ Switched to execution phase - ML actions can now execute");
            LoggingService.LogInfo("🚀 EXECUTION PHASE: All NodeGraphs generated, ML actions will now execute");
        }
                else
        {
            LoggingService.LogInfo($"⏳ PLANNING PHASE: Waiting for all HL actions to complete planning...");
            ExecutionFlowLogger.LogPlanningEvent("IN_PROGRESS", "Waiting for planning services to complete");
        }
    }
    
   
    
    private bool AreAllPlanningServicesComplete()
    {
        // Since this service is attached to a composite node, we can directly access it
        if (AttachedNode is FlowNode flowNode)
        {
            var children = flowNode.GetChildren();
            
            foreach (var child in children)
            {
                if (child is DynamicFlowNode dynamicNode)
                {
                    // Check if this dynamic node has a planning service
                    if (dynamicNode.ServicePlanning is ServicePlanning plannerService)
                    {
                        // Check if planning has generated a NodeGraph
                        if (!plannerService.HasGeneratedNodeGraph())
                        {
                            LoggingService.LogInfo($"⏳ Planning still in progress for {dynamicNode.GetNodeName()}");
                            return false; // Still planning
                        }
                    }
                    else
                    {
                        LoggingService.LogWarning($"⚠️ Dynamic node {dynamicNode.GetNodeName()} has no planning service");
                        return false; // No planning service means not ready
                    }
                }
                else if (child is FlowNode childFlowNode)
                {
                    // Recursively check child flow nodes
                    if (!AreChildPlanningServicesComplete(childFlowNode))
                    {
                        return false;
                    }
                }
            }
        }
        
        return true; // All planning complete
    }

    private bool AreChildPlanningServicesComplete(FlowNode node)
    {
        foreach (var child in node.GetChildren())
        {
            if (child is DynamicFlowNode dn)
            {
                if (dn.ServicePlanning is ServicePlanning ps && !ps.HasGeneratedNodeGraph())
                    return false;
            }
            else if (child is FlowNode fn)
            {
                if (!AreChildPlanningServicesComplete(fn))
                    return false;
            }
        }
        return true;
    }
}
