using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Transforms raw Fast Downward planner output into the APTree DSL NodeGraph format.
///
/// Fast Downward is a highly configurable classical planner that supports both
/// optimal and satisficing planning depending on the search algorithm used.
/// It writes its plan to sas_plan in the standard format:
///   (travelml r1 pr2 ep1)
///   (equipeml r1 vg1 ep1)
///   ; cost = 5 (unit cost)
///
/// Common search configurations (passed as EnhspConfig field in requests):
///   "astar-lmcut"    → optimal:     --search "astar(lmcut())"
///   "astar-blind"    → optimal:     --search "astar(blind())"
///   "astar-ipdb"     → optimal:     --search "astar(ipdb())"
///   "lazy-ff"        → satisficing: --search "lazy_greedy([ff()])"
///   "lazy-cea"       → satisficing: --search "lazy_greedy([cea()])"
///   (null/default)   → satisficing: lazy_greedy([ff(), cea()])
/// </summary>
public class PlannerFastDownward : Planner
{
    public override string[] PlannerNames => new[] { "DOWNWARD", "FAST-DOWNWARD", "FD" };

    public override string DefaultDomainFile => "Plannerinputs/static/DomainML.pddl";
    public override string DefaultProblemFile => "Plannerinputs/static/problemC1.pddl";
    public override string DefaultPlannerPath => "downward";
    public override string DefaultPlannerName => "DOWNWARD";

    public override int DefaultTimeoutSeconds => 60;
    public override int DefaultMaxPlanLength => 40;

    // Lines starting with these prefixes are diagnostic output, not action lines
    private static readonly string[] SkipPrefixes = new[]
    {
        "[", "INFO", "Solution found", "Peak memory", "peak memory",
        "Remove intermediate", "search exit code", "time", "g=",
        "New best", "Initial", "Conducting", "Plan length", "Plan cost",
        "Expanded", "Reopened", "Evaluated", "Evaluations", "Generated",
        "Dead ends", "Number of registered", "Int hash", "Search time",
        "Total time", "reading input", "done reading", "Initializing",
        "Generating", "Building", "Variables", "FactPairs",
        "Bytes per state", "Simplifying", "time to simplify",
        "time for successor", "Landmarks generation", "Discovered",
        "edges", "approx. reasonable", "Landmark graph",
        "Landmark graph contains", "Landmark graph generation",
        "Actual search", "translate", "Writing", "Parsing", "Removing"
    };

    /// <summary>
    /// Parses raw Fast Downward output (sas_plan content):
    ///   (travelml r1 pr2 ep1)
    ///   ; cost = 5 (unit cost)
    /// </summary>
    protected override List<(string Name, string[] Parameters)> ParseRawOutput(string output)
    {
        var lines = output.Split('\n');

        // ── Primary: sas_plan format "(actionname p1 p2 ...)" ────────────────
        var sasActions = new List<(string Name, string[] Parameters)>();
        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (string.IsNullOrEmpty(line)) continue;
            if (line.StartsWith(";")) continue;

            if (line.StartsWith("(") && line.EndsWith(")"))
            {
                var inner = line.Substring(1, line.Length - 2).Trim();
                var parts = inner.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 1)
                {
                    var actionName = parts[0].ToLowerInvariant();
                    var parameters = parts.Skip(1).Select(p => p.ToLowerInvariant()).ToArray();
                    sasActions.Add((actionName, parameters));
                }
            }
        }

        if (sasActions.Count > 0)
            return sasActions;

        // ── Fallback: diagnostic search output with inline cost "(N)" ────────
        var actions = new List<(string Name, string[] Parameters)>();
        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (string.IsNullOrEmpty(line)) continue;

            bool skip = false;
            foreach (var prefix in SkipPrefixes)
            {
                if (line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    skip = true;
                    break;
                }
            }
            if (skip) continue;

            if (!line.Contains('(') || !line.Contains(')'))
                continue;

            int costStart = line.LastIndexOf(" (");
            if (costStart == -1) continue;

            var actionPart = line.Substring(0, costStart).Trim();
            var parts = actionPart.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 1) continue;

            actions.Add((parts[0].ToLowerInvariant(), parts.Skip(1).Select(p => p.ToLowerInvariant()).ToArray()));
        }

        return actions;
    }
}
