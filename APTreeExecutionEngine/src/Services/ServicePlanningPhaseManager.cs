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
        if (AttachedNode is not FlowNode flowNode)
            return true;

        return AreAllPlanningServicesComplete(flowNode);
    }

    private bool AreAllPlanningServicesComplete(FlowNode flowNode)
    {
        foreach (var child in flowNode.GetChildren())
        {
            if (child is not FlowNode childFlowNode)
                continue;

            if (childFlowNode.ServicePlanning is ServicePlanning plannerService)
            {
                if (!plannerService.HasGeneratedNodeGraph())
                {
                    LoggingService.LogInfo($"⏳ Planning still in progress for {childFlowNode.GetNodeName()}");
                    return false;
                }

                continue;
            }

            if (childFlowNode.GetChildren().Count > 0 &&
                !AreAllPlanningServicesComplete(childFlowNode))
                return false;

            if (childFlowNode.GetChildren().Count == 0)
            {
                LoggingService.LogWarning($"⚠️ Flow node {childFlowNode.GetNodeName()} has no planning service");
                return false;
            }
        }

        return true;
    }
    

    

}
