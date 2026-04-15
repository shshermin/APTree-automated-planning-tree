using System;
using BehaviorTreeMainProject.Log.Services;

/// <summary>
/// Abstract base class for decorators that resolve missing parameters on ML actions.
/// Subclasses implement Resolve() to fill in parameters from different sources
/// (goal state, blackboard values, sensors, config files, etc.).
///
/// Resolution runs once before the first tick and never blocks execution.
/// </summary>
public abstract class DecoratorResolveParameters : Decorator
{
    public override bool CanPostProcessTickResult => false;
    private bool _resolved = false;

    protected DecoratorResolveParameters(PActionNode action) : base(false)
    {
        AttachedAction = action;
    }

    protected override bool OnEvaluate(float InDeltaTime)
    {
        if (_resolved)
            return true;

        if (AttachedAction == null)
            return true;

        try
        {
            Resolve(AttachedAction, LinkedBlackboard);
            _resolved = true;
        }
        catch (Exception ex)
        {
            LoggingService.LogWarning($"⚠️ {GetType().Name}: Error resolving parameters for {AttachedAction.InstanceName}: {ex.Message}");
        }

        return true; // Never block execution
    }

    public override BTNodeResult PostProcessTickResult(BTNodeResult InResult)
    {
        return InResult;
    }

    /// <summary>
    /// Fill in missing parameters on the given action using whatever data source this subclass provides.
    /// Called exactly once, before the first tick.
    /// </summary>
    protected abstract void Resolve(PActionNode action, Blackboard<FastName> blackboard);
}
