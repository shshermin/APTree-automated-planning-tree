using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Transforms raw Scorpion planner output into the APTree DSL NodeGraph format.
///
/// Scorpion is an optimal classical planner built on Fast Downward.
/// It writes its plan to sas_plan in the standard Fast Downward format:
///   (travelml r1 pr2 ep1)
///   (equipeml r1 vg1 ep1)
///   ; cost = 5 (unit cost)
///
/// Output format is identical to LAMA-first — only the invocation differs.
/// Use domain/problem files with STRIPS/ADL (no :numeric-fluents or :durative-actions).
/// </summary>
public class PlannerScorpion : Planner
{
    public override string[] PlannerNames => new[] { "SCORPION" };

    public override string DefaultDomainFile => "Plannerinputs/static/DomainML.pddl";
    public override string DefaultProblemFile => "Plannerinputs/static/problemC1.pddl";
    public override string DefaultPlannerPath => "scorpion";
    public override string DefaultPlannerName => "SCORPION";

    // Scorpion is optimal — allow longer plans since it guarantees minimum cost
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
        "Actual search", "Saturated cost", "heuristic"
    };

    /// <summary>
    /// Parses raw Scorpion (Fast Downward) output.
    /// Primary format (sas_plan file content):
    ///   (travelml r1 pr2 ep1)
    ///   ; cost = 5 (unit cost)
    /// </summary>
    protected override List<(string Name, string[] Parameters)> ParseRawOutput(string output)
    {
        var lines = output.Split('\n');

        // ── Primary: sas_plan format  "(actionname p1 p2 ...)" ──────────────
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
