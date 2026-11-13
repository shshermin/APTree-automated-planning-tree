using AIPlanning;
using BehaviorTreeMainProject.Decorators.Replan;
using BehaviorTreeMainProject.Services.AIPlanning;
using ModelLoader.ParameterTypes;
using ModelLoader.PredicateTypes;
using Xunit;

namespace BehaviorTreeMainProject.Tests;

/// <summary>Stands in for BehaviorTreeRunner's DynamicFlowNodeWrapper, which only embeds an HL layer.</summary>
public class TestFlowNodeWrapper : PActionNode
{
    public override bool IsFlowNodeWrapper => true;
    protected override State Preconditions => null;
    protected override State Effects => null;

    public TestFlowNodeWrapper(DynamicFlowNode layer, Blackboard<FastName> blackboard)
        : base("FlowNodeWrapper", layer.DebugDisplayName, blackboard)
    {
        IsHighLevelAction = true;
        HighLevelSubtree = layer;
        layer.SetParentNode(this);
    }
}

/// <summary>
/// Regressions from running the Demonstrator through the JSON runner (--run) against
/// a real ENHSP: the HL layers sit below a wrapper action, which made the engine treat
/// them as ML subtrees, and a failed plan was requested again on every tick.
/// </summary>
public class FlowNodeWrapperPlanningTests
{
    private static (ServicePDDLPlanning service, DynamicFlowNode layer, Blackboard<FastName> blackboard, FakePlannerCommunicator comm)
        NewLayer(PlanningResult result, bool wrapped)
    {
        var blackboard = new Blackboard<FastName>(new DictionaryPredicateStore());
        var tree = new BehaviorTree();
        tree.Initialise(blackboard, "Test");
        var p1 = new FirstPos { NameKey = new FastName("p1") };
        blackboard.SetLocation(p1.NameKey, p1);
        blackboard.SetElement(new FastName("e1"), new Beam { NameKey = new FastName("e1"), Loc = p1 });
        blackboard.SetAgent(new FastName("r1"), new Robot { NameKey = new FastName("r1") });
        blackboard.SetTool(new FastName("g1"), new Gripper { NameKey = new FastName("g1") });

        var request = new PDDLPlanningRequest("domain.pddl", "problem.pddl", "enhsp.jar", "Enhsp");
        var comm = new FakePlannerCommunicator(result);
        var service = new ServicePDDLPlanning(tree, request, comm);
        var layer = new DynamicFlowNode(new FastName("Layers1_2"), tree, SuccessCriteria.ALL, 1.0f, false);
        if (wrapped)
            new TestFlowNodeWrapper(layer, blackboard);
        service.SetOwningFlowNode(layer);
        return (service, layer, blackboard, comm);
    }

    private static readonly PlanningResult PlannerCrash = new()
    {
        Success = false,
        Status = PlanStatus.Error,
        Error = "ENHSP failed with return code 255: "
    };

    [Fact]
    public void AnHLLayerBelowAWrapper_HasNoParentHLAction()
    {
        var (_, layer, _, _) = NewLayer(PlannerCrash, wrapped: true);

        Assert.Null(ServicePDDLPlanning.ParentHLAction(layer));
    }

    [Fact]
    public void AnMLSubtreeBelowARealHLAction_StillHasItsParent()
    {
        var blackboard = new Blackboard<FastName>(new DictionaryPredicateStore());
        var tree = new BehaviorTree();
        tree.Initialise(blackboard, "Test");
        var subtree = new DynamicFlowNode(new FastName("PickUpSubtree"), tree, SuccessCriteria.ALL, 1.0f, false);
        var hlAction = new TestAction("pickUpHL", blackboard);
        subtree.SetParentNode(hlAction);

        Assert.Same(hlAction, ServicePDDLPlanning.ParentHLAction(subtree));
    }

    [Fact]
    public void AWrappedLayersPlan_IsNotRegisteredAsAnInjectedSubtree()
    {
        var (service, layer, blackboard, _) = NewLayer(new PlanningResult
        {
            Success = true,
            Status = PlanStatus.Solved,
            Plan = "0.0: (pickuphl e1 p1 r1 g1)",
            PlannerUsed = "Enhsp"
        }, wrapped: true);

        service.OnEvaluate(0.1f);

        Assert.True(service.WasSuccessful, service.LastError);
        Assert.DoesNotContain(layer, blackboard.GetAllInjectedSubtrees());
    }

    [Fact]
    public void AFailingPlan_IsRetriedAFewTimes_NotOnEveryTick()
    {
        var (service, _, _, comm) = NewLayer(PlannerCrash, wrapped: true);

        for (int tick = 0; tick < 20; tick++)
            service.OnEvaluate(0.1f);

        Assert.Equal(1 + DecoratorResetFailedPlan.MaxRetriesPerState, comm.CallCount);
        Assert.False(service.WasSuccessful);
    }

    [Fact]
    public void AFailingPlan_IsRetriedAgainOnceTheWorldStateChanges()
    {
        var (service, _, blackboard, comm) = NewLayer(PlannerCrash, wrapped: true);
        for (int tick = 0; tick < 20; tick++)
            service.OnEvaluate(0.1f);
        int callsBefore = comm.CallCount;

        var clear = new Clear(new Beam { NameKey = new FastName("e1") }, false);
        blackboard.SetPredicateSync(clear.PredicateName, clear);
        for (int tick = 0; tick < 20; tick++)
            service.OnEvaluate(0.1f);

        // The new state gets a fresh set of retries (the failure itself is already latched).
        Assert.Equal(callsBefore + DecoratorResetFailedPlan.MaxRetriesPerState, comm.CallCount);
    }
}
