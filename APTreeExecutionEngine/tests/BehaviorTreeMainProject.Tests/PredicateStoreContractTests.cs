using ModelLoader.PredicateTypes;
using Xunit;

namespace BehaviorTreeMainProject.Tests;

/// <summary>
/// The same behavioral contract, run against
/// every IPredicateStore implementation via the concrete subclasses below
/// (DictionaryPredicateStoreTests, SqlitePredicateStoreTests). Parity by
/// construction - a divergence between implementations shows up as one
/// subclass failing a test the other passes.
/// </summary>
public abstract class PredicateStoreContractTests
{
    protected abstract IPredicateStore CreateStore();

    [Fact]
    public void Upsert_Then_TryGet_ReturnsTheSamePredicate()
    {
        using var store = CreateStore();
        var predicate = PredicateStoreTestFixtures.AtPlace();

        store.Upsert(predicate.PredicateName, predicate);

        Assert.True(store.TryGet(predicate.PredicateName, out var found));
        Assert.Same(predicate, found);
    }

    [Fact]
    public void TryGet_ReturnsFalse_ForMissingKey()
    {
        using var store = CreateStore();

        Assert.False(store.TryGet(new FastName("does-not-exist"), out var found));
        Assert.Null(found);
    }

    [Fact]
    public void ContainsKey_ReflectsWhatWasUpserted()
    {
        using var store = CreateStore();
        var predicate = PredicateStoreTestFixtures.Holding();

        Assert.False(store.ContainsKey(predicate.PredicateName));
        store.Upsert(predicate.PredicateName, predicate);
        Assert.True(store.ContainsKey(predicate.PredicateName));
    }

    [Fact]
    public void RemoveKey_DeletesEntry_AndReturnsTrue()
    {
        using var store = CreateStore();
        var predicate = PredicateStoreTestFixtures.AtPlace();
        store.Upsert(predicate.PredicateName, predicate);

        Assert.True(store.RemoveKey(predicate.PredicateName));
        Assert.False(store.ContainsKey(predicate.PredicateName));
    }

    [Fact]
    public void RemoveKey_ReturnsFalse_WhenKeyDoesNotExist()
    {
        using var store = CreateStore();

        Assert.False(store.RemoveKey(new FastName("does-not-exist")));
    }

    [Fact]
    public void Count_ReflectsNumberOfDistinctUpsertedEntries()
    {
        using var store = CreateStore();
        var atPlace = PredicateStoreTestFixtures.AtPlace();
        var holding = PredicateStoreTestFixtures.Holding();

        store.Upsert(atPlace.PredicateName, atPlace);
        store.Upsert(holding.PredicateName, holding);
        Assert.Equal(2, store.Count);

        // Re-upserting the same key must not increase the count.
        store.Upsert(atPlace.PredicateName, atPlace);
        Assert.Equal(2, store.Count);
    }

    [Fact]
    public void All_ReturnsEveryUpsertedPredicate()
    {
        using var store = CreateStore();
        var atPlace = PredicateStoreTestFixtures.AtPlace();
        var holding = PredicateStoreTestFixtures.Holding();
        store.Upsert(atPlace.PredicateName, atPlace);
        store.Upsert(holding.PredicateName, holding);

        var all = store.All();

        Assert.Equal(2, all.Count);
        Assert.Contains(all, p => p.PredicateName.Equals(atPlace.PredicateName));
        Assert.Contains(all, p => p.PredicateName.Equals(holding.PredicateName));
    }

    [Fact]
    public void AllTrue_ExcludesNegatedPredicates()
    {
        using var store = CreateStore();
        var positive = PredicateStoreTestFixtures.AtPlace(negated: false);
        var negative = PredicateStoreTestFixtures.Holding(negated: true);
        store.Upsert(positive.PredicateName, positive);
        store.Upsert(negative.PredicateName, negative);

        var allTrue = store.AllTrue();

        Assert.Single(allTrue);
        Assert.Equal(positive.PredicateName, allTrue[0].PredicateName);
    }

    [Fact]
    public void UpdateNegation_FlipsTheFlagOnTheStoredPredicate()
    {
        using var store = CreateStore();
        var predicate = PredicateStoreTestFixtures.AtPlace(negated: false);
        store.Upsert(predicate.PredicateName, predicate);

        store.UpdateNegation(predicate.PredicateName, negated: true);

        store.TryGet(predicate.PredicateName, out var found);
        Assert.True(found!.not);
    }

