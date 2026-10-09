using System.Collections.Generic;
using System.Linq;
using AIPlanning;
using BehaviorTreeMainProject.Services.AIPlanning;
using Xunit;

namespace BehaviorTreeMainProject.Tests;

/// <summary>
/// Covers the per-mode graph builders, which the real planning path never
/// calls: relations come from the plan string, so ExecutionMode currently has
/// no effect (see ServicePDDLPlanning.CreateNodeGraphWithExecutionMode).
/// </summary>
public class PDDLPlanningExecutionModeTests
{
    private static ServicePDDLPlanning NewService()
    {
        var blackboard = new Blackboard<FastName>(new DictionaryPredicateStore());
        var tree = new BehaviorTree();
        tree.Initialise(blackboard, "Test");
        var request = new PDDLPlanningRequest("domain.pddl", "problem.pddl", "enhsp.jar", "Enhsp");
        return new ServicePDDLPlanning(tree, request);
    }

    private static List<PActionNode> NewActions(int count, Blackboard<FastName> blackboard) =>
        Enumerable.Range(0, count).Select(i => (PActionNode)new TestAction($"a{i}", blackboard)).ToList();

    [Fact]
    public void Sequential_ChainsEveryActionWithMeets()
    {
        var service = NewService();
        var blackboard = new Blackboard<FastName>(new DictionaryPredicateStore());
        var actions = NewActions(3, blackboard);

        service.ExecutionMode = ServicePDDLPlanning.ParallelExecutionMode.Sequential;
        var graph = service.CreateNodeGraphWithExecutionMode(actions);

        AssertRelation(graph, actions[0], actions[1], TemporalType.MEETS);
        AssertRelation(graph, actions[1], actions[2], TemporalType.MEETS);
    }

    [Fact]
    public void Parallel_FirstActionOverlapsEveryOtherAction()
    {
        var service = NewService();
        var blackboard = new Blackboard<FastName>(new DictionaryPredicateStore());
        var actions = NewActions(3, blackboard);

        service.ExecutionMode = ServicePDDLPlanning.ParallelExecutionMode.Parallel;
        var graph = service.CreateNodeGraphWithExecutionMode(actions);

        AssertRelation(graph, actions[0], actions[1], TemporalType.OVERLAPS);
        AssertRelation(graph, actions[0], actions[2], TemporalType.OVERLAPS);
    }

    [Fact]
    public void Hybrid_WithFourActions_MixesSequentialAndParallelRelations()
    {
        var service = NewService();
        var blackboard = new Blackboard<FastName>(new DictionaryPredicateStore());
        var actions = NewActions(4, blackboard);

        service.ExecutionMode = ServicePDDLPlanning.ParallelExecutionMode.Hybrid;
        var graph = service.CreateNodeGraphWithExecutionMode(actions);

        // action0 -> action1: sequential (MEETS)
        AssertRelation(graph, actions[0], actions[1], TemporalType.MEETS);
        // action1 -> action2, action1 -> action3: parallel (OVERLAPS)
        AssertRelation(graph, actions[1], actions[2], TemporalType.OVERLAPS);
        AssertRelation(graph, actions[1], actions[3], TemporalType.OVERLAPS);
    }

    [Fact]
    public void Hybrid_WithTwoOrFewerActions_FallsBackToParallel()
    {
        var service = NewService();
        var blackboard = new Blackboard<FastName>(new DictionaryPredicateStore());
        var actions = NewActions(2, blackboard);

        service.ExecutionMode = ServicePDDLPlanning.ParallelExecutionMode.Hybrid;
        var graph = service.CreateNodeGraphWithExecutionMode(actions);

        AssertRelation(graph, actions[0], actions[1], TemporalType.OVERLAPS);
    }

    private static void AssertRelation(NodeGraph graph, PActionNode from, PActionNode to, TemporalType expected)
    {
        var fromNode = graph.GetNodeInfo(from);
        Assert.NotNull(fromNode);
        var relation = fromNode.Successors.FirstOrDefault(r => r.To.ActionNode == to);
        Assert.NotNull(relation);
        Assert.Equal(expected, relation.tempType);
    }
}
