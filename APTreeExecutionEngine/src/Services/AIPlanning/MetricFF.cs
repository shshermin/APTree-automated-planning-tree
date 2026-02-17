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
public class MetricFF : Planner
{
    public override string[] PlannerNames => new[] { "FF", "METRIC-FF" };

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

        return actions;
    }
}