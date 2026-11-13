using System;
using System.Linq;
using BehaviorTreeMainProject.Log.Services;
using BehaviorTreeMainProject.Services.AIPlanning.Replan;

namespace BehaviorTreeMainProject.Decorators.Replan
{
    /// <summary>
    /// PrePlan decorator: clears the owning planning service's latched
    /// "completed + failed" state at the start of a tick so a single
    /// planner failure (e.g. ENHSP exit 255) does not permanently poison
    /// the flow node. Without this, <c>ServicePlanning.OnEvaluate</c>
    /// short-circuits every subsequent tick with an instant FAILED result
    /// while <c>DecoratorRetryOnFailure</c> at an outer level keeps forcing
    /// re-entry, producing a fast infinite loop.
    ///
    /// The same problem gives the same answer, so retries are bounded: at most
    /// <see cref="MaxRetriesPerState"/> for an unchanged world state (enough for
    /// a transient error), and the count starts over once the blackboard's true
    /// predicates change (e.g. after a fault or another cassette's effects) or a
    /// plan succeeds. It used to clear the failure on every tick, which sent one
    /// planner request per tick for as long as the tree ran.
    ///
    /// Must run FIRST in the replan pipeline so downstream decorators
    /// (goal-check, extras, regeneration) see a fresh service state.
    /// Cached successful plans are untouched — only the failed-latch is cleared.
    /// </summary>
    public class DecoratorResetFailedPlan : Decorator, IReplanModifier
    {
        /// <summary>Retries allowed while the world state stays the same.</summary>
        public const int MaxRetriesPerState = 3;

        private int _retries;
        private string _stateAtLastRetry;

        public override bool CanPostProcessTickResult => false;
        public override BTNodeResult PostProcessTickResult(BTNodeResult InResult) => InResult;
        protected override bool OnEvaluate(float InDeltaTime) => true;

        public PrePlanResult PrePlan(PDDLPlanningContext ctx)
        {
            var svc = ctx.Service;
            if (svc == null) return PrePlanResult.Proceed;

            if (svc.HasCompleted && svc.WasSuccessful)
            {
                _retries = 0;
                _stateAtLastRetry = null;
                return PrePlanResult.Proceed;
            }

            if (svc.HasCompleted && !svc.WasSuccessful)
            {
                string state = StateSignature(ctx.Blackboard);
                if (state != _stateAtLastRetry)
                {
                    _retries = 0;
                    _stateAtLastRetry = state;
                }

                if (_retries >= MaxRetriesPerState)
                {
                    if (_retries == MaxRetriesPerState)
                    {
                        LoggingService.LogWarning(
                            $"🛑 DecoratorResetFailedPlan: {svc.GetType().Name} (owner: {ctx.FlowNode?.DebugDisplayName}) failed {MaxRetriesPerState + 1} times on an unchanged world state — no more retries until the state changes");
                        _retries++;
                    }
                    return PrePlanResult.Proceed;
                }

                _retries++;
                LoggingService.LogInfo(
                    $"🔄 DecoratorResetFailedPlan: Clearing latched planner failure on {svc.GetType().Name} (owner: {ctx.FlowNode?.DebugDisplayName}) — retry {_retries}/{MaxRetriesPerState}");
                svc.ClearFailedCompletion();
            }
            return PrePlanResult.Proceed;
        }

        public void ApplyModifications(PDDLPlanningContext ctx) { /* no-op */ }

        // The true predicates, order-independent; equal strings mean the planner would see the same state.
        private static string StateSignature(Blackboard<FastName> blackboard)
        {
            if (blackboard == null) return "";
            var names = blackboard.GetTruePredicates().Select(p => p.PredicateName.ToString()).ToList();
            names.Sort(StringComparer.Ordinal);
            return string.Join("|", names);
        }
    }
}
