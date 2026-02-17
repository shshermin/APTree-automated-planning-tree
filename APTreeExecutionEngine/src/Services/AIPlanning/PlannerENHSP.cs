using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Transforms raw ENHSP planner output into the APTree DSL NodeGraph format.
/// 
/// ENHSP raw output lines:  "0.0: (travelml r1 pr2 ep1)"
/// </summary>
public class PlannerENHSP : Planner
{
    public override string[] PlannerNames => new[] { "ENHSP" };

    public override string DefaultDomainFile => "Plannerinputs/static/domain.pddl";
    public override string DefaultProblemFile => "Plannerinputs/static/problemC1.pddl";
    public override string DefaultPlannerPath => "/home/shermin/ENHSP-Public/enhsp.jar";
    public override string DefaultPlannerName => "ENHSP";
    public override int DefaultMaxPlanLength => 40;

    /// <summary>
    /// Parses raw ENHSP stdout to extract ordered actions.
    /// Expected line format: "0.0: (travelml r1 pr2 ep1)"
    /// </summary>
    protected override List<(string Name, string[] Parameters)> ParseRawOutput(string output)
    {
        var actions = new List<(string Name, string[] Parameters)>();
        var lines = output.Split('\n');

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();

            // ENHSP action lines: timestamp, colon, parenthesized action
            if (!line.Contains(':') || !line.Contains('(') || !line.EndsWith(')'))
                continue;

            int colonIndex = line.IndexOf(':');
            if (colonIndex == -1)
                continue;

            var actionPart = line.Substring(colonIndex + 1).Trim();

            if (!actionPart.StartsWith("(") || !actionPart.EndsWith(")"))
                continue;

            // Remove parentheses
            var actionStr = actionPart.Substring(1, actionPart.Length - 2);
            var parts = actionStr.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 1)
                continue;

            actions.Add((parts[0], parts.Skip(1).ToArray()));
        }

        return actions;
    }
}