using Xunit;

namespace BehaviorTreeMainProject.Tests;

/// <summary>
/// Test each TemporalType, checked against
/// NodeGraph.IsTemporalConstraintSatisfied's actual coded semantics.
///
/// Finding: STARTS, FINISHES and EQUALS all compile to the exact same
/// expression - `from.IsExecuting || from.IsCompleted` (NodeGraph.cs
/// ~lines 390-416) - so despite being distinct Allen's-interval-algebra
/// relations in the DSL grammar, they are behaviorally IDENTICAL at
/// schedule time: predecessor started is both necessary and sufficient for
/// all three. Only MEETS (requires predecessor fully completed, successor
/// not yet started/completed), PRECEDES (requires completed + successor
/// not executing) and CONTAINS (requires predecessor still executing, not
/// yet completed) have distinguishable behavior. Documented via the
/// three parameterized-by-hand tests below rather than fixed - collapsing
/// three DSL-level relations onto one scheduling behavior may be
/// intentional simplification, not obviously a bug, but worth a second
/// pair of eyes before publishing since the DSL grammar promises more
/// than the scheduler delivers.
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
