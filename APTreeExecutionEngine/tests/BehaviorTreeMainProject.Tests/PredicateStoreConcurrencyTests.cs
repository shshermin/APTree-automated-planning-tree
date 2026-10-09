using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ModelLoader.ParameterTypes;
using ModelLoader.PredicateTypes;
using Xunit;

namespace BehaviorTreeMainProject.Tests;

/// <summary>
/// Concurrent writes must not be lost. A race shows up only intermittently,
/// so treat any failure here as real - don't add retries or tolerance.
/// </summary>
public class PredicateStoreConcurrencyTests
{
    private const int WriterCount = 200;

    [Fact]
    public async Task DictionaryPredicateStore_ParallelDistinctWrites_DoNotCorruptState()
    {
        using var store = new DictionaryPredicateStore();
        await AssertParallelWritesSucceed(store);
    }

    [Fact]
    public async Task SqlitePredicateStore_ParallelDistinctWrites_DoNotCorruptState()
    {
        using var store = new SqlitePredicateStore(":memory:");
        await AssertParallelWritesSucceed(store);
    }

    private static async Task AssertParallelWritesSucceed(IPredicateStore store)
    {
        var predicates = Enumerable.Range(0, WriterCount)
            .Select(i =>
            {
                var loc = new FirstPos { NameKey = new FastName($"fp{i}") };
                var beam = new Beam { NameKey = new FastName($"beam{i}"), Loc = loc };
                return (Predicate)new AtPlace(beam, loc, false);
            })
            .ToList();

        var exceptions = new List<System.Exception>();

        await Task.WhenAll(predicates.Select(p => Task.Run(() =>
        {
            try
            {
                store.Upsert(p.PredicateName, p);
            }
            catch (System.Exception ex)
            {
                lock (exceptions) exceptions.Add(ex);
            }
        })));

        Assert.Empty(exceptions);
        Assert.Equal(WriterCount, store.Count);
        foreach (var p in predicates)
            Assert.True(store.ContainsKey(p.PredicateName), $"missing key after concurrent writes: {p.PredicateName}");
    }
}
