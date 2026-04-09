using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Transforms raw OPTIC planner output into the APTree DSL NodeGraph format.
///
/// OPTIC is a temporal planner for PDDL 2.1 problems with:
///   - :durative-actions
///   - :numeric-fluents (continuous and discrete)
///   - :preferences
///   - :timed-initial-literals
///
/// OPTIC raw output (printed to stdout):
///   0.000: (travelml r1 pr2 ep1) [1.000]
///   1.000: (equipeml r1 vg1 ep1) [2.000]
///
/// The timestamp is the action start time; [duration] is the action duration.
/// Actions are sorted by start time. Durations are stripped for APTree output.
/// </summary>
public class PlannerOPTIC : Planner
{
    public override string[] PlannerNames => new[] { "OPTIC" };

    // OPTIC handles :numeric-fluents and :durative-actions → use HL domain
    public override string DefaultDomainFile => "Plannerinputs/static/DomainHL.pddl";
    public override string DefaultProblemFile => "Plannerinputs/static/problemC1.pddl";
    public override string DefaultPlannerPath => "optic";
    public override string DefaultPlannerName => "OPTIC";

    public override int DefaultTimeoutSeconds => 60;
    public override int DefaultMaxPlanLength => 40;

    /// <summary>
    /// Parses raw OPTIC stdout to extract ordered actions.
    ///
    /// Expected line format:  "N.NNN: (actionname p1 p2) [duration]"
    ///
    /// Lines not matching this pattern (diagnostics, blank, comments) are skipped.
    /// Actions are returned in order of appearance (OPTIC outputs them sorted by start time).
    /// </summary>
    protected override List<(string Name, string[] Parameters)> ParseRawOutput(string output)
    {
        var actions = new List<(string Name, string[] Parameters)>();
        var lines = output.Split('\n');

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (string.IsNullOrEmpty(line)) continue;

            // Temporal plan lines: "N.NNN: (action p1 p2) [duration]"
            // Must contain a colon, opening/closing parentheses, and a duration bracket
            if (!line.Contains(':') || !line.Contains('(') || !line.Contains(')'))
                continue;

            int colonIndex = line.IndexOf(':');
            if (colonIndex == -1) continue;

            // The part before the colon must be a number (timestamp)
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

            actions.Add((parts[0].ToLowerInvariant(), parts.Skip(1).Select(p => p.ToLowerInvariant()).ToArray()));
        }

        return actions;
    }
}
