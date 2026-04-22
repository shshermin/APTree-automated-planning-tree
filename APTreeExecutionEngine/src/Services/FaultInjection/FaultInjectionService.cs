using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using BehaviorTreeMainProject.Log.Services;
using BehaviorTreeMainProject.ModelLoader;
using BehaviorTreeMainProject.Services.AIPlanning;
using ModelLoader;
using ModelLoader.ParameterTypes;
using ModelLoader.PredicateTypes;

namespace BehaviorTreeMainProject.Services.FaultInjection
{
    /// <summary>
    /// Always-on service attached to the tree root that watches for
    /// configured fault triggers and, when matched, mutates the blackboard,
    /// patches the PDDL parent problem file, and triggers a replan on the
    /// active ML flow node.
    ///
    /// This simulates physical faults (like a dropped stick) that a real
    /// sensor/camera would otherwise have to detect.
    /// </summary>
    public class FaultInjectionService : Service
    {
        private readonly FaultInjectionConfig _config;

        // Latch: fault id → activation count observed so far
        private readonly Dictionary<string, int> _activationCounts = new();
        // Latch: fault id → already fired
        private readonly HashSet<string> _firedFaults = new();
        // Track which ML actions we've already seen as InProgress (per fault)
        private readonly Dictionary<string, HashSet<string>> _seenActiveActions = new();

        public FaultInjectionService(IBehaviorTree owningTree, FaultInjectionConfig config)
            : base(owningTree)
        {
            _config = config ?? new FaultInjectionConfig();
        }

        public override bool OnEvaluate(float InDeltaTime)
        {
            if (_config?.Faults == null || _config.Faults.Count == 0)
                return true;

            foreach (var fault in _config.Faults)
            {
                if (fault == null || string.IsNullOrEmpty(fault.Id)) continue;
                if (_firedFaults.Contains(fault.Id)) continue;

                var mlAction = FindActiveMLActionForTrigger(fault.Trigger);
                if (mlAction == null) continue;

                // Count this as a new activation only if we haven't seen this exact
                // ML instance active before for this fault (edge-triggered)
                var seen = _seenActiveActions.GetValueOrDefault(fault.Id)
                           ?? (_seenActiveActions[fault.Id] = new HashSet<string>());
                string instanceKey = mlAction.InstanceName?.ToString() ?? "";
                if (seen.Contains(instanceKey)) continue;
                seen.Add(instanceKey);

                int count = _activationCounts.GetValueOrDefault(fault.Id) + 1;
                _activationCounts[fault.Id] = count;

                if (count < Math.Max(1, fault.Trigger?.OnActivationCount ?? 1))
                    continue;

                try
                {
                    ApplyFault(fault, mlAction);
                    _firedFaults.Add(fault.Id);
                }
                catch (Exception ex)
                {
                    LoggingService.LogError($"🧪 FaultInjection[{fault.Id}]: Failed to apply fault: {ex.Message}");
                    LoggingService.LogError(ex.StackTrace ?? "");
                }
            }

            return true;
        }

        // ─── Trigger matching ────────────────────────────────────────────

