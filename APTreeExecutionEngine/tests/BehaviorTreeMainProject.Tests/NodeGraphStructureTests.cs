using Xunit;

namespace BehaviorTreeMainProject.Tests;

public class NodeGraphStructureTests
{
    private static Blackboard<FastName> NewBlackboard() => new(new DictionaryPredicateStore());

    /// <summary>
    /// Known issue: TopologicalSort silently linearizes cyclic graphs - no
    /// exception or warning. AddOrderRelation only rejects direct 2-node
    /// reversals, and AddTemporalConstraint has no cycle check.
    /// </summary>
    [Fact]
    public void GetExecutionOrder_OnACycle_ProducesACompleteOrderingWithNoErrorOrWarning()
    {
        var blackboard = NewBlackboard();
        var graph = new NodeGraph();
        var a = new TestAction("a", blackboard);
        var b = new TestAction("b", blackboard);
        var c = new TestAction("c", blackboard);
        var outsider = new TestAction("outsider", blackboard);
        graph.AddNode(a);
        graph.AddNode(b);
        graph.AddNode(c);
        graph.AddNode(outsider);

        // a -> b -> c -> a
        graph.AddTemporalConstraint(a, b, TemporalType.MEETS);
        graph.AddTemporalConstraint(b, c, TemporalType.MEETS);
        graph.AddTemporalConstraint(c, a, TemporalType.MEETS);

        var order = graph.GetExecutionOrder();

        Assert.Contains(a, order);
        Assert.Contains(b, order);
        Assert.Contains(c, order);
        Assert.Contains(outsider, order);
        Assert.Equal(4, order.Count);
    }

    /// <summary>A node without relations is still part of the graph and immediately executable.</summary>
    [Fact]
    public void DisconnectedNode_IsIncludedInExecutionOrder_AndImmediatelyExecutable()
    {
        var blackboard = NewBlackboard();
        var graph = new NodeGraph();
        var connectedA = new TestAction("connectedA", blackboard);
        var connectedB = new TestAction("connectedB", blackboard);
        var disconnected = new TestAction("disconnected", blackboard);
        graph.AddNode(connectedA);
        graph.AddNode(connectedB);
        graph.AddNode(disconnected);
        graph.AddTemporalConstraint(connectedA, connectedB, TemporalType.MEETS);

        Assert.Contains(disconnected, graph.GetExecutionOrder());
        Assert.Contains(disconnected, graph.GetExecutableNodesInternal());
    }
}
