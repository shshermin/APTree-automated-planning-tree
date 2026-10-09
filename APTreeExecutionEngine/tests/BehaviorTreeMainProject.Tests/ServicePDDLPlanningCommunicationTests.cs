using AIPlanning;
using BehaviorTreeMainProject.Services.AIPlanning;
using ModelLoader.ParameterTypes;
using Xunit;

namespace BehaviorTreeMainProject.Tests;

/// <summary>
/// Uses ServicePDDLPlanning's internal constructor to inject a fake
/// IPlannerCommunicator; the public one hardcodes a REST client for localhost:5000.
/// </summary>
public class ServicePDDLPlanningCommunicationTests
{
    private static (ServicePDDLPlanning service, Blackboard<FastName> blackboard, FakePlannerCommunicator comm) NewService(PlanningResult result)
    {
        var blackboard = new Blackboard<FastName>(new DictionaryPredicateStore());
        var tree = new BehaviorTree();
        tree.Initialise(blackboard, "Test");
        var request = new PDDLPlanningRequest("domain.pddl", "problem.pddl", "enhsp.jar", "Enhsp");
        var comm = new FakePlannerCommunicator(result);
        var service = new ServicePDDLPlanning(tree, request, comm);

        var flowNode = new DynamicFlowNode(new FastName("Main"), tree, SuccessCriteria.ALL, 1.0f, false);
        service.SetOwningFlowNode(flowNode);

        return (service, blackboard, comm);
    }

    /// <summary>
    /// Known issue: without an owning flow node, OnEvaluate dereferences
    /// OwningFlowNode.ParentNode unguarded. The surrounding catch turns the
    /// NullReferenceException into a planning failure with an unhelpful message.
    /// </summary>
    [Fact]
    public void MissingOwningFlowNode_SilentlyFailsWithAnUnhelpfulNullReferenceMessage()
    {
        var blackboard = new Blackboard<FastName>(new DictionaryPredicateStore());
        var tree = new BehaviorTree();
        tree.Initialise(blackboard, "Test");
        var request = new PDDLPlanningRequest("domain.pddl", "problem.pddl", "enhsp.jar", "Enhsp");
        var comm = new FakePlannerCommunicator(new PlanningResult
        {
            Success = true,
            Plan = "0.0: (pickuphl e1 p1 r1 g1)",
            PlannerUsed = "Enhsp"
        });
        var service = new ServicePDDLPlanning(tree, request, comm);
        // Deliberately NOT calling service.SetOwningFlowNode(...).

        var exception = Record.Exception(() => service.OnEvaluate(0.1f));

        Assert.Null(exception); // caught internally, not rethrown
        Assert.True(service.HasCompleted);
        Assert.False(service.WasSuccessful);
        Assert.Contains("Object reference not set", service.LastError);
    }

    [Fact]
    public void SuccessfulPlan_ProducesACompletedGeneratedNodeGraph()
    {
        var (service, blackboard, _) = NewService(new PlanningResult
        {
            Success = true,
            Plan = "0.0: (pickuphl e1 p1 r1 g1)",
            PlannerUsed = "Enhsp"
        });

        // Action creation resolves e1/p1/r1/g1 against the blackboard and silently
        // skips actions whose entities aren't registered.
        var p1 = new FirstPos { NameKey = new FastName("p1") };
        var e1 = new Beam { NameKey = new FastName("e1"), Loc = p1 };
        var r1 = new Robot { NameKey = new FastName("r1") };
        var g1 = new Gripper { NameKey = new FastName("g1") };
        blackboard.SetLocation(p1.NameKey, p1);
        blackboard.SetElement(e1.NameKey, e1);
        blackboard.SetAgent(r1.NameKey, r1);
        blackboard.SetTool(g1.NameKey, g1);

        service.OnEvaluate(0.1f);

        Assert.True(service.HasCompleted);
        Assert.True(service.WasSuccessful, service.LastError);
        Assert.True(service.HasPlanGenerated);
        Assert.NotNull(service.GeneratedNodeGraph);
        Assert.Equal(1, service.GeneratedNodeGraph.GetAllActionNodes().Count);
    }

    [Fact]
    public void CommunicationTimeout_RetriesRatherThanFailingPermanently()
    {
        var (service, _, comm) = NewService(new PlanningResult
        {
            Success = false,
            Error = "Planning request timed out: the operation was canceled"
        });

        service.OnEvaluate(0.1f);

        // Timeouts count as communication errors and are retried on the next tick.
        Assert.False(service.HasCompleted);
        Assert.False(service.WasSuccessful);
        Assert.Equal(1, comm.CallCount);
    }

    [Fact]
    public void CommunicationTimeout_BecomesPermanentFailure_AfterThreeRetries()
    {
        var (service, _, comm) = NewService(new PlanningResult
        {
            Success = false,
            Error = "Planning request timed out: the operation was canceled"
        });

        // MAX_COMMUNICATION_RETRIES is 3, so the 4th failure is permanent.
        for (int i = 0; i < 4; i++)
            service.OnEvaluate(0.1f);

        Assert.True(service.HasCompleted);
        Assert.False(service.WasSuccessful);
        Assert.Equal(4, comm.CallCount);
    }

    [Fact]
    public void PlannerRejection_FailsPermanently_WithoutRetrying()
    {
        // A planner rejection is not a communication error, so no retry.
        var (service, _, comm) = NewService(new PlanningResult
        {
            Success = false,
            Error = "ENHSP: no plan found for the given problem"
        });

        service.OnEvaluate(0.1f);

        Assert.True(service.HasCompleted);
        Assert.False(service.WasSuccessful);
        Assert.Equal(1, comm.CallCount);
    }

    [Fact]
    public void EmptyPlanResponse_FailsPermanently_WithNodeGraphGenerationError()
    {
        var (service, _, _) = NewService(new PlanningResult
        {
            Success = true,
            Plan = "",
            PlannerUsed = "Enhsp"
        });

        service.OnEvaluate(0.1f);

        Assert.True(service.HasCompleted);
        Assert.False(service.WasSuccessful);
        Assert.False(service.HasPlanGenerated);
        Assert.Equal("Failed to generate NodeGraph from planner result", service.LastError);
    }

    /// <summary>
    /// Known issue: unparseable plan content (and plans referencing unregistered
    /// entities) produce an empty NodeGraph that is reported as a successful plan,
    /// because GenerateNodeGraphFromResult only fails on an empty Plan string.
    /// </summary>
    [Fact]
    public void GarbagePlanContent_IsSilentlyReportedAsSuccessWithAnEmptyNodeGraph()
    {
        var (service, _, _) = NewService(new PlanningResult
        {
            Success = true,
            Plan = "this is not planner output at all, just noise",
            PlannerUsed = "Enhsp"
        });

        service.OnEvaluate(0.1f);

        Assert.True(service.HasCompleted);
        Assert.True(service.WasSuccessful, service.LastError);
        Assert.True(service.HasPlanGenerated);
        Assert.NotNull(service.GeneratedNodeGraph);
        Assert.Empty(service.GeneratedNodeGraph.GetAllActionNodes());
    }
}
