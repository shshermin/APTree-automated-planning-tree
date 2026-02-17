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
    public static class BTFileWriter
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
                LoggingService.LogWarning($"⚠️ BTFileWriter: .bt file not found at {filePath}");
                return;
            }

            if (string.IsNullOrWhiteSpace(nodeGraphDsl))
            {
                LoggingService.LogWarning($"⚠️ BTFileWriter: Empty NodeGraph DSL for {cassetteName}, skipping .bt update");
                return;
            }

            try
            {
                var content = File.ReadAllText(filePath);

                // Step 1: Find the FlowNode header for this cassette name
                var headerPattern = $@"FlowNode\s+{Regex.Escape(cassetteName)}\b";
                var headerMatch = Regex.Match(content, headerPattern, RegexOptions.IgnoreCase);

                if (!headerMatch.Success)
                {
                    LoggingService.LogWarning($"⚠️ BTFileWriter: Could not find FlowNode '{cassetteName}' in {filePath}");
                    return;
                }

                // Step 2: Find "NodeGraph" keyword after the FlowNode header
                int searchStart = headerMatch.Index + headerMatch.Length;
                var nodeGraphKeyword = Regex.Match(content.Substring(searchStart), @"\bNodeGraph\s*\{");

                if (!nodeGraphKeyword.Success)
                {
                    LoggingService.LogWarning($"⚠️ BTFileWriter: Could not find NodeGraph block for FlowNode '{cassetteName}' in {filePath}");
                    return;
                }

                // Absolute positions in the full content string
                int nodeGraphStart = searchStart + nodeGraphKeyword.Index; // start of "NodeGraph"
                int openBracePos = searchStart + nodeGraphKeyword.Index + nodeGraphKeyword.Length - 1; // the '{'

                // Step 3: Walk forward from the opening '{' to find the matching '}' (brace-balanced)
                int depth = 1;
                int pos = openBracePos + 1;
                while (pos < content.Length && depth > 0)
                {
                    if (content[pos] == '{') depth++;
                    else if (content[pos] == '}') depth--;
                    pos++;
                }

                if (depth != 0)
                {
                    LoggingService.LogWarning($"⚠️ BTFileWriter: Unbalanced braces in NodeGraph for FlowNode '{cassetteName}'");
                    return;
                }

                int nodeGraphEnd = pos; // one past the closing '}'

                // Step 4: Compute indentation from the NodeGraph keyword's line
                int lineStart = content.LastIndexOf('\n', nodeGraphStart);
                lineStart = lineStart < 0 ? 0 : lineStart + 1;
                string baseIndent = "";
                for (int i = lineStart; i < nodeGraphStart && char.IsWhiteSpace(content[i]); i++)
                    baseIndent += content[i];

                // Step 5: Replace the entire NodeGraph block with the new DSL.
                // Cut at lineStart (not nodeGraphStart) to avoid doubling the whitespace:
                // content.Substring(0, lineStart) ends right after the preceding '\n',
                // and IndentNodeGraph prepends baseIndent to the first line.
                var indented = IndentNodeGraph(nodeGraphDsl, baseIndent);
                var updated = content.Substring(0, lineStart) + indented + content.Substring(nodeGraphEnd);

                File.WriteAllText(filePath, updated);

                LoggingService.LogSuccess($"✅ BTFileWriter: Updated NodeGraph for FlowNode '{cassetteName}' in {Path.GetFileName(filePath)}");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"❌ BTFileWriter: Error updating .bt file for '{cassetteName}': {ex.Message}");
            }
        }

        /// <summary>
        /// Appends a new subtree BehaviorTree block after the main BT in the .bt file.
        /// Creates an empty subtree with a single FlowNode containing an empty NodeGraph,
        /// so the existing planner flow can later find and populate it.
        /// </summary>
        /// <param name="subtreeBTName">The BehaviorTree name, e.g. "PickUpHL_lp1_fp1_r1Subtree".</param>
        /// <param name="flowNodeName">The FlowNode name inside the subtree (must match the runtime DynamicFlowNode name).</param>
        /// <param name="plannerServiceName">The PlanningService instance name, e.g. "subtreeSrv_PickUpHL_lp1_fp1_r1".</param>
        /// <param name="plannerTypeName">The planner type reference, e.g. "Enhsp1".</param>
        /// <param name="btFilePath">Optional override for the .bt file path.</param>
        public static void AppendSubtreeBTModel(
            string subtreeBTName,
            string flowNodeName,
            string plannerServiceName,
            string plannerTypeName,
            string btFilePath = null)
        {
            var filePath = btFilePath ?? DefaultBtFilePath;

            if (!File.Exists(filePath))
            {
                LoggingService.LogWarning($"⚠️ BTFileWriter: .bt file not found at {filePath}");
                return;
            }

            try
            {
                var content = File.ReadAllText(filePath);

                // Check if this subtree BT already exists
                if (content.Contains($"BehaviorTree {subtreeBTName}"))
                {
                    LoggingService.LogInfo($"ℹ️ BTFileWriter: Subtree BT '{subtreeBTName}' already exists, skipping");
                    return;
                }

                // Build the subtree BT model following the APTree grammar:
                //   APTree = "BehaviorTree" name "{" "Root" root:FlowNode "}";
                //   DynamicFlowNode = "FlowNode" name "{" (Service)* SuccessCriteria ChildType NodeGraph "}";
                var sb = new System.Text.StringBuilder();
                sb.AppendLine();
                sb.AppendLine($"BehaviorTree {subtreeBTName} {{");
                sb.AppendLine($"    Root FlowNode {flowNodeName} {{");
                sb.AppendLine($"        PlanningService {plannerServiceName} {plannerTypeName}");
                sb.AppendLine($"        All");
                sb.AppendLine($"        AllAction");
                sb.AppendLine($"        NodeGraph {{ }}");
                sb.AppendLine($"    }}");
                sb.AppendLine($"}}");

                // Append after the main BT (at end of file)
                content = content.TrimEnd() + Environment.NewLine + sb.ToString();

                File.WriteAllText(filePath, content);

                LoggingService.LogSuccess($"✅ BTFileWriter: Appended subtree BT '{subtreeBTName}' to {Path.GetFileName(filePath)}");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"❌ BTFileWriter: Error appending subtree BT '{subtreeBTName}': {ex.Message}");
            }
        }

        /// <summary>
        /// Adds a @subtreeAnnotation to an existing action line in the .bt file.
        /// Finds the action by its instance name and inserts @subtreeBTName after the
        /// closing parenthesis of its parameter list.
        /// </summary>
        /// <param name="actionInstanceName">The unique action instance name, e.g. "PickUpHL_lp1_fp1_r1".</param>
        /// <param name="subtreeBTName">The BehaviorTree name to annotate, e.g. "PickUpHL_lp1_fp1_r1Subtree".</param>
        /// <param name="btFilePath">Optional override for the .bt file path.</param>
        public static void AnnotateActionWithSubtree(
            string actionInstanceName,
            string subtreeBTName,
            string btFilePath = null)
        {
            var filePath = btFilePath ?? DefaultBtFilePath;

            if (!File.Exists(filePath))
            {
                LoggingService.LogWarning($"⚠️ BTFileWriter: .bt file not found at {filePath}");
                return;
            }

            try
            {
                var content = File.ReadAllText(filePath);

                // Check if annotation already present
                if (content.Contains($"@{subtreeBTName}"))
                {
                    LoggingService.LogInfo($"ℹ️ BTFileWriter: Action already annotated with @{subtreeBTName}, skipping");
                    return;
                }

                // Match: Action <Type> <instanceName> (<params>)
                // Capture everything up to and including the closing ')'
                var pattern = $@"(Action\s+\w+\s+{Regex.Escape(actionInstanceName)}\s*\([^)]*\))";
                var match = Regex.Match(content, pattern);

                if (!match.Success)
                {
                    LoggingService.LogWarning($"⚠️ BTFileWriter: Could not find action '{actionInstanceName}' in {Path.GetFileName(filePath)}");
                    return;
                }

                // Insert @subtreeBTName right after the matched action pattern
                var replacement = $"{match.Groups[1].Value} @{subtreeBTName}";
                content = content.Substring(0, match.Index)
                        + replacement
                        + content.Substring(match.Index + match.Length);

                File.WriteAllText(filePath, content);

                LoggingService.LogSuccess($"✅ BTFileWriter: Annotated action '{actionInstanceName}' with @{subtreeBTName}");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"❌ BTFileWriter: Error annotating action '{actionInstanceName}': {ex.Message}");
            }
        }

        /// <summary>
        /// Indents a NodeGraph DSL block so it sits at the correct depth inside the .bt file.
        /// Preserves the relative indentation from the planner DSL (e.g. Action lines indented
        /// 4 spaces deeper than NodeGraph, arrow lines 8 spaces deeper) while shifting the
        /// whole block to start at <paramref name="baseIndent"/>.
        /// </summary>
        private static string IndentNodeGraph(string nodeGraphDsl, string baseIndent)
        {
            var lines = nodeGraphDsl.TrimEnd().Split('\n');
            var result = new System.Text.StringBuilder();

            // Determine the minimum leading whitespace across non-empty lines
            // so we can strip it and replace with baseIndent.
            int minIndent = int.MaxValue;
            foreach (var rawLine in lines)
            {
                var line = rawLine.TrimEnd('\r');
                if (string.IsNullOrWhiteSpace(line)) continue;
                int leading = 0;
                while (leading < line.Length && char.IsWhiteSpace(line[leading])) leading++;
                if (leading < minIndent) minIndent = leading;
            }
            if (minIndent == int.MaxValue) minIndent = 0;

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].TrimEnd('\r');
                if (string.IsNullOrWhiteSpace(line)) continue;

                // Strip the common minimum indent and prepend baseIndent,
                // preserving each line's relative offset.
                string stripped = line.Length > minIndent ? line.Substring(minIndent) : line.TrimStart();
                result.AppendLine(baseIndent + stripped);
            }

            return result.ToString().TrimEnd('\r', '\n');
        }
    }
}
