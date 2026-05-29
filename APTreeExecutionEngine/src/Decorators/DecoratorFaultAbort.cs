using System.Linq;
using BehaviorTreeMainProject.Log.Services;
using BehaviorTreeMainProject.Services;
using BehaviorTreeMainProject.Services.FaultInjection;

/// <summary>
/// Two-phase abort decorator attached to ML-level DynamicFlowNodes.
///
/// A sensor/camera signals an abort by writing to the blackboard:
///   key = BlackboardKeys.AbortKey(flowNodeName), value = true
///
/// Phase 1 — OnEvaluate (pre-execution gate):
///   • Abort pending AND no LL action currently InProgress
///     → return false — the DFN is blocked before any new action ticks.
///       PostProcessTickResult resets and returns InProgress.
///   • Abort pending AND an LL action IS InProgress (e.g. travel mid-execution)
///     → return true — let it finish. PostProcessTickResult waits.
///
/// Phase 2 — PostProcessTickResult (post-execution gate):
///   • Abort pending AND InResult == InProgress (action still running)
///     → pass through unchanged, keep waiting.
///   • Abort pending AND InResult is terminal (Success or Failure)
///     → clear flag, reset planner, return InProgress.
///     DecoratorRetryOnFailure (next in pipeline) sees InProgress → no retry loop.
///
/// This guarantees that when the replan fires, the completed action's effects
/// are already on the blackboard, so no steps are repeated.
///
/// Attach order in DynamicFlowNode constructor:
///   1. DecoratorPlanningComplete
///   2. DecoratorFaultAbort        ← this one
///   3. DecoratorRetryOnFailure
/// </summary>
public class DecoratorFaultAbort : Decorator
{
    /// <summary>
    /// Set to true in OnEvaluate when we decide to let a running action finish.
    /// Cleared when the abort is consumed or cancelled.
    /// </summary>
    private bool _waitingForActionToComplete = false;

    /// <summary>
    /// Absolute time when the fault fired (read from blackboard once abort flag is
    /// first detected). Used to compute the true Recovery Time = t_resume - t_fault,
    /// which includes any LL-action draining time before TriggerReplan() is called.
    /// </summary>
    private DateTime _faultDetectedAt = default;

    /// <summary>
    /// Timestamp recorded when TriggerReplan() fires. Used together with
    /// _faultDetectedAt to report both Replan Latency and full Recovery Time.
    /// Reset once the "resumed" log is emitted.
    /// </summary>
    private DateTime _replanTriggeredAt = default;

    public override bool CanPostProcessTickResult => true;

    public DecoratorFaultAbort(DynamicFlowNode attachedNode) : base(false)
    {
        AttachedNode = attachedNode;
    }

    // ─── Phase 1: pre-execution gate ─────────────────────────────────────

    protected override bool OnEvaluate(float InDeltaTime)
    {
        if (!IsAbortFlagSet())
        {
            _waitingForActionToComplete = false;

            // Log once when we return to clean execution after a replan
            if (_replanTriggeredAt != default)
            {
                var tResume = DateTime.Now;
                var replanLatency  = (tResume - _replanTriggeredAt).TotalMilliseconds;
                var recoveryTime   = _faultDetectedAt != default
                    ? (tResume - _faultDetectedAt).TotalMilliseconds
                    : replanLatency;
                string nodeName = AttachedNode?.DebugDisplayName ?? "";
                LoggingService.LogSuccess(
                    $"✅ DecoratorFaultAbort [{nodeName}]: Normal execution resumed");
                LoggingService.LogSuccess(
                    $"📋 FAULT_METRIC | t_resume={tResume:HH:mm:ss.fff} | node={nodeName} | type=Execution" +
                    $" | recovery_ms={recoveryTime:F0} | replan_latency_ms={replanLatency:F0}");
                FaultRecoveryLogger.LogResumed(nodeName, success: true);
                _replanTriggeredAt = default;
                _faultDetectedAt   = default;
                ClearFaultTimestamp();
            }

            return true;  // no fault pending — normal execution
        }

        // Abort is pending — capture t_fault from blackboard the first time we see it
        if (_faultDetectedAt == default)
        {
            _faultDetectedAt = ReadFaultTimestamp();
            FaultRecoveryLogger.LogFaultDetected(AttachedNode?.DebugDisplayName ?? "", "LL");
        }

        if (HasActiveInProgressLLAction())
        {
            // An LL action is mid-execution. Let it finish so its effects land
            // on the blackboard before we replan. Phase 2 will handle the reset.
            _waitingForActionToComplete = true;
            LoggingService.LogWarning(
                $"🔴 DecoratorFaultAbort [{AttachedNode?.DebugDisplayName}]: Abort pending — LL action in progress, waiting for it to complete");
            return true;
        }

        // No LL action is running — safe to block immediately.
        // PostProcessTickResult will fire with the blocked result and reset.
        _waitingForActionToComplete = false;
        LoggingService.LogWarning(
            $"🔴 DecoratorFaultAbort [{AttachedNode?.DebugDisplayName}]: Abort pending — no active LL action, blocking DFN now");
        return false;
    }

