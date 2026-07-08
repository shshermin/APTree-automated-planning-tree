using System;
using System.IO;
using System.Text.Json;
using BehaviorTreeMainProject.Log.Services;

namespace BehaviorTreeMainProject.ModelLoader
{
    /// <summary>
    /// External configuration for runtime settings that are NOT part of the DSL model.
    /// PDDL file paths are resolved by naming convention in the JSON generator —
    /// this class only holds planner timeouts, tick settings, blackboard files,
    /// and ML subtree configuration.
    /// 
    /// Can be loaded from a JSON file or constructed programmatically.
    /// </summary>
    public class BehaviorTreeConfiguration
    {
        // ── Planner settings ──

        /// <summary>Default planner timeout in seconds.</summary>
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>Default max plan length.</summary>
        public int MaxPlanLength { get; set; } = 20;

        /// <summary>Default execution mode: Sequential, Parallel, or Hybrid.</summary>
        public string ExecutionMode { get; set; } = "Sequential";

        /// <summary>Explicit planner path sent to the Flask service.
        /// Leave empty/null to let Flask fall back to its own DEFAULT_ENHSP_PATH.
        /// Example on the Ubuntu VM: "/home/ubuntu/ENHSP-Public/enhsp.jar".</summary>
        public string PlannerPath { get; set; } = "";

        // ── ML subtree (HL→ML decomposition) configuration ──

        /// <summary>Name for the subtree config registered with ServiceSubtreeInject.</summary>
        public string SubtreeConfigName { get; set; } = "ENHSP_Default";

        /// <summary>Planner name used for ML subtrees (e.g. "Enhsp").</summary>
        public string SubtreePlannerName { get; set; } = "Enhsp";

        /// <summary>Domain file for ML-level planning.</summary>
        public string SubtreeDomainFile { get; set; }

        /// <summary>Problem file template for ML-level planning.</summary>
        public string SubtreeProblemFile { get; set; }

        /// <summary>ENHSP config for ML-level planner (e.g. "opt-hmax").</summary>
        public string SubtreeEnhspConfig { get; set; } = "opt-hmax";

        /// <summary>Explicit planner path sent to Flask for ML subtree planners.
        /// Leave empty/null to fall back to the top-level PlannerPath, which in turn
        /// falls back to Flask's own default.</summary>
        public string SubtreePlannerPath { get; set; } = "";

        /// <summary>Timeout for ML subtree planners.</summary>
        public int SubtreeTimeoutSeconds { get; set; } = 30;

        /// <summary>Max plan length for ML subtree planners.</summary>
        public int SubtreeMaxPlanLength { get; set; } = 20;

        /// <summary>Execution mode for ML subtree planners.</summary>
        public string SubtreeExecutionMode { get; set; } = "Sequential";

        // ── Blackboard input files ──

        /// <summary>Path to setup objects JSON file (parameter instances).</summary>
        public string SetupObjectsFile { get; set; }

        /// <summary>Path to initial state predicates JSON file.</summary>
        public string InitialStateFile { get; set; }

        /// <summary>Path to goal state predicates JSON file.</summary>
        public string GoalStateFile { get; set; }

        /// <summary>Path to action instances text file.</summary>
        public string ActionInstancesFile { get; set; }

        // ── Robot execution toggle ──

        /// <summary>
        /// When true, ServiceLLSubtreeInject expands ML actions into LL subtrees and
        /// sends robot commands via the Flask service.
        /// When false, LL injection is skipped — only HL→ML PDDL planning runs (planning-only mode).
        /// </summary>
        public bool ExecutionActive { get; set; } = true;

        // ── Predicate store ──

        /// <summary>
        /// Which predicate store to use.  Accepted values:
        ///   "Dictionary"  (default) — in-process Dictionary, zero overhead.
        ///   "Sqlite"               — Dictionary hot-index + embedded SQLite for
        ///                            HasSimilar / CleanupAtAgent indexed queries.
        /// </summary>
        public string PredicateStoreType { get; set; } = "Dictionary";

        /// <summary>
        /// File path for the SQLite predicate store.
        /// ":memory:" (default) keeps the database entirely in RAM.
        /// Set to an absolute path to persist it across runs or inspect it
        /// with an external tool (e.g. DB Browser for SQLite).
        /// Ignored when PredicateStoreType != "Sqlite".
        /// </summary>
        public string SqlitePredicateStorePath { get; set; } = ":memory:";

        // ── Tick loop settings ──

        /// <summary>Maximum number of ticks before stopping execution.</summary>
        public int MaxTicks { get; set; } = 0;

        /// <summary>Delay in milliseconds between ticks.</summary>
        public int TickDelayMs { get; set; } = 10;

        // ── Execution mode parsing ──

        /// <summary>
        /// Parses the ExecutionMode string to the ServicePDDLPlanning enum.
        /// </summary>
        public Services.AIPlanning.ServicePDDLPlanning.ParallelExecutionMode GetExecutionMode()
        {
            return ExecutionMode?.ToLowerInvariant() switch
            {
                "parallel" => Services.AIPlanning.ServicePDDLPlanning.ParallelExecutionMode.Parallel,
                "hybrid" => Services.AIPlanning.ServicePDDLPlanning.ParallelExecutionMode.Hybrid,
                _ => Services.AIPlanning.ServicePDDLPlanning.ParallelExecutionMode.Sequential
            };
        }

        // ── Serialization ──

        /// <summary>
        /// Loads a BehaviorTreeConfiguration from a JSON file.
        /// </summary>
        public static BehaviorTreeConfiguration LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Configuration file not found: {filePath}");

            string json = File.ReadAllText(filePath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };
            return JsonSerializer.Deserialize<BehaviorTreeConfiguration>(json, options);
        }

        /// <summary>
        /// Saves this configuration to a JSON file.
        /// </summary>
        public void SaveToFile(string filePath)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            string json = JsonSerializer.Serialize(this, options);
            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// Creates a default Demonstrator configuration for backward compatibility.
        /// PDDL paths are now in the JSON model — this only sets runtime defaults.
        /// </summary>
        public static BehaviorTreeConfiguration CreateDemonstratorDefault()
        {
            string basePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "src");

            return new BehaviorTreeConfiguration
            {
                TimeoutSeconds = 30,
                ExecutionMode = "Sequential",

                SubtreeConfigName = "ENHSP_Demonstrator",
                SubtreePlannerName = "Enhsp",
                SubtreeDomainFile = "./Plannerinputs/static/Demonstrator/DomainMLTruss.pddl",
                SubtreeProblemFile = "./Plannerinputs/static/Demonstrator/ProblemL1L2.pddl",
                SubtreeEnhspConfig = "opt-hmax",
                SubtreeTimeoutSeconds = 30,
                SubtreeExecutionMode = "Sequential",

                MaxTicks = 0,
                TickDelayMs = 10
            };
        }
    }
}
