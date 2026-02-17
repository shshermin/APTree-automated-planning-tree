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
    /// Parses raw LAMA-First stdout to extract ordered actions.
    /// Action lines look like: "travelml r1 pr3 fp9 (1)"
    /// where the trailing " (N)" is the cost — everything before it is the action.
    /// </summary>
    protected override List<(string Name, string[] Parameters)> ParseRawOutput(string output)
    {
        var actions = new List<(string Name, string[] Parameters)>();
        var lines = output.Split('\n');

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (string.IsNullOrEmpty(line))
                continue;

            // Skip known diagnostic prefixes
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

            // Action lines end with " (cost)" e.g. " (1)"
            if (!line.Contains('(') || !line.Contains(')'))
                continue;

            // Strip the trailing cost: everything after the last " ("
            int costStart = line.LastIndexOf(" (");
            if (costStart == -1)
                continue;

            var actionPart = line.Substring(0, costStart).Trim();
            var parts = actionPart.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 1)
                continue;

            // LAMA outputs lowercase already, but normalize just in case
            var actionName = parts[0].ToLowerInvariant();
            var parameters = parts.Skip(1).Select(p => p.ToLowerInvariant()).ToArray();

            actions.Add((actionName, parameters));
        }

        return actions;
    }
}