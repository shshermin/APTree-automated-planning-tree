using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BehaviorTreeMainProject.Log.Services;

namespace BehaviorTreeMainProject.Tests
{
    /// <summary>
    /// Parity test: populates DictionaryPredicateStore and SqlitePredicateStore
    /// with identical data from the Demonstrator input files, then diffs the
    /// outputs of every IPredicateStore method.
    ///
    /// Run this after any change to SqlitePredicateStore to verify that
    /// both stores produce identical results.
    /// </summary>
    public static class PredicateStoreParityTest
    {
        public static void Run()
        {
            LoggingService.Initialize("PredicateStoreParityTest", enableConsole: true, enableFile: false);
            LoggingService.LogSection("PREDICATE STORE PARITY TEST");

            int passed = 0;
            int failed = 0;

            using var dictStore   = new DictionaryPredicateStore();
            using var sqliteStore = new SqlitePredicateStore(":memory:");

            // ── 1. Populate both stores via Blackboard + BlackboardWriter ──────

            using var dictBb   = new Blackboard<FastName>(dictStore);
            using var sqliteBb = new Blackboard<FastName>(sqliteStore);

            string basePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "src");

            string setupObjects  = Path.Combine(basePath, "ModelLoader", "DemonstratorSetupObjects.json");
            string initState     = Path.Combine(basePath, "ModelLoader", "DemonstratorInitState.json");
            string actionInstances = Path.Combine(basePath, "InputInstances", "ActionInstances.txt");
            string goalState     = Path.Combine(basePath, "ModelLoader", "DemonstratorGoalState.json");

            foreach (var (bb, label) in new[] { (dictBb, "Dict"), (sqliteBb, "Sqlite") })
            {
                var writer = new BlackboardWriter(bb);
                writer.RegisterAllTypes();
                writer.RegisterAllInstances(setupObjects, initState, actionInstances);
                writer.RegisterGoalStatePredicates(goalState);
                LoggingService.LogInfo($"[{label}] Loaded {bb.GetAllPredicates().Count} predicates");
            }

            // ── 2. All() ──────────────────────────────────────────────────────

            var dictAll   = dictStore.All().Select(p => BlackboardExtensions.FormatPredicate(p)).OrderBy(s => s).ToList();
            var sqliteAll = sqliteStore.All().Select(p => BlackboardExtensions.FormatPredicate(p)).OrderBy(s => s).ToList();
            Check("All() count",   dictAll.Count == sqliteAll.Count, ref passed, ref failed);
            Check("All() content", dictAll.SequenceEqual(sqliteAll),  ref passed, ref failed);

            if (dictAll.Count != sqliteAll.Count)
            {
                var onlyInDict   = dictAll.Except(sqliteAll).ToList();
                var onlyInSqlite = sqliteAll.Except(dictAll).ToList();
                if (onlyInDict.Any())   LoggingService.LogWarning($"  Only in Dict:   {string.Join(", ", onlyInDict.Take(5))}");
                if (onlyInSqlite.Any()) LoggingService.LogWarning($"  Only in Sqlite: {string.Join(", ", onlyInSqlite.Take(5))}");
            }

            // ── 3. AllTrue() ──────────────────────────────────────────────────

            var dictTrue   = dictStore.AllTrue().Select(p => BlackboardExtensions.FormatPredicate(p)).OrderBy(s => s).ToList();
            var sqliteTrue = sqliteStore.AllTrue().Select(p => BlackboardExtensions.FormatPredicate(p)).OrderBy(s => s).ToList();
            Check("AllTrue() count",   dictTrue.Count == sqliteTrue.Count, ref passed, ref failed);
            Check("AllTrue() content", dictTrue.SequenceEqual(sqliteTrue),  ref passed, ref failed);

            // ── 4. Count ──────────────────────────────────────────────────────

            Check("Count", dictStore.Count == sqliteStore.Count, ref passed, ref failed);

            // ── 5. HasSimilar — spot-check every predicate from the dict ──────

            int similarMismatch = 0;
            foreach (var p in dictStore.All())
            {
                bool inSqlite = sqliteStore.HasSimilar(p);
                if (!inSqlite) similarMismatch++;
            }
            Check($"HasSimilar (all {dictStore.Count} predicates found in Sqlite)", similarMismatch == 0, ref passed, ref failed);

            // ── 6. HasFormattedDuplicate — spot-check ─────────────────────────

            int fmtMismatch = 0;
            foreach (var p in dictStore.All())
            {
                string fmt = BlackboardExtensions.FormatPredicate(p);
                bool inSqlite = sqliteStore.HasFormattedDuplicate(fmt);
                if (!inSqlite) fmtMismatch++;
            }
            Check($"HasFormattedDuplicate (all {dictStore.Count} predicates)", fmtMismatch == 0, ref passed, ref failed);

            // ── 7. ContainsKey — spot-check ───────────────────────────────────

            int keyMismatch = 0;
            foreach (var p in dictStore.All())
            {
                var key = p.PredicateName;
                if (dictStore.ContainsKey(key) != sqliteStore.ContainsKey(key))
                    keyMismatch++;
            }
            Check($"ContainsKey (all {dictStore.Count} keys)", keyMismatch == 0, ref passed, ref failed);

            // ── 8. CleanupAtAgentPredicates ───────────────────────────────────

            // Find robot names from agents registered on the blackboard
            var agents = dictBb.GetAllAgents();
            if (agents.Any())
            {
                string robotName = agents[0].NameKey.ToString();

                int beforeDict   = dictStore.Count;
                int beforeSqlite = sqliteStore.Count;
                dictStore.CleanupAtAgentPredicates(robotName);
                sqliteStore.CleanupAtAgentPredicates(robotName);
                int removedDict   = beforeDict   - dictStore.Count;
                int removedSqlite = beforeSqlite - sqliteStore.Count;

                Check($"CleanupAtAgentPredicates('{robotName}') same removal count ({removedDict})",
                    removedDict == removedSqlite, ref passed, ref failed);
            }
            else
            {
                LoggingService.LogWarning("[ParityTest] No agents found — skipping CleanupAtAgentPredicates check");
            }

            // ── Summary ───────────────────────────────────────────────────────

            LoggingService.LogSection($"RESULT: {passed} passed, {failed} failed");
            if (failed == 0)
                LoggingService.LogSuccess("All parity checks passed — SqlitePredicateStore is a drop-in replacement.");
            else
                LoggingService.LogError($"{failed} parity check(s) failed — see warnings above.");
        }

        private static void Check(string label, bool condition, ref int passed, ref int failed)
        {
            if (condition)
            {
                LoggingService.LogSuccess($"  ✓ {label}");
                passed++;
            }
            else
            {
                LoggingService.LogError($"  ✗ {label}");
                failed++;
            }
        }
    }
}
