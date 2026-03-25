using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Transforms raw POPF2 planner output into the APTree DSL NodeGraph format.
///
/// POPF2 (Partial Order Planning Forwards) is an anytime temporal planner for
/// PDDL 2.1 problems with:
///   - :durative-actions
///   - :numeric-fluents (including linear continuous effects)
///
/// POPF2 raw output (printed to stdout):
///   0.000: (travelml r1 pr2 ep1) [1.000]
///   1.000: (equipeml r1 vg1 ep1) [2.000]
///
/// Output format is identical to OPTIC (timestamp + parenthesised action + duration).
/// POPF2 may print multiple improving plans; we use the last complete plan found.
/// </summary>
public class PlannerPOPF : Planner
{
    public override string[] PlannerNames => new[] { "POPF", "POPF2" };

    // POPF handles :numeric-fluents and :durative-actions → use HL domain
    public override string DefaultDomainFile => "Plannerinputs/static/DomainHL.pddl";
    public override string DefaultProblemFile => "Plannerinputs/static/problemC1.pddl";
    public override string DefaultPlannerPath => "popf";
    public override string DefaultPlannerName => "POPF";

    public override int DefaultTimeoutSeconds => 60;
    public override int DefaultMaxPlanLength => 40;

    /// <summary>
    /// Parses raw POPF2 stdout.
    ///
    /// Expected line format:  "N.NNN: (actionname p1 p2) [duration]"
    ///
    /// POPF2 may output multiple improving plans separated by blank lines or
    /// solution-found headers. We collect all action lines and, if multiple
    /// complete plans are present, return the last one (best quality found).
    /// </summary>
    protected override List<(string Name, string[] Parameters)> ParseRawOutput(string output)
    {
        var allPlans = new List<List<(string Name, string[] Parameters)>>();
        var currentPlan = new List<(string Name, string[] Parameters)>();
        var lines = output.Split('\n');

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();

            // Blank line or solution header → end of a plan block
            if (string.IsNullOrEmpty(line) ||
                line.StartsWith(";", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("Solution", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith(";;;", StringComparison.OrdinalIgnoreCase))
            {
                if (currentPlan.Count > 0)
                {
                    allPlans.Add(new List<(string, string[])>(currentPlan));
                    currentPlan.Clear();
                }
                continue;
            }

            // Temporal plan lines: "N.NNN: (action p1 p2) [duration]"
            if (!line.Contains(':') || !line.Contains('(') || !line.Contains(')'))
                continue;

            int colonIndex = line.IndexOf(':');
            if (colonIndex == -1) continue;

            var timestampPart = line.Substring(0, colonIndex).Trim();
            if (!double.TryParse(timestampPart, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out _))
                continue;

            var actionPart = line.Substring(colonIndex + 1).Trim();

            // Strip optional duration "[N.NNN]" at the end
            if (actionPart.EndsWith("]"))
            {
                int bracketStart = actionPart.LastIndexOf('[');
                if (bracketStart > 0)
                    actionPart = actionPart.Substring(0, bracketStart).Trim();
            }

            if (!actionPart.StartsWith("(") || !actionPart.EndsWith(")"))
                continue;

            var inner = actionPart.Substring(1, actionPart.Length - 2).Trim();
            var parts = inner.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 1) continue;

            currentPlan.Add((parts[0].ToLowerInvariant(), parts.Skip(1).Select(p => p.ToLowerInvariant()).ToArray()));
        }

        // Flush last plan block
        if (currentPlan.Count > 0)
            allPlans.Add(currentPlan);

        // Return the last (best) plan found, or empty if none
        return allPlans.Count > 0 ? allPlans[allPlans.Count - 1] : new List<(string, string[])>();
    }
}
