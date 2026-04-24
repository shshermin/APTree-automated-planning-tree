using BehaviorTreeMainProject.Log.Services;
using BehaviorTreeMainProject.Services.AIPlanning.Replan;

namespace BehaviorTreeMainProject.Decorators.Replan
{
    /// <summary>
    /// PrePlan decorator: clears the owning planning service's latched
    /// "completed + failed" state at the start of each tick so a single
    /// planner failure (e.g. ENHSP exit 255) does not permanently poison
    /// the flow node. Without this, <c>ServicePlanning.OnEvaluate</c>
    /// short-circuits every subsequent tick with an instant FAILED result
    /// while <c>DecoratorRetryOnFailure</c> at an outer level keeps forcing
    /// re-entry, producing a fast infinite loop.
    ///
    /// Must run FIRST in the replan pipeline so downstream decorators
    /// (goal-check, extras, regeneration) see a fresh service state.
    /// Cached successful plans are untouched — only the failed-latch is cleared.
    /// </summary>
    public class DecoratorResetFailedPlan : Decorator, IReplanModifier
    {
        public override bool CanPostProcessTickResult => false;
        public override BTNodeResult PostProcessTickResult(BTNodeResult InResult) => InResult;
        protected override bool OnEvaluate(float InDeltaTime) => true;

        public PrePlanResult PrePlan(PDDLPlanningContext ctx)
        {
            var svc = ctx.Service;
            if (svc == null) return PrePlanResult.Proceed;

            if (svc.HasCompleted && !svc.WasSuccessful)
            {
                LoggingService.LogInfo(
                    $"🔄 DecoratorResetFailedPlan: Clearing latched planner failure on {svc.GetType().Name} (owner: {ctx.FlowNode?.DebugDisplayName}) — allowing retry");
                svc.ClearFailedCompletion();
            }
            return PrePlanResult.Proceed;
        }

        public void ApplyModifications(PDDLPlanningContext ctx) { /* no-op */ }
    }
}
