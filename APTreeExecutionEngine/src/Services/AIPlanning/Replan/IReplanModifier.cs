namespace BehaviorTreeMainProject.Services.AIPlanning.Replan
{
    /// <summary>
    /// Outcome of a decorator's <see cref="IReplanModifier.PrePlan"/> hook.
    /// Lets decorators short-circuit the planning call.
    /// </summary>
    public enum PrePlanResult
    {
        /// <summary>Continue to the next decorator (or to planner invocation).</summary>
        Proceed,
        /// <summary>Skip the planner. Mark the service as completed-successful (no plan needed).</summary>
        SkipAsSuccess,
        /// <summary>Skip the planner. Mark the service as completed-failed.</summary>
        SkipAsFailure,
    }

    /// <summary>
    /// Hook contract implemented by decorators that want to participate in the
    /// PDDL replanning pipeline hosted by <c>ServicePDDLPlanning</c>.
    ///
    /// Flow per tick:
    ///   1. The service builds a <see cref="PDDLPlanningContext"/>.
    ///   2. Each <c>IReplanModifier</c> attached to the owning flow node has
    ///      <see cref="PrePlan"/> invoked in attach order. Any non-Proceed
    ///      result short-circuits the pipeline.
    ///   3. Each modifier then has <see cref="ApplyModifications"/> invoked
    ///      in attach order, mutating the context (extra objects, problem
    ///      file regeneration, HL patching, …).
    ///   4. The service then invokes the planner with the mutated request.
    ///
    /// Attach order matters: e.g. <c>DecoratorExtraPDDLObjects</c> should come
    /// BEFORE <c>DecoratorRegenerateFromBlackboard</c> so extras are folded
    /// into the regenerated problem file.
    /// </summary>
    public interface IReplanModifier
    {
        /// <summary>
        /// Inspect state before planning. Return non-<see cref="PrePlanResult.Proceed"/>
        /// to short-circuit (e.g. "all goals already met ⇒ skip as success").
        /// </summary>
        PrePlanResult PrePlan(PDDLPlanningContext ctx);

        /// <summary>
        /// Mutate the planning context: add extra object declarations, regenerate
        /// the problem file, patch the :init block, adjust the goal, etc.
        /// </summary>
        void ApplyModifications(PDDLPlanningContext ctx);
    }
}
