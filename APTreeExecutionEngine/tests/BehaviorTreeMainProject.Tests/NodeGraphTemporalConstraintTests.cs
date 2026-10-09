using Xunit;

namespace BehaviorTreeMainProject.Tests;

/// <summary>
/// Checks each TemporalType against NodeGraph.IsTemporalConstraintSatisfied.
/// Note: STARTS, FINISHES and EQUALS all evaluate to
/// `from.IsExecuting || from.IsCompleted`, so the scheduler treats these three
/// DSL relations identically.
/// </summary>
public class NodeGraphTemporalConstraintTests
{
    private static Blackboard<FastName> NewBlackboard() => new(new DictionaryPredicateStore());

    private static (NodeGraph graph, TestAction from, TestAction to) BuildTwoNodeGraph(TemporalType constraint)
    {
        var blackboard = NewBlackboard();
        var graph = new NodeGraph();
        var from = new TestAction("from", blackboard);
        var to = new TestAction("to", blackboard);
        graph.AddNode(from);
        graph.AddNode(to);
        graph.AddTemporalConstraint(from, to, constraint);
        return (graph, from, to);
    }

    [Fact]
    public void Meets_SuccessorCannotStart_UntilPredecessorCompletes()
    {
        var (graph, from, to) = BuildTwoNodeGraph(TemporalType.MEETS);

        Assert.DoesNotContain(to, graph.GetExecutableNodesInternal());

        graph.MarkNodeStarted(from);
        Assert.DoesNotContain(to, graph.GetExecutableNodesInternal());

        graph.MarkNodeCompleted(from);
        Assert.Contains(to, graph.GetExecutableNodesInternal());
    }

    [Fact]
    public void Precedes_SuccessorCannotStart_UntilPredecessorCompletes()
    {
        var (graph, from, to) = BuildTwoNodeGraph(TemporalType.PRECEDES);

        graph.MarkNodeStarted(from);
        Assert.DoesNotContain(to, graph.GetExecutableNodesInternal());

        graph.MarkNodeCompleted(from);
        Assert.Contains(to, graph.GetExecutableNodesInternal());
    }

    [Fact]
    public void Overlaps_SuccessorCanStart_WhilePredecessorStillExecuting()
    {
        var (graph, from, to) = BuildTwoNodeGraph(TemporalType.OVERLAPS);

        Assert.DoesNotContain(to, graph.GetExecutableNodesInternal());

        graph.MarkNodeStarted(from);
        Assert.Contains(to, graph.GetExecutableNodesInternal());
    }

    [Fact]
    public void Contains_SuccessorCanStart_OnlyWhilePredecessorIsMidExecution()
    {
        var (graph, from, to) = BuildTwoNodeGraph(TemporalType.CONTAINS);

        Assert.DoesNotContain(to, graph.GetExecutableNodesInternal());

        graph.MarkNodeStarted(from);
        Assert.Contains(to, graph.GetExecutableNodesInternal());

        // Unlike OVERLAPS, CONTAINS drops the successor's eligibility once
        // the predecessor completes (from.IsExecuting becomes false).
        graph.MarkNodeCompleted(from);
        Assert.DoesNotContain(to, graph.GetExecutableNodesInternal());
    }

    [Theory]
    [InlineData(TemporalType.STARTS)]
    [InlineData(TemporalType.FINISHES)]
    [InlineData(TemporalType.EQUALS)]
    public void StartsFinishesEquals_AreAllSatisfiedAsSoonAsPredecessorStarts(TemporalType constraint)
    {
        var (graph, from, to) = BuildTwoNodeGraph(constraint);

        Assert.DoesNotContain(to, graph.GetExecutableNodesInternal());

        graph.MarkNodeStarted(from);
        Assert.Contains(to, graph.GetExecutableNodesInternal());
    }
}
