using Xunit;

namespace BehaviorTreeMainProject.Tests;

/// <summary>Test-plan section 3, #34-35.</summary>
public class NodeGraphStructureTests
{
    private static Blackboard<FastName> NewBlackboard() => new(new DictionaryPredicateStore());

    /// <summary>
    /// It doesn't hang or throw, but "graceful" oversells what actually
    /// happens: NodeGraph.TopologicalSort's cycle check ("if
    /// tempVisited.Contains(node) return" - NodeGraph.cs ~line 659) only
    /// aborts the single redundant recursive call that closes the cycle; it
    /// does not stop the enclosing calls from unwinding normally and still
    /// inserting every cyclic node into the result. Net effect, verified
    /// below: a genuinely cyclic graph produces a complete, cycle-free-
    /// LOOKING linearization with no warning, no exception, and no log line
    /// that a cycle existed - the order is just whatever the recursion
    /// happened to settle on when it broke the cycle at an arbitrary point.
    /// A caller has no way to detect that its input was invalid.
    /// AddOrderRelation only guards against a direct A-to-B-and-back
    /// reversal (the immediate reverse relation); it does not detect longer
    /// cycles, so a 3+-node cycle can still be constructed exactly as done
    /// here via AddTemporalConstraint (which has no cycle guard at all).
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

        // a -> b -> c -> a: a 3-cycle, built via AddTemporalConstraint since
        // AddOrderRelation's reverse-relation check would only catch a
        // direct 2-node reversal, not this.
        graph.AddTemporalConstraint(a, b, TemporalType.MEETS);
        graph.AddTemporalConstraint(b, c, TemporalType.MEETS);
        graph.AddTemporalConstraint(c, a, TemporalType.MEETS);

        // No exception, no hang (the test completing at all proves that).
        var order = graph.GetExecutionOrder();

        // All three cyclic nodes are silently included anyway - there is no
        // signal anywhere that the input graph was actually invalid.
        Assert.Contains(a, order);
        Assert.Contains(b, order);
        Assert.Contains(c, order);
        Assert.Contains(outsider, order);
        Assert.Equal(4, order.Count);
    }

    /// <summary>
    /// #35: a node with no predecessors or successors is still part of the
    /// graph and immediately executable - it isn't dropped or blocked just
    /// because it has no relations.
    /// </summary>
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
