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
    /// Lists all faults that the <see cref="Services.FaultInjection.DummyCameraService"/>
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

        /// <summary>Optional: require a specific LL step (by InstanceName substring,
        /// case-insensitive) inside the ML action's LL subtree to have started or
        /// completed before the fault fires. Lets faults target points like
        /// "after gripper close, during retract" (e.g. "moveToRetract").
        /// When null/empty, the fault fires as soon as the ML action is InProgress.</summary>
        public string AfterLLStep { get; set; }
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

        /// <summary>Optional: location where the robot currently is (atagent <robot> <from>=true).
        /// If set, the fault flips that predicate to false.</summary>
        public string RobotFromLocation { get; set; }

        /// <summary>Optional: location where the robot should appear after the fault
        /// (atagent <robot> <to>=true). If the predicate does not exist on the blackboard,
        /// a new one is created.</summary>
        public string RobotToLocation { get; set; }

        // ── BlockerOnTop fault fields ────────────────────────────────────

        /// <summary>Target element that becomes blocked (e.g. "stick9"). The
        /// planner must first move the blocker off this object before picking it up.</summary>
        public string TargetObject { get; set; }

        /// <summary>Element that sits on top of the target, blocking access.
        /// If the element does not exist on the blackboard it is created
        /// dynamically as a Stick at fault-fire time.</summary>
        public string BlockerObject { get; set; }

        /// <summary>PDDL type for the blocker element in the problem file
        /// (e.g. "stick"). Defaults to "stick".</summary>
        public string BlockerPddlType { get; set; } = "stick";

        /// <summary>Location at which the blocker is placed (typically the
        /// target's own initial location, e.g. "initlocstick9").</summary>
        public string BlockerAtLocation { get; set; }

        /// <summary>Optional: additional location names that should be marked
        /// positionfree=false at fault time. Use this to remove competing
        /// staging candidates so the planner must use the injected temp
        /// location as the putDown target.</summary>
        public List<string> BlockPositionfreeLocations { get; set; }

        // ── DislodgedAfterStack fault fields ─────────────────────────────

        /// <summary>Element that was stacked and is now dislodged back to its
        /// initial location (e.g. "stick5").</summary>
        public string DislodgedObject { get; set; }

        /// <summary>Location the dislodged object returns to (must already
        /// exist in the static PDDL :objects, e.g. "initlocstick5").</summary>
        public string ReturnToLocation { get; set; }

        /// <summary>PDDL type of the return location (e.g. "firstposition").
        /// Only used if new objects must be registered.</summary>
        public string ReturnToLocationPddlType { get; set; } = "firstposition";

        /// <summary>The element the dislodged object was resting on (e.g.
        /// "cube2"). Its accessible and clear predicates are restored to
        /// true after dislodgement.</summary>
        public string BaseObject { get; set; }

        // ── DislodgedAfterStack — stacking completion fields ─────────────

        /// <summary>Optional: the element actively being stacked when the fault fires
        /// (e.g. "stick2"). When set, ApplyDislodge first applies StackML completion
        /// effects for this object so the HL replan sees it as already placed.</summary>
        public string StackingObject { get; set; }

        /// <summary>Optional: the element it was being stacked onto (e.g. "table1").</summary>
        public string StackingOnObject { get; set; }

        /// <summary>Optional: the final position the stacking object lands at
        /// (e.g. "finallocstick2").</summary>
        public string StackingPosition { get; set; }
    }
}
