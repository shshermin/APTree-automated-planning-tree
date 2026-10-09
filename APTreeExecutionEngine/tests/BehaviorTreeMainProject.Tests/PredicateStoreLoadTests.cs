using System.Diagnostics;
using System.Linq;
using ModelLoader.ParameterTypes;
using ModelLoader.PredicateTypes;
using Xunit;

namespace BehaviorTreeMainProject.Tests;

/// <summary>
/// Load tests for the predicate stores at realistic blackboard sizes. Opt-in
/// because they assert on wall-clock time; the bounds are deliberately loose
/// and only catch order-of-magnitude regressions.
/// </summary>
public class PredicateStoreLoadTests
{
    private static Predicate[] BuildDistinctPredicates(int count)
    {
        return Enumerable.Range(0, count)
            .Select(i =>
            {
                var loc = new FirstPos { NameKey = new FastName($"fp{i}") };
                var beam = new Beam { NameKey = new FastName($"beam{i}"), Loc = loc };
                return (Predicate)new AtPlace(beam, loc, false);
            })
            .ToArray();
    }

    [SlowFact("populates 8000 predicates and times HasSimilar under load")]
    public void DictionaryPredicateStore_HandlesEightThousandPredicates_WithoutCatastrophicSlowdown()
    {
        using var store = new DictionaryPredicateStore();
        AssertHandlesLoad(store, count: 8000, maxSeconds: 15);
    }

    [SlowFact("populates 8000 predicates and times HasSimilar under load")]
    public void SqlitePredicateStore_HandlesEightThousandPredicates_WithoutCatastrophicSlowdown()
    {
        using var store = new SqlitePredicateStore(":memory:");
        AssertHandlesLoad(store, count: 8000, maxSeconds: 15);
    }

    private static void AssertHandlesLoad(IPredicateStore store, int count, int maxSeconds)
    {
        var predicates = BuildDistinctPredicates(count);

        var insertWatch = Stopwatch.StartNew();
        foreach (var p in predicates)
            store.Upsert(p.PredicateName, p);
        insertWatch.Stop();

        Assert.Equal(count, store.Count);
        Assert.True(insertWatch.Elapsed.TotalSeconds < maxSeconds,
            $"Inserting {count} predicates into {store.StoreType} took {insertWatch.Elapsed.TotalSeconds:F2}s (budget {maxSeconds}s)");

        // Misses are the worst case: a linear scan has to visit every entry.
        var missingPredicates = Enumerable.Range(count, 300)
            .Select(i =>
            {
                var loc = new FirstPos { NameKey = new FastName($"fp{i}") };
                var beam = new Beam { NameKey = new FastName($"beam{i}"), Loc = loc };
                return (Predicate)new AtPlace(beam, loc, false);
            })
            .ToArray();

        var scanWatch = Stopwatch.StartNew();
        foreach (var p in missingPredicates)
            Assert.False(store.HasSimilar(p));
        scanWatch.Stop();

        Assert.True(scanWatch.Elapsed.TotalSeconds < maxSeconds,
            $"{missingPredicates.Length} HasSimilar misses against {count} predicates in {store.StoreType} " +
            $"took {scanWatch.Elapsed.TotalSeconds:F2}s (budget {maxSeconds}s)");
    }

    /// <summary>
    /// SqlitePredicateStore's indexed HasSimilar should scale better than
    /// DictionaryPredicateStore's linear scan; margins are generous for noisy CI.
    /// </summary>
    [SlowFact("compares HasSimilar scaling between predicate counts")]
    public void DictionaryPredicateStore_HasSimilarCost_GrowsWithStoreSize_UnlikeSqlite()
    {
        double smallAvgMs = AverageHasSimilarMissMs(new DictionaryPredicateStore(), smallCount: 200);
        double largeAvgMs = AverageHasSimilarMissMs(new DictionaryPredicateStore(), smallCount: 8000);

        double sqliteSmallAvgMs = AverageHasSimilarMissMs(new SqlitePredicateStore(":memory:"), smallCount: 200);
        double sqliteLargeAvgMs = AverageHasSimilarMissMs(new SqlitePredicateStore(":memory:"), smallCount: 8000);

        // Guard against a near-zero denominator making the ratio meaningless.
        double dictionaryRatio = largeAvgMs / System.Math.Max(smallAvgMs, 0.001);
        double sqliteRatio = sqliteLargeAvgMs / System.Math.Max(sqliteSmallAvgMs, 0.001);

        Assert.True(dictionaryRatio > 3.0,
            $"Expected DictionaryPredicateStore.HasSimilar to visibly slow down at 40x the predicate count " +
            $"(O(n) scan) - got {smallAvgMs:F4}ms -> {largeAvgMs:F4}ms (ratio {dictionaryRatio:F1}x).");

        Assert.True(sqliteRatio < dictionaryRatio,
            $"Expected SqlitePredicateStore's indexed HasSimilar to scale better than DictionaryPredicateStore's " +
            $"O(n) scan - got Sqlite ratio {sqliteRatio:F1}x vs Dictionary ratio {dictionaryRatio:F1}x.");
    }

    private static double AverageHasSimilarMissMs(IPredicateStore store, int smallCount)
    {
        using (store)
        {
            var predicates = BuildDistinctPredicates(smallCount);
            foreach (var p in predicates)
                store.Upsert(p.PredicateName, p);

            var missingPredicates = Enumerable.Range(smallCount, 100)
                .Select(i =>
                {
                    var loc = new FirstPos { NameKey = new FastName($"fp{i}") };
                    var beam = new Beam { NameKey = new FastName($"beam{i}"), Loc = loc };
                    return (Predicate)new AtPlace(beam, loc, false);
                })
                .ToArray();

            var watch = Stopwatch.StartNew();
            foreach (var p in missingPredicates)
                store.HasSimilar(p);
            watch.Stop();

            return watch.Elapsed.TotalMilliseconds / missingPredicates.Length;
        }
    }
}
