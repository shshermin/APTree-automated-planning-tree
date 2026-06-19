using System.Collections.Generic;
using System.Linq;

/// <summary>
/// In-process Dictionary-backed predicate store.  This is the default; it
/// preserves the exact semantics of the old Blackboard internals and is a
/// safe fallback when the embedded graph store is not needed.
/// </summary>
public sealed class DictionaryPredicateStore : IPredicateStore
{
    private readonly Dictionary<FastName, Predicate> _dict = new();

    public string StoreType => "Dictionary";
    public int Count => _dict.Count;

    public void Upsert(FastName key, Predicate p) => _dict[key] = p;

    public void UpdateNegation(FastName key, bool negated)
    {
        if (_dict.TryGetValue(key, out var existing))
            existing.not = negated;
    }

    public bool RemoveKey(FastName key) => _dict.Remove(key);

    public bool TryGet(FastName key, out Predicate? p) => _dict.TryGetValue(key, out p);

    public bool ContainsKey(FastName key) => _dict.ContainsKey(key);

    public IReadOnlyList<Predicate> All() => _dict.Values.ToList();

    public IReadOnlyList<Predicate> AllTrue() =>
        _dict.Values.Where(p => !p.not).ToList();

    /// <summary>
    /// O(n) scan identical to the old Blackboard.HasSimilarPredicate.
    /// Use SqlitePredicateStore to replace this with an indexed query.
    /// </summary>
    public bool HasSimilar(Predicate newPredicate)
    {
        string newType = newPredicate.PredicateTypeName;
        var newParams = newPredicate.GetPDDLParameterValues();

        foreach (var existing in _dict.Values)
        {
            if (existing.PredicateTypeName != newType) continue;
            var existingParams = existing.GetPDDLParameterValues();
            if (existingParams.Count != newParams.Count) continue;

            bool match = true;
            for (int i = 0; i < newParams.Count; i++)
            {
                if (existingParams[i] != newParams[i]) { match = false; break; }
            }
            if (match) return true;
        }
        return false;
    }

    /// <summary>
    /// O(n) formatted-string duplicate check identical to the old
    /// SetPredicateSync inline scan.
    /// </summary>
    public bool HasFormattedDuplicate(string formattedStr) =>
        _dict.Values.Any(p => BlackboardExtensions.FormatPredicate(p) == formattedStr);

    /// <summary>
    /// Remove all atAgent predicates whose first PDDL parameter is <paramref name="robotName"/>.
    /// Preserves the exact key-string scan logic of the old
    /// CleanupConflictingAtAgentPredicates.
    /// </summary>
    public void CleanupAtAgentPredicates(string robotName)
    {
        var toRemove = _dict.Keys
            .Where(k => {
                var ks = k.ToString();
                return ks.Contains("atAgent", System.StringComparison.OrdinalIgnoreCase)
                    && ks.Contains(robotName);
            })
            .ToList();

        foreach (var k in toRemove)
            _dict.Remove(k);
    }

    public void Dispose() { /* nothing to free */ }
}
