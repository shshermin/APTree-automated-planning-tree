using BehaviorTreeMainProject.Log.Services;

/// <summary>
/// Gate decorator that enforces exclusive branch execution based on the blackboard's ChosenExecutingBranch.
/// 
/// - If a ChosenExecutingBranch is set on the blackboard, ONLY that exact branch is allowed to execute.
/// - If no ChosenExecutingBranch is set, all branches are blocked until the batch-level
///   LowestCost decorator selects one.
/// 
/// This decorator does NOT set or clear the chosen branch — that is the responsibility of 
/// DecoratorLowestCostExecution at the batch level.
/// </summary>
public class DecoratorExclusiveBranchGate : Decorator
{
    public override bool CanPostProcessTickResult => false;
    public override BTNodeResult PostProcessTickResult(BTNodeResult InResult) => InResult;

    public DecoratorExclusiveBranchGate(DynamicFlowNode attachedNode) : base(false)
    {
        this.AttachedNode = attachedNode;
    }

    protected override bool OnEvaluate(float InDeltaTime)
    {
        // Read the chosen branch from the blackboard
        var chosenBranch = LinkedBlackboard.ChosenExecutingBranch;

        // Selection happens at the batch level before its injected branches are ticked.
        // If planning completed during this child pass, wait for the next batch tick to select.
        if (chosenBranch == null)
        {
            LoggingService.LogInfo($"🚪 ExclusiveBranchGate: No chosen branch set — BLOCKING '{AttachedNode.InstanceName}' until batch selection");
            return false;
        }

        if (ReferenceEquals(AttachedNode, chosenBranch))
        {
            LoggingService.LogSuccess($"🚪 ExclusiveBranchGate: '{AttachedNode.InstanceName}' IS the chosen executing branch — ALLOW");
            return true;
        }
        else
        {
            LoggingService.LogInfo($"🚪 ExclusiveBranchGate: '{AttachedNode.InstanceName}' is NOT the chosen branch (chosen: '{chosenBranch.InstanceName}') — BLOCKING");
            return false;
        }
    }
}
