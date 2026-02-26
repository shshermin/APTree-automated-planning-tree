using System;
using BehaviorTreeMainProject.Log.Services;
using BehaviorTreeMainProject.Services;

/// <summary>
/// Post-processing decorator attached to HL actions.
/// When the action's subtree completes with Success, this decorator
/// resets the planning state (cassette flags + non-successful NodeGraphs)
/// so the next planning cycle starts fresh.
///
/// This replaces the former PActionNode.ResetPlanningStateAfterSuccess()
/// and keeps the reset policy outside of the core action logic.
/// </summary>
public class DecoratorResetOnSubtreeSuccess : Decorator
{
    public override bool CanPostProcessTickResult => true;

    public DecoratorResetOnSubtreeSuccess(PActionNode action) : base(false)
    {
        AttachedAction = action;
    }

    /// <summary>
    /// Pre-tick evaluation — always returns true (does not block execution).
    /// </summary>
    protected override bool OnEvaluate(float InDeltaTime)
    {
        return true;
    }

    /// <summary>
    /// After the action tick completes, if the result is Success and this is
    /// an HL action, reset the planning state for the next cycle.
    /// </summary>
    public override BTNodeResult PostProcessTickResult(BTNodeResult InResult)
    {
        if (InResult != BTNodeResult.Success)
            return InResult;

        if (AttachedAction == null || !AttachedAction.IsHighLevelAction)
            return InResult;

        try
        {
            LoggingService.LogInfo($"🔄 ResetOnSubtreeSuccess: HL action {AttachedAction.InstanceName} succeeded — resetting planning state");

            var subtreeInjectionService = AttachedAction.ServiceSubtreeInject;
            if (subtreeInjectionService != null)
            {
                subtreeInjectionService.resetAfterSuccessFullExecution();
            }
            else
            {
                LoggingService.LogWarning($"⚠️ ResetOnSubtreeSuccess: ServiceSubtreeInject not found for {AttachedAction.InstanceName}");
            }
        }
        catch (Exception ex)
        {
            LoggingService.LogError($"❌ ResetOnSubtreeSuccess: Error during planning state reset: {ex.Message}");
        }

        return InResult;
    }
}
