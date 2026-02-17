using BehaviorTreeMainProject.Log.Services;

/// <summary>
/// Gate decorator that enforces exclusive branch execution based on the blackboard's ChosenExecutingBranch.
/// 
/// - If a ChosenExecutingBranch is set on the blackboard, ONLY that branch is allowed to execute.
///   All other branches are blocked.
/// - If no ChosenExecutingBranch is set, all branches are allowed through (so LowestCost can pick one).
/// 
/// This decorator does NOT set or clear the chosen branch — that is the responsibility of 
/// BTDecoratorLowestCostExecution. This decorator must be evaluated BEFORE LowestCost 
/// (added to the decorator list first).
/// </summary>
public class BTDecoratorExclusiveBranchGate : BTDecoratorBase
{
    public override bool CanPostProcessTickResult => false;
    public override BTNodeResult PostProcessTickResult(BTNodeResult InResult) => InResult;

    public BTDecoratorExclusiveBranchGate(BTFlowNodeDynamic attachedNode) : base(false)
    {
        this.AttachedNode = attachedNode;
    }

    protected override bool OnEvaluate(float InDeltaTime)
    {
        // Read the chosen branch from the blackboard
        var chosenBranch = LinkedBlackboard.ChosenExecutingBranch;

        // If no branch is chosen yet, allow all through (LowestCost will pick one next)
        if (chosenBranch == null)
        {
            LoggingService.LogInfo($"🚪 ExclusiveBranchGate: No chosen branch set — ALLOW '{AttachedNode.InstanceName}' through to LowestCost evaluation");
            return true;
        }

        // If the chosen branch has already SUCCEEDED, it's stale — allow through so 
        // LowestCost can clear it and pick a new branch. Without this, we deadlock:
        // the old succeeded subtree never ticks again, so LowestCost never runs to clear it.
        if (chosenBranch.status == BTNodeResult.Success)
        {
            LoggingService.LogInfo($"🚪 ExclusiveBranchGate: Chosen branch '{chosenBranch.InstanceName}' already SUCCEEDED — ALLOW '{AttachedNode.InstanceName}' through so LowestCost can re-evaluate");
            return true;
        }

        // A branch is actively chosen — only that branch may execute
        bool isChosenBranch = (AttachedNode == chosenBranch);

        if (isChosenBranch)
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
