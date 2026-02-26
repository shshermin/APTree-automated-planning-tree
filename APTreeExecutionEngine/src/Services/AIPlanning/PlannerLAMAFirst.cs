using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Transforms raw LAMA-First planner output into the APTree DSL NodeGraph format.
///
/// LAMA-First raw output lines (lowercase, cost in parentheses at end):
///   travelml r1 pr3 fp9 (1)
///   pickupml lp2 fp9 r1 vg1 (1)
///   placeml lp2 pr3 r1 vg1 (1)
/// </summary>
public class PlannerLAMAFirst : Planner
{
    public override string[] PlannerNames => new[] { "LAMA-FIRST", "LAMA" };

    public override string DefaultDomainFile => "Plannerinputs/static/DomainML.pddl";
    public override string DefaultProblemFile => "Plannerinputs/static/problemC1.pddl";
    public override string DefaultPlannerPath => "lama-first";
    public override string DefaultPlannerName => "LAMA-FIRST";

    // Lines starting with these prefixes are diagnostic, not action lines
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
        "Actual search"
    };

    /// <summary>
    /// Parses raw LAMA-First output to extract ordered actions.
    ///
    /// Fast Downward writes its final plan to sas_plan in this format:
    ///   (travelml r1 pr2 ep1)
    ///   (equipeml r1 vg1 ep1)
    ///   (initializeml r1 vg1)
    ///   ; cost = 5 (unit cost)
    ///
    /// This is what `cat sas_plan` returns. The parser tries this format first.
    /// If no sas_plan lines are found it falls back to the inline cost format
    /// printed during the landmark search: "travelml r1 pr3 fp9 (1)"
    /// (that intermediate output is incomplete and should not be used).
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
            if (line.StartsWith(";")) continue; // sas_plan footer comment

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

        // ── Fallback: inline cost format  "travelml r1 pr3 fp9 (1)" ─────────
        // This is intermediate search output and may be an incomplete plan;
        // only used if sas_plan produced no parseable lines.
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

            var actionName = parts[0].ToLowerInvariant();
            var parameters = parts.Skip(1).Select(p => p.ToLowerInvariant()).ToArray();
            actions.Add((actionName, parameters));
        }

        return actions;
    }
}