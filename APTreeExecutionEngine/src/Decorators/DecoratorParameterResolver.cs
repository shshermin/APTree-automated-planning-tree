using System;
using System.Linq;
using System.Reflection;
using BehaviorTreeMainProject.Log.Services;
using ModelLoader;
using ModelLoader.ParameterTypes;
using ModelLoader.PredicateTypes;

/// <summary>
/// Resolves null Location-typed parameters on ML actions by matching
/// the action's effect predicates against the goal state predicates on the blackboard.
///
/// Example: NailingML has obj1=Stick6, obj2=Stick1, nailloc=null.
/// Its effect contains Nailed(Stick6, Stick1, null).
/// The goal state has Nailed(Stick6, Stick1, nailoc1).
/// This decorator finds the match and sets NailingML.nailloc = nailoc1.
///
/// Works generically for any ML action that has null Location properties
/// whose values can be inferred from matching goal state predicates.
/// </summary>
public class DecoratorParameterResolver : DecoratorResolveParameters
{
    public DecoratorParameterResolver(PActionNode action) : base(action)
    {
    }

    protected override void Resolve(PActionNode action, Blackboard<FastName> blackboard)
    {
        ResolveNullLocationParameters(action, blackboard);
    }

    /// <summary>
    /// Static entry point so FactoryAction can resolve null Location parameters
    /// at action birth without needing a decorator instance.
    /// </summary>
    public static void ResolveNullLocationParameters(PActionNode action, Blackboard<FastName> blackboard)
    {
        var actionType = action.GetType();
        var actionName = action.InstanceName.ToString();

        // Find writable Location-typed properties that are currently null
        var nullLocationProps = actionType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(p => typeof(Location).IsAssignableFrom(p.PropertyType)
                        && p.CanWrite
                        && p.GetValue(action) == null)
            .ToList();

        if (nullLocationProps.Count == 0)
            return;

        LoggingService.LogInfo($"🔍 DecoratorParameterResolver: Action '{actionName}' has {nullLocationProps.Count} null Location parameter(s) to resolve");

        // Get the action's effect predicates
        var effects = action.GetEffects();
        if (effects == null)
            return;

        var goalPredicates = blackboard.GetGoalStatePredicates();
        if (goalPredicates == null || goalPredicates.Count == 0)
        {
            LoggingService.LogWarning($"⚠️ DecoratorParameterResolver: No goal state predicates available");
            return;
        }

        // For each effect predicate, find a matching goal state predicate
        foreach (var effectPredicate in effects.GetAllPredicates())
        {
            // Find goal predicates of the same type
            var matchingGoalPredicates = goalPredicates
                .Where(gp => gp.GetType() == effectPredicate.GetType())
                .ToList();

            foreach (var goalPredicate in matchingGoalPredicates)
            {
                if (TryMatchAndResolve(action, effectPredicate, goalPredicate, nullLocationProps))
                {
                    // Re-check which props are still null
                    nullLocationProps = nullLocationProps
                        .Where(p => p.GetValue(action) == null)
                        .ToList();

                    if (nullLocationProps.Count == 0)
                        return; // All resolved
                }
            }
        }

        // Log any remaining unresolved parameters
        foreach (var prop in nullLocationProps)
        {
            LoggingService.LogWarning($"⚠️ DecoratorParameterResolver: Could not resolve '{prop.Name}' on action '{actionName}'");
        }
    }

    /// <summary>
    /// Checks if a goal predicate matches an effect predicate on all non-null non-Location
    /// parameters (e.g., obj1, obj2). If they match, copies any non-null Location values
    /// from the goal predicate to the corresponding null properties on the action.
    /// </summary>
    private static bool TryMatchAndResolve(PActionNode action, Predicate effectPred, Predicate goalPred, System.Collections.Generic.List<PropertyInfo> nullProps)
    {
        var predType = effectPred.GetType();
        var predProperties = predType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        // First pass: verify all non-Location properties match between effect and goal
        foreach (var prop in predProperties)
        {
            if (typeof(Location).IsAssignableFrom(prop.PropertyType))
                continue; // Skip Location fields — these are what we want to resolve

            var effectVal = prop.GetValue(effectPred);
            var goalVal = prop.GetValue(goalPred);

            if (effectVal == null && goalVal == null)
                continue;

            if (effectVal == null || goalVal == null)
                return false;

            // Compare by NameKey for CustomProperty types
            if (effectVal is CustomProperty cpEffect && goalVal is CustomProperty cpGoal)
            {
                if (cpEffect.NameKey?.ToString()?.ToLowerInvariant() != cpGoal.NameKey?.ToString()?.ToLowerInvariant())
                    return false;
            }
            else if (!effectVal.Equals(goalVal))
            {
                return false;
            }
        }

        // Second pass: copy non-null Location values from goal to the action
        bool anyResolved = false;
        foreach (var prop in predProperties)
        {
            if (!typeof(Location).IsAssignableFrom(prop.PropertyType))
                continue;

            var goalVal = prop.GetValue(goalPred) as Location;
            if (goalVal == null)
                continue;

            // Find the corresponding null property on the action with the same name
            var actionProp = nullProps.FirstOrDefault(p =>
                p.Name.Equals(prop.Name, StringComparison.OrdinalIgnoreCase));

            if (actionProp != null)
            {
                actionProp.SetValue(action, goalVal);
                LoggingService.LogSuccess(
                    $"✅ DecoratorParameterResolver: Resolved '{actionProp.Name}' on '{action.InstanceName}' → '{goalVal.ID}' " +
                    $"(from goal predicate {goalPred.PredicateName})");
                anyResolved = true;
            }
        }

        return anyResolved;
    }
}