    [Fact]
    public void UpdateNegation_IsANoOp_WhenKeyDoesNotExist()
    {
        using var store = CreateStore();

        var exception = Record.Exception(() => store.UpdateNegation(new FastName("does-not-exist"), true));

        Assert.Null(exception);
    }

    [Fact]
    public void HasSimilar_TrueForSameTypeAndParameters_RegardlessOfKeyOrNegation()
    {
        using var store = CreateStore();
        var stored = PredicateStoreTestFixtures.AtPlace(negated: false);
        store.Upsert(stored.PredicateName, stored);

        // Same type + same params, but a different (negated) instance.
        var candidate = PredicateStoreTestFixtures.AtPlace(negated: true);

        Assert.True(store.HasSimilar(candidate));
    }

    [Fact]
    public void HasSimilar_FalseForDifferentParameters()
    {
        using var store = CreateStore();
        var stored = new AtPlace(
            PredicateStoreTestFixtures.Beam1, PredicateStoreTestFixtures.Fp1, false);
        store.Upsert(stored.PredicateName, stored);

        var different = new AtPlace(
            PredicateStoreTestFixtures.Beam2, PredicateStoreTestFixtures.Fp2, false);

        Assert.False(store.HasSimilar(different));
    }

    [Fact]
    public void HasFormattedDuplicate_TrueOnlyForAMatchingFormattedString()
    {
        using var store = CreateStore();
        var predicate = PredicateStoreTestFixtures.AtPlace();
        store.Upsert(predicate.PredicateName, predicate);
        string formatted = BlackboardExtensions.FormatPredicate(predicate);

        Assert.True(store.HasFormattedDuplicate(formatted));
        Assert.False(store.HasFormattedDuplicate("not-a-real-formatted-predicate"));
    }

    /// <summary>
    /// Per the IPredicateStore.CleanupAtAgentPredicates doc comment: "Remove
    /// all 'atAgent' predicates whose first parameter MATCHES robotName" -
    /// an exact match, not a substring. Robot names "r1" and "r10" are
    /// deliberately chosen: "r1" is a string-prefix of "r10", so a
    /// substring-based implementation over-deletes.
    ///
    /// As of this writing DictionaryPredicateStore does exactly that (see
    /// its EXCLUDED-free, unguarded ks.Contains(robotName) check) while
    /// SqlitePredicateStore does an exact match on param0 - so this test is
    /// expected to FAIL for DictionaryPredicateStoreTests until that's
    /// fixed. Left failing rather than routed around, since it is real
    /// production-code behavior, not a test-fixture/grammar issue.
    /// </summary>
    [Fact]
    public void CleanupAtAgentPredicates_MatchesTheRobotNameExactly_NotAsASubstring()
    {
        using var store = CreateStore();
        var atAgentR1 = PredicateStoreTestFixtures.AtAgentOf(PredicateStoreTestFixtures.R1, PredicateStoreTestFixtures.Fp1);
        var atAgentR10 = PredicateStoreTestFixtures.AtAgentOf(PredicateStoreTestFixtures.R10, PredicateStoreTestFixtures.Fp2);
        store.Upsert(atAgentR1.PredicateName, atAgentR1);
        store.Upsert(atAgentR10.PredicateName, atAgentR10);

        store.CleanupAtAgentPredicates("r1");

        Assert.False(store.ContainsKey(atAgentR1.PredicateName), "the exact-match robot's predicate should be removed");
        Assert.True(store.ContainsKey(atAgentR10.PredicateName), "a robot whose name merely starts with 'r1' must not be removed");
    }

    [Fact]
    public void CleanupAtAgentPredicates_LeavesOtherPredicateTypesForTheSameRobotAlone()
    {
        using var store = CreateStore();
        var atAgent = PredicateStoreTestFixtures.AtAgentOf(PredicateStoreTestFixtures.R1, PredicateStoreTestFixtures.Fp1);
        var holding = PredicateStoreTestFixtures.Holding(); // also references r1, but is a different predicate type
        store.Upsert(atAgent.PredicateName, atAgent);
        store.Upsert(holding.PredicateName, holding);

        store.CleanupAtAgentPredicates("r1");

        Assert.False(store.ContainsKey(atAgent.PredicateName));
        Assert.True(store.ContainsKey(holding.PredicateName), "cleanup should only touch atAgent predicates");
    }
}
