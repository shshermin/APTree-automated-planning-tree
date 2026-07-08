using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using BehaviorTreeMainProject.Log.Services;

/// <summary>
/// Two-tier predicate store: in-process Dictionary hot-index for O(1) point
/// reads, plus an embedded SQLite database for the expensive scan/pattern
/// queries that were O(n) reflection loops in the old Blackboard.
///
/// Layout
/// ──────
///   Hot index  — serves TryGet / ContainsKey / All / AllTrue unchanged.
///   SQLite     — serves HasSimilar / HasFormattedDuplicate /
///                CleanupAtAgentPredicates via indexed queries.
///
/// Writes go to both tiers synchronously.  The SQLite file path defaults to
/// ":memory:" (in-process, no disk I/O) but can be set to a real path for
/// external inspection or the optional Bolt mirror via the Cypher export in
/// PddlExporter.
///
/// PDDL projection compatibility
/// ──────────────────────────────
/// The 'predicates' table stores the same structured data that the PDDL
/// exporter and a future Isaac-Sim bridge consume via SQL or Cypher queries,
/// making world-state → PDDL a deterministic, queryable projection rather
/// than an ad-hoc C# scan (see PddlExporter).
///
/// Isaac Sim compatibility note
/// ────────────────────────────
/// A USD→predicate bridge maps Isaac scene prims/relationships to this table.
/// Implement IIsaacSceneBridge and call IngestFromIsaac() to populate the
/// store from a running sim session without modifying BT nodes.
/// </summary>
public sealed class SqlitePredicateStore : IPredicateStore
{
    // ── hot index ─────────────────────────────────────────────────────────────
    private readonly Dictionary<FastName, Predicate> _hot = new();

    // ── SQLite ────────────────────────────────────────────────────────────────
    private readonly SqliteConnection _db;
    private bool _disposed;

    public string StoreType => "Sqlite";
    public int Count => _hot.Count;

    /// <param name="dbPath">
    ///   SQLite connection string path.  Use ":memory:" (default) for an
    ///   ephemeral in-process store, or an absolute path for persistence.
    /// </param>
    public SqlitePredicateStore(string dbPath = ":memory:")
    {
        _db = new SqliteConnection($"Data Source={dbPath}");
        _db.Open();
        InitSchema();
        LoggingService.LogInfo($"[SqlitePredicateStore] opened — path={dbPath}");
    }

    // ── Schema ────────────────────────────────────────────────────────────────

