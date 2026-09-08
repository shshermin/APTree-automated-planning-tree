using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ModelLoader.ParameterTypes;
using ModelLoader.PredicateTypes;
using Xunit;

namespace BehaviorTreeMainProject.Tests;

/// <summary>
/// Parallel writers to the same store must not
/// corrupt state. Neither DictionaryPredicateStore (a plain, unguarded
/// Dictionary&lt;FastName, Predicate&gt;) nor SqlitePredicateStore (a plain
/// Dictionary hot-index plus one shared, unsynchronized SqliteConnection)
/// take any lock anywhere in Blackboard/*.cs (grep confirms the only lock
/// usage in that folder is in Singleton.cs / NameManager.cs, unrelated to
/// predicate storage) - so this is expected to demonstrate real corruption
/// or exceptions, not just assert a foregone conclusion.
///
/// NOTE: this is a genuine data race, so it is inherently flaky - it does
/// not fail on every run (a race that always loses would arguably be a
/// worse race). Observed losing 1-4 of 200 concurrent writes on
/// DictionaryPredicateStore across repeated runs on this machine. A run
/// that passes does not mean the bug is gone; a run that fails proves it
/// is real. Do not "fix" this test by adding retries or increasing
/// tolerance - that would hide the underlying missing-synchronization bug.
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
