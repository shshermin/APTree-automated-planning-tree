using System;
using System.Linq;
using BehaviorTreeMainProject.Log.Services;
using BehaviorTreeMainProject.Services;

/// <summary>
/// Post-processing decorator for ML actions that intercepts Failure results
/// and drops into a console recovery prompt where the operator can:
///   - Edit predicates (list, set)
///   - Retry:  re-execute the same LL subtree
///   - Replan: reset the ML-level FlowNode so ServicePDDLPlanning re-plans from the current blackboard
///   - Skip:   force success and continue
///   - Abort:  let the failure propagate upward
/// </summary>
public class DecoratorRecovery : Decorator
{
    public override bool CanPostProcessTickResult => true;

    public DecoratorRecovery(PActionNode action) : base(false)
    {
        AttachedAction = action;
    }

    protected override bool OnEvaluate(float InDeltaTime)
    {
        return true; // Never block execution
    }

    public override BTNodeResult PostProcessTickResult(BTNodeResult InResult)
    {
        if (InResult != BTNodeResult.Failure)
            return InResult;

        if (AttachedAction == null)
            return InResult;

        // Only act on ML actions that have LL subtrees
        if (!AttachedAction.IsHighLevelAction || AttachedAction.HighLevelSubtree == null)
            return InResult;

        var actionTypeName = AttachedAction.actionType.ToString();
        if (!actionTypeName.EndsWith("ML"))
            return InResult;

        var actionName = AttachedAction.InstanceName.ToString();
        var blackboard = AttachedAction.Blackboard;

        LoggingService.LogError($"");
        LoggingService.LogError($"╔══════════════════════════════════════════════════════════════╗");
        LoggingService.LogError($"║  ML ACTION FAILED: {actionTypeName} / {actionName}");
        LoggingService.LogError($"╚══════════════════════════════════════════════════════════════╝");
        LoggingService.LogInfo($"Entering recovery mode. Commands:");
        LoggingService.LogInfo($"  list          — show true predicates");
        LoggingService.LogInfo($"  list all      — show all predicates (incl. negated)");
        LoggingService.LogInfo($"  set <type> <p1> <p2> ... <true|false>  — toggle predicate");
        LoggingService.LogInfo($"  retry         — re-execute the same LL actions");
        LoggingService.LogInfo($"  replan        — re-plan from current blackboard state");
        LoggingService.LogInfo($"  skip          — force success and continue");
        LoggingService.LogInfo($"  abort         — let failure propagate");

        while (true)
        {
            Console.Write("recovery> ");
            string input = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(input)) continue;

            if (input.Equals("retry", StringComparison.OrdinalIgnoreCase))
            {
                return DoRetry(actionName);
            }
            else if (input.Equals("replan", StringComparison.OrdinalIgnoreCase))
            {
                return DoReplan(actionName);
            }
            else if (input.Equals("skip", StringComparison.OrdinalIgnoreCase))
            {
                return DoSkip(actionName);
            }
            else if (input.Equals("abort", StringComparison.OrdinalIgnoreCase))
            {
                LoggingService.LogWarning($"🛑 DecoratorRecovery: ABORT — failure propagates for '{actionName}'");
                return BTNodeResult.Failure;
            }
            else if (input.Equals("list", StringComparison.OrdinalIgnoreCase))
            {
                var predicates = blackboard.GetTruePredicates();
                LoggingService.LogInfo($"--- True predicates ({predicates.Count}) ---");
                foreach (var p in predicates.OrderBy(p => p.GetPredicateType()))
                    LoggingService.LogInfo($"  {p.GetPredicateType()} {string.Join(" ", p.GetPDDLParameterValues())}");
            }
            else if (input.Equals("list all", StringComparison.OrdinalIgnoreCase))
            {
                var predicates = blackboard.GetAllPredicates();
                LoggingService.LogInfo($"--- All predicates ({predicates.Count}) ---");
                foreach (var p in predicates.OrderBy(p => p.GetPredicateType()))
                {
                    string neg = p.not ? " [NEGATED]" : "";
                    LoggingService.LogInfo($"  {p.GetPredicateType()} {string.Join(" ", p.GetPDDLParameterValues())}{neg}");
                }
            }
            else if (input.StartsWith("set ", StringComparison.OrdinalIgnoreCase))
            {
                HandleSetCommand(input, blackboard);
            }
            else
            {
                LoggingService.LogWarning($"Unknown command: {input}");
                LoggingService.LogInfo("Commands: list | list all | set ... | retry | replan | skip | abort");
            }
        }
    }

    private void HandleSetCommand(string input, Blackboard<FastName> blackboard)
    {
        var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3)
        {
            LoggingService.LogWarning("Usage: set <predicateType> <param1> [param2] ... <true|false>");
            return;
        }

        string boolStr = parts[^1];
        if (!boolStr.Equals("true", StringComparison.OrdinalIgnoreCase) &&
            !boolStr.Equals("false", StringComparison.OrdinalIgnoreCase))
        {
            LoggingService.LogWarning("Last argument must be 'true' or 'false'");
            return;
        }

        bool setTrue = boolStr.Equals("true", StringComparison.OrdinalIgnoreCase);
        string predType = parts[1].ToLower();
        var paramValues = parts[2..^1];

        var allPredicates = blackboard.GetAllPredicates();
        var match = allPredicates.FirstOrDefault(p =>
            p.GetPredicateType().Equals(predType, StringComparison.OrdinalIgnoreCase) &&
            p.GetPDDLParameterValues().Select(v => v.ToLower()).SequenceEqual(
                paramValues.Select(v => v.ToLower())));

        if (match != null)
        {
            bool oldNot = match.not;
            match.not = !setTrue;
            LoggingService.LogSuccess($"✅ Updated: {predType} {string.Join(" ", paramValues)} — not: {oldNot} → {match.not}");
        }
        else
        {
            LoggingService.LogWarning($"Predicate not found: {predType}_{string.Join("_", paramValues)}");
            LoggingService.LogInfo("Tip: use 'list all' to see available predicates and their exact parameter names");
        }
    }

    private BTNodeResult DoRetry(string actionName)
    {
        LoggingService.LogInfo($"🔄 DecoratorRecovery: RETRY — resetting LL subtree for '{actionName}'");

        var llFlowNode = AttachedAction.HighLevelSubtree as DynamicFlowNode;
        if (llFlowNode == null)
        {
            LoggingService.LogError($"❌ DecoratorRecovery: No LL subtree found for '{actionName}'");
            return BTNodeResult.Failure;
        }

        // Reset all nodes in-place (statuses + GraphNode flags) without clearing the graph
        var graph = llFlowNode.GetActionGraph();
        graph.ResetAllNodeStatuses();

        // Lightweight reset — preserves actionGraph
        llFlowNode.ResetForRetry();

        // Reset the ML action so it re-enters
        AttachedAction.Reset();

        LoggingService.LogSuccess($"✅ DecoratorRecovery: RETRY complete — will re-execute on next tick");
        return BTNodeResult.InProgress;
    }

    private BTNodeResult DoReplan(string actionName)
    {
        LoggingService.LogInfo($"🔄 DecoratorRecovery: REPLAN — resetting ML-level planning for '{actionName}'");

        var mlFlowNode = FindParentMLFlowNode();
        if (mlFlowNode == null)
        {
            LoggingService.LogError($"❌ DecoratorRecovery: Could not find parent ML FlowNode for '{actionName}'");
            return BTNodeResult.Failure;
        }

        LoggingService.LogInfo($"🔄 DecoratorRecovery: Found parent ML FlowNode: {mlFlowNode.DebugDisplayName}");

        if (mlFlowNode.ServicePlanning is ServicePlanning plannerService)
        {
            LoggingService.LogInfo($"🔄 DecoratorRecovery: Resetting planning service");
            plannerService.ResetPlanningService();
        }

        mlFlowNode.ResetForNextRound();

        LoggingService.LogSuccess($"✅ DecoratorRecovery: REPLAN complete — will re-plan on next tick");
        return BTNodeResult.InProgress;
    }

    private BTNodeResult DoSkip(string actionName)
    {
        LoggingService.LogWarning($"⏭️ DecoratorRecovery: SKIP — forcing success for '{actionName}' (effects NOT applied)");
        return BTNodeResult.Success;
    }

    private DynamicFlowNode FindParentMLFlowNode()
    {
        var current = AttachedAction.ParentNode;
        while (current != null)
        {
            if (current is DynamicFlowNode dfn)
                return dfn;
            current = current.ParentNode;
        }
        return null;
    }
}
