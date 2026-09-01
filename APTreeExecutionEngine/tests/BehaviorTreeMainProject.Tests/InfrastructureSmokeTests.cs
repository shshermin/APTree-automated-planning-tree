using Xunit;

namespace BehaviorTreeMainProject.Tests;

// Phase 0 smoke tests: prove the xUnit project is wired up correctly
// (references the main project, resolves its types, runs under `dotnet test`).
// Real coverage per test-plan section lands in later phases.
public class InfrastructureSmokeTests
{
    [Fact]
    public void DictionaryPredicateStore_StartsEmpty()
    {
        using var store = new DictionaryPredicateStore();

        Assert.Equal(0, store.Count);
        Assert.Equal("Dictionary", store.StoreType);
    }

    [Fact]
    public void FastName_EqualityIsValueBased()
    {
        var a = new FastName("Foo");
        var b = new FastName("Foo");
        var c = new FastName("Bar");

        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
    }
}
