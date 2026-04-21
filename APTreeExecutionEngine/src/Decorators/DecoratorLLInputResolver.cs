using System;
using System.Collections.Generic;
using BehaviorTreeMainProject.Log.Services;

/// <summary>
/// Generic decorator for LL (ExeAction) nodes that resolves ML input objects
/// into the action's typed properties before the first tick.
///
/// Works with any LL action that implements <see cref="ILLInputBindable"/>.
/// The decorator iterates the resolved ML objects and calls BindInput() for each,
/// letting the LL action match by type internally.
///
/// Resolution runs once before the first tick and never blocks execution.
/// </summary>
public class DecoratorLLInputResolver : DecoratorResolveParameters
{
    private readonly Dictionary<string, object> _mlInputs;

    public DecoratorLLInputResolver(ExeAction action, Dictionary<string, object> mlInputs)
        : base(action)
    {
        _mlInputs = mlInputs ?? new Dictionary<string, object>();
    }

    protected override void Resolve(PActionNode action, Blackboard<FastName> blackboard)
    {
        if (action is not ILLInputBindable bindable)
        {
            LoggingService.LogWarning($"⚠️ DecoratorLLInputResolver: {action.InstanceName} does not implement ILLInputBindable, skipping");
            return;
        }

        if (_mlInputs.Count == 0)
        {
            LoggingService.LogInfo($"ℹ️ DecoratorLLInputResolver: No ML inputs for '{action.InstanceName}', skipping");
            return;
        }

        int bound = 0;
        foreach (var kv in _mlInputs)
        {
            if (kv.Value == null) continue;
            try
            {
                bindable.BindInput(kv.Value);
                bound++;
                LoggingService.LogInfo($"✅ DecoratorLLInputResolver: '{action.InstanceName}' received {kv.Value.GetType().Name} from ML param '{kv.Key}'");
            }
            catch (Exception ex)
            {
                LoggingService.LogWarning($"⚠️ DecoratorLLInputResolver: Error binding ML param '{kv.Key}' ({kv.Value.GetType().Name}) on '{action.InstanceName}': {ex.Message}");
            }
        }

        LoggingService.LogInfo($"✅ DecoratorLLInputResolver: Passed {bound} objects to '{action.InstanceName}'");
    }
}
