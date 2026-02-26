using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

/// <summary>
/// Base class for all planner output transformers.
/// Subclasses implement ParseRawOutput to extract (Name, Parameters[]) tuples
/// from the planner-specific stdout format, and set their own default planning
/// configuration via the properties below.
/// </summary>
public abstract class Planner
{
    // ── Reflection-based action name cache (shared by all planners) ──

    private static Dictionary<string, string> _actionNameCache;
    private static readonly object _cacheLock = new object();

    /// <summary>
    /// Lazily built lookup: lowercase class name → actual class name,
    /// from all concrete PActionNode subclasses.
    /// </summary>
    protected static Dictionary<string, string> ActionNameCache
    {
        get
        {
            if (_actionNameCache == null)
            {
                lock (_cacheLock)
                {
                    if (_actionNameCache == null)
                    {
                        var assembly = typeof(PActionNode).Assembly;
                        _actionNameCache = assembly.GetTypes()
                            .Where(t => t.IsSubclassOf(typeof(PActionNode)) && !t.IsAbstract)
                            .ToDictionary(t => t.Name.ToLowerInvariant(), t => t.Name);
                    }
                }
            }
            return _actionNameCache;
        }
    }

    /// <summary>
    /// The name(s) this planner is known by (e.g. "ENHSP", "FF").
    /// Used by FromName to resolve the correct subclass at runtime via reflection.
    /// Return all accepted aliases in uppercase.
    /// </summary>
    public abstract string[] PlannerNames { get; }

    // ── Default planning configuration (each subclass sets its own values) ──

    /// <summary>Path to the PDDL domain file used by this planner.</summary>
    public abstract string DefaultDomainFile { get; }

    /// <summary>Fallback PDDL problem file (overridden at runtime by generated problems).</summary>
    public abstract string DefaultProblemFile { get; }

    /// <summary>Command or path to invoke the planner (e.g. "ff", "lama-first", path to jar).</summary>
    public abstract string DefaultPlannerPath { get; }

    /// <summary>Canonical planner name passed to PDDLPlanningRequest (e.g. "FF", "ENHSP").</summary>
    public abstract string DefaultPlannerName { get; }

    /// <summary>Maximum seconds to wait for a plan.</summary>
    public virtual int DefaultTimeoutSeconds => 30;

    /// <summary>Maximum number of actions in a plan.</summary>
    public virtual int DefaultMaxPlanLength => 20;

    /// <summary>
    /// Optional ENHSP -planner config name (e.g. "opt-hmax", "opt-blind").
    /// Null means use the Flask default ("pt-blind").
    /// Only relevant when PlannerName == "ENHSP".
    /// </summary>
    public virtual string DefaultEnhspConfig => null;

    /// <summary>True when a specific ENHSP config should be sent to Flask.</summary>
    public bool HasEnhspConfig => !string.IsNullOrWhiteSpace(DefaultEnhspConfig);

    // ── Public API ──

    /// <summary>
    /// Transforms raw planner stdout into the APTree DSL NodeGraph format.
    /// Template method: calls ParseRawOutput (planner-specific) then ConvertToAPTreePlanString (shared).
    /// </summary>
    public virtual string TransformToAPTreeModel(string plannerOutput)
    {
        if (string.IsNullOrEmpty(plannerOutput))
            return string.Empty;

        var actions = ParseRawOutput(plannerOutput);

        if (actions.Count == 0)
            return string.Empty;

        return ConvertToAPTreePlanString(actions);
    }

    /// <summary>
    /// Planner-specific: parse raw stdout into an ordered action list.
    /// </summary>
    protected abstract List<(string Name, string[] Parameters)> ParseRawOutput(string output);

    // ── Shared helpers ──

    /// <summary>
    /// Converts a parsed action list into the APTree DSL NodeGraph format.
    /// </summary>
    protected string ConvertToAPTreePlanString(List<(string Name, string[] Parameters)> actions)
    {
        var sb = new StringBuilder();
        var entries = new List<(string Type, string InstanceName, string[] Params)>();

        foreach (var action in actions)
        {
            var type = NormalizeActionName(action.Name);
            var paramSuffix = string.Join("_", action.Parameters);
            var instanceName = string.IsNullOrEmpty(paramSuffix) ? type : $"{type}_{paramSuffix}";
            entries.Add((type, instanceName, action.Parameters));
        }

        sb.AppendLine("NodeGraph {");

        for (int i = 0; i < entries.Count; i++)
        {
            var (type, name, parms) = entries[i];
            var paramList = string.Join(" ", parms);

            if (i < entries.Count - 1)
            {
                sb.AppendLine($"    Action {type} {name} ({paramList}) {{");
                sb.AppendLine($"        --[Meets]--> {entries[i + 1].InstanceName};");
                sb.AppendLine($"    }}");
            }
            else
            {
                sb.AppendLine($"    Action {type} {name} ({paramList})");
            }
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    /// <summary>
    /// Normalizes a PDDL action name (any casing) to the actual C# class name
    /// via reflection lookup; falls back to suffix-aware casing.
    /// </summary>
    protected string NormalizeActionName(string actionName)
    {
        var lower = actionName.ToLowerInvariant();

        if (ActionNameCache.TryGetValue(lower, out var className))
            return className;

        foreach (var suffix in new[] { "hl", "ml", "ll" })
        {
            if (lower.EndsWith(suffix))
            {
                var prefix = lower.Substring(0, lower.Length - 2);
                return char.ToUpper(prefix[0]) + prefix.Substring(1) + suffix.ToUpper();
            }
        }

        return char.ToUpper(lower[0]) + lower.Substring(1);
    }

    // ── Planner registry (reflection-based, auto-discovers all subclasses) ──

    private static Dictionary<string, Type> _plannerRegistry;
    private static readonly object _registryLock = new object();

    private static Dictionary<string, Type> PlannerRegistry
    {
        get
        {
            if (_plannerRegistry == null)
            {
                lock (_registryLock)
                {
                    if (_plannerRegistry == null)
                    {
                        _plannerRegistry = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
                        var assembly = typeof(Planner).Assembly;
                        foreach (var type in assembly.GetTypes()
                            .Where(t => t.IsSubclassOf(typeof(Planner)) && !t.IsAbstract))
                        {
                            var instance = (Planner)Activator.CreateInstance(type);
                            foreach (var name in instance.PlannerNames)
                            {
                                _plannerRegistry[name] = type;
                            }
                        }
                    }
                }
            }
            return _plannerRegistry;
        }
    }

    /// <summary>
    /// Resolves the correct Planner subclass by planner name (e.g. "ENHSP", "FF", "LAMA-FIRST").
    /// New Planner subclasses are discovered automatically via reflection — no manual registration needed.
    /// </summary>
    public static Planner FromName(string plannerName)
    {
        if (plannerName == null)
            throw new ArgumentException("Planner name cannot be null.");

        if (PlannerRegistry.TryGetValue(plannerName, out var type))
            return (Planner)Activator.CreateInstance(type);

        var known = string.Join(", ", PlannerRegistry.Keys.OrderBy(k => k));
        throw new ArgumentException($"Unknown planner: '{plannerName}'. Known planners: {known}");
    }
}