using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// Transforms raw TFD (Temporal Fast Downward) planner output into the APTree DSL NodeGraph format.
///
/// TFD raw output lines (temporal format with timestamps and durations):
///   0.000: (travelml r1 pr2 ep1) [5.000]
///   5.000: (equipeml r1 vg1 ep1) [2.000]
///   7.000: (initializeml r1 vg1) [3.000]
///
/// Or sas_plan format (parenthesized actions without timestamps):
///   (travelml r1 pr2 ep1)
///   (equipeml r1 vg1 ep1)
///   ; cost = 10 (unit cost)
///
/// Temporal information is ignored — actions are extracted in order and treated sequentially.
/// </summary>
public class PlannerTFD : Planner
{
    public override string[] PlannerNames => new[] { "TFD" };

    public override string DefaultDomainFile => "Plannerinputs/static/DomainML.pddl";
    public override string DefaultProblemFile => "Plannerinputs/static/problemC1.pddl";
    public override string DefaultPlannerPath => "tfd";
    public override string DefaultPlannerName => "TFD";

    // Regex for temporal format: "0.000: (action p1 p2) [5.000]"
    private static readonly Regex TemporalLineRegex = new Regex(
        @"^\s*[\d.]+\s*:\s*\(([^)]+)\)\s*\[[\d.]+\]",
        RegexOptions.Compiled);

    /// <summary>
    /// Parses raw TFD output to extract ordered actions.
    /// Tries temporal format first (timestamp lines), then falls back to sas_plan format.
    /// Temporal ordering is ignored — actions are returned in the order they appear.
    /// </summary>
    protected override List<(string Name, string[] Parameters)> ParseRawOutput(string output)
    {
        var lines = output.Split('\n');

        // ── Primary: temporal format  "0.000: (action p1 p2) [5.000]" ──────
        var temporalActions = new List<(double Timestamp, string Name, string[] Parameters)>();
        foreach (var rawLine in lines)
        {
            var match = TemporalLineRegex.Match(rawLine);
            if (match.Success)
            {
                var inner = match.Groups[1].Value.Trim();
                var parts = inner.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 1)
                {
                    var actionName = parts[0].ToLowerInvariant();
                    var parameters = parts.Skip(1).Select(p => p.ToLowerInvariant()).ToArray();

                    // Extract timestamp for sorting
                    var timestampStr = rawLine.TrimStart().Split(':')[0].Trim();
                    double.TryParse(timestampStr, System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out double timestamp);

                    temporalActions.Add((timestamp, actionName, parameters));
                }
            }
        }

        if (temporalActions.Count > 0)
        {
            // Sort by timestamp to ensure sequential ordering
            return temporalActions
                .OrderBy(a => a.Timestamp)
                .Select(a => (a.Name, a.Parameters))
                .ToList();
        }

        // ── Fallback: sas_plan format "(action p1 p2)" ─────────────────────
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

        return sasActions;
    }
}
