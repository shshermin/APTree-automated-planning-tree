using AIPlanning;
using BehaviorTreeMainProject.Services.AIPlanning;
using ModelLoader.ParameterTypes;
using Xunit;

namespace BehaviorTreeMainProject.Tests;

/// <summary>
/// Runs the same HL planning pipeline against both predicate stores and checks
/// they produce the same NodeGraph. ExecutionMode isn't part of the matrix
/// because the real path ignores it (see PDDLPlanningExecutionModeTests).
/// </summary>
public class ConfigDrivenPredicateStoreMatrixTests
{
    private static (ServicePDDLPlanning service, Blackboard<FastName> blackboard) NewServiceWithStore(
        IPredicateStore store, PlanningResult result)
    {
        var blackboard = new Blackboard<FastName>(store);
        var tree = new BehaviorTree();
        tree.Initialise(blackboard, "Test");
        var request = new PDDLPlanningRequest("domain.pddl", "problem.pddl", "enhsp.jar", "Enhsp");
        var comm = new FakePlannerCommunicator(result);
        var service = new ServicePDDLPlanning(tree, request, comm);

        var flowNode = new DynamicFlowNode(new FastName("Main"), tree, SuccessCriteria.ALL, 1.0f, false);
        service.SetOwningFlowNode(flowNode);

        return (service, blackboard);
    }

    private static void RegisterEntities(Blackboard<FastName> blackboard)
    {
        var p1 = new FirstPos { NameKey = new FastName("p1") };
        var p2 = new FirstPos { NameKey = new FastName("p2") };
        var e1 = new Beam { NameKey = new FastName("e1"), Loc = p1 };
        var r1 = new Robot { NameKey = new FastName("r1") };
        var g1 = new Gripper { NameKey = new FastName("g1") };

        blackboard.SetLocation(p1.NameKey, p1);
        blackboard.SetLocation(p2.NameKey, p2);
        blackboard.SetElement(e1.NameKey, e1);
        blackboard.SetAgent(r1.NameKey, r1);
        blackboard.SetTool(g1.NameKey, g1);
    }

    // Two actions so the generated graph contains a MEETS relation.
    private const string TwoActionPlan = "0.0: (pickuphl e1 p1 r1 g1)\n1.0: (placehl e1 p2 r1)";

    [Theory]
    [InlineData("Dictionary")]
    [InlineData("Sqlite")]
    public void TwoActionPlan_ProducesTheSameNodeGraphShape_RegardlessOfPredicateStoreType(string storeType)
    {
        IPredicateStore store = storeType == "Sqlite"
            ? new SqlitePredicateStore(":memory:")
            : new DictionaryPredicateStore();

        var (service, blackboard) = NewServiceWithStore(store, new PlanningResult
        {
            Success = true,
            Plan = TwoActionPlan,
            PlannerUsed = "Enhsp",
        });
        RegisterEntities(blackboard);

        service.OnEvaluate(0.1f);

        Assert.True(service.HasCompleted);
        Assert.True(service.WasSuccessful, service.LastError);
        Assert.True(service.HasPlanGenerated);

        var actions = service.GeneratedNodeGraph.GetAllActionNodes();
        Assert.Equal(2, actions.Count);
        Assert.StartsWith("PickUpHL", actions[0].InstanceName.ToString());
        Assert.StartsWith("PlaceHL", actions[1].InstanceName.ToString());

        store.Dispose();
    }

    [Fact]
    public void SqlitePredicateStore_IsUsableAsTheStoreBehindTheSameBlackboardApiActionsQueryDuringParsing()
    {
        // Action parameters are resolved against the blackboard during parsing.
        using var store = new SqlitePredicateStore(":memory:");
        var (service, blackboard) = NewServiceWithStore(store, new PlanningResult
        {
            Success = true,
            Plan = "0.0: (pickuphl e1 p1 r1 g1)",
            PlannerUsed = "Enhsp",
        });
        RegisterEntities(blackboard);

        service.OnEvaluate(0.1f);

        Assert.True(service.WasSuccessful, service.LastError);
        Assert.Equal(1, service.GeneratedNodeGraph.GetAllActionNodes().Count);
    }
}