    // ─── Phase 2: post-execution gate ────────────────────────────────────

    public override BTNodeResult PostProcessTickResult(BTNodeResult InResult)
    {
        if (!IsAbortFlagSet()) return InResult;  // no fault — pass through normally

        if (_waitingForActionToComplete && InResult == BTNodeResult.InProgress)
        {
            // Current LL action hasn't finished yet — keep waiting
            return InResult;
        }

        // Either:
        //   (A) OnEvaluate blocked (no active action) → InResult is whatever the blocked DFN returned
        //   (B) LL action just completed (terminal result) → effects are on blackboard
        // Either way it is safe to replan now.
        TriggerReplan();
        return BTNodeResult.InProgress;
    }

    // ─── Helpers ─────────────────────────────────────────────────────────

    private bool IsAbortFlagSet()
    {
        var bb = AttachedNode?.OwningTree?.linkedBlackboard;
        if (bb == null) return false;
        try { return bb.GetBool(BlackboardKeys.AbortKey(AttachedNode?.DebugDisplayName ?? "")); }
        catch { return false; }
    }

    private void ClearAbortFlag()
    {
        var bb = AttachedNode?.OwningTree?.linkedBlackboard;
        bb?.SetBool(BlackboardKeys.AbortKey(AttachedNode?.DebugDisplayName ?? ""), false);
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
        return DateTime.Now;  // fallback: use current time
    }

    private void ClearFaultTimestamp()
    {
        var bb = AttachedNode?.OwningTree?.linkedBlackboard;
        bb?.SetString(BlackboardKeys.FaultTimestampKey(AttachedNode?.DebugDisplayName ?? ""), "");
    }

    /// <summary>
    /// Returns true if any LL-level action node inside the DFN's current action
    /// graph has status <see cref="BTNodeResult.InProgress"/>.
    /// </summary>
    private bool HasActiveInProgressLLAction()
    {
        var graph = (AttachedNode as DynamicFlowNode)?.GetActionGraph();
        if (graph == null) return false;

        foreach (var node in graph.GetAllActionNodes())
        {
            if (node is PActionNode mlAction
                && mlAction.IsHighLevelAction
                && mlAction.status == BTNodeResult.InProgress
                && mlAction.HighLevelSubtree != null)
            {
                var llGraph = mlAction.HighLevelSubtree.GetActionGraph();
                if (llGraph != null && llGraph.GetAllActionNodes().Any(n => n.status == BTNodeResult.InProgress))
                    return true;
            }
        }
        return false;
    }

    private void TriggerReplan()
    {
        string nodeName = AttachedNode?.DebugDisplayName ?? "";
        ClearAbortFlag();
        _waitingForActionToComplete = false;

        _replanTriggeredAt = DateTime.Now;
        LoggingService.LogDebug(
            $"🔴 DecoratorFaultAbort [{nodeName}]: Triggering replan — resetting planner and DFN");
        FaultRecoveryLogger.LogReplanTriggered(nodeName);

        if (AttachedNode?.ServicePlanning is ServicePlanning svc)
            svc.ResetPlanningService();

        AttachedNode?.ResetForNextRound();
    }
}

