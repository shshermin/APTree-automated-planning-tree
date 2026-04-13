using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BehaviorTreeMainProject.Log.Services
{
    /// <summary>
    /// Dedicated logger for tracking every robot motion command sent via REST.
    /// Records: command number, timestamp, parent ML action, LL action type,
    /// command type (movej/movel/planned/etc.), target position, resolved coordinates,
    /// end effector, velocity, acceleration, execution time, and success/failure.
    /// Produces both a per-command log and a CSV summary on close.
    /// </summary>
    public class RobotCommandLogger : BaseLogger
    {
        private static RobotCommandLogger? instance;
        private static readonly object lockObject = new object();

        private readonly List<RobotCommandRecord> commandRecords = new List<RobotCommandRecord>();
        private int commandCounter = 0;

        public static RobotCommandLogger Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (lockObject)
                    {
                        if (instance == null)
                        {
                            instance = new RobotCommandLogger();
                        }
                    }
                }
                return instance;
            }
        }

        private RobotCommandLogger()
        {
            base.Initialize("RobotCommands", enableConsole: false, enableFile: true);
            WriteSectionHeader("ROBOT COMMAND LOGGER INITIALIZED");
            WriteLog("Tracking all robot motion/gripper commands sent via REST");
            WriteLog($"Columns: CmdNumber, Timestamp, LLActionType, InstanceName, CommandType, TargetPosition, Pose, Joints, EndEffector, Velocity, Acceleration, Success, ExecutionTimeSec, PlanningTimeSec, Error");
            WriteSeparator();
        }

        // ── Public static API ────────────────────────────────────────────

        /// <summary>
        /// Log the start of a robot command. Returns a command ID to pair with LogCommandEnd.
        /// </summary>
        public static int LogCommandStart(
            string llActionType,
            string instanceName,
            string commandType,
            string targetPosition,
            double[]? pose,
            double[]? joints,
            string? endEffectorType,
            double velocity,
            double acceleration,
            string? parentMLAction = null)
        {
            return Instance.LogCommandStartInternal(
                llActionType, instanceName, commandType, targetPosition,
                pose, joints, endEffectorType, velocity, acceleration, parentMLAction);
        }

        /// <summary>
        /// Log the successful end of a robot command.
        /// </summary>
        public static void LogCommandEnd(int commandId, bool success, double executionTimeSeconds, double planningTimeSeconds = 0, string? message = null)
        {
            Instance.LogCommandEndInternal(commandId, success, executionTimeSeconds, planningTimeSeconds, message, null);
        }

        /// <summary>
        /// Log a failed robot command.
        /// </summary>
        public static void LogCommandFailed(int commandId, double executionTimeSeconds, string error)
        {
            Instance.LogCommandEndInternal(commandId, false, executionTimeSeconds, 0, null, error);
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

        private int LogCommandStartInternal(
            string llActionType, string instanceName, string commandType,
            string targetPosition, double[]? pose, double[]? joints,
            string? endEffectorType, double velocity, double acceleration,
            string? parentMLAction)
        {
            lock (lockObject)
            {
                commandCounter++;
                var record = new RobotCommandRecord
                {
                    CommandNumber = commandCounter,
                    StartTime = DateTime.Now,
                    LLActionType = llActionType ?? "Unknown",
                    InstanceName = instanceName ?? "Unknown",
                    CommandType = commandType ?? "Unknown",
                    TargetPosition = targetPosition ?? "",
                    Pose = pose,
                    Joints = joints,
                    EndEffectorType = endEffectorType ?? "",
                    Velocity = velocity,
                    Acceleration = acceleration,
                    ParentMLAction = parentMLAction ?? ""
                };
                commandRecords.Add(record);

                var poseStr = pose != null ? $"[{string.Join(", ", pose.Select(p => p.ToString("F4")))}]" : "none";
                var jointsStr = joints != null ? $"[{string.Join(", ", joints.Select(j => j.ToString("F4")))}]" : "none";

                WriteLog($"[CMD #{commandCounter}] START | {llActionType} '{instanceName}' | {commandType} → {targetPosition}");
                WriteLog($"  Pose: {poseStr}");
                WriteLog($"  Joints: {jointsStr}");
                WriteLog($"  EndEffector: {endEffectorType ?? "default"} | Vel: {velocity:F2} | Accel: {acceleration:F2}");
                if (!string.IsNullOrEmpty(parentMLAction))
                    WriteLog($"  Parent ML Action: {parentMLAction}");

                return commandCounter;
            }
        }

        private void LogCommandEndInternal(int commandId, bool success, double executionTimeSeconds, double planningTimeSeconds, string? message, string? error)
        {
            lock (lockObject)
            {
                var record = commandRecords.FirstOrDefault(r => r.CommandNumber == commandId);
                if (record == null)
                {
                    WriteLog($"[CMD #{commandId}] END | WARNING: No matching start record found");
                    return;
                }

                record.EndTime = DateTime.Now;
                record.Success = success;
                record.ExecutionTimeSeconds = executionTimeSeconds;
                record.PlanningTimeSeconds = planningTimeSeconds;
                record.Error = error;
                record.Message = message;
                record.Completed = true;

                var totalMs = (record.EndTime.Value - record.StartTime).TotalMilliseconds;
                var execMs = executionTimeSeconds * 1000.0;
                var planMs = planningTimeSeconds * 1000.0;

                if (success)
                {
                    var planInfo = planningTimeSeconds > 0 ? $" | PlanTime: {planMs:F0}ms" : "";
                    WriteLog($"[CMD #{commandId}] SUCCESS | {record.CommandType} → {record.TargetPosition} | ExecTime: {execMs:F0}ms{planInfo} | TotalTime: {totalMs:F0}ms");
                }
                else
                {
                    WriteLog($"[CMD #{commandId}] FAILED | {record.CommandType} → {record.TargetPosition} | ExecTime: {execMs:F0}ms | TotalTime: {totalMs:F0}ms | Error: {error ?? "Unknown"}");
                }
            }
        }

        private void GenerateCSVSummaryInternal()
        {
            lock (lockObject)
            {
                WriteSectionHeader("ROBOT COMMAND SUMMARY");

                // ── Per-command CSV ─────────────────────────────
                var csvPerCmd = new StringBuilder();
                csvPerCmd.AppendLine("CmdNumber,Timestamp,LLActionType,InstanceName,CommandType,TargetPosition,EndEffector,Velocity,Acceleration,Pose,Joints,Success,ExecTimeMs,PlanTimeMs,TotalTimeMs,ParentMLAction,Error");

                foreach (var r in commandRecords)
                {
                    var totalMs = r.Completed && r.EndTime.HasValue
                        ? (r.EndTime.Value - r.StartTime).TotalMilliseconds : 0;
                    var execMs = r.ExecutionTimeSeconds * 1000.0;
                    var planMs = r.PlanningTimeSeconds * 1000.0;
                    var poseStr = r.Pose != null ? string.Join(";", r.Pose.Select(p => p.ToString("F6"))) : "";
                    var jointsStr = r.Joints != null ? string.Join(";", r.Joints.Select(j => j.ToString("F6"))) : "";
                    var errorEscaped = (r.Error ?? "").Replace(",", ";").Replace("\n", " ");

                    csvPerCmd.AppendLine($"{r.CommandNumber},{r.StartTime:yyyy-MM-dd HH:mm:ss.fff},{r.LLActionType},{r.InstanceName},{r.CommandType},{r.TargetPosition},{r.EndEffectorType},{r.Velocity:F2},{r.Acceleration:F2},\"{poseStr}\",\"{jointsStr}\",{r.Success},{execMs:F2},{planMs:F2},{totalMs:F2},{r.ParentMLAction},{errorEscaped}");
                }

                WriteLog("Per-Command CSV:");
                WriteLog(csvPerCmd.ToString());

                // Write per-command CSV to file
                try
                {
                    var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                    var perCmdPath = $"WrittenLogs/RobotCommands_{timestamp}.csv";
                    System.IO.File.WriteAllText(perCmdPath, csvPerCmd.ToString(), Encoding.UTF8);
                    WriteLog($"Per-command CSV written to: {perCmdPath}");
                }
                catch (Exception ex)
                {
                    WriteLog($"Warning: Could not write per-command CSV: {ex.Message}");
                }

                // ── Aggregated summary by command type ───────────
                var grouped = commandRecords
                    .GroupBy(r => r.CommandType)
                    .Select(g => new
                    {
                        CommandType = g.Key,
                        Count = g.Count(),
                        Successful = g.Count(r => r.Success),
                        Failed = g.Count(r => !r.Success),
                        AvgExecTimeMs = g.Where(r => r.Completed).Select(r => r.ExecutionTimeSeconds * 1000.0).DefaultIfEmpty(0).Average(),
                        AvgPlanTimeMs = g.Where(r => r.Completed && r.PlanningTimeSeconds > 0).Select(r => r.PlanningTimeSeconds * 1000.0).DefaultIfEmpty(0).Average(),
                        TotalExecTimeMs = g.Sum(r => r.ExecutionTimeSeconds * 1000.0),
                        TotalPlanTimeMs = g.Sum(r => r.PlanningTimeSeconds * 1000.0)
                    })
                    .OrderByDescending(g => g.Count)
                    .ToList();

                var csvSummary = new StringBuilder();
                csvSummary.AppendLine("CommandType,Count,Successful,Failed,SuccessRate,AvgExecTimeMs,AvgPlanTimeMs,TotalExecTimeMs,TotalPlanTimeMs");

                foreach (var g in grouped)
                {
                    var rate = g.Count > 0 ? (double)g.Successful / g.Count * 100 : 0;
                    csvSummary.AppendLine($"{g.CommandType},{g.Count},{g.Successful},{g.Failed},{rate:F2}%,{g.AvgExecTimeMs:F2},{g.AvgPlanTimeMs:F2},{g.TotalExecTimeMs:F2},{g.TotalPlanTimeMs:F2}");
                }

                // TOTAL row
                var totalCmds = commandRecords.Count;
                var totalSuccess = commandRecords.Count(r => r.Success);
                var totalFailed = totalCmds - totalSuccess;
                var totalRate = totalCmds > 0 ? (double)totalSuccess / totalCmds * 100 : 0;
                var avgExec = totalCmds > 0 ? commandRecords.Where(r => r.Completed).Average(r => r.ExecutionTimeSeconds * 1000.0) : 0;
                var avgPlan = commandRecords.Where(r => r.Completed && r.PlanningTimeSeconds > 0).Select(r => r.PlanningTimeSeconds * 1000.0).DefaultIfEmpty(0).Average();
                var grandExec = commandRecords.Sum(r => r.ExecutionTimeSeconds * 1000.0);
                var grandPlan = commandRecords.Sum(r => r.PlanningTimeSeconds * 1000.0);

                csvSummary.AppendLine($"TOTAL,{totalCmds},{totalSuccess},{totalFailed},{totalRate:F2}%,{avgExec:F2},{avgPlan:F2},{grandExec:F2},{grandPlan:F2}");

                WriteLog("Aggregated Summary CSV:");
                WriteLog(csvSummary.ToString());

                try
                {
                    var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                    var summaryPath = $"WrittenLogs/RobotCommandSummary_{timestamp}.csv";
                    System.IO.File.WriteAllText(summaryPath, csvSummary.ToString(), Encoding.UTF8);
                    WriteLog($"Summary CSV written to: {summaryPath}");
                }
                catch (Exception ex)
                {
                    WriteLog($"Warning: Could not write summary CSV: {ex.Message}");
                }

                // ── Human-readable summary ───────────────────────
                WriteSeparator('=');
                WriteLog($"Total robot commands: {totalCmds}");
                WriteLog($"Successful: {totalSuccess} ({totalRate:F1}%)");
                WriteLog($"Failed: {totalFailed}");
                WriteLog($"Total execution time: {grandExec:F0}ms ({grandExec / 1000.0:F1}s)");
                WriteLog($"Total planning time (MoveIt): {grandPlan:F0}ms ({grandPlan / 1000.0:F1}s)");
                WriteLog($"Total motion time: {(grandExec + grandPlan):F0}ms ({(grandExec + grandPlan) / 1000.0:F1}s)");
                WriteSeparator('=');
            }
        }

        private void CloseInternal()
        {
            WriteSectionHeader("ROBOT COMMAND LOGGER CLOSED");
            base.Close();
            instance = null;
        }

        // ── Inner record class ───────────────────────────────────────────

        private class RobotCommandRecord
        {
            public int CommandNumber { get; set; }
            public DateTime StartTime { get; set; }
            public DateTime? EndTime { get; set; }
            public string LLActionType { get; set; } = "";
            public string InstanceName { get; set; } = "";
            public string CommandType { get; set; } = "";
            public string TargetPosition { get; set; } = "";
            public double[]? Pose { get; set; }
            public double[]? Joints { get; set; }
            public string EndEffectorType { get; set; } = "";
            public double Velocity { get; set; }
            public double Acceleration { get; set; }
            public string ParentMLAction { get; set; } = "";
            public bool Success { get; set; }
            public double ExecutionTimeSeconds { get; set; }
            public double PlanningTimeSeconds { get; set; }
            public string? Error { get; set; }
            public string? Message { get; set; }
            public bool Completed { get; set; }
        }
    }
}
