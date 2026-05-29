using System;
using System.Collections.Generic;
using BehaviorTreeMainProject.Log.Services;
using BehaviorTreeMainProject.ModelLoader;
using ModelLoader;

namespace BehaviorTreeMainProject.Services.FaultInjection
{
    /// <summary>
    /// Always-on service that simulates a physical camera/sensor detecting
    /// faults during execution (e.g. a dropped stick).
    ///
    /// Responsibilities:
    ///  • Poll configured fault triggers each tick (trigger detection only).
    ///  • Walk the BT to find the active ML action and its owning flow node.
    ///  • Delegate all blackboard/world-state mutations to <see cref="WorldStateManager"/>.
    ///
    /// This class has no knowledge of predicate types, PDDL objects, or abort signals.
    /// </summary>
    public class DummyCameraService : Service
    {
        private readonly FaultInjectionConfig _config;

        // Latch: fault id → activation count observed so far
        private readonly Dictionary<string, int> _activationCounts = new();
        // Latch: fault id → already fired
        private readonly HashSet<string> _firedFaults = new();
        // Track which ML actions we've already seen as InProgress (per fault)
        private readonly Dictionary<string, HashSet<string>> _seenActiveActions = new();

        private readonly WorldStateManager _wsm;

        public DummyCameraService(IBehaviorTree owningTree, FaultInjectionConfig config, WorldStateManager wsm)
            : base(owningTree)
        {
            _config = config ?? new FaultInjectionConfig();
            _wsm = wsm ?? throw new ArgumentNullException(nameof(wsm));
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

                        // Optional: wait until a specific LL step inside the ML subtree
                        // has started (InProgress) or completed (Success). Allows faults
                        // to target points like "after close + retract".
                        if (!string.IsNullOrEmpty(trigger.AfterLLStep)
                            && !HasLLStepReached(mlAction, trigger.AfterLLStep))
                        {
                            continue;
                        }

                        return mlAction;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Returns true if an LL step whose InstanceName contains <paramref name="stepNameContains"/>
        /// (case-insensitive) inside the ML action's LL subtree is currently InProgress
        /// or already Success. Used by FaultTrigger.AfterLLStep to delay firing until
        /// a particular point in the LL sequence (e.g. retract motion) is reached.
        /// </summary>
        private static bool HasLLStepReached(PActionNode mlAction, string stepNameContains)
        {
            var llSubtree = mlAction?.HighLevelSubtree;
            var llGraph = llSubtree?.GetActionGraph();
            if (llGraph == null) return false;

            foreach (var n in llGraph.GetAllActionNodes())
            {
                string iname = n.InstanceName?.ToString() ?? "";
                if (iname.IndexOf(stepNameContains, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                if (n.status == BTNodeResult.InProgress || n.status == BTNodeResult.Success)
                    return true;
            }
            return false;
        }

        // ─── Fault application ───────────────────────────────────────────

        private void ApplyFault(FaultDefinition fault, PActionNode mlAction)
        {
            string faultType = (fault.Type ?? "").Trim();
            LoggingService.LogWarning("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            LoggingService.LogWarning($"🧪 DummyCameraService[{fault.Id}] FIRING — type={faultType} on {mlAction.InstanceName} at {DateTime.Now:HH:mm:ss.fff}");
            LoggingService.LogWarning("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

            if (faultType.Equals("DropAfterClose", StringComparison.OrdinalIgnoreCase))
            {
                var mlFlow = FindOwningMLFlowNode(mlAction);
                FaultRecoveryLogger.LogFaultInjected(
                    faultId: fault.Id ?? "",
                    faultType: faultType,
                    scope: "LL",
                    dfnName: mlFlow?.DebugDisplayName ?? "",
                    mlActionInstance: mlAction.InstanceName.ToString(),
                    parentHlInstance: mlAction.ParentNode?.DebugDisplayName ?? "",
                    targetObject: fault.Effects?.DroppedObject ?? "",
                    extraDetails: $"robot={fault.Effects?.Robot};gripper={fault.Effects?.Gripper}");
                _wsm.ApplyDrop(fault.Effects, mlFlow);
            }
            else if (faultType.Equals("BlockerOnTop", StringComparison.OrdinalIgnoreCase))
            {
                var targetHlFlow = FindHLSubtreeForPickUpOfTarget(fault.Effects?.TargetObject ?? "");
                FaultRecoveryLogger.LogFaultInjected(
                    faultId: fault.Id ?? "",
                    faultType: faultType,
                    scope: "HL",
                    dfnName: targetHlFlow?.DebugDisplayName ?? "",
                    mlActionInstance: mlAction.InstanceName.ToString(),
                    parentHlInstance: mlAction.ParentNode?.DebugDisplayName ?? "",
                    targetObject: fault.Effects?.TargetObject ?? "",
                    extraDetails: $"blocker={fault.Effects?.BlockerObject}");
                _wsm.ApplyBlocker(fault.Effects, targetHlFlow);
            }
            else if (faultType.Equals("DislodgedAfterStack", StringComparison.OrdinalIgnoreCase))
            {
                var activeDfn = FindActiveDFN();
                if (activeDfn == null)
                    LoggingService.LogWarning($"🧪 DummyCameraService[{fault.Id}]: No active DFN found — DislodgedAfterStack skipped");
                else
                {
                    FaultRecoveryLogger.LogFaultInjected(
                        faultId: fault.Id ?? "",
                        faultType: faultType,
                        scope: "HL",
                        dfnName: activeDfn.DebugDisplayName,
                        mlActionInstance: mlAction.InstanceName.ToString(),
                        parentHlInstance: mlAction.ParentNode?.DebugDisplayName ?? "",
                        targetObject: fault.Effects?.DislodgedObject ?? "",
                        extraDetails: $"returnTo={fault.Effects?.ReturnToLocation}");
                    _wsm.ApplyDislodge(fault.Effects, activeDfn);
                }
            }
            else
            {
                LoggingService.LogWarning($"🧪 DummyCameraService[{fault.Id}]: Unknown fault type '{faultType}' — skipping");
            }
        }

        // ─── Tree walking ─────────────────────────────────────────────────

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

        private IEnumerable<DynamicFlowNode> EnumerateAllMLFlowNodes()
        {
            var compositeRoot = OwningTree?.root as BTFlowNodeComposite;
            if (compositeRoot == null) yield break;

            foreach (var child in compositeRoot.GetChildren())
            {
                if (child is not DynamicFlowNode hlFlow) continue;
                var graph = hlFlow.GetActionGraph();
                if (graph == null) continue;

                foreach (var action in graph.GetAllActionNodes())
                {
                    if (action is not PActionNode hlAction) continue;
                    if (!hlAction.IsHighLevelAction) continue;
                    if (hlAction.HighLevelSubtree == null) continue;
                    yield return hlAction.HighLevelSubtree;
                }
            }
        }

        /// <summary>
        /// Returns the <see cref="DynamicFlowNode"/> at the top level of the composite
        /// whose execution status is currently <see cref="BTNodeResult.InProgress"/>.
        /// Used by DislodgedAfterStack faults to target the DFN that is actively
        /// planning/executing so its abort + HL-replan flags can be set.
        /// </summary>
        private DynamicFlowNode FindActiveDFN()
        {
            var compositeRoot = OwningTree?.root as BTFlowNodeComposite;
            if (compositeRoot == null) return null;

            foreach (var child in compositeRoot.GetChildren())
            {
                if (child is DynamicFlowNode dfn && dfn.status == BTNodeResult.InProgress)
                    return dfn;
            }
            return null;
        }

        /// <summary>
        /// Find the ML-level <see cref="DynamicFlowNode"/> belonging to the PickUpHL
        /// action whose InstanceName contains <paramref name="target"/>.
        /// </summary>
        private DynamicFlowNode FindHLSubtreeForPickUpOfTarget(string target)
        {
            if (string.IsNullOrEmpty(target)) return null;

            foreach (var mlFlow in EnumerateAllMLFlowNodes())
            {
                if (mlFlow.ParentNode is not PActionNode hlAction) continue;
                string type = hlAction.actionType?.ToString() ?? "";
                if (!type.StartsWith("PickUpHL", StringComparison.OrdinalIgnoreCase))
                    continue;
                string iname = hlAction.InstanceName?.ToString() ?? "";
                if (iname.IndexOf(target, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                return mlFlow;
            }
            return null;
        }
    }
}
