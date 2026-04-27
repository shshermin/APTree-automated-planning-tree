using System;
using BehaviorTreeMainProject.Log.Services;
using BehaviorTreeMainProject.Services;
using BehaviorTreeMainProject.Services.FaultInjection;

/// <summary>
/// Execution-phase abort decorator for HL-level DynamicFlowNodes.
///
/// A fault handler signals an HL replan by writing to the blackboard:
///   key = BlackboardKeys.HLReplanKey(flowNodeName), value = true
///
/// Unlike <see cref="DecoratorFaultAbort"/> (which waits for LL actions to
/// finish and then replans at the ML level), this decorator immediately
/// intercepts the HL DFN's tick result and triggers a full HL-level replan
/// via <see cref="DynamicFlowNode.ResetForNextRound"/>.
///
/// The <c>HLReplanKey</c> flag is intentionally NOT cleared here — it is
/// consumed by <see cref="BehaviorTreeMainProject.Decorators.Replan.DecoratorHLFaultReplan"/>
/// during the next planning phase to rebuild the PDDL <c>(:init)</c> block
/// from the current blackboard state.
///
/// Attach order in DynamicFlowNode constructor (FIFO = PostProcessTickResult order):
///   1. DecoratorPlanningComplete
///   2. DecoratorFaultAbort        (ML faults via abort_&lt;dfn&gt;)
///   3. DecoratorHLFaultAbort      ← this one  (HL faults via hl_replan_&lt;dfn&gt;)
///   4. DecoratorRetryOnFailure
/// </summary>
public class DecoratorHLFaultAbort : Decorator
{
    /// <summary>
    /// Set to true when the HL-replan flag is first detected so we don't
    /// re-trigger on every subsequent tick while replanning is in progress.
    /// Reset when the DFN returns a terminal result (plan completed).
    /// </summary>
    private bool _triggered = false;

    private DateTime _faultDetectedAt = default;
    private DateTime _replanTriggeredAt = default;

    public override bool CanPostProcessTickResult => true;

    public DecoratorHLFaultAbort(DynamicFlowNode attachedNode) : base(false)
    {
        AttachedNode = attachedNode;
    }

    protected override bool OnEvaluate(float InDeltaTime)
    {
        // Log resume when execution returns to normal after a completed replan
        if (_replanTriggeredAt != default && !_triggered && !IsHLReplanFlagSet())
        {
            var tResume = DateTime.Now;
            var replanLatency = (tResume - _replanTriggeredAt).TotalMilliseconds;
            var recoveryTime = _faultDetectedAt != default
                ? (tResume - _faultDetectedAt).TotalMilliseconds
                : replanLatency;
            string nodeName = AttachedNode?.DebugDisplayName ?? "";
            LoggingService.LogSuccess(
                $"✅ DecoratorHLFaultAbort [{nodeName}]: Normal HL execution resumed after replan");
            LoggingService.LogSuccess(
                $"📋 FAULT_METRIC | t_resume={tResume:HH:mm:ss.fff} | node={nodeName} | type=HL_Dislodge" +
                $" | recovery_ms={recoveryTime:F0} | replan_latency_ms={replanLatency:F0}");
            _replanTriggeredAt = default;
            _faultDetectedAt = default;
            ClearFaultTimestamp();
        }

        return true; // never block in OnEvaluate — intercept in PostProcessTickResult instead
    }

    public override BTNodeResult PostProcessTickResult(BTNodeResult InResult)
    {
        // When the DFN finishes its plan cycle, reset so future faults can trigger
        if (InResult == BTNodeResult.Success || InResult == BTNodeResult.Failure)
            _triggered = false;

        // Already handled this fault — pass through until the plan cycle completes
        if (_triggered) return InResult;

        // Check if an HL fault has been signalled
        bool flagSet = IsHLReplanFlagSet();
        LoggingService.LogInfo(
            $"🔎 DecoratorHLFaultAbort [{AttachedNode?.DebugDisplayName}]: PostProcessTickResult InResult={InResult}, flagSet={flagSet}, _triggered={_triggered}");
        if (!flagSet) return InResult;

        // First detection — capture fault timestamp
        _faultDetectedAt = ReadFaultTimestamp();
        _triggered = true;
        _replanTriggeredAt = DateTime.Now;

        string name = AttachedNode?.DebugDisplayName ?? "";
        LoggingService.LogWarning(
            $"🔴 DecoratorHLFaultAbort [{name}]: HL fault detected — triggering full HL replan");

        // Reset the planning service (clears DecoratorHLStatePatch._patched etc.)
        if (AttachedNode?.ServicePlanning is ServicePlanning svc)
            svc.ResetPlanningService();

        // Reset the DFN so it re-enters the planning phase next tick
        // The hl_replan_<dfn> flag stays set for DecoratorHLFaultReplan to consume
        AttachedNode?.ResetForNextRound();

        return BTNodeResult.InProgress;
    }

    // ─── Helpers ─────────────────────────────────────────────────────────

    private bool IsHLReplanFlagSet()
    {
        var bb = AttachedNode?.OwningTree?.linkedBlackboard;
        if (bb == null) return false;
        try { return bb.GetBool(BlackboardKeys.HLReplanKey(AttachedNode?.DebugDisplayName ?? "")); }
        catch { return false; }
    }

    private DateTime ReadFaultTimestamp()
    {
        var bb = AttachedNode?.OwningTree?.linkedBlackboard;
        if (bb == null) return DateTime.Now;
        try
        {
            string raw = bb.GetString(BlackboardKeys.FaultTimestampKey(AttachedNode?.DebugDisplayName ?? ""));
            if (!string.IsNullOrEmpty(raw) && long.TryParse(raw, out long ticks))
                return new DateTime(ticks);
        }
        catch { }
        return DateTime.Now;
    }

    private void ClearFaultTimestamp()
    {
        var bb = AttachedNode?.OwningTree?.linkedBlackboard;
        bb?.SetString(BlackboardKeys.FaultTimestampKey(AttachedNode?.DebugDisplayName ?? ""), "");
    }
}
