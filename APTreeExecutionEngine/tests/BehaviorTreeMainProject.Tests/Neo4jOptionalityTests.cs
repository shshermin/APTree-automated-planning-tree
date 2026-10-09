using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace BehaviorTreeMainProject.Tests;

/// <summary>
/// Neo4j is an optional mirror - the engine must work without one, 
/// and an unreachable one must not throw or hang forever.
/// No Neo4j instance is needed or used: everything here runs with none.
/// </summary>
public class Neo4jOptionalityTests
{
    private readonly ITestOutputHelper _output;
    public Neo4jOptionalityTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public void BlackboardWithoutNeo4j_StoresPredicatesNormally()
    {
        using var blackboard = new Blackboard<FastName>(new DictionaryPredicateStore());
        var predicate = PredicateStoreTestFixtures.AtPlace();

        blackboard.SetPredicateSync(predicate.PredicateName, predicate);

        Assert.Equal("Dictionary", blackboard.PredicateStoreType);
        Assert.Contains(blackboard.GetAllPredicates(), p => p.PredicateName.Equals(predicate.PredicateName));
    }

    [Fact]
    public void ParameterlessBlackboard_DoesNotRequireNeo4j()
    {
        using var blackboard = new Blackboard<FastName>();
        var predicate = PredicateStoreTestFixtures.Holding();

        blackboard.SetPredicateSync(predicate.PredicateName, predicate);

        Assert.Single(blackboard.GetAllPredicates());
    }

    /// <summary>
    /// Returns false, but only after the driver's ~30s retry window, which
    /// EnvironmentGraph doesn't expose a way to shorten. Opt-in because of the runtime.
    /// </summary>
    [SlowFact("~31s: Neo4j driver retries a refused connection for 30s")]
    public async Task EnvironmentGraph_TestConnection_ReturnsFalse_WhenNothingIsListening()
    {
        // Port 1 on loopback: connection refused immediately, no Neo4j involved.
        using var graph = new EnvironmentGraph("bolt://127.0.0.1:1", "neo4j", "irrelevant");

        var sw = Stopwatch.StartNew();
        var connected = await graph.TestConnection();
        sw.Stop();
        _output.WriteLine($"TestConnection against a closed port took {sw.Elapsed.TotalSeconds:F1}s");

        Assert.False(connected);
    }
}
