using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BehaviorTreeMainProject.Log.Services
{
    /// <summary>
    /// One row per injected fault, covering the full detection → replan → resume lifecycle.
    /// Designed for paper evaluation tables (MTTR, detection latency, replan planning time,
    /// recovery success rate, wasted work).
    ///
    /// Lifecycle hooks (keyed by DFN name; one open event per DFN at a time):
    ///   LogFaultInjected  → from DummyCameraService.ApplyFault (or any external injector)
    ///   LogFaultDetected  → from DecoratorFaultAbort / DecoratorHLFaultAbort first-detection branch
    ///   LogReplanTriggered → from each decorator's TriggerReplan / triggered branch
    ///   LogResumed        → from each decorator's "Normal execution resumed" branch
    ///
    /// Cross-references PlannerCallLogger.GetCompletedCalls() at shutdown to attach the
    /// planner call that ran between ReplanTriggered and Resumed (yielding planning time + plan length).
    /// </summary>
    public class FaultRecoveryLogger : BaseLogger
    {
        private static FaultRecoveryLogger? instance;
        private static readonly object lockObject = new object();

        private readonly List<FaultRecoveryRecord> records = new List<FaultRecoveryRecord>();
        private readonly Dictionary<string, FaultRecoveryRecord> activeByDfn = new();
        private int counter = 0;

        public static FaultRecoveryLogger Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (lockObject)
                    {
                        if (instance == null) instance = new FaultRecoveryLogger();
                    }
                }
                return instance;
            }
        }

        private FaultRecoveryLogger()
        {
            base.Initialize("FaultRecovery", enableConsole: false, enableFile: true);
            WriteSectionHeader("FAULT RECOVERY LOGGER INITIALIZED");
            WriteLog("One row per injected fault. Tracks detection → replan → resume.");
            WriteSeparator();
        }

        // ── Public API ───────────────────────────────────────────────────

        /// <summary>
        /// Called by the fault injector immediately before the abort/replan flag is written
        /// to the blackboard. Returns the new event id (also stored internally, keyed by dfnName).
        /// </summary>
        public static int LogFaultInjected(
            string faultId,
            string faultType,
            string scope,                 // "LL" (abort_) or "HL" (hl_replan_)
            string dfnName,               // owning DFN name = key for matching back from decorators
            string mlActionInstance,
            string parentHlInstance = "",
            string targetObject = "",
            string extraDetails = "")
        {
            return Instance.LogFaultInjectedInternal(faultId, faultType, scope, dfnName,
                mlActionInstance, parentHlInstance, targetObject, extraDetails);
        }

        public static void LogFaultDetected(string dfnName, string scope)
        {
            Instance.LogFaultDetectedInternal(dfnName, scope);
        }

        public static void LogReplanTriggered(string dfnName)
        {
            Instance.LogReplanTriggeredInternal(dfnName);
        }

        public static void LogResumed(string dfnName, bool success = true)
        {
            Instance.LogResumedInternal(dfnName, success);
        }

        public static void GenerateCSVSummary() => Instance.GenerateCSVSummaryInternal();

        public new static void Close() => Instance.CloseInternal();

        // ── Internal ─────────────────────────────────────────────────────

        private int LogFaultInjectedInternal(string faultId, string faultType, string scope,
            string dfnName, string mlActionInstance, string parentHlInstance,
            string targetObject, string extraDetails)
        {
            lock (lockObject)
            {
                // Close any prior open event on the same DFN (rare; means previous fault never resumed)
                if (activeByDfn.TryGetValue(dfnName ?? "", out var prior) && !prior.Closed)
                {
                    prior.ResumeTime = DateTime.Now;
                    prior.RecoverySuccess = false;
                    prior.Closed = true;
                    WriteLog($"[FAULT #{prior.EventNumber}] SUPERSEDED by new injection on dfn='{dfnName}'");
                }

                counter++;
                var rec = new FaultRecoveryRecord
                {
                    EventNumber = counter,
                    FaultId = faultId ?? "",
                    FaultType = faultType ?? "",
                    Scope = scope ?? "",
                    DfnName = dfnName ?? "",
                    MLActionInstance = mlActionInstance ?? "",
                    ParentHlInstance = parentHlInstance ?? "",
                    TargetObject = targetObject ?? "",
                    ExtraDetails = extraDetails ?? "",
                    InjectionTime = DateTime.Now,
                };
                records.Add(rec);
                activeByDfn[rec.DfnName] = rec;

                WriteLog($"[FAULT #{rec.EventNumber}] INJECTED id={faultId} type={faultType} scope={scope} dfn={dfnName} ml={mlActionInstance} target={targetObject} at {rec.InjectionTime:HH:mm:ss.fff}");
                return rec.EventNumber;
            }
        }

        private void LogFaultDetectedInternal(string dfnName, string scope)
        {
            lock (lockObject)
            {
                if (!activeByDfn.TryGetValue(dfnName ?? "", out var rec) || rec.Closed) return;
                if (rec.DetectionTime != default) return; // already detected
                rec.DetectionTime = DateTime.Now;
                rec.DetectedScope = scope ?? rec.Scope;
                var latency = (rec.DetectionTime - rec.InjectionTime).TotalMilliseconds;
                WriteLog($"[FAULT #{rec.EventNumber}] DETECTED dfn={dfnName} scope={scope} | detection_latency_ms={latency:F0}");
            }
        }

        private void LogReplanTriggeredInternal(string dfnName)
        {
            lock (lockObject)
            {
                if (!activeByDfn.TryGetValue(dfnName ?? "", out var rec) || rec.Closed) return;
                if (rec.ReplanTriggeredTime != default) return; // already triggered
                rec.ReplanTriggeredTime = DateTime.Now;
                var abortLatency = rec.DetectionTime != default
                    ? (rec.ReplanTriggeredTime - rec.DetectionTime).TotalMilliseconds
                    : (rec.ReplanTriggeredTime - rec.InjectionTime).TotalMilliseconds;
                WriteLog($"[FAULT #{rec.EventNumber}] REPLAN_TRIGGERED dfn={dfnName} | abort_latency_ms={abortLatency:F0}");
            }
        }

        private void LogResumedInternal(string dfnName, bool success)
        {
            lock (lockObject)
            {
                if (!activeByDfn.TryGetValue(dfnName ?? "", out var rec) || rec.Closed) return;
                rec.ResumeTime = DateTime.Now;
                rec.RecoverySuccess = success;
                rec.Closed = true;
                activeByDfn.Remove(rec.DfnName);
                var recoveryMs = (rec.ResumeTime - rec.InjectionTime).TotalMilliseconds;
                WriteLog($"[FAULT #{rec.EventNumber}] RESUMED dfn={dfnName} success={success} | recovery_ms={recoveryMs:F0}");
            }
        }

        private void GenerateCSVSummaryInternal()
        {
            lock (lockObject)
            {
                WriteSectionHeader("FAULT RECOVERY SUMMARY");

                // Pull all completed planner calls once, then attach to each fault event by time window.
                var plannerCalls = PlannerCallLogger.GetCompletedCalls();

                // ── Per-event CSV ──────────────────────────────────────
                var csv = new StringBuilder();
                csv.AppendLine("EventNumber,FaultId,FaultType,Scope,DetectedScope,DfnName,MLActionInstance,ParentHLInstance,TargetObject,"
                    + "InjectionTime,DetectionTime,ReplanTriggeredTime,ResumeTime,"
                    + "DetectionLatencyMs,AbortLatencyMs,RecoveryDurationMs,"
                    + "ReplanCallNumber,ReplanPlanningTimeMs,NewPlanLength,ReplanCallSuccess,"
                    + "RecoverySuccess,Closed,ExtraDetails");

                foreach (var r in records)
                {
                    // Find first planner call that started after replan-trigger (or after injection if no trigger captured)
                    var anchor = r.ReplanTriggeredTime != default ? r.ReplanTriggeredTime : r.InjectionTime;
                    var endBound = r.ResumeTime != default ? r.ResumeTime : DateTime.MaxValue;
                    var match = plannerCalls.FirstOrDefault(c => c.Start >= anchor && c.End <= endBound);
                    // Fallback: closest call starting after anchor regardless of end bound
                    if (match.CallNumber == 0)
                        match = plannerCalls.FirstOrDefault(c => c.Start >= anchor);

                    var detectionMs = (r.DetectionTime != default)
                        ? (r.DetectionTime - r.InjectionTime).TotalMilliseconds : -1;
                    var abortMs = (r.DetectionTime != default && r.ReplanTriggeredTime != default)
                        ? (r.ReplanTriggeredTime - r.DetectionTime).TotalMilliseconds : -1;
                    var recoveryMs = (r.ResumeTime != default)
                        ? (r.ResumeTime - r.InjectionTime).TotalMilliseconds : -1;

                    string fmt(DateTime t) => t == default ? "" : t.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    string esc(string s) => (s ?? "").Replace(",", ";").Replace("\n", " ");

                    csv.AppendLine(string.Join(",",
                        r.EventNumber,
                        esc(r.FaultId),
                        esc(r.FaultType),
                        esc(r.Scope),
                        esc(r.DetectedScope),
                        esc(r.DfnName),
                        esc(r.MLActionInstance),
                        esc(r.ParentHlInstance),
                        esc(r.TargetObject),
                        fmt(r.InjectionTime),
                        fmt(r.DetectionTime),
                        fmt(r.ReplanTriggeredTime),
                        fmt(r.ResumeTime),
                        detectionMs >= 0 ? detectionMs.ToString("F2") : "",
                        abortMs >= 0 ? abortMs.ToString("F2") : "",
                        recoveryMs >= 0 ? recoveryMs.ToString("F2") : "",
                        match.CallNumber > 0 ? match.CallNumber.ToString() : "",
                        match.CallNumber > 0 ? match.DurationMs.ToString("F2") : "",
                        match.CallNumber > 0 ? match.PlanLength.ToString() : "",
                        match.CallNumber > 0 ? match.Success.ToString() : "",
                        r.RecoverySuccess,
                        r.Closed,
                        esc(r.ExtraDetails)));
                }

                try
                {
                    var ts = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                    var path = $"WrittenLogs/FaultRecovery_{ts}.csv";
                    System.IO.File.WriteAllText(path, csv.ToString(), Encoding.UTF8);
                    WriteLog($"Per-event CSV written to: {path}");
                }
                catch (Exception ex)
                {
                    WriteLog($"Warning: could not write per-event CSV: {ex.Message}");
                }
                WriteLog("Per-Event CSV:");
                WriteLog(csv.ToString());

                // ── Aggregated summary by fault type ────────────────────
                var grouped = records
                    .GroupBy(r => r.FaultType)
                    .Select(g =>
                    {
                        var closed = g.Where(r => r.Closed && r.ResumeTime != default).ToList();
                        var detected = g.Where(r => r.DetectionTime != default).ToList();
                        var withReplan = g.Where(r => r.ReplanTriggeredTime != default).ToList();
                        double recAvg = closed.Count > 0
                            ? closed.Average(r => (r.ResumeTime - r.InjectionTime).TotalMilliseconds) : 0;
                        double recP95 = closed.Count > 0
                            ? Percentile(closed.Select(r => (r.ResumeTime - r.InjectionTime).TotalMilliseconds).ToList(), 0.95)
                            : 0;
                        double detAvg = detected.Count > 0
                            ? detected.Average(r => (r.DetectionTime - r.InjectionTime).TotalMilliseconds) : 0;
                        double abortAvg = withReplan.Where(r => r.DetectionTime != default).Any()
                            ? withReplan.Where(r => r.DetectionTime != default)
                                .Average(r => (r.ReplanTriggeredTime - r.DetectionTime).TotalMilliseconds)
                            : 0;
                        int successful = closed.Count(r => r.RecoverySuccess);
                        return new
                        {
                            FaultType = g.Key,
                            Total = g.Count(),
                            Closed = closed.Count,
                            Successful = successful,
                            SuccessRatePct = closed.Count > 0 ? successful * 100.0 / closed.Count : 0,
                            DetectionLatencyAvgMs = detAvg,
                            AbortLatencyAvgMs = abortAvg,
                            MTTRms = recAvg,
                            RecoveryP95ms = recP95,
                        };
                    })
                    .OrderByDescending(x => x.Total)
                    .ToList();

                var sum = new StringBuilder();
                sum.AppendLine("FaultType,Total,Closed,Successful,SuccessRatePct,DetectionLatencyAvgMs,AbortLatencyAvgMs,MTTRms,RecoveryP95ms");
                foreach (var g in grouped)
                {
                    sum.AppendLine($"{g.FaultType},{g.Total},{g.Closed},{g.Successful},{g.SuccessRatePct:F2},{g.DetectionLatencyAvgMs:F2},{g.AbortLatencyAvgMs:F2},{g.MTTRms:F2},{g.RecoveryP95ms:F2}");
                }
                // TOTAL row
                var allClosed = records.Where(r => r.Closed && r.ResumeTime != default).ToList();
                var totalRec = allClosed.Count > 0
                    ? allClosed.Average(r => (r.ResumeTime - r.InjectionTime).TotalMilliseconds) : 0;
                var totalSucc = allClosed.Count(r => r.RecoverySuccess);
                var totalRate = allClosed.Count > 0 ? totalSucc * 100.0 / allClosed.Count : 0;
                sum.AppendLine($"TOTAL,{records.Count},{allClosed.Count},{totalSucc},{totalRate:F2},,,{totalRec:F2},");

                try
                {
                    var ts = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                    var path = $"WrittenLogs/FaultRecoverySummary_{ts}.csv";
                    System.IO.File.WriteAllText(path, sum.ToString(), Encoding.UTF8);
                    WriteLog($"Aggregated summary CSV written to: {path}");
                }
                catch (Exception ex)
                {
                    WriteLog($"Warning: could not write summary CSV: {ex.Message}");
                }
                WriteLog("Aggregated Summary CSV:");
                WriteLog(sum.ToString());

                // ── Human-readable ──────────────────────────────────────
                WriteSeparator('=');
                WriteLog($"Total fault events:    {records.Count}");
                WriteLog($"Closed (resumed):      {allClosed.Count}");
                WriteLog($"Successful recoveries: {totalSucc} ({totalRate:F1}%)");
                if (allClosed.Count > 0)
                    WriteLog($"MTTR (mean recovery):  {totalRec:F0}ms ({totalRec / 1000.0:F2}s)");
                WriteSeparator('=');
            }
        }

        private static double Percentile(List<double> values, double p)
        {
            if (values == null || values.Count == 0) return 0;
            var sorted = values.OrderBy(v => v).ToList();
            double idx = p * (sorted.Count - 1);
            int lo = (int)Math.Floor(idx);
            int hi = (int)Math.Ceiling(idx);
            if (lo == hi) return sorted[lo];
            return sorted[lo] + (sorted[hi] - sorted[lo]) * (idx - lo);
        }

        private void CloseInternal()
        {
            WriteSectionHeader("FAULT RECOVERY LOGGER CLOSED");
            base.Close();
            instance = null;
        }

        // ── Inner record ─────────────────────────────────────────────────

        private class FaultRecoveryRecord
        {
            public int EventNumber { get; set; }
            public string FaultId { get; set; } = "";
            public string FaultType { get; set; } = "";
            public string Scope { get; set; } = "";          // LL / HL — declared at injection
            public string DetectedScope { get; set; } = "";  // which decorator actually fired
            public string DfnName { get; set; } = "";
            public string MLActionInstance { get; set; } = "";
            public string ParentHlInstance { get; set; } = "";
            public string TargetObject { get; set; } = "";
            public string ExtraDetails { get; set; } = "";
            public DateTime InjectionTime { get; set; }
            public DateTime DetectionTime { get; set; }
            public DateTime ReplanTriggeredTime { get; set; }
            public DateTime ResumeTime { get; set; }
            public bool RecoverySuccess { get; set; }
            public bool Closed { get; set; }
        }
    }
}
