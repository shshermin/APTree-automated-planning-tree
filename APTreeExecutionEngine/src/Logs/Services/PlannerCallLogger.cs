using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BehaviorTreeMainProject.Log.Services
{
    /// <summary>
    /// Dedicated logger for tracking every individual planner call.
    /// Records: call number, timestamp, planner type, problem file, HL action instance,
    /// success/failure, planner time, actions generated, and plan size.
    /// Produces both a per-call log and a CSV summary on close.
    /// </summary>
    public class PlannerCallLogger : BaseLogger
    {
        private static PlannerCallLogger? instance;
        private static readonly object lockObject = new object();

        private readonly List<PlannerCallRecord> callRecords = new List<PlannerCallRecord>();
        private int callCounter = 0;

        public static PlannerCallLogger Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (lockObject)
                    {
                        if (instance == null)
                        {
                            instance = new PlannerCallLogger();
                        }
                    }
                }
                return instance;
            }
        }

        private PlannerCallLogger()
        {
            base.Initialize("PlannerCalls", enableConsole: false, enableFile: true);
            WriteSectionHeader("PLANNER CALL LOGGER INITIALIZED");
            WriteLog("Tracking all individual planner invocations");
            WriteLog($"Columns: CallNumber, Timestamp, PlannerName, HLAction, ProblemFile, Success, PlannerTimeMs, ActionsGenerated, PlanLength, Error");
            WriteSeparator();
        }

        // ── Public static API ────────────────────────────────────────────

        /// <summary>
        /// Log the start of a planner call. Returns a call ID to pair with LogCallEnd.
        /// </summary>
        public static int LogCallStart(string plannerName, string hlActionInstance, string problemFile)
        {
            return Instance.LogCallStartInternal(plannerName, hlActionInstance, problemFile);
        }

        /// <summary>
        /// Log the end of a planner call (success path).
        /// </summary>
        public static void LogCallEnd(int callId, bool success, double plannerTimeSeconds, int actionsGenerated, int planLength, string plannerUsed = null)
        {
            Instance.LogCallEndInternal(callId, success, plannerTimeSeconds, actionsGenerated, planLength, plannerUsed, null);
        }

        /// <summary>
        /// Log the end of a planner call (failure/error path).
        /// </summary>
        public static void LogCallFailed(int callId, double plannerTimeSeconds, string error)
        {
            Instance.LogCallEndInternal(callId, false, plannerTimeSeconds, 0, 0, null, error);
        }

        /// <summary>
        /// Generate the CSV summary and close the logger.
        /// </summary>
        public static void GenerateCSVSummary()
        {
            Instance.GenerateCSVSummaryInternal();
        }

        /// <summary>
        /// Close the logger.
        /// </summary>
        public new static void Close()
        {
            Instance.CloseInternal();
        }

        // ── Internal implementation ──────────────────────────────────────

        private int LogCallStartInternal(string plannerName, string hlActionInstance, string problemFile)
        {
            lock (lockObject)
            {
                callCounter++;
                var record = new PlannerCallRecord
                {
                    CallNumber = callCounter,
                    StartTime = DateTime.Now,
                    PlannerName = plannerName ?? "Unknown",
                    HLActionInstance = hlActionInstance ?? "Unknown",
                    ProblemFile = problemFile ?? "Unknown"
                };
                callRecords.Add(record);

                WriteLog($"[CALL #{callCounter}] START | Planner: {record.PlannerName} | HL Action: {record.HLActionInstance} | Problem: {record.ProblemFile}");

                return callCounter;
            }
        }

        private void LogCallEndInternal(int callId, bool success, double plannerTimeSeconds, int actionsGenerated, int planLength, string plannerUsed, string error)
        {
            lock (lockObject)
            {
                var record = callRecords.FirstOrDefault(r => r.CallNumber == callId);
                if (record == null)
                {
                    WriteLog($"[CALL #{callId}] END | WARNING: No matching start record found");
                    return;
                }

                record.EndTime = DateTime.Now;
                record.Success = success;
                record.PlannerTimeSeconds = plannerTimeSeconds;
                record.ActionsGenerated = actionsGenerated;
                record.PlanLength = planLength;
                record.Error = error;
                record.Completed = true;

                if (!string.IsNullOrEmpty(plannerUsed))
                {
                    record.PlannerName = plannerUsed; // Update with actual planner used (returned by service)
                }

                var totalMs = (record.EndTime.Value - record.StartTime).TotalMilliseconds;
                var plannerMs = plannerTimeSeconds * 1000.0;

                if (success)
                {
                    WriteLog($"[CALL #{callId}] SUCCESS | Planner: {record.PlannerName} | Actions: {actionsGenerated} | PlanLength: {planLength} | PlannerTime: {plannerMs:F0}ms | TotalTime: {totalMs:F0}ms");
                }
                else
                {
                    WriteLog($"[CALL #{callId}] FAILED | Planner: {record.PlannerName} | PlannerTime: {plannerMs:F0}ms | TotalTime: {totalMs:F0}ms | Error: {error ?? "Unknown"}");
                }
            }
        }

        private void GenerateCSVSummaryInternal()
        {
            lock (lockObject)
            {
                WriteSectionHeader("PLANNER CALL SUMMARY");

                // ── Per-call CSV ─────────────────────────────────
                var csvPerCall = new StringBuilder();
                csvPerCall.AppendLine("CallNumber,Timestamp,PlannerName,HLActionInstance,ProblemFile,Success,PlannerTimeMs,TotalTimeMs,ActionsGenerated,PlanLength,Error");

                foreach (var r in callRecords)
                {
                    var totalMs = r.Completed && r.EndTime.HasValue
                        ? (r.EndTime.Value - r.StartTime).TotalMilliseconds
                        : 0;
                    var plannerMs = r.PlannerTimeSeconds * 1000.0;
                    var errorEscaped = (r.Error ?? "").Replace(",", ";").Replace("\n", " ");

                    csvPerCall.AppendLine($"{r.CallNumber},{r.StartTime:yyyy-MM-dd HH:mm:ss.fff},{r.PlannerName},{r.HLActionInstance},{r.ProblemFile},{r.Success},{plannerMs:F2},{totalMs:F2},{r.ActionsGenerated},{r.PlanLength},{errorEscaped}");
                }

                WriteLog("Per-Call CSV:");
                WriteLog(csvPerCall.ToString());

                // Write per-call CSV to file
                try
                {
                    var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                    var perCallPath = $"WrittenLogs/PlannerCalls_{timestamp}.csv";
                    System.IO.File.WriteAllText(perCallPath, csvPerCall.ToString(), Encoding.UTF8);
                    WriteLog($"Per-call CSV written to: {perCallPath}");
                }
                catch (Exception ex)
                {
                    WriteLog($"Warning: Could not write per-call CSV: {ex.Message}");
                }

                // ── Aggregated summary CSV ───────────────────────
                var grouped = callRecords
                    .GroupBy(r => r.PlannerName)
                    .Select(g => new
                    {
                        PlannerType = g.Key,
                        CallCount = g.Count(),
                        SuccessfulCalls = g.Count(r => r.Success),
                        FailedCalls = g.Count(r => !r.Success),
                        AvgPlannerTimeMs = g.Average(r => r.PlannerTimeSeconds * 1000.0),
                        AvgTotalTimeMs = g.Where(r => r.Completed && r.EndTime.HasValue)
                                          .Select(r => (r.EndTime.Value - r.StartTime).TotalMilliseconds)
                                          .DefaultIfEmpty(0)
                                          .Average(),
                        AvgActionsGenerated = g.Average(r => (double)r.ActionsGenerated),
                        AvgPlanLength = g.Where(r => r.Success).Select(r => (double)r.PlanLength).DefaultIfEmpty(0).Average(),
                        TotalPlannerTimeMs = g.Sum(r => r.PlannerTimeSeconds * 1000.0),
                        TotalServiceTimeMs = g.Where(r => r.Completed && r.EndTime.HasValue)
                                              .Sum(r => (r.EndTime.Value - r.StartTime).TotalMilliseconds)
                    })
                    .OrderByDescending(g => g.CallCount)
                    .ToList();

                var csvSummary = new StringBuilder();
                csvSummary.AppendLine("PlannerType,CallCount,SuccessfulCalls,FailedCalls,SuccessRate,AvgPlannerTimeMs,AvgTotalTimeMs,AvgActionsGenerated,AvgPlanLength,TotalPlannerTimeMs,TotalServiceTimeMs");

                foreach (var g in grouped)
                {
                    var rate = g.CallCount > 0 ? (double)g.SuccessfulCalls / g.CallCount * 100 : 0;
                    csvSummary.AppendLine($"{g.PlannerType},{g.CallCount},{g.SuccessfulCalls},{g.FailedCalls},{rate:F2}%,{g.AvgPlannerTimeMs:F2},{g.AvgTotalTimeMs:F2},{g.AvgActionsGenerated:F1},{g.AvgPlanLength:F1},{g.TotalPlannerTimeMs:F2},{g.TotalServiceTimeMs:F2}");
                }

                // Add TOTAL row
                var totalCalls = callRecords.Count;
                var totalSuccess = callRecords.Count(r => r.Success);
                var totalFailed = totalCalls - totalSuccess;
                var totalRate = totalCalls > 0 ? (double)totalSuccess / totalCalls * 100 : 0;
                var totalAvgPlannerMs = totalCalls > 0 ? callRecords.Average(r => r.PlannerTimeSeconds * 1000.0) : 0;
                var totalAvgServiceMs = callRecords.Where(r => r.Completed && r.EndTime.HasValue)
                    .Select(r => (r.EndTime.Value - r.StartTime).TotalMilliseconds)
                    .DefaultIfEmpty(0).Average();
                var totalAvgActions = totalCalls > 0 ? callRecords.Average(r => (double)r.ActionsGenerated) : 0;
                var totalAvgPlanLen = callRecords.Where(r => r.Success).Select(r => (double)r.PlanLength).DefaultIfEmpty(0).Average();
                var grandTotalPlannerMs = callRecords.Sum(r => r.PlannerTimeSeconds * 1000.0);
                var grandTotalServiceMs = callRecords.Where(r => r.Completed && r.EndTime.HasValue)
                    .Sum(r => (r.EndTime.Value - r.StartTime).TotalMilliseconds);

                csvSummary.AppendLine($"TOTAL,{totalCalls},{totalSuccess},{totalFailed},{totalRate:F2}%,{totalAvgPlannerMs:F2},{totalAvgServiceMs:F2},{totalAvgActions:F1},{totalAvgPlanLen:F1},{grandTotalPlannerMs:F2},{grandTotalServiceMs:F2}");

                WriteLog("Aggregated Summary CSV:");
                WriteLog(csvSummary.ToString());

                // Write summary CSV to file
                try
                {
                    var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                    var summaryPath = $"WrittenLogs/PlannerSummary_{timestamp}.csv";
                    System.IO.File.WriteAllText(summaryPath, csvSummary.ToString(), Encoding.UTF8);
                    WriteLog($"Summary CSV written to: {summaryPath}");
                }
                catch (Exception ex)
                {
                    WriteLog($"Warning: Could not write summary CSV: {ex.Message}");
                }

                // ── Human-readable summary ───────────────────────
                WriteSeparator('=');
                WriteLog($"Total planner calls: {totalCalls}");
                WriteLog($"Successful: {totalSuccess} ({totalRate:F1}%)");
                WriteLog($"Failed: {totalFailed}");
                WriteLog($"Total planner time: {grandTotalPlannerMs:F0}ms ({grandTotalPlannerMs / 1000.0:F1}s)");
                WriteLog($"Total service time: {grandTotalServiceMs:F0}ms ({grandTotalServiceMs / 1000.0:F1}s)");
                WriteSeparator('=');
            }
        }

        private void CloseInternal()
        {
            WriteSectionHeader("PLANNER CALL LOGGER CLOSED");
            base.Close();
            instance = null;
        }

        // ── Inner record class ───────────────────────────────────────────

        private class PlannerCallRecord
        {
            public int CallNumber { get; set; }
            public DateTime StartTime { get; set; }
            public DateTime? EndTime { get; set; }
            public string PlannerName { get; set; } = "";
            public string HLActionInstance { get; set; } = "";
            public string ProblemFile { get; set; } = "";
            public bool Success { get; set; }
            public double PlannerTimeSeconds { get; set; }
            public int ActionsGenerated { get; set; }
            public int PlanLength { get; set; }
            public string? Error { get; set; }
            public bool Completed { get; set; }
        }
    }
}
