using BehaviorTreeMainProject.Log.Services;

/// <summary>
/// Post-processing decorator that converts Failure → InProgress for retry.
/// 
/// When the owner BTFlowNode_Dynamic fails (all children finished but success criteria not met),
/// this decorator:
///   1. Resets failed children back to ReadyToTick
///   2. Resets their NodeGraph execution states (IsCompleted, IsExecuting)
///   3. Converts the result from Failure → InProgress
///
/// This keeps the flow node alive so children can be retried on the next tick,
/// following the standard behavior tree pattern of decorator-driven retry.
/// 
/// The decorator does NOT interfere with pre-tick evaluation (OnEvaluate always returns true).
/// It only acts in the post-processing phase via PostProcessTickResult.
/// </summary>
public class BTDecorator_RetryOnFailure : BTDecoratorBase
{
    public override bool CanPostProcessTickResult => true;

    public BTDecorator_RetryOnFailure(BTFlowNode_Dynamic attachedNode) : base(false)
    {
        AttachedNode = attachedNode;
    }

    /// <summary>
    /// Pre-tick: always allow execution (no gating).
    /// </summary>
    protected override bool OnEvaluate(float InDeltaTime)
    {
        return true;
    }

    /// <summary>
    /// Post-processing: when the flow node fails, reset failed children and convert to InProgress.
    /// </summary>
    public override BTNodeResult PostProcessTickResult(BTNodeResult InResult)
    {
        if (InResult != BTNodeResult.Failure)
        {
            return InResult; // Only intercept failures
        }

        // Find the owning BTFlowNode_Dynamic to access its NodeGraph
        if (AttachedNode == null)
        {
            LoggingService.LogWarning($"⚠️ BTDecorator_RetryOnFailure: No AttachedNode set, cannot reset children");
            return InResult; // Can't act without the flow node reference
        }

        var actionGraph = AttachedNode.GetActionGraph();
        if (actionGraph == null)
        {
            LoggingService.LogWarning($"⚠️ BTDecorator_RetryOnFailure: No action graph found on {AttachedNode.DebugDisplayName}");
            return InResult;
        }

        var allNodes = actionGraph.GetAllActionNodes();
        if (allNodes.Count == 0)
        {
            return InResult; // No children to reset
        }

        // Only retry when children actually executed and failed.
        // If all children are still ReadyToTick, the failure came from a decorator block (e.g., PlanningComplete),
        // not from actual execution — don't interfere.
        bool anyChildFailed = allNodes.Any(node => node.status == BTNodeResult.Failure);
        if (!anyChildFailed)
        {
            LoggingService.LogInfo($"🔄 BTDecorator_RetryOnFailure: No failed children on {AttachedNode.DebugDisplayName} — failure is from decorator block, not execution. Passing through.");
            return InResult;
        }

        // Reset NodeGraph execution states so failed nodes can be re-executed
        LoggingService.LogInfo($"🔄 BTDecorator_RetryOnFailure: Execution failure detected on {AttachedNode.DebugDisplayName}, resetting failed children for retry");
        actionGraph.Reset();

        // Reset each child action's status back to ReadyToTick
        int resetCount = 0;
        foreach (var node in allNodes)
        {
            if (node.status == BTNodeResult.Failure)
            {
                node.Reset();
                resetCount++;
                LoggingService.LogInfo($"   🔄 Reset failed child: {node.InstanceName.ToString()} → ReadyToTick");
            }
            else if (node.status == BTNodeResult.Success)
            {
                // Leave successful nodes alone — they already contributed to the criteria
                LoggingService.LogInfo($"   ✅ Preserving successful child: {node.InstanceName.ToString()}");
            }
        }

        LoggingService.LogInfo($"🔄 BTDecorator_RetryOnFailure: Reset {resetCount} failed children, converting Failure → InProgress");

        // Convert Failure → InProgress so the node continues ticking
        return BTNodeResult.InProgress;
    }
}
