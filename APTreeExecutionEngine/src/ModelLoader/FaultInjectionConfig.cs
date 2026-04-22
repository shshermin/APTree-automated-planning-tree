using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using BehaviorTreeMainProject.Log.Services;

namespace BehaviorTreeMainProject.ModelLoader
{
    /// <summary>
    /// Top-level fault injection config loaded from JSON.
    /// Lists all faults that the <see cref="Services.FaultInjection.FaultInjectionService"/>
    /// should watch for during execution.
    /// </summary>
    public class FaultInjectionConfig
    {
        public List<FaultDefinition> Faults { get; set; } = new();

        public static FaultInjectionConfig LoadFromFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                LoggingService.LogInfo("🧪 FaultInjectionConfig: No fault config path provided — fault injection disabled");
                return new FaultInjectionConfig();
            }

            string fullPath = Path.GetFullPath(path);
            if (!File.Exists(fullPath))
            {
                LoggingService.LogWarning($"🧪 FaultInjectionConfig: File not found: {fullPath} — fault injection disabled");
                return new FaultInjectionConfig();
            }

            try
            {
                string json = File.ReadAllText(fullPath);
                var opts = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                };
                var cfg = JsonSerializer.Deserialize<FaultInjectionConfig>(json, opts)
                          ?? new FaultInjectionConfig();
                LoggingService.LogSuccess($"🧪 FaultInjectionConfig: Loaded {cfg.Faults.Count} fault definition(s) from {fullPath}");
                return cfg;
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"🧪 FaultInjectionConfig: Failed to parse {fullPath}: {ex.Message}");
                return new FaultInjectionConfig();
            }
        }
    }

    /// <summary>
    /// A single fault definition. Currently only DropAfterClose is implemented,
    /// but the schema is generic so more fault types can be added later
    /// (e.g. ProtectiveStop, PlaceUnavailable).
    /// </summary>
    public class FaultDefinition
    {
        /// <summary>Unique id of this fault, used for latching and logging.</summary>
        public string Id { get; set; }

        /// <summary>Fault type: "DropAfterClose" (only type supported for now).</summary>
        public string Type { get; set; }

        /// <summary>Trigger rule — when should this fault fire.</summary>
        public FaultTrigger Trigger { get; set; } = new();

        /// <summary>Effects to apply when triggered.</summary>
        public FaultEffects Effects { get; set; } = new();
    }

    public class FaultTrigger
    {
        /// <summary>ML action type to match, e.g. "PickUpML".</summary>
        public string MlActionType { get; set; }

        /// <summary>Substring that must appear in the parent HL action InstanceName,
        /// e.g. "stick4" to target only PickUpHL instances carrying stick4.</summary>
        public string ParentInstanceContains { get; set; }

        /// <summary>How many times the matching ML action must have become active
        /// before firing. Default 1 (fire on first activation).</summary>
        public int OnActivationCount { get; set; } = 1;
    }

    public class FaultEffects
    {
        /// <summary>Element that is dropped (e.g. "stick4").</summary>
        public string DroppedObject { get; set; }

        /// <summary>Agent holding the dropped object (e.g. "robot1").</summary>
        public string Robot { get; set; }

        /// <summary>Gripper that becomes empty (e.g. "gripper1").</summary>
        public string Gripper { get; set; }

        /// <summary>Name for the new temporary location where the object lands
        /// (e.g. "temploc1"). Will be added to the blackboard and the PDDL :objects.</summary>
        public string TempLocationName { get; set; } = "temploc1";

        /// <summary>PDDL type for the temp location in the problem file (e.g. "firstposition").</summary>
        public string TempLocationPddlType { get; set; } = "firstposition";

        /// <summary>Position [x,y,z] of the temp location. Defaults to (0,0,0).</summary>
        public double[] TempLocationPosition { get; set; } = new double[] { 0.0, 0.0, 0.0 };
    }
}
