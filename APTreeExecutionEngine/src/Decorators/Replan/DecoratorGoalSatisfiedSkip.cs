using BehaviorTreeMainProject.Log.Services;
using BehaviorTreeMainProject.Services.AIPlanning.Replan;
using System.Linq;

namespace BehaviorTreeMainProject.Decorators.Replan
{
    /// <summary>
    /// PrePlan decorator: if every goal predicate of the parent HL action is
    /// already satisfied on the blackboard, short-circuits planning as success
    /// (no plan needed). Replaces the inline goal-satisfaction check that
    /// used to live in <c>ServicePDDLPlanning.OnEvaluate</c>.
    /// </summary>
    public class DecoratorGoalSatisfiedSkip : Decorator, IReplanModifier
    {
        public override bool CanPostProcessTickResult => false;
        public override BTNodeResult PostProcessTickResult(BTNodeResult InResult) => InResult;
        protected override bool OnEvaluate(float InDeltaTime) => true;

        public PrePlanResult PrePlan(PDDLPlanningContext ctx)
        {
            if (ctx.ParentAction == null || ctx.Blackboard == null)
                return PrePlanResult.Proceed;

            var goalPredicates = ctx.ParentAction.GetActionEffects();
            if (goalPredicates == null || goalPredicates.Count == 0)
                return PrePlanResult.Proceed;

            var currentState = ctx.Blackboard.GetTruePredicates();
            bool allGoalsMet = goalPredicates.All(goal =>
                currentState.Any(init => init.PredicateName == goal.PredicateName
                                      && init.not == goal.not));

            if (allGoalsMet)
            {
                LoggingService.LogInfo(
                    $"⏭️ DecoratorGoalSatisfiedSkip: All {goalPredicates.Count} goal predicates already satisfied — skipping planning for {ctx.ParentAction.InstanceName}");
                return PrePlanResult.SkipAsSuccess;
            }
            return PrePlanResult.Proceed;
        }

        public void ApplyModifications(PDDLPlanningContext ctx) { /* no-op */ }
    }
}
