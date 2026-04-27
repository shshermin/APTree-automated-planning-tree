using BehaviorTreeMainProject.Log.Services;
using BehaviorTreeMainProject.Services.AIPlanning;
using BehaviorTreeMainProject.Services.AIPlanning.Replan;
using BehaviorTreeMainProject.Services.FaultInjection;

namespace BehaviorTreeMainProject.Decorators.Replan
{
    /// <summary>
    /// Detects the <see cref="BlackboardKeys.HLReplanKey"/> flag and performs a full
    /// rebuild of the HL PDDL problem file's <c>(:init …)</c> section from the
    /// current blackboard state before the planner is invoked.
    ///
    /// This mirrors what <see cref="DecoratorRegenerateFromBlackboard"/> does for ML
    /// level: all true blackboard predicates are written into <c>(:init)</c>, filtered
    /// to objects declared in the static file's <c>(:objects)</c> block. The
    /// <c>(:objects)</c> and <c>(:goal)</c> sections are preserved verbatim.
    ///
    /// Pipeline position: must be attached BEFORE <see cref="DecoratorHLStatePatch"/>
    /// so HLStatePatch can further patch robot-state predicates on top of the rebuilt
    /// content.
    ///
    /// Flow per planning tick when the flag is set:
    ///  1. <see cref="PrePlan"/>: consumes the flag, resets <see cref="DecoratorHLStatePatch"/>,
    ///     clears <c>ProblemFileContent</c>, marks <c>_active = true</c>.
    ///  2. <see cref="ApplyModifications"/>: loads static file, calls
    ///     <see cref="ServicePDDLPlanning.RebuildHLInitFromBlackboard"/> to do a full
    ///     <c>(:init)</c> rebuild, writes result to <c>ctx.PlanningRequest.ProblemFileContent</c>.
    ///  3. <c>PopulateInlineFileContents</c> skips the problem file (already non-null).
    ///  4. <see cref="DecoratorHLStatePatch"/> patches robot-state predicates on top.
    /// </summary>
    public class DecoratorHLFaultReplan : Decorator, IReplanModifier
    {
        private bool _active = false;

        public override bool CanPostProcessTickResult => false;
        public override BTNodeResult PostProcessTickResult(BTNodeResult InResult) => InResult;
        protected override bool OnEvaluate(float InDeltaTime) => true;

        public PrePlanResult PrePlan(PDDLPlanningContext ctx)
        {
            if (ctx.ParentAction != null) return PrePlanResult.Proceed; // HL only
            if (ctx.Blackboard == null || ctx.FlowNode == null) return PrePlanResult.Proceed;

            var hlReplanKey = BlackboardKeys.HLReplanKey(ctx.FlowNode.DebugDisplayName);
            bool hlFlagSet;
            try { hlFlagSet = ctx.Blackboard.GetBool(hlReplanKey); }
            catch { hlFlagSet = false; }
            if (!hlFlagSet) return PrePlanResult.Proceed;

            // NOTE: Flag is intentionally NOT consumed here. It is consumed in
            // ApplyModifications (which only runs when HasCompleted=false after
            // DecoratorHLFaultAbort has reset the planning service).
            // Consuming here would make the flag invisible to DecoratorHLFaultAbort's
            // PostProcessTickResult, which runs AFTER this GeneralServices phase.
            bool svcHasCompleted = ctx.Service?.HasCompleted ?? false;
            LoggingService.LogWarning(
                $"🔄 DecoratorHLFaultReplan [{ctx.FlowNode.DebugDisplayName}]: HL replan flag detected in PrePlan" +
                $" (HasCompleted={svcHasCompleted}, IsExecuting={ctx.Service?.IsExecuting ?? false})" +
                $" — flag preserved for DecoratorHLFaultAbort");

            if (svcHasCompleted)
            {
                // Planning service has already completed with the old plan.
                // Just mark as active and let DecoratorHLFaultAbort (PostProcessTickResult)
                // call ResetPlanningService + ResetForNextRound on this same tick.
                // Next tick: HasCompleted=false → ApplyModifications will run.
                _active = true;
                return PrePlanResult.Proceed;
            }

            // HasCompleted=false: planning service was already reset (abort happened
            // last tick). Now we can fully prepare for replanning.
            _active = true;

            // Reset DecoratorHLStatePatch so it re-patches robot-state this cycle
            foreach (var dec in ctx.FlowNode.GetDecorators().OfType<DecoratorHLStatePatch>())
                dec.Reset();

            // Clear cached content so PopulateInlineFileContents doesn't overwrite
            // our result if we fail early.
            ctx.PlanningRequest.ProblemFile = ctx.OriginalProblemFile;
            ctx.PlanningRequest.ProblemFileContent = null;

            LoggingService.LogWarning(
                $"🔄 DecoratorHLFaultReplan [{ctx.FlowNode.DebugDisplayName}]: planning service is fresh — proceeding to ApplyModifications");

            return PrePlanResult.Proceed;
        }

        public void ApplyModifications(PDDLPlanningContext ctx)
        {
            if (!_active) return;
            _active = false;

            if (ctx.ParentAction != null) return; // HL only
            if (ctx.Blackboard == null || ctx.PlanningRequest == null) return;

            string originalContent = ctx.Service?.LoadOriginalProblemContent();
            if (string.IsNullOrEmpty(originalContent))
            {
                LoggingService.LogError(
                    $"🔄 DecoratorHLFaultReplan [{ctx.FlowNode?.DebugDisplayName}]: Could not load original problem file — (:init) NOT rebuilt");
                return;
            }

            // Full rebuild: strip entire (:init), replace with all true blackboard
            // predicates filtered to objects declared in (:objects). Identical
            // approach to GenerateDynamicPDDLProblem for ML-level replanning.
            string rebuilt = ServicePDDLPlanning.RebuildHLInitFromBlackboard(originalContent, ctx.Blackboard);
            ctx.PlanningRequest.ProblemFileContent = rebuilt;

            // NOW consume the flag — abort has already fired and reset the service.
            var hlReplanKey = BlackboardKeys.HLReplanKey(ctx.FlowNode?.DebugDisplayName ?? "");
            try { ctx.Blackboard.SetBool(hlReplanKey, false); } catch { }

            LoggingService.LogWarning(
                $"🔄 DecoratorHLFaultReplan [{ctx.FlowNode?.DebugDisplayName}]: (:init) rebuilt from blackboard and flag consumed — sending to HL planner");
            LoggingService.LogInfo(
                $"🔄 DecoratorHLFaultReplan: rebuilt (:init) preview (first 400 chars):\n" +
                (rebuilt?.Length > 400 ? rebuilt.Substring(0, 400) + "..." : rebuilt ?? "(null)"));
        }
    }
}
