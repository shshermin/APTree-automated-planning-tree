using System;
using System.Collections.Generic;
using System.Linq;

namespace AIPlanning
{
    /// <summary>
    /// Predicate state a plan is simulated on. Starts as a copy of the blackboard and
    /// follows the rules of Blackboard.SetPredicateSync, without touching the blackboard.
    /// </summary>
    public sealed class PlanSimulationState
    {
        private readonly Dictionary<FastName, Predicate> _predicates = new Dictionary<FastName, Predicate>();

        public PlanSimulationState(IEnumerable<Predicate> initial)
        {
            foreach (var predicate in initial)
                _predicates[predicate.PredicateName] = predicate.Clone();
        }

        /// <summary>The simulated predicates. Callers may flip their <c>not</c> flag.</summary>
        public IEnumerable<Predicate> All => _predicates.Values;

        /// <summary>The predicate stored under <paramref name="key"/>, or null.</summary>
        public Predicate Get(FastName key) => _predicates.TryGetValue(key, out var predicate) ? predicate : null;

        /// <summary>
        /// Applies one effect like Blackboard.SetPredicateSync: a positive atagent replaces
        /// the robot's other atagent facts, an existing key only changes its negation, and
        /// a duplicate under another key is ignored.
        /// </summary>
        public void Set(Predicate effect)
        {
            if (string.Equals(effect.GetPredicateType(), "atagent", StringComparison.OrdinalIgnoreCase) && !effect.not)
            {
                var robot = effect.GetPDDLParameterValues().FirstOrDefault();
                if (robot != null)
                {
                    var stale = _predicates
                        .Where(kv => string.Equals(kv.Value.GetPredicateType(), "atagent", StringComparison.OrdinalIgnoreCase)
                                     && kv.Value.GetPDDLParameterValues().FirstOrDefault() == robot)
                        .Select(kv => kv.Key)
                        .ToList();
                    foreach (var key in stale)
                        _predicates.Remove(key);
                }
            }

            if (_predicates.TryGetValue(effect.PredicateName, out var existing))
            {
                existing.not = effect.not;
                return;
            }

            string formatted = BlackboardExtensions.FormatPredicate(effect);
            if (_predicates.Values.Any(p => BlackboardExtensions.FormatPredicate(p) == formatted))
                return;

            _predicates[effect.PredicateName] = effect.Clone();
        }
    }
}
