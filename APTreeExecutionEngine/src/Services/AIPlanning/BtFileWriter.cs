using System;
using System.IO;
using System.Text.RegularExpressions;
using BehaviorTreeMainProject.Log.Services;

namespace BehaviorTreeMainProject.Services.AIPlanning
{
    /// <summary>
    /// Writes generated planner NodeGraph DSL strings back into the APTreeLivematFinal.bt file,
    /// replacing the empty "NodeGraph { }" placeholder for the matching cassette FlowNode.
    /// </summary>
    public static class BtFileWriter
    {
        /// <summary>
        /// Default path to the .bt file.
        /// </summary>
        private static readonly string DefaultBtFilePath = Path.GetFullPath(
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
            "..", "..", "..", "..", "APTreeDSL", "src", "test", "resources",
            "valid", "behavior_trees", "APTreeLivematFinal.bt"));

        /// <summary>
        /// Replaces the empty "NodeGraph { }" inside the FlowNode matching <paramref name="cassetteName"/>
        /// with the generated <paramref name="nodeGraphDsl"/> string.
        /// The match is case-insensitive.
        /// </summary>
        /// <param name="cassetteName">
        /// The FlowNode name, e.g. "cassette1" or "Cassette1".
        /// </param>
        /// <param name="nodeGraphDsl">
        /// The full DSL NodeGraph block produced by Planner.TransformToAPTreeModel,
        /// e.g. "NodeGraph {\n    Action TravelML ...\n}".
        /// </param>
        /// <param name="btFilePath">
        /// Optional override for the .bt file path. Uses DefaultBtFilePath when null.
        /// </param>
        public static void UpdateCassetteNodeGraph(string cassetteName, string nodeGraphDsl, string btFilePath = null)
        {
            var filePath = btFilePath ?? DefaultBtFilePath;

            if (!File.Exists(filePath))
            {
                LoggingService.LogWarning($"⚠️ BtFileWriter: .bt file not found at {filePath}");
                return;
            }

            if (string.IsNullOrWhiteSpace(nodeGraphDsl))
            {
                LoggingService.LogWarning($"⚠️ BtFileWriter: Empty NodeGraph DSL for {cassetteName}, skipping .bt update");
                return;
            }

            try
            {
                var content = File.ReadAllText(filePath);

                // Pattern: find the FlowNode block for this cassette name (case-insensitive),
                // then match the "NodeGraph { }" or "NodeGraph {<whitespace>}" inside it.
                // We look for:  FlowNode  <CassetteName> ... NodeGraph { }
                // The regex captures everything from "FlowNode  CassetteName" up to and including
                // the first "NodeGraph" keyword, then expects "{ }" (possibly with whitespace).
                var pattern = $@"(FlowNode\s+{Regex.Escape(cassetteName)}\s*\{{[^}}]*?)NodeGraph\s*\{{\s*\}}";

                var match = Regex.Match(content, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);

                if (!match.Success)
                {
                    LoggingService.LogWarning($"⚠️ BtFileWriter: Could not find empty NodeGraph for FlowNode '{cassetteName}' in {filePath}");
                    return;
                }

                // Indent the generated NodeGraph to match the .bt file's indentation (4 levels = 16 spaces)
                var indented = IndentNodeGraph(nodeGraphDsl, "                ");

                var replacement = match.Groups[1].Value + indented;
                var updated = content.Substring(0, match.Index) + replacement + content.Substring(match.Index + match.Length);

                File.WriteAllText(filePath, updated);

                LoggingService.LogSuccess($"✅ BtFileWriter: Updated NodeGraph for FlowNode '{cassetteName}' in APTreeLivematFinal.bt");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"❌ BtFileWriter: Error updating .bt file for '{cassetteName}': {ex.Message}");
            }
        }

        /// <summary>
        /// Indents a NodeGraph DSL block so it sits at the correct depth inside the .bt file.
        /// The first line ("NodeGraph {") gets the base indent; inner lines get base + 4 spaces.
        /// </summary>
        private static string IndentNodeGraph(string nodeGraphDsl, string baseIndent)
        {
            var lines = nodeGraphDsl.TrimEnd().Split('\n');
            var result = new System.Text.StringBuilder();

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].TrimEnd('\r');

                if (i == 0)
                {
                    // "NodeGraph {" — use base indent
                    result.AppendLine(baseIndent + line.TrimStart());
                }
                else
                {
                    // Inner lines and closing "}" — add base indent + preserve relative indent
                    result.AppendLine(baseIndent + line.TrimStart());
                }
            }

            return result.ToString().TrimEnd('\r', '\n');
        }
    }
}
