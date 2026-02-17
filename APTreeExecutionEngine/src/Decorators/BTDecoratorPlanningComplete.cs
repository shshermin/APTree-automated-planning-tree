using BehaviorTreeMainProject.Services;
using BehaviorTreeMainProject.Log.Services;

/// <summary>
/// Decorator that ensures planning is completed before allowing node execution.
/// This decorator checks the global PlanningPhase flag on the blackboard.
/// </summary>
public class BTDecoratorPlanningComplete : Decorator
{

    public override bool CanPostProcessTickResult => false;
    public override BTNodeResult PostProcessTickResult(BTNodeResult InResult) => InResult;
    
    public BTDecoratorPlanningComplete() : base(false)
    {
    }
    
    
    protected override bool OnEvaluate(float InDeltaTime)
    {
        // Check if we're in planning phase
        if (LinkedBlackboard.PlanningPhase)
        {
            LoggingService.LogInfo($"⏳ PlanningCompleteDecorator: Planning phase active, blocking execution (will re-evaluate on next tick)");
            ExecutionFlowLogger.LogDecoratorTick("PlanningComplete", "PlanningPhase", "Unknown", "BLOCK_FOR_RE_EVAL");
            return false; // Block execution during planning phase, will re-evaluate on next tick
        }
        
        LoggingService.LogInfo($"✅ PlanningCompleteDecorator: Planning phase completed, allowing execution");
        ExecutionFlowLogger.LogDecoratorTick("PlanningComplete", "PlanningPhase", "Unknown", "ALLOW");
        return true; // Allow execution when planning is complete
    }
    

}
