using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Transforms raw Pyperplan output into the APTree DSL NodeGraph format.
///
/// Pyperplan is a lightweight Python-based STRIPS planner intended for
/// educational use. It supports PDDL 1.2 STRIPS (no numeric fluents,
/// no durative actions). Use it only with purely classical domains.
///
/// Pyperplan writes the plan to a .soln file (cat'd to stdout by the Flask service):
///   (travelml r1 pr2 ep1)
///   (equipeml r1 vg1 ep1)
///   (initializeml r1 vg1)
///
/// Format is the same as Fast Downward sas_plan but without the "; cost = N" footer.
/// </summary>
public class PlannerPyperplan : Planner
{
    public override string[] PlannerNames => new[] { "PYPERPLAN" };

    // Pyperplan supports STRIPS only — use classical ML domain
    public override string DefaultDomainFile => "Plannerinputs/static/DomainML.pddl";
    public override string DefaultProblemFile => "Plannerinputs/static/problemC1.pddl";
    public override string DefaultPlannerPath => "pyperplan";
    public override string DefaultPlannerName => "PYPERPLAN";

    // Pyperplan is slower than native planners
    public override int DefaultTimeoutSeconds => 60;
    public override int DefaultMaxPlanLength => 30;

    /// <summary>
    /// Parses raw Pyperplan output.
    ///
    /// The .soln file contains one action per line in parenthesised format:
    ///   (travelml r1 pr2 ep1)
    ///
    /// Lines that are comments (;) or blank are skipped.
    /// </summary>
    protected override List<(string Name, string[] Parameters)> ParseRawOutput(string output)
    {
        var actions = new List<(string Name, string[] Parameters)>();
        var lines = output.Split('\n');

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
                    actions.Add((actionName, parameters));
                }
            }
        }

        return actions;
    }
}
