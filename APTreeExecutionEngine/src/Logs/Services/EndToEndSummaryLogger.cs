using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BehaviorTreeMainProject.Log.Services
{
    /// <summary>
    /// Produces a single end-to-end summary file combining metrics from all loggers.
    /// Designed for direct use in paper evaluation tables and charts.
    /// 
    /// Captures:
    ///  - Total task time (wall clock)
    ///  - Task planning metrics (from PlannerCallLogger)
    ///  - Motion execution metrics (from RobotCommandLogger)
    ///  - Hierarchical decomposition counts (from HierarchicalTraceLogger)
    ///  - Planning-to-execution ratio
    ///  - Per-phase timing breakdown
    /// </summary>
    public class EndToEndSummaryLogger : BaseLogger
    {
        private static EndToEndSummaryLogger? instance;
        private static readonly object lockObject = new object();

        private DateTime? _taskStartTime;
        private DateTime? _taskEndTime;
        private string _scenarioName = "";

        // Phase timing
        private readonly List<PhaseRecord> _phases = new List<PhaseRecord>();
        private int _phaseCounter = 0;

        // Replanning events
        private int _replanCount = 0;
        private int _recoveryCount = 0;

        // Pause tracking
        private DateTime? _pauseStartTime;
        private double _totalPauseDurationSec = 0;

        public static EndToEndSummaryLogger Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (lockObject)
                    {
                        if (instance == null)
                        {
                            instance = new EndToEndSummaryLogger();
                        }
                    }
                }
                return instance;
            }
        }

        private EndToEndSummaryLogger()
        {
            base.Initialize("EndToEndSummary", enableConsole: false, enableFile: true);
            WriteSectionHeader("END-TO-END TASK SUMMARY");
            WriteLog("Aggregated metrics for paper evaluation");
            WriteSeparator();
        }

        // ── Public API ───────────────────────────────────────────────────

        /// <summary>Mark the start of the entire task execution.</summary>
        public static void LogTaskStart(string scenarioName)
        {
            lock (lockObject)
            {
                Instance._taskStartTime = DateTime.Now;
                Instance._scenarioName = scenarioName ?? "Unknown";
                Instance.WriteLog($"[TASK] START '{scenarioName}' at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
            }
        }

        /// <summary>Mark the end of the entire task execution.</summary>
        public static void LogTaskEnd(bool success)
        {
            lock (lockObject)
            {
                Instance._taskEndTime = DateTime.Now;
                var elapsed = Instance._taskStartTime.HasValue
                    ? (Instance._taskEndTime.Value - Instance._taskStartTime.Value).TotalSeconds : 0;
                Instance.WriteLog($"[TASK] {(success ? "COMPLETED" : "FAILED")} '{Instance._scenarioName}' at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} ({elapsed:F1}s)");
            }
        }

        /// <summary>Log start of a phase (e.g. "Layers1_2", "Layers3_4").</summary>
        public static int LogPhaseStart(string phaseName)
        {
            lock (lockObject)
            {
                Instance._phaseCounter++;
                Instance._phases.Add(new PhaseRecord
                {
                    Id = Instance._phaseCounter,
                    Name = phaseName ?? "Unknown",
                    StartTime = DateTime.Now
                });
                Instance.WriteLog($"[PHASE #{Instance._phaseCounter}] START '{phaseName}'");
                return Instance._phaseCounter;
            }
        }

        /// <summary>Log end of a phase.</summary>
        public static void LogPhaseEnd(int phaseId, bool success)
        {
            lock (lockObject)
            {
                var phase = Instance._phases.FirstOrDefault(p => p.Id == phaseId);
                if (phase != null)
                {
                    phase.EndTime = DateTime.Now;
                    phase.Success = success;
                    var elapsed = (phase.EndTime.Value - phase.StartTime).TotalSeconds;
                    Instance.WriteLog($"[PHASE #{phaseId}] {(success ? "COMPLETED" : "FAILED")} '{phase.Name}' ({elapsed:F1}s)");
                }
            }
        }

        /// <summary>Increment the replanning counter.</summary>
        public static void LogReplan()
        {
            lock (lockObject)
            {
                Instance._replanCount++;
                Instance.WriteLog($"[REPLAN #{Instance._replanCount}] Replanning triggered at {DateTime.Now:HH:mm:ss.fff}");
            }
        }

        /// <summary>Increment the recovery counter (operator retry after LL failure).</summary>
        public static void LogRecovery()
        {
            lock (lockObject)
            {
                Instance._recoveryCount++;
                Instance.WriteLog($"[RECOVERY #{Instance._recoveryCount}] Recovery triggered at {DateTime.Now:HH:mm:ss.fff}");
            }
        }

        /// <summary>Call when execution is paused (e.g. user presses P).</summary>
        public static void LogPauseStart()
        {
            lock (lockObject)
            {
                Instance._pauseStartTime = DateTime.Now;
                Instance.WriteLog($"[PAUSE] Started at {DateTime.Now:HH:mm:ss.fff}");
            }
        }

        /// <summary>Call when execution resumes after a pause.</summary>
        public static void LogPauseEnd()
        {
            lock (lockObject)
            {
                if (Instance._pauseStartTime.HasValue)
                {
                    var pauseDuration = (DateTime.Now - Instance._pauseStartTime.Value).TotalSeconds;
                    Instance._totalPauseDurationSec += pauseDuration;
                    Instance.WriteLog($"[PAUSE] Ended at {DateTime.Now:HH:mm:ss.fff} (paused {pauseDuration:F1}s, total paused: {Instance._totalPauseDurationSec:F1}s)");
                    Instance._pauseStartTime = null;
                }
            }
        }

        /// <summary>Generate the final summary and close.</summary>
        public static void GenerateFinalSummary()
        {
            Instance.GenerateFinalSummaryInternal();
        }

        /// <summary>Close the logger.</summary>
        public new static void Close()
        {
            Instance.CloseInternal();
        }

        // ── Internal ─────────────────────────────────────────────────────

        private void GenerateFinalSummaryInternal()
        {
            lock (lockObject)
            {
                WriteSectionHeader("FINAL END-TO-END SUMMARY");
                WriteLog($"Scenario: {_scenarioName}");
                WriteLog($"Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                WriteSeparator();

                // Wall clock
                double wallClockSec = 0;
                if (_taskStartTime.HasValue && _taskEndTime.HasValue)
                {
                    wallClockSec = (_taskEndTime.Value - _taskStartTime.Value).TotalSeconds;
                    WriteLog($"Total wall-clock time: {wallClockSec:F1}s ({wallClockSec / 60.0:F1} min)");
                }
                else if (_taskStartTime.HasValue)
                {
                    wallClockSec = (DateTime.Now - _taskStartTime.Value).TotalSeconds;
                    WriteLog($"Elapsed time (task not formally ended): {wallClockSec:F1}s ({wallClockSec / 60.0:F1} min)");
                }

                // Pause-adjusted time
                if (_totalPauseDurationSec > 0)
                {
                    double activeTimeSec = wallClockSec - _totalPauseDurationSec;
                    WriteLog($"Total pause time: {_totalPauseDurationSec:F1}s");
                    WriteLog($"Active execution time: {activeTimeSec:F1}s ({activeTimeSec / 60.0:F1} min)");
                }

                WriteSeparator();

                // Phase breakdown
                WriteSubsectionHeader("Phase Breakdown");
                foreach (var p in _phases)
                {
                    var dur = p.EndTime.HasValue ? (p.EndTime.Value - p.StartTime).TotalSeconds : 0;
                    WriteLog($"  Phase '{p.Name}': {dur:F1}s — {(p.Success ? "Success" : "Failed/In-progress")}");
                }

                WriteSeparator();

                // Replanning/recovery
                WriteSubsectionHeader("Robustness");
                WriteLog($"  Replanning events: {_replanCount}");
                WriteLog($"  Recovery events (operator retry): {_recoveryCount}");

                WriteSeparator();

                // Summary CSV (one row per run, suitable for multi-run comparison)
                WriteSubsectionHeader("CSV Summary (one row per run)");
                double activeTimeCsv = wallClockSec - _totalPauseDurationSec;
                var csv = new StringBuilder();
                csv.AppendLine("Scenario,Date,WallClockSec,ActiveTimeSec,PauseTimeSec,Phases,ReplanCount,RecoveryCount");
                csv.AppendLine($"{_scenarioName},{DateTime.Now:yyyy-MM-dd HH:mm:ss},{wallClockSec:F2},{activeTimeCsv:F2},{_totalPauseDurationSec:F2},{_phases.Count},{_replanCount},{_recoveryCount}");
                WriteLog(csv.ToString());

                try
                {
                    var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                    var path = $"WrittenLogs/EndToEndSummary_{timestamp}.csv";
                    System.IO.File.WriteAllText(path, csv.ToString(), Encoding.UTF8);
                    WriteLog($"CSV written to: {path}");
                }
                catch (Exception ex)
                {
                    WriteLog($"Warning: Could not write CSV: {ex.Message}");
                }

                WriteSeparator('=');
            }
        }

        private void CloseInternal()
        {
            WriteSectionHeader("END-TO-END SUMMARY LOGGER CLOSED");
            base.Close();
            instance = null;
        }

        private class PhaseRecord
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public DateTime StartTime { get; set; }
            public DateTime? EndTime { get; set; }
            public bool Success { get; set; }
        }
    }
}
