using System.Linq;

namespace AIPlanning
{
    /// <summary>
    /// Validator for plans from symbolic (PDDL) planners. Checks that every ML action's
    /// predicates have resolved parameters, then plays the plan forward from the current
    /// blackboard state with the rules the engine applies at runtime: preconditions are
    /// checked for ML actions only (HL actions run in parallel cassettes and are not
    /// checked at runtime either), and every action's effects are applied as
    /// applyEffects() would apply them. A plan rejected here would fail during execution.
    /// </summary>
    public class SymbolicPlanValidator : PlanValidator
    {
        public override PlanValidationResult ValidatePlan(NodeGraph plan, Blackboard<FastName> blackboard)
        {
            if (plan == null)
                return PlanValidationResult.Invalid("No plan graph");

            var steps = plan.GetExecutionOrder();
            for (int i = 0; i < steps.Count; i++)
            {
                var action = steps[i];
                if (action == null)
                    return PlanValidationResult.Invalid($"Step {i + 1} has no action");
                if (IsHighLevel(action))
                    continue;

                // Generated predicates render a missing parameter as "null".
                foreach (var predicate in Declared(action.GetPreconditions()).Concat(Declared(action.GetEffects())))
                {
                    var parameters = predicate.GetPDDLParameterValues();
                    if (parameters.Any(p => string.IsNullOrEmpty(p) || p == "null"))
                        return PlanValidationResult.Invalid(
                            $"Step {i + 1} ({action.InstanceName}): {predicate.GetPredicateType()} has an unresolved parameter");
                }
            }

            var state = new PlanSimulationState(blackboard?.GetAllPredicates() ?? Enumerable.Empty<Predicate>());
            for (int i = 0; i < steps.Count; i++)
            {
                var action = steps[i];
                if (!IsHighLevel(action))
                {
                    foreach (var precondition in Declared(action.GetPreconditions()))
                    {
                        if (!PActionNode.PreconditionHolds(precondition, state.Get(precondition.PredicateName)))
                            return PlanValidationResult.Invalid(
                                $"Step {i + 1} ({action.InstanceName}): precondition {(precondition.not ? "not " : "")}{precondition.PredicateName} does not hold");
                    }
                }
                action.SimulateEffects(state);
            }

            return PlanValidationResult.Valid();
        }

        // Same rule the PActionNode constructor uses to tell the levels apart.
        private static bool IsHighLevel(PActionNode action) => action.GetType().Name.EndsWith("HL");

        private static System.Collections.Generic.IEnumerable<Predicate> Declared(State state)
            => state?.GetAllPredicates() ?? Enumerable.Empty<Predicate>();
    }
}