        private PActionNode FindActiveMLActionForTrigger(FaultTrigger trigger)
        {
            if (trigger == null) return null;

            var compositeRoot = OwningTree?.root as BTFlowNodeComposite;
            if (compositeRoot == null) return null;

            foreach (var child in compositeRoot.GetChildren())
            {
                if (child is not DynamicFlowNode dfn) continue;
                var graph = dfn.GetActionGraph();
                if (graph == null) continue;

                foreach (var action in graph.GetAllActionNodes())
                {
                    if (action is not PActionNode hlAction) continue;
                    if (!hlAction.IsHighLevelAction) continue;
                    if (hlAction.status != BTNodeResult.InProgress) continue;
                    if (hlAction.HighLevelSubtree == null) continue;

                    // Parent HL instance name filter (e.g. must contain "stick4")
                    if (!string.IsNullOrEmpty(trigger.ParentInstanceContains))
                    {
                        string parentInstance = hlAction.InstanceName?.ToString() ?? "";
                        if (parentInstance.IndexOf(trigger.ParentInstanceContains,
                                StringComparison.OrdinalIgnoreCase) < 0)
                            continue;
                    }

                    var mlGraph = hlAction.HighLevelSubtree.GetActionGraph();
                    if (mlGraph == null) continue;

                    foreach (var mlNode in mlGraph.GetAllActionNodes())
                    {
                        if (mlNode is not PActionNode mlAction) continue;
                        if (mlAction.status != BTNodeResult.InProgress) continue;

                        string mlType = mlAction.actionType?.ToString() ?? "";
                        if (!mlType.EndsWith("ML", StringComparison.OrdinalIgnoreCase))
                            continue;

                        if (!string.IsNullOrEmpty(trigger.MlActionType) &&
                            !mlType.Equals(trigger.MlActionType, StringComparison.OrdinalIgnoreCase) &&
                            !mlType.StartsWith(trigger.MlActionType, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        return mlAction;
                    }
                }
            }
            return null;
        }

        // ─── Fault application ───────────────────────────────────────────

        private void ApplyFault(FaultDefinition fault, PActionNode mlAction)
        {
            string faultType = (fault.Type ?? "").Trim();
            LoggingService.LogWarning("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            LoggingService.LogWarning($"🧪 FaultInjection[{fault.Id}] FIRING — type={faultType} on {mlAction.InstanceName}");
            LoggingService.LogWarning("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

            if (!faultType.Equals("DropAfterClose", StringComparison.OrdinalIgnoreCase))
            {
                LoggingService.LogWarning($"🧪 FaultInjection[{fault.Id}]: Unknown fault type '{faultType}' — skipping");
                return;
            }

            ApplyDropFault(fault, mlAction);
        }

        private void ApplyDropFault(FaultDefinition fault, PActionNode mlAction)
        {
            var effects = fault.Effects ?? new FaultEffects();
            string dropped = effects.DroppedObject ?? "";
            string robot = effects.Robot ?? "";
            string gripper = effects.Gripper ?? "";
            string tempLocName = string.IsNullOrWhiteSpace(effects.TempLocationName)
                ? "temploc1" : effects.TempLocationName;
            string tempLocType = string.IsNullOrWhiteSpace(effects.TempLocationPddlType)
                ? "firstposition" : effects.TempLocationPddlType;

            if (string.IsNullOrEmpty(dropped) || string.IsNullOrEmpty(robot) || string.IsNullOrEmpty(gripper))
            {
                LoggingService.LogError($"🧪 FaultInjection[{fault.Id}]: DropAfterClose requires DroppedObject, Robot, Gripper");
                return;
            }

            var bb = linkedBlackboard;

            // 1) Register TempLocation on blackboard
            var pos = effects.TempLocationPosition ?? new double[] { 0.0, 0.0, 0.0 };
            double px = pos.Length > 0 ? pos[0] : 0.0;
            double py = pos.Length > 1 ? pos[1] : 0.0;
            double pz = pos.Length > 2 ? pos[2] : 0.0;

            var tempLoc = new InitialLocation(
                tempLocName,
                new Coordinate(px, py, pz),
                new Coordinate(0, 0, 0));
            bb.SetLocation(new FastName(tempLocName), tempLoc);
            LoggingService.LogSuccess($"🧪 FaultInjection: Registered TempLocation '{tempLocName}' at ({px},{py},{pz})");

            // 2) Resolve Element references
            Element droppedElement = null;
            try { droppedElement = bb.GetElement(new FastName(dropped)); }
            catch { /* resolved via predicate search below */ }

            // 3) Mutate existing predicates (find-by-type+params, flip .not)
            FlipPredicate(bb, "holding", new[] { robot, dropped }, newNot: true);
            FlipPredicate(bb, "gripperempty", new[] { gripper }, newNot: false);
            FlipPredicate(bb, "clear", new[] { dropped }, newNot: false);
            // The old atplace(stick, initlocstick) → negate (stick no longer there)
            NegateAllPredicatesOfType(bb, "atplace", requiredParam0: dropped);

            // 4) Add new atplace(stick, temploc) = true
            if (droppedElement != null)
            {
                var newAtPlace = new AtPlace(droppedElement, tempLoc, isNegated: false);
                bb.SetPredicateSync(newAtPlace.GetUniqueKey(), newAtPlace);
                LoggingService.LogSuccess($"🧪 FaultInjection: Added predicate atplace({dropped}, {tempLocName}) = true");
            }
            else
            {
                LoggingService.LogWarning($"🧪 FaultInjection: Could not resolve Element '{dropped}' — atplace predicate NOT added");
            }

            // 5) Patch parent PDDL problem file on disk: add `temploc1 - <type>` to :objects
            PatchParentProblemFile(mlAction, tempLocName, tempLocType);

            // 6) Trigger replan on the owning ML flow node
            TriggerReplan(mlAction);
        }

        private static void FlipPredicate(Blackboard<FastName> bb, string predType,
            string[] paramValues, bool newNot)
        {
            var match = bb.GetAllPredicates().FirstOrDefault(p =>
                p.GetPredicateType().Equals(predType, StringComparison.OrdinalIgnoreCase) &&
                p.GetPDDLParameterValues()
                    .Select(v => (v ?? "").ToLowerInvariant())
                    .SequenceEqual(paramValues.Select(v => (v ?? "").ToLowerInvariant())));

            if (match != null)
            {
                bool oldNot = match.not;
                match.not = newNot;
                LoggingService.LogSuccess(
                    $"🧪 FaultInjection: Flipped {predType}({string.Join(",", paramValues)}) not: {oldNot} → {newNot}");
            }
            else
            {
                LoggingService.LogWarning(
                    $"🧪 FaultInjection: No existing predicate {predType}({string.Join(",", paramValues)}) to flip");
            }
        }

        private static void NegateAllPredicatesOfType(Blackboard<FastName> bb, string predType,
            string requiredParam0)
        {
            int count = 0;
            foreach (var p in bb.GetAllPredicates())
            {
                if (!p.GetPredicateType().Equals(predType, StringComparison.OrdinalIgnoreCase)) continue;
                var vals = p.GetPDDLParameterValues();
                if (vals == null || vals.Count == 0) continue;
                if (!string.Equals(vals[0], requiredParam0, StringComparison.OrdinalIgnoreCase)) continue;
                if (p.not) continue; // already negated
                p.not = true;
                count++;
            }
            if (count > 0)
                LoggingService.LogSuccess($"🧪 FaultInjection: Negated {count} existing {predType}({requiredParam0},*) predicate(s)");
        }

        private static void PatchParentProblemFile(PActionNode mlAction, string tempLocName, string pddlType)
        {
            // The ML action's parent flow node holds the ML ServicePlanning;
            // its PlanningRequest.ProblemFile points to the STATIC HL problem file
            // whose :objects block is read by GetRelevantObjects(). We patch that file.
            DynamicFlowNode mlFlowNode = FindOwningMLFlowNode(mlAction);
            if (mlFlowNode?.ServicePlanning is not ServicePDDLPlanning plannerService)
            {
                LoggingService.LogWarning("🧪 FaultInjection: Could not find ServicePDDLPlanning to patch problem file");
                return;
            }

            string problemPath = plannerService.PlanningRequest?.ProblemFile;
            if (string.IsNullOrEmpty(problemPath))
            {
                LoggingService.LogWarning("🧪 FaultInjection: PlanningRequest.ProblemFile is empty — cannot patch");
                return;
            }

            // Resolve to a local path similar to how ServicePDDLPlanning does
            string resolved = ResolveLocalPath(problemPath);
            if (resolved == null || !File.Exists(resolved))
            {
                LoggingService.LogWarning($"🧪 FaultInjection: Problem file not found locally for patching: {problemPath}");
                return;
            }

            string content = File.ReadAllText(resolved);
            string objectLine = $"    {tempLocName} - {pddlType}";

            if (Regex.IsMatch(content, $@"\b{Regex.Escape(tempLocName)}\s*-\s*\w+",
                    RegexOptions.IgnoreCase))
            {
                LoggingService.LogInfo($"🧪 FaultInjection: '{tempLocName}' already present in {resolved} — no patch needed");
                return;
            }

            // Insert inside the (:objects ... ) block just before its closing paren
            var objectsBlock = Regex.Match(content, @"\(:objects\b(?<body>[\s\S]*?)\)",
                RegexOptions.IgnoreCase);
            if (!objectsBlock.Success)
            {
                LoggingService.LogWarning($"🧪 FaultInjection: No (:objects ...) block in {resolved}");
                return;
            }

            int insertAt = objectsBlock.Index + objectsBlock.Length - 1; // before the ')'
            string patched = content.Insert(insertAt, $"\n    ;; Fault-injected temp location\n{objectLine}\n");
            File.WriteAllText(resolved, patched);
            LoggingService.LogSuccess($"🧪 FaultInjection: Patched {resolved} — added '{tempLocName} - {pddlType}'");
        }

        private static string ResolveLocalPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            if (File.Exists(path)) return Path.GetFullPath(path);

            // Try a few common relative roots
            string trimmed = path.TrimStart('.', '/', '\\');
            string[] candidates =
            {
                Path.Combine(Directory.GetCurrentDirectory(), trimmed),
                Path.Combine(AppContext.BaseDirectory, trimmed),
                Path.Combine(AppContext.BaseDirectory, "python_service", trimmed),
            };
            foreach (var c in candidates)
            {
                if (File.Exists(c)) return Path.GetFullPath(c);
            }
            return null;
        }

        private static DynamicFlowNode FindOwningMLFlowNode(PActionNode mlAction)
        {
            var current = mlAction?.ParentNode;
            while (current != null)
            {
                if (current is DynamicFlowNode dfn) return dfn;
                current = current.ParentNode;
            }
            return null;
        }

        private static void TriggerReplan(PActionNode mlAction)
        {
            var mlFlowNode = FindOwningMLFlowNode(mlAction);
            if (mlFlowNode == null)
            {
                LoggingService.LogError("🧪 FaultInjection: No parent ML FlowNode — cannot trigger replan");
                return;
            }

            if (mlFlowNode.ServicePlanning is ServicePlanning plannerService)
                plannerService.ResetPlanningService();

            mlFlowNode.ResetForNextRound();
            LoggingService.LogSuccess(
                $"🧪 FaultInjection: Replan triggered on ML FlowNode '{mlFlowNode.DebugDisplayName}' — " +
                "problem will be regenerated from updated blackboard on next tick");
        }
    }
}
