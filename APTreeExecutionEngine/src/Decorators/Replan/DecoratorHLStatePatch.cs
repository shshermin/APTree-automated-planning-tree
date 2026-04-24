using BehaviorTreeMainProject.Log.Services;
using BehaviorTreeMainProject.Services.AIPlanning;
using BehaviorTreeMainProject.Services.AIPlanning.Replan;

namespace BehaviorTreeMainProject.Decorators.Replan
{
    /// <summary>
    /// For HL (cross-cassette) planning only: patches the static HL problem
    /// file's (:init ...) section with live robot-state predicates from the
    /// blackboard (gripperempty, positionfree, activetool, hastool, …) so
    /// planning starts from the actual robot state rather than the
    /// assumptions baked into the static file.
    ///
    /// Runs once per service activation — the internal <c>_patched</c> flag
    /// prevents repeated edits. Call <see cref="Reset"/> on cross-cassette
    /// reset (the service's <c>ResetPlanningService</c> does this).
    /// </summary>
    public class DecoratorHLStatePatch : Decorator, IReplanModifier
    {
        private bool _patched = false;

        public override bool CanPostProcessTickResult => false;
        public override BTNodeResult PostProcessTickResult(BTNodeResult InResult) => InResult;
        protected override bool OnEvaluate(float InDeltaTime) => true;

        /// <summary>Re-enable patching after a cross-cassette reset.</summary>
        public void Reset() => _patched = false;

        public PrePlanResult PrePlan(PDDLPlanningContext ctx) => PrePlanResult.Proceed;

        public void ApplyModifications(PDDLPlanningContext ctx)
        {
            // Only HL (no parent HL action) and only once per activation.
            if (_patched) return;
            if (ctx.ParentAction != null) return;
            if (ctx.Blackboard == null || ctx.PlanningRequest == null) return;
            if (string.IsNullOrEmpty(ctx.PlanningRequest.ProblemFileContent)) return;

            ctx.PlanningRequest.ProblemFileContent =
                ctx.Service.PatchRobotStatePredicatesPublic(ctx.PlanningRequest.ProblemFileContent, ctx.Blackboard);
            _patched = true;
            LoggingService.LogInfo(
                "🔄 DecoratorHLStatePatch: Patched live robot-state predicates into HL problem file");
        }
    }
}
