using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BehaviorTreeMainProject.Log.Services
{
    /// <summary>
    /// Dedicated logger for tracking the hierarchical decomposition trace:
    ///   HL Action → ML Actions (from PDDL planner) → LL Steps (robot primitives)
    /// 
    /// This logger is the centrepiece for paper evaluation, showing how the
    /// behavior tree orchestrates task planning (PDDL) and motion planning
    /// (MoveIt / URScript) as services within a unified architecture.
    /// 
    /// Output example:
    ///   [HL #1] START Layers1_2 — planning with PDDL
    ///   [HL #1] PLANNED 33 ML actions in 2098ms (planner: ENHSP)
    ///     [ML #1] START PickUpML_robot1_rpmanipulate_rppickup (parent: Layers1_2)
    ///       [LL #1] MoveToLL (movel) → rppickup [SUCCESS 3200ms]
    ///       [LL #2] CloseGripperLL [SUCCESS 800ms]
    ///       [LL #3] MoveToLL (planned/MoveIt) → rpplace [SUCCESS 8400ms]
    ///     [ML #1] COMPLETED PickUpML — 3/3 LL steps succeeded — 12400ms
    ///   [HL #1] COMPLETED Layers1_2 — 33/33 ML actions — 145000ms
    /// </summary>
    public class HierarchicalTraceLogger : BaseLogger
    {
        private static HierarchicalTraceLogger? instance;
        private static readonly object lockObject = new object();

        private readonly List<HLTraceRecord> hlRecords = new List<HLTraceRecord>();
        private readonly List<MLTraceRecord> mlRecords = new List<MLTraceRecord>();
        private readonly List<LLTraceRecord> llRecords = new List<LLTraceRecord>();

        private int hlCounter = 0;
        private int mlCounter = 0;
        private int llCounter = 0;

        public static HierarchicalTraceLogger Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (lockObject)
                    {
                        if (instance == null)
                        {
                            instance = new HierarchicalTraceLogger();
                        }
                    }
                }
                return instance;
            }
        }

        private HierarchicalTraceLogger()
        {
            base.Initialize("HierarchicalTrace", enableConsole: false, enableFile: true);
            WriteSectionHeader("HIERARCHICAL DECOMPOSITION TRACE");
            WriteLog("Tracking HL → ML → LL decomposition for TAMP-as-services evaluation");
            WriteLog("Indentation: [HL] top-level, [ML] mid-level (1 indent), [LL] low-level (2 indents)");
            WriteSeparator();
        }

        // ── HL-level API ─────────────────────────────────────────────────

        /// <summary>Log the start of an HL action's planning phase.</summary>
        public static int LogHLStart(string hlActionName, string plannerType)
        {
            return Instance.LogHLStartInternal(hlActionName, plannerType);
        }

        /// <summary>Log that an HL action's planner produced ML actions.</summary>
        public static void LogHLPlanned(int hlId, int mlActionCount, double plannerTimeMs, string plannerUsed = null)
        {
            Instance.LogHLPlannedInternal(hlId, mlActionCount, plannerTimeMs, plannerUsed);
        }

        /// <summary>Log the completion of an HL action (all ML actions done).</summary>
        public static void LogHLCompleted(int hlId, bool success)
        {
            Instance.LogHLCompletedInternal(hlId, success);
        }

        // ── ML-level API ─────────────────────────────────────────────────

        /// <summary>Log the start of an ML action execution.</summary>
        public static int LogMLStart(string mlActionType, string mlInstanceName, string parentHLAction = null)
        {
            return Instance.LogMLStartInternal(mlActionType, mlInstanceName, parentHLAction);
        }

        /// <summary>Log the completion of an ML action.</summary>
        public static void LogMLCompleted(int mlId, bool success, int llStepsTotal = 0, int llStepsSucceeded = 0)
        {
            Instance.LogMLCompletedInternal(mlId, success, llStepsTotal, llStepsSucceeded);
        }

        // ── LL-level API ─────────────────────────────────────────────────

        /// <summary>Log an LL step execution result.</summary>
        public static void LogLLStep(
            string llActionType,
            string instanceName,
            string commandType,
            string targetPosition,
            bool success,
            double executionTimeMs,
            string parentMLAction = null)
        {
            Instance.LogLLStepInternal(llActionType, instanceName, commandType, targetPosition, success, executionTimeMs, parentMLAction);
        }

        /// <summary>Generate CSV summaries and close.</summary>
        public static void GenerateCSVSummary()
        {
            Instance.GenerateCSVSummaryInternal();
        }

        /// <summary>Close the logger.</summary>
        public new static void Close()
        {
            Instance.CloseInternal();
        }

        // ── Internal implementation ──────────────────────────────────────

        private int LogHLStartInternal(string hlActionName, string plannerType)
        {
            lock (lockObject)
            {
                hlCounter++;
                var record = new HLTraceRecord
                {
                    Id = hlCounter,
                    ActionName = hlActionName ?? "Unknown",
                    PlannerType = plannerType ?? "PDDL",
                    StartTime = DateTime.Now
                };
                hlRecords.Add(record);

                WriteLog($"[HL #{hlCounter}] START {hlActionName} — planning with {plannerType}");
                return hlCounter;
            }
        }

        private void LogHLPlannedInternal(int hlId, int mlActionCount, double plannerTimeMs, string plannerUsed)
        {
            lock (lockObject)
            {
                var record = hlRecords.FirstOrDefault(r => r.Id == hlId);
                if (record != null)
                {
                    record.MLActionCount = mlActionCount;
                    record.PlannerTimeMs = plannerTimeMs;
                    if (!string.IsNullOrEmpty(plannerUsed))
                        record.PlannerType = plannerUsed;
                }
                WriteLog($"[HL #{hlId}] PLANNED {mlActionCount} ML actions in {plannerTimeMs:F0}ms (planner: {plannerUsed ?? "PDDL"})");
            }
        }

        private void LogHLCompletedInternal(int hlId, bool success)
        {
            lock (lockObject)
            {
                var record = hlRecords.FirstOrDefault(r => r.Id == hlId);
                if (record != null)
                {
                    record.EndTime = DateTime.Now;
                    record.Success = success;
                    record.Completed = true;
                }

                var elapsed = record != null && record.EndTime.HasValue
                    ? (record.EndTime.Value - record.StartTime).TotalMilliseconds : 0;
                var mlDone = mlRecords.Count(m => m.ParentHLAction == record?.ActionName && m.Completed);
                var mlTotal = record?.MLActionCount ?? 0;
                var statusStr = success ? "COMPLETED" : "FAILED";

                WriteLog($"[HL #{hlId}] {statusStr} {record?.ActionName} — {mlDone}/{mlTotal} ML actions — {elapsed:F0}ms");
                WriteSeparator();
            }
        }

        private int LogMLStartInternal(string mlActionType, string mlInstanceName, string parentHLAction)
        {
            lock (lockObject)
            {
                mlCounter++;
                var record = new MLTraceRecord
                {
                    Id = mlCounter,
                    ActionType = mlActionType ?? "Unknown",
                    InstanceName = mlInstanceName ?? "Unknown",
                    ParentHLAction = parentHLAction ?? "",
                    StartTime = DateTime.Now
                };
                mlRecords.Add(record);

                var parentInfo = !string.IsNullOrEmpty(parentHLAction) ? $" (parent: {parentHLAction})" : "";
                WriteLog($"  [ML #{mlCounter}] START {mlActionType} '{mlInstanceName}'{parentInfo}");
                return mlCounter;
            }
        }

        private void LogMLCompletedInternal(int mlId, bool success, int llStepsTotal, int llStepsSucceeded)
        {
            lock (lockObject)
            {
                var record = mlRecords.FirstOrDefault(r => r.Id == mlId);
                if (record != null)
                {
                    record.EndTime = DateTime.Now;
                    record.Success = success;
                    record.LLStepsTotal = llStepsTotal;
                    record.LLStepsSucceeded = llStepsSucceeded;
                    record.Completed = true;
                }

                var elapsed = record != null && record.EndTime.HasValue
                    ? (record.EndTime.Value - record.StartTime).TotalMilliseconds : 0;
                var statusStr = success ? "COMPLETED" : "FAILED";

                WriteLog($"  [ML #{mlId}] {statusStr} {record?.ActionType} — {llStepsSucceeded}/{llStepsTotal} LL steps — {elapsed:F0}ms");
            }
        }

        private void LogLLStepInternal(
            string llActionType, string instanceName, string commandType,
            string targetPosition, bool success, double executionTimeMs, string parentMLAction)
        {
            lock (lockObject)
            {
                llCounter++;
                var record = new LLTraceRecord
                {
                    Id = llCounter,
                    ActionType = llActionType ?? "Unknown",
                    InstanceName = instanceName ?? "Unknown",
                    CommandType = commandType ?? "",
                    TargetPosition = targetPosition ?? "",
                    Success = success,
                    ExecutionTimeMs = executionTimeMs,
                    ParentMLAction = parentMLAction ?? "",
                    Timestamp = DateTime.Now
                };
                llRecords.Add(record);

                var statusStr = success ? "SUCCESS" : "FAILED";
                var targetInfo = !string.IsNullOrEmpty(targetPosition) ? $" → {targetPosition}" : "";
                var cmdInfo = !string.IsNullOrEmpty(commandType) ? $" ({commandType})" : "";

                WriteLog($"    [LL #{llCounter}] {llActionType}{cmdInfo}{targetInfo} [{statusStr} {executionTimeMs:F0}ms]");
            }
        }

        private void GenerateCSVSummaryInternal()
        {
            lock (lockObject)
            {
                WriteSectionHeader("HIERARCHICAL TRACE SUMMARY");

                // ── HL Summary CSV ───────────────────────────────
                var hlCsv = new StringBuilder();
                hlCsv.AppendLine("HLId,ActionName,PlannerType,MLActionCount,PlannerTimeMs,TotalTimeMs,Success");
                foreach (var r in hlRecords)
                {
                    var totalMs = r.Completed && r.EndTime.HasValue ? (r.EndTime.Value - r.StartTime).TotalMilliseconds : 0;
                    hlCsv.AppendLine($"{r.Id},{r.ActionName},{r.PlannerType},{r.MLActionCount},{r.PlannerTimeMs:F2},{totalMs:F2},{r.Success}");
                }
                WriteLog("HL Actions CSV:");
                WriteLog(hlCsv.ToString());

                // ── ML Summary CSV ───────────────────────────────
                var mlCsv = new StringBuilder();
                mlCsv.AppendLine("MLId,ActionType,InstanceName,ParentHLAction,LLStepsTotal,LLStepsSucceeded,TotalTimeMs,Success");
                foreach (var r in mlRecords)
                {
                    var totalMs = r.Completed && r.EndTime.HasValue ? (r.EndTime.Value - r.StartTime).TotalMilliseconds : 0;
                    mlCsv.AppendLine($"{r.Id},{r.ActionType},{r.InstanceName},{r.ParentHLAction},{r.LLStepsTotal},{r.LLStepsSucceeded},{totalMs:F2},{r.Success}");
                }
                WriteLog("ML Actions CSV:");
                WriteLog(mlCsv.ToString());

                // ── LL Summary CSV ───────────────────────────────
                var llCsv = new StringBuilder();
                llCsv.AppendLine("LLId,ActionType,InstanceName,CommandType,TargetPosition,ParentMLAction,Success,ExecTimeMs");
                foreach (var r in llRecords)
                {
                    llCsv.AppendLine($"{r.Id},{r.ActionType},{r.InstanceName},{r.CommandType},{r.TargetPosition},{r.ParentMLAction},{r.Success},{r.ExecutionTimeMs:F2}");
                }
                WriteLog("LL Steps CSV:");
                WriteLog(llCsv.ToString());

                // Write CSV files
                try
                {
                    var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

                    System.IO.File.WriteAllText($"WrittenLogs/HierarchicalTrace_HL_{timestamp}.csv", hlCsv.ToString(), Encoding.UTF8);
                    System.IO.File.WriteAllText($"WrittenLogs/HierarchicalTrace_ML_{timestamp}.csv", mlCsv.ToString(), Encoding.UTF8);
                    System.IO.File.WriteAllText($"WrittenLogs/HierarchicalTrace_LL_{timestamp}.csv", llCsv.ToString(), Encoding.UTF8);
                    WriteLog($"CSV files written to WrittenLogs/");
                }
                catch (Exception ex)
                {
                    WriteLog($"Warning: Could not write CSV files: {ex.Message}");
                }

                // ── Aggregated metrics ───────────────────────────
                WriteSeparator('=');
                WriteLog("AGGREGATED METRICS:");

                var totalHL = hlRecords.Count;
                var hlSuccess = hlRecords.Count(r => r.Success);
                WriteLog($"  HL Actions: {totalHL} total, {hlSuccess} succeeded ({(totalHL > 0 ? (double)hlSuccess/totalHL*100 : 0):F1}%)");
                if (hlRecords.Any(r => r.PlannerTimeMs > 0))
                    WriteLog($"  HL Planning: avg {hlRecords.Where(r => r.PlannerTimeMs > 0).Average(r => r.PlannerTimeMs):F0}ms, total {hlRecords.Sum(r => r.PlannerTimeMs):F0}ms");

                var totalML = mlRecords.Count;
                var mlSuccess = mlRecords.Count(r => r.Success);
                var avgMLTime = mlRecords.Where(r => r.Completed && r.EndTime.HasValue).Select(r => (r.EndTime.Value - r.StartTime).TotalMilliseconds).DefaultIfEmpty(0).Average();
                WriteLog($"  ML Actions: {totalML} total, {mlSuccess} succeeded ({(totalML > 0 ? (double)mlSuccess/totalML*100 : 0):F1}%), avg time {avgMLTime:F0}ms");

                var totalLL = llRecords.Count;
                var llSuccess = llRecords.Count(r => r.Success);
                var avgLLTime = llRecords.Select(r => r.ExecutionTimeMs).DefaultIfEmpty(0).Average();
                WriteLog($"  LL Steps: {totalLL} total, {llSuccess} succeeded ({(totalLL > 0 ? (double)llSuccess/totalLL*100 : 0):F1}%), avg time {avgLLTime:F0}ms");

                // By command type
                var byType = llRecords.GroupBy(r => r.CommandType).Where(g => !string.IsNullOrEmpty(g.Key));
                foreach (var g in byType)
                {
                    var gSuccess = g.Count(r => r.Success);
                    var gAvg = g.Average(r => r.ExecutionTimeMs);
                    WriteLog($"    {g.Key}: {g.Count()} cmds, {gSuccess} succeeded, avg {gAvg:F0}ms");
                }

                // Total wall-clock time
                if (hlRecords.Any(r => r.Completed && r.EndTime.HasValue))
                {
                    var firstStart = hlRecords.Min(r => r.StartTime);
                    var lastEnd = hlRecords.Where(r => r.EndTime.HasValue).Max(r => r.EndTime.Value);
                    var wallClock = (lastEnd - firstStart).TotalSeconds;
                    WriteLog($"  Wall-clock time (first HL start → last HL end): {wallClock:F1}s");
                }

                var totalPlanningMs = hlRecords.Sum(r => r.PlannerTimeMs);
                var totalMotionMs = llRecords.Sum(r => r.ExecutionTimeMs);
                WriteLog($"  Total planning time: {totalPlanningMs:F0}ms ({totalPlanningMs/1000.0:F1}s)");
                WriteLog($"  Total motion time: {totalMotionMs:F0}ms ({totalMotionMs/1000.0:F1}s)");
                if (totalPlanningMs + totalMotionMs > 0)
                    WriteLog($"  Planning-to-motion ratio: {totalPlanningMs / (totalPlanningMs + totalMotionMs) * 100:F1}% planning, {totalMotionMs / (totalPlanningMs + totalMotionMs) * 100:F1}% motion");

                WriteSeparator('=');
            }
        }

        private void CloseInternal()
        {
            WriteSectionHeader("HIERARCHICAL TRACE LOGGER CLOSED");
            base.Close();
            instance = null;
        }

        // ── Inner record classes ─────────────────────────────────────────

        private class HLTraceRecord
        {
            public int Id { get; set; }
            public string ActionName { get; set; } = "";
            public string PlannerType { get; set; } = "";
            public int MLActionCount { get; set; }
            public double PlannerTimeMs { get; set; }
            public DateTime StartTime { get; set; }
            public DateTime? EndTime { get; set; }
            public bool Success { get; set; }
            public bool Completed { get; set; }
        }

        private class MLTraceRecord
        {
            public int Id { get; set; }
            public string ActionType { get; set; } = "";
            public string InstanceName { get; set; } = "";
            public string ParentHLAction { get; set; } = "";
            public int LLStepsTotal { get; set; }
            public int LLStepsSucceeded { get; set; }
            public DateTime StartTime { get; set; }
            public DateTime? EndTime { get; set; }
            public bool Success { get; set; }
            public bool Completed { get; set; }
        }

        private class LLTraceRecord
        {
            public int Id { get; set; }
            public string ActionType { get; set; } = "";
            public string InstanceName { get; set; } = "";
            public string CommandType { get; set; } = "";
            public string TargetPosition { get; set; } = "";
            public string ParentMLAction { get; set; } = "";
            public bool Success { get; set; }
            public double ExecutionTimeMs { get; set; }
            public DateTime Timestamp { get; set; }
        }
    }
}
