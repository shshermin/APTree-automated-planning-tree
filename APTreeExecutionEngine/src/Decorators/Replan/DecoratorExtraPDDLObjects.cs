using System.Collections.Generic;
using BehaviorTreeMainProject.Log.Services;
using BehaviorTreeMainProject.Services.AIPlanning.Replan;

namespace BehaviorTreeMainProject.Decorators.Replan
{
    /// <summary>
    /// Declarative registry of additional PDDL object declarations to inject
    /// into every regenerated problem file. Each entry appends a line of the
    /// form "<c>name - pddlType</c>" to the <c>(:objects ...)</c> block.
    ///
    /// Used e.g. by fault injection to declare a temporary drop location
    /// (<c>temploc1 - firstposition</c>) so that the predicate
    /// <c>(atplace stick4 temploc1)</c> referenced in the regenerated init
    /// state passes the "object is declared" filter.
    ///
    /// IMPORTANT: attach this decorator BEFORE <c>DecoratorRegenerateFromBlackboard</c>
    /// so extras are collected into the context before regeneration runs.
    /// </summary>
    public class DecoratorExtraPDDLObjects : Decorator, IReplanModifier
    {
        private readonly Dictionary<string, string> _extras
            = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);

        public override bool CanPostProcessTickResult => false;
        public override BTNodeResult PostProcessTickResult(BTNodeResult InResult) => InResult;
        protected override bool OnEvaluate(float InDeltaTime) => true;

        /// <summary>
        /// Register (or update) an extra PDDL object declaration. Safe to
        /// call multiple times with the same name — later calls overwrite.
        /// </summary>
        public void AddObject(string objectName, string pddlType)
        {
            if (string.IsNullOrWhiteSpace(objectName) || string.IsNullOrWhiteSpace(pddlType))
                return;
            _extras[objectName.Trim()] = pddlType.Trim();
            LoggingService.LogInfo(
                $"🧩 DecoratorExtraPDDLObjects: Registered '{objectName.Trim()} - {pddlType.Trim()}'");
        }

        /// <summary>Remove a previously-registered object declaration.</summary>
        public bool RemoveObject(string objectName)
            => !string.IsNullOrEmpty(objectName) && _extras.Remove(objectName.Trim());

        public IReadOnlyDictionary<string, string> Extras => _extras;

        public PrePlanResult PrePlan(PDDLPlanningContext ctx) => PrePlanResult.Proceed;

        public void ApplyModifications(PDDLPlanningContext ctx)
        {
            foreach (var kv in _extras)
                ctx.ExtraObjects[kv.Key] = kv.Value;
        }
    }
}
