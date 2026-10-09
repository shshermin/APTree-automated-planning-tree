using Xunit;

namespace BehaviorTreeMainProject.Tests;

// Checks the test project references the main project and runs under `dotnet test`.
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
