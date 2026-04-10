using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BehaviorTreeMainProject.Log;

namespace BehaviorTreeMainProject.Log.Services
{
    /// <summary>
    /// Logger for tracking tick timing statistics for behavior tree nodes
    /// </summary>
    public class TickTimingLogger : BaseLogger
    {
        private static TickTimingLogger? instance;
        private static readonly object lockObject = new object();

        // Tick timing statistics by node type
        private readonly Dictionary<string, TickTimingStats> nodeTypeStats = new Dictionary<string, TickTimingStats>();

        public static TickTimingLogger Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (lockObject)
                    {
                        if (instance == null)
                        {
                            instance = new TickTimingLogger();
                        }
                    }
                }
                return instance;
            }
        }

        private TickTimingLogger()
        {
            base.Initialize("TickTiming", true, true);

            WriteSectionHeader("⏱️ TICK TIMING LOGGER INITIALIZED");
            WriteLog("Ready to track tick timing statistics for behavior tree nodes");
        }

        /// <summary>
        /// Track tick timing for a node that completed the full tick cycle
        /// </summary>
        public static void TrackTickTiming(BTNode node)
        {
            Instance.TrackTickTimingInternal(node);
        }

        private void TrackTickTimingInternal(BTNode node)
        {
            lock (lockObject)
            {
                if (!node.HasCompletedFullTick)
                {
                    // Only track nodes that completed the full tick cycle
                    return;
                }

                var nodeType = GetNodeType(node);

                if (!nodeTypeStats.ContainsKey(nodeType))
                {
                    nodeTypeStats[nodeType] = new TickTimingStats
                    {
                        NodeType = nodeType,
                        TotalTicks = 0,
                        TotalServicesTime = TimeSpan.Zero,
                        TotalDecoratorsTime = TimeSpan.Zero,
                        TotalNodeLogicTime = TimeSpan.Zero,
                        TotalChildrenTime = TimeSpan.Zero,
                        TotalTickTime = TimeSpan.Zero
                    };
                }

                var stats = nodeTypeStats[nodeType];
                stats.TotalTicks++;
                stats.TotalServicesTime += node.ServicesDuration;
                stats.TotalDecoratorsTime += node.DecoratorsDuration;
                stats.TotalNodeLogicTime += node.NodeLogicDuration;
                stats.TotalChildrenTime += node.ChildrenDuration;
                stats.TotalTickTime += node.TotalTickDuration;

                WriteLog($"⏱️ Tracked tick timing for {nodeType}: Services={node.ServicesDuration.TotalMilliseconds:F2}ms, Decorators={node.DecoratorsDuration.TotalMilliseconds:F2}ms, NodeLogic={node.NodeLogicDuration.TotalMilliseconds:F2}ms, Children={node.ChildrenDuration.TotalMilliseconds:F2}ms, Total={node.TotalTickDuration.TotalMilliseconds:F2}ms");
            }
        }

        /// <summary>
        /// Generate and export the comprehensive tick timing CSV summary
        /// </summary>
        public static void GenerateCSVSummary()
        {
            Instance.GenerateCSVSummaryInternal();
        }

        private void GenerateCSVSummaryInternal()
        {
            lock (lockObject)
            {
                WriteSectionHeader("⏱️ TICK TIMING CSV SUMMARY");

                // Generate CSV content
                var csvContent = GenerateCSVContent();

                // Write CSV to log
                WriteLog("CSV Summary:");
                WriteLog(csvContent);

                // Also write to a separate CSV file
                WriteCSVToFile(csvContent);
            }
        }

        private string GenerateCSVContent()
        {
            var csv = new StringBuilder();

            // CSV Header
            csv.AppendLine("NodeType,TotalTicks,AverageServicesTimeMs,AverageDecoratorsTimeMs,AverageNodeLogicTimeMs,AverageChildrenTimeMs,AverageTotalTickTimeMs,TotalServicesTimeMs,TotalDecoratorsTimeMs,TotalNodeLogicTimeMs,TotalChildrenTimeMs,TotalTickTimeMs");

            // CSV Rows for each node type
            foreach (var kvp in nodeTypeStats.OrderBy(x => x.Key))
            {
                var nodeType = kvp.Key;
                var stats = kvp.Value;

                var avgServicesTime = stats.TotalTicks > 0 ? stats.TotalServicesTime.TotalMilliseconds / stats.TotalTicks : 0;
                var avgDecoratorsTime = stats.TotalTicks > 0 ? stats.TotalDecoratorsTime.TotalMilliseconds / stats.TotalTicks : 0;
                var avgNodeLogicTime = stats.TotalTicks > 0 ? stats.TotalNodeLogicTime.TotalMilliseconds / stats.TotalTicks : 0;
                var avgChildrenTime = stats.TotalTicks > 0 ? stats.TotalChildrenTime.TotalMilliseconds / stats.TotalTicks : 0;
                var avgTotalTickTime = stats.TotalTicks > 0 ? stats.TotalTickTime.TotalMilliseconds / stats.TotalTicks : 0;

                csv.AppendLine($"{nodeType},{stats.TotalTicks},{avgServicesTime:F2},{avgDecoratorsTime:F2},{avgNodeLogicTime:F2},{avgChildrenTime:F2},{avgTotalTickTime:F2},{stats.TotalServicesTime.TotalMilliseconds:F2},{stats.TotalDecoratorsTime.TotalMilliseconds:F2},{stats.TotalNodeLogicTime.TotalMilliseconds:F2},{stats.TotalChildrenTime.TotalMilliseconds:F2},{stats.TotalTickTime.TotalMilliseconds:F2}");
            }

            // Add totals row if we have data
            if (nodeTypeStats.Any())
            {
                var totalTicks = nodeTypeStats.Values.Sum(s => s.TotalTicks);
                var totalServicesTime = TimeSpan.FromMilliseconds(nodeTypeStats.Values.Sum(s => s.TotalServicesTime.TotalMilliseconds));
                var totalDecoratorsTime = TimeSpan.FromMilliseconds(nodeTypeStats.Values.Sum(s => s.TotalDecoratorsTime.TotalMilliseconds));
                var totalNodeLogicTime = TimeSpan.FromMilliseconds(nodeTypeStats.Values.Sum(s => s.TotalNodeLogicTime.TotalMilliseconds));
                var totalChildrenTime = TimeSpan.FromMilliseconds(nodeTypeStats.Values.Sum(s => s.TotalChildrenTime.TotalMilliseconds));
                var totalTickTime = TimeSpan.FromMilliseconds(nodeTypeStats.Values.Sum(s => s.TotalTickTime.TotalMilliseconds));

                var overallAvgServicesTime = totalTicks > 0 ? totalServicesTime.TotalMilliseconds / totalTicks : 0;
                var overallAvgDecoratorsTime = totalTicks > 0 ? totalDecoratorsTime.TotalMilliseconds / totalTicks : 0;
                var overallAvgNodeLogicTime = totalTicks > 0 ? totalNodeLogicTime.TotalMilliseconds / totalTicks : 0;
                var overallAvgChildrenTime = totalTicks > 0 ? totalChildrenTime.TotalMilliseconds / totalTicks : 0;
                var overallAvgTotalTickTime = totalTicks > 0 ? totalTickTime.TotalMilliseconds / totalTicks : 0;

                csv.AppendLine($"TOTAL,{totalTicks},{overallAvgServicesTime:F2},{overallAvgDecoratorsTime:F2},{overallAvgNodeLogicTime:F2},{overallAvgChildrenTime:F2},{overallAvgTotalTickTime:F2},{totalServicesTime.TotalMilliseconds:F2},{totalDecoratorsTime.TotalMilliseconds:F2},{totalNodeLogicTime.TotalMilliseconds:F2},{totalChildrenTime.TotalMilliseconds:F2},{totalTickTime.TotalMilliseconds:F2}");
            }

            // Ensure we have rows for all three main planner types (FF, ENHSP, LAMA-FIRST)
            // even if they don't have any data
            var requiredPlannerTypes = new[] { "FF", "Enhsp", "LAMA-FIRST" };

            foreach (var plannerType in requiredPlannerTypes)
            {
                var nodeTypeKey = $"BTFlowNodeDynamic_{plannerType}";

                // Only add if we don't already have this row
                if (!nodeTypeStats.ContainsKey(nodeTypeKey))
                {
                    csv.AppendLine($"{nodeTypeKey},0,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00");
                }
            }

            return csv.ToString();
        }

        private void WriteCSVToFile(string csvContent)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                var csvFilePath = $"WrittenLogs/TickTimingSummary_{timestamp}.csv";

                System.IO.File.WriteAllText(csvFilePath, csvContent, Encoding.UTF8);
                WriteLog($"📄 CSV summary written to: {csvFilePath}");
            }
            catch (Exception ex)
            {
                WriteLog($"⚠️ Error writing CSV file: {ex.Message}");
            }
        }

        /// <summary>
        /// Get the node type for categorization with planner-specific details
        /// </summary>
        private string GetNodeType(BTNode node)
        {
            string typeName = node.TypeName;

            // Categorize into main types
            if (typeName.Contains("GenericBTAction"))
                return "GenericBTAction";
            else if (typeName.Contains("BTFlowNodeComposite"))
                return "BTFlowNodeComposite";
            else if (typeName.Contains("DynamicFlowNode"))
            {
                // For dynamic flow nodes, try to determine the planner type
                try
                {
                    var flowNode = node as DynamicFlowNode;
                    if (flowNode?.ServicePlanning?.planningRequest != null)
                    {
                        var plannerType = flowNode.ServicePlanning.CurrentPlanner?.DefaultPlannerName
                            ?? flowNode.ServicePlanning.planningRequest.PlanningType;
                        return $"BTFlowNodeDynamic_{plannerType}";
                    }
                }
                catch (Exception ex)
                {
                    WriteLog($"⚠️ Error determining planner type for {node.DebugDisplayName}: {ex.Message}");
                }

                // Fallback to generic dynamic node
                return "BTFlowNodeDynamic_Unknown";
            }
            else
                return typeName; // Use full type name for other types
        }

        /// <summary>
        /// Close the logger
        /// </summary>
        public new static void Close()
        {
            Instance.CloseInternal();
        }

        private void CloseInternal()
        {
            WriteSectionHeader("🏁 TICK TIMING LOGGER CLOSED");
            base.Close();
        }

        /// <summary>
        /// Tick timing statistics data structure
        /// </summary>
        private class TickTimingStats
        {
            public string NodeType { get; set; } = "";
            public int TotalTicks { get; set; }
            public TimeSpan TotalServicesTime { get; set; }
            public TimeSpan TotalDecoratorsTime { get; set; }
            public TimeSpan TotalNodeLogicTime { get; set; }
            public TimeSpan TotalChildrenTime { get; set; }
            public TimeSpan TotalTickTime { get; set; }
        }
    }
}
