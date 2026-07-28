using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// Transforms raw Metric-FF planner output into the APTree DSL NodeGraph format.
///
/// FF raw output lines (UPPERCASE, no parentheses):
///   step    0: TRAVELML R1 PR2 EP1
///            1: EQUIPEML R1 VG1 EP1
///            2: INITIALIZEML R1 VG1
///   or simply:
///   0: TRAVELML R1 PR2 EP1
/// </summary>
public class PlannerMetricFF : Planner
{
    public override string[] PlannerNames => new[] { "FF", "METRIC-FF" };

    public override string DefaultDomainFile => "Plannerinputs/static/DomainML.pddl";
    public override string DefaultProblemFile => "Plannerinputs/static/bigproblem.pddl";
    public override string DefaultPlannerPath => "ff";
    public override string DefaultPlannerName => "FF";

    /// <summary>
    /// Parses raw FF stdout to extract ordered actions.
    /// Matches lines starting with "step N:" or just "N:" where N is a digit.
    /// Action name and parameters are UPPERCASE and space-separated after the colon.
    /// Parameters are lowercased to match blackboard entity names.
    /// </summary>
    protected override List<(string Name, string[] Parameters)> ParseRawOutput(string output)
    {
        var actions = new List<(string Name, string[] Parameters)>();
        var lines = output.Split('\n');

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();

            // FF action lines: "step    0: ACTION P1 P2" or "0: ACTION P1 P2"
            bool isStepLine = line.StartsWith("step ", StringComparison.OrdinalIgnoreCase);
            bool isNumberedLine = line.Length > 0 && char.IsDigit(line[0]) && line.Contains(':');

            if (!isStepLine && !isNumberedLine)
                continue;

            int colonIndex = line.IndexOf(':');
            if (colonIndex == -1)
                continue;

            var actionPart = line.Substring(colonIndex + 1).Trim();
            var parts = actionPart.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 1)
                continue;

            // FF outputs UPPERCASE — lowercase everything for consistency
            var actionName = parts[0].ToLowerInvariant();
            var parameters = parts.Skip(1).Select(p => p.ToLowerInvariant()).ToArray();

            actions.Add((actionName, parameters));
        }

        return SimplifyConsecutiveTravelActions(actions);
    }

    private static List<(string Name, string[] Parameters)> SimplifyConsecutiveTravelActions(
        List<(string Name, string[] Parameters)> actions)
    {
        var simplified = new List<(string Name, string[] Parameters)>();

        foreach (var action in actions)
        {
            if (action.Name == "travelml" && action.Parameters.Length == 3 && simplified.Count > 0)
            {
                var previous = simplified[^1];
                if (previous.Name == "travelml" && previous.Parameters.Length == 3 &&
                    previous.Parameters[0] == action.Parameters[0] &&
                    previous.Parameters[2] == action.Parameters[1])
                {
                    simplified[^1] = (previous.Name,
                        new[] { previous.Parameters[0], previous.Parameters[1], action.Parameters[2] });
                    continue;
                }
            }

            simplified.Add(action);
        }

        return simplified;
    }
}