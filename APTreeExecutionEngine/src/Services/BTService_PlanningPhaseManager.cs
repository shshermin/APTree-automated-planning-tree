using BehaviorTreeMainProject.Services;
using BehaviorTreeMainProject.Log.Services;

/// <summary>
/// Service that manages the transition from planning phase to execution phase.
/// This service runs during the planning phase and automatically switches to execution
/// when all planning services have completed successfully.
/// </summary>
public class BTService_PlanningPhaseManager : BTServiceBase
{
    public string DebugDisplayName { get; protected set; } = "PlanningPhaseManager";

    public BTService_PlanningPhaseManager(IBehaviorTree InOwningTree, BTFlowNodeBase InOwningFlowNode) : base(InOwningTree)
    {
        AttachedNode = InOwningFlowNode;
    }

    public override bool OnEvaluate(float inDeltaTime)
    {
        LoggingService.LogInfo($"🔧 BTService_PlanningPhaseManager.Tick: Starting tick...");
        
        // This service runs even during planning phase
        LoggingService.LogInfo($"🔧 BTService_PlanningPhaseManager.Tick: Calling CheckAndSwitchToExecutionPhase()...");
        CheckAndSwitchToExecutionPhase();
        
        LoggingService.LogInfo($"🔧 BTService_PlanningPhaseManager.Tick: Tick completed");
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
            
            // Track planning phase transition for execution summary
            ExecutionSummaryLogger.TrackPlanningPhaseTransition(false);
            
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
        if (AttachedNode is BTFlowNode_Composite compositeNode)
        {
            var children = compositeNode.GetChildren();
            
            foreach (var child in children)
            {
                if (child is BTFlowNode_Dynamic dynamicNode)
                {
                    // Check if this dynamic node has a planning service
                    if (dynamicNode.PlanningService is PlanningService plannerService)
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
                else if (child is BTFlowNode_Composite childCompositeNode)
                {
                    // Recursively check composite nodes
                    if (!childCompositeNode.AreAllPlanningServicesComplete())
                    {
                        return false; // Child composite still planning
                    }
                }
            }
        }
        
        return true; // All planning complete
    }
    

    

}
