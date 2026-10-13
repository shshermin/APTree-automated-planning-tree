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

    private static void RegisterPlanEntities(Blackboard<FastName> blackboard)
    {
        var p1 = new FirstPos { NameKey = new FastName("p1") };
        var e1 = new Beam { NameKey = new FastName("e1"), Loc = p1 };
        var r1 = new Robot { NameKey = new FastName("r1") };
        var g1 = new Gripper { NameKey = new FastName("g1") };
        blackboard.SetLocation(p1.NameKey, p1);
        blackboard.SetElement(e1.NameKey, e1);
        blackboard.SetAgent(r1.NameKey, r1);
        blackboard.SetTool(g1.NameKey, g1);
    }

    /// <summary>
    /// Regression: a missing owning flow node used to throw a NullReferenceException
    /// that the catch-all turned into a misleading planning failure.
    /// </summary>
    [Fact]
    public void MissingOwningFlowNode_StillCompletesThePlan()
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
        RegisterPlanEntities(blackboard);
        // Deliberately NOT calling service.SetOwningFlowNode(...).

        service.OnEvaluate(0.1f);

        Assert.True(service.HasCompleted);
        Assert.True(service.WasSuccessful, service.LastError);
        Assert.Single(service.GeneratedNodeGraph.GetAllActionNodes());
    }

    [Fact]
    public void PlanReferencingUnregisteredEntities_FailsInsteadOfDroppingActions()
    {
        var (service, blackboard, _) = NewService(new PlanningResult
        {
            Success = true,
            Plan = "0.0: (pickuphl e1 p1 r1 g1)\n1.0: (placehl e1 p2 r1)",
            PlannerUsed = "Enhsp"
        });
        RegisterPlanEntities(blackboard); // p2 is missing, so PlaceHL can't be created

        service.OnEvaluate(0.1f);

        Assert.True(service.HasCompleted);
        Assert.False(service.WasSuccessful);
        Assert.False(service.HasPlanGenerated);
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

        RegisterPlanEntities(blackboard);

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
    /// Known issue: output without recognizable actions yields an empty NodeGraph
    /// reported as success. It can't simply be failed: the service returns raw
    /// planner stdout, so a legitimate zero-step plan looks the same.
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
