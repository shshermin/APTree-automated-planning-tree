using BehaviorTreeMainProject.Log.Services;

/// <summary>
/// Gate decorator that enforces exclusive branch execution based on the blackboard's ChosenExecutingBranch.
/// 
/// - If a ChosenExecutingBranch is set on the blackboard, ONLY subtrees that belong to
///   that branch (or ARE that branch) are allowed to execute. All other branches are blocked.
/// - If no ChosenExecutingBranch is set, all branches are allowed through.
/// 
/// The chosen branch is set by BTDecoratorFairBranchProgress at the cassette level.
/// Since this decorator is attached to injected subtrees (children of cassettes),
/// it walks up the parent chain to check if the subtree belongs to the chosen cassette.
/// </summary>
public class BTDecoratorExclusiveBranchGate : Decorator
{
    public override bool CanPostProcessTickResult => false;
    public override BTNodeResult PostProcessTickResult(BTNodeResult InResult) => InResult;

    public BTDecoratorExclusiveBranchGate(DynamicFlowNode attachedNode) : base(false)
    {
        this.AttachedNode = attachedNode;
    }

    protected override bool OnEvaluate(float InDeltaTime)
    {
        // Read the chosen branch from the blackboard
        var chosenBranch = LinkedBlackboard.ChosenExecutingBranch;

        // If no branch is chosen yet, allow all through
        if (chosenBranch == null)
        {
            LoggingService.LogInfo($"🚪 ExclusiveBranchGate: No chosen branch set — ALLOW '{AttachedNode.InstanceName}' through");
            return true;
        }

        // If the chosen branch has already SUCCEEDED, it's stale — allow through so
        // FairBranchProgress can clear it and pick a new branch on next tick.
        if (chosenBranch.status == BTNodeResult.Success)
        {
            LoggingService.LogInfo($"🚪 ExclusiveBranchGate: Chosen branch '{chosenBranch.InstanceName}' already SUCCEEDED — ALLOW '{AttachedNode.InstanceName}' through for re-evaluation");
            return true;
        }

        // Check if this subtree IS the chosen branch or BELONGS TO the chosen branch
        // (walk up the parent chain to find if chosenBranch is an ancestor)
        bool belongsToChosenBranch = IsOrBelongsTo(AttachedNode, chosenBranch);

        if (belongsToChosenBranch)
        {
            LoggingService.LogSuccess($"🚪 ExclusiveBranchGate: '{AttachedNode.InstanceName}' belongs to chosen branch '{chosenBranch.InstanceName}' — ALLOW");
            return true;
        }
        else
        {
            LoggingService.LogInfo($"🚪 ExclusiveBranchGate: '{AttachedNode.InstanceName}' does NOT belong to chosen branch '{chosenBranch.InstanceName}' — BLOCKING");
            return false;
        }
    }

    /// <summary>
    /// Checks if 'node' is the target or is a descendant of 'target' by walking up the parent chain.
    /// </summary>
    private bool IsOrBelongsTo(DynamicFlowNode node, DynamicFlowNode target)
    {
        // Direct match
        if (node == target)
            return true;

        // Walk up the parent chain
        IBTNode current = node.ParentNode;
        while (current != null)
        {
            if (current == target)
                return true;
            current = current.ParentNode;
        }

        return false;
    }
}
