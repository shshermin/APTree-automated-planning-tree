/// <summary>
/// Storage abstraction for world-state predicates.
///
/// Two implementations ship with the engine:
///   DictionaryPredicateStore  — in-process Dictionary, zero-overhead default.
///   SqlitePredicateStore      — Dictionary hot-index for O(1) point reads +
///                               SQLite for HasSimilar / CleanupAtAgent queries
///                               (replaces O(n) reflection scans).
///
/// Callers interact with Blackboard&lt;T&gt; as before; the store is an internal
/// implementation detail selected by BehaviorTreeConfiguration.PredicateStoreType.
/// </summary>
public interface IPredicateStore : IDisposable
{
    /// <summary>Human-readable name used in diagnostics ("Dictionary", "Sqlite", …).</summary>
    string StoreType { get; }

    int Count { get; }

    // ── Writes ────────────────────────────────────────────────────────────────

    /// <summary>Insert or fully replace the predicate at <paramref name="key"/>.</summary>
    void Upsert(FastName key, Predicate p);

    /// <summary>Flip only the negation flag on an existing entry; no-op if not found.</summary>
    void UpdateNegation(FastName key, bool negated);

    /// <summary>Remove by key. Returns true if an entry was actually deleted.</summary>
    bool RemoveKey(FastName key);

    // ── Point reads (O(1) via hot-index in graph-backed implementations) ──────

    bool TryGet(FastName key, out Predicate? p);
    bool ContainsKey(FastName key);

    // ── Scans ─────────────────────────────────────────────────────────────────

    IReadOnlyList<Predicate> All();

    /// <summary>All predicates with <c>not == false</c>.</summary>
    IReadOnlyList<Predicate> AllTrue();

    // ── Pattern queries (indexed in SQLite / graph backends) ──────────────────

    /// <summary>
    /// Returns true if a predicate with the same type AND same parameters
    /// already exists (regardless of the key or negation state).
    /// Replaces the O(n) reflection scan in the old Blackboard.HasSimilarPredicate.
    /// </summary>
    bool HasSimilar(Predicate p);

    /// <summary>
    /// Returns true if any predicate whose formatted string equals
    /// <paramref name="formattedStr"/> already exists.
    /// Used for content-based duplicate detection in SetPredicateSync.
    /// </summary>
    bool HasFormattedDuplicate(string formattedStr);

    /// <summary>
    /// Remove all "atAgent" predicates whose first parameter matches
    /// <paramref name="robotName"/>.  Replaces the O(n) key-string scan
    /// in CleanupConflictingAtAgentPredicates.
    /// </summary>
    void CleanupAtAgentPredicates(string robotName);
}
