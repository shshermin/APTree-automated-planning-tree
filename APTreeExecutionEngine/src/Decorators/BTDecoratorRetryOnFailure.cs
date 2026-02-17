using BehaviorTreeMainProject.Log.Services;

/// <summary>
/// Post-processing decorator that converts Failure → InProgress on the flow node it is attached to.
/// That is its sole purpose. No resetting of children, no resetting of action graphs.
/// </summary>
public class BTDecoratorRetryOnFailure : BTDecoratorBase
{
    public override bool CanPostProcessTickResult => true;

    public BTDecoratorRetryOnFailure(BTFlowNodeDynamic attachedNode) : base(false)
    {
        AttachedNode = attachedNode;
    }

    protected override bool OnEvaluate(float InDeltaTime)
    {
        return true;
    }

    public override BTNodeResult PostProcessTickResult(BTNodeResult InResult)
    {
        if (InResult == BTNodeResult.Failure)
        {
            LoggingService.LogInfo($"🔄 BTDecoratorRetryOnFailure: Failure on {AttachedNode?.DebugDisplayName} → converting to InProgress");
            return BTNodeResult.InProgress;
        }
        return InResult;
    }
}