    private void InitSchema()
    {
        Execute(@"
            CREATE TABLE IF NOT EXISTS predicates (
                key            TEXT    PRIMARY KEY,
                predicate_type TEXT    NOT NULL,
                negated        INTEGER NOT NULL DEFAULT 0,
                formatted_str  TEXT    NOT NULL,
                param0         TEXT    NOT NULL DEFAULT '',
                param1         TEXT    NOT NULL DEFAULT ''
            );
            CREATE INDEX IF NOT EXISTS idx_fmt
                ON predicates(formatted_str);
            CREATE INDEX IF NOT EXISTS idx_type_p0_p1
                ON predicates(predicate_type, param0, param1);
            CREATE INDEX IF NOT EXISTS idx_type_p0
                ON predicates(predicate_type, param0);
            CREATE INDEX IF NOT EXISTS idx_negated
                ON predicates(negated);
        ");
    }

    // ── IPredicateStore — Writes ──────────────────────────────────────────────

    public void Upsert(FastName key, Predicate p)
    {
        _hot[key] = p;

        var pv = p.GetPDDLParameterValues();
        using var cmd = _db.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO predicates
                (key, predicate_type, negated, formatted_str, param0, param1)
            VALUES
                (@key, @type, @neg, @fmt, @p0, @p1)
            ON CONFLICT(key) DO UPDATE SET
                predicate_type = excluded.predicate_type,
                negated        = excluded.negated,
                formatted_str  = excluded.formatted_str,
                param0         = excluded.param0,
                param1         = excluded.param1";
        cmd.Parameters.AddWithValue("@key",  key.ToString());
        cmd.Parameters.AddWithValue("@type", p.PredicateTypeName);
        cmd.Parameters.AddWithValue("@neg",  p.not ? 1 : 0);
        cmd.Parameters.AddWithValue("@fmt",  BlackboardExtensions.FormatPredicate(p));
        cmd.Parameters.AddWithValue("@p0",   pv.Count > 0 ? pv[0] : "");
        cmd.Parameters.AddWithValue("@p1",   pv.Count > 1 ? pv[1] : "");
        cmd.ExecuteNonQuery();
    }

    public void UpdateNegation(FastName key, bool negated)
    {
        if (_hot.TryGetValue(key, out var p)) p.not = negated;

        using var cmd = _db.CreateCommand();
        cmd.CommandText = "UPDATE predicates SET negated = @neg WHERE key = @key";
        cmd.Parameters.AddWithValue("@neg", negated ? 1 : 0);
        cmd.Parameters.AddWithValue("@key", key.ToString());
        cmd.ExecuteNonQuery();
    }

    public bool RemoveKey(FastName key)
    {
        if (!_hot.Remove(key)) return false;

        using var cmd = _db.CreateCommand();
        cmd.CommandText = "DELETE FROM predicates WHERE key = @key";
        cmd.Parameters.AddWithValue("@key", key.ToString());
        cmd.ExecuteNonQuery();
        return true;
    }

    // ── IPredicateStore — Point reads (hot-index) ─────────────────────────────

    public bool TryGet(FastName key, out Predicate? p) => _hot.TryGetValue(key, out p);
    public bool ContainsKey(FastName key) => _hot.ContainsKey(key);

    // ── IPredicateStore — Scans (hot-index, simple filter) ────────────────────

    public IReadOnlyList<Predicate> All() => _hot.Values.ToList();

    public IReadOnlyList<Predicate> AllTrue() =>
        _hot.Values.Where(p => !p.not).ToList();

    // ── IPredicateStore — Pattern queries (indexed SQLite) ────────────────────

    /// <summary>
    /// O(1) indexed lookup: does a predicate with this type + parameters exist?
    /// Replaces the old O(n) reflection scan.
    /// </summary>
    public bool HasSimilar(Predicate p)
    {
        var pv = p.GetPDDLParameterValues();
        using var cmd = _db.CreateCommand();
        cmd.CommandText = @"
            SELECT 1 FROM predicates
            WHERE predicate_type = @type
              AND param0 = @p0
              AND param1 = @p1
            LIMIT 1";
        cmd.Parameters.AddWithValue("@type", p.PredicateTypeName);
        cmd.Parameters.AddWithValue("@p0",   pv.Count > 0 ? pv[0] : "");
        cmd.Parameters.AddWithValue("@p1",   pv.Count > 1 ? pv[1] : "");
        return cmd.ExecuteScalar() != null;
    }

    /// <summary>
    /// O(1) indexed lookup on the formatted-string column.
    /// Replaces the old LINQ Any() scan in SetPredicateSync.
    /// </summary>
    public bool HasFormattedDuplicate(string formattedStr)
    {
        using var cmd = _db.CreateCommand();
        cmd.CommandText =
            "SELECT 1 FROM predicates WHERE formatted_str = @fmt LIMIT 1";
        cmd.Parameters.AddWithValue("@fmt", formattedStr);
        return cmd.ExecuteScalar() != null;
    }

    /// <summary>
    /// Single DELETE + hot-index sync.  Replaces the O(n) key-string
    /// loop in CleanupConflictingAtAgentPredicates.
    /// </summary>
    public void CleanupAtAgentPredicates(string robotName)
    {
        var keysToRemove = new List<string>();
        using (var sel = _db.CreateCommand())
        {
            sel.CommandText =
                "SELECT key FROM predicates WHERE predicate_type = 'atagent' AND param0 = @robot";
            sel.Parameters.AddWithValue("@robot", robotName);
            using var reader = sel.ExecuteReader();
            while (reader.Read()) keysToRemove.Add(reader.GetString(0));
        }

        foreach (var ks in keysToRemove)
            _hot.Remove(new FastName(ks));

        if (keysToRemove.Count == 0) return;
        using var del = _db.CreateCommand();
        del.CommandText =
            "DELETE FROM predicates WHERE predicate_type = 'atagent' AND param0 = @robot";
        del.Parameters.AddWithValue("@robot", robotName);
        del.ExecuteNonQuery();

        LoggingService.LogInfo(
            $"[SqlitePredicateStore] CleanupAtAgent: removed {keysToRemove.Count} predicates for {robotName}");
    }

    // ── Isaac Sim bridge hook ─────────────────────────────────────────────────

    /// <summary>
    /// Ingest world-state predicates from an Isaac Sim scene via a bridge
    /// that implements <see cref="IIsaacSceneBridge"/>.  Each predicate
    /// emitted by the bridge is upserted into the store; the optional
    /// <paramref name="upsertCallback"/> lets the caller (e.g. Blackboard)
    /// mirror the change to its own state.
    /// </summary>
    public void IngestFromIsaac(IIsaacSceneBridge bridge, Action<FastName, Predicate> upsertCallback)
    {
        int count = 0;
        foreach (var (key, predicate) in bridge.GetPredicates())
        {
            Upsert(key, predicate);
            upsertCallback(key, predicate);
            count++;
        }
        LoggingService.LogInfo($"[SqlitePredicateStore] Ingested {count} predicates from Isaac Sim");
    }

    // ── SQLite helper ─────────────────────────────────────────────────────────

    private void Execute(string sql)
    {
        using var cmd = _db.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    // ── Dispose ───────────────────────────────────────────────────────────────

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _db.Close();
        _db.Dispose();
    }
}
