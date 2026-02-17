using System;
using BehaviorTreeMainProject.Log;

namespace BehaviorTreeMainProject.Log.Services
{
    /// <summary>
    /// Lightweight logger that records only the final result (SUCCESS / FAILED)
    /// of ML-level actions.  Produces a compact log file alongside the verbose
    /// ActionExecution log.
    /// </summary>
    public class MLActionResultLogger : BaseLogger
    {
        private static MLActionResultLogger? instance;
        private static readonly object lockObject = new object();
        private int resultCounter = 0;
        private readonly DateTime startTime;

        public static MLActionResultLogger Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (lockObject)
                    {
                        if (instance == null)
                        {
                            instance = new MLActionResultLogger();
                        }
                    }
                }
                return instance;
            }
        }

        private MLActionResultLogger()
        {
            startTime = DateTime.Now;

            base.Initialize("MLActionResult", true, true);

            WriteToLog("=== ML Action Result Log ===");
            WriteToLog($"Started at: {startTime:yyyy-MM-dd HH:mm:ss.fff}");
            WriteToLog("Only final SUCCESS / FAILED outcomes for ML actions.");
            WriteToLog("Format: [Counter] [Timestamp] [ActionName] [InstanceName] [Result] (+elapsed)");
            WriteToLog("============================");
        }

        // ── Public API ──────────────────────────────────────────────

        /// <summary>
        /// Record a SUCCESS result for an ML action.
        /// </summary>
        public void LogSuccess(string actionName, string instanceName)
        {
            Log(actionName, instanceName, "SUCCESS");
        }

        /// <summary>
        /// Record a FAILED result for an ML action.
        /// </summary>
        public void LogFailure(string actionName, string instanceName)
        {
            Log(actionName, instanceName, "FAILED");
        }

        // ── Internals ───────────────────────────────────────────────

        private void Log(string actionName, string instanceName, string result)
        {
            lock (lockObject)
            {
                resultCounter++;
                var now = DateTime.Now;
                var elapsed = now - startTime;
                var ts = now.ToString("HH:mm:ss.fff");

                WriteToLog($"[{resultCounter:D4}] [{ts}] [{actionName}] [{instanceName}] [{result}] (+{elapsed.TotalMilliseconds:F0}ms)");
            }
        }

        private void WriteToLog(string message)
        {
            base.WriteLog(message);
        }

        public new string GetLogFilePath()
        {
            return base.GetLogFilePath();
        }

        public new static void Close()
        {
            lock (lockObject)
            {
                if (instance != null)
                {
                    ((BaseLogger)instance).Close();
                    instance = null;
                }
            }
        }
    }
}
