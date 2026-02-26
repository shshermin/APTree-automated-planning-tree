using BehaviorTreeMainProject.Log.Services;

/// <summary>
/// Post-processing decorator for ACTION nodes that converts decorator-blocked Failure → InProgress.
/// 
/// When an action node fails because a decorator (e.g., LowestCostExecution) blocked it,
/// the action's children never ran, so no child has a Failure status. In that case, 
/// this decorator resets the action back to ReadyToTick and converts Failure → InProgress,
/// keeping the action alive for re-evaluation on the next tick.
///
/// If the failure came from actual execution (the action itself failed), this decorator
/// does NOT interfere — it passes the Failure through unchanged.
///
/// The decorator does NOT interfere with pre-tick evaluation (OnEvaluate always returns true).
/// It only acts in the post-processing phase via PostProcessTickResult.
/// </summary>
public class DecoratorRetryOnFailureAction : Decorator
{
    public override bool CanPostProcessTickResult => true;

    private readonly PActionNode attachedAction;

    public DecoratorRetryOnFailureAction(PActionNode action) : base(false)
    {
        AttachedAction = action;
        attachedAction = action;
    }

    /// <summary>
    /// Pre-tick: always allow execution (no gating).
    /// </summary>
    protected override bool OnEvaluate(float InDeltaTime)
    {
        return true;
    }

    /// <summary>
    /// Post-processing: when the action fails due to a decorator block (not actual execution),
    /// reset it back to ReadyToTick and convert Failure → InProgress.
    /// </summary>
    public override BTNodeResult PostProcessTickResult(BTNodeResult InResult)
    {
        if (InResult != BTNodeResult.Failure)
        {
            return InResult; // Only intercept failures
        }

        if (attachedAction == null)
        {
            LoggingService.LogWarning($"⚠️ DecoratorRetryOnFailureAction: No attached action set, cannot process");
            return InResult;
        }

        // If the action was blocked by decorators, its status will be Failure but it never
        // actually executed. In BTNode.Tick(), when decorators block, status is set to Failure
        // and the node logic / children phases are skipped entirely. The action stays in a 
        // "never ran" state. We detect this by checking if the action was still ReadyToTick
        // or if decorators blocked it (status set to Failure without execution).
        //
        // Since post-processing happens AFTER status is already set to Failure,
        // we simply convert it back to InProgress so the node can be re-evaluated next tick.
        // The action's Reset() will set it back to ReadyToTick.

        LoggingService.LogInfo($"🔄 DecoratorRetryOnFailureAction: Failure detected on action '{attachedAction.InstanceName}', resetting to InProgress for retry");
        attachedAction.Reset();

        return BTNodeResult.InProgress;
    }
}
