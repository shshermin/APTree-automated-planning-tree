using AIPlanning;
using BehaviorTreeMainProject.Services.AIPlanning;
using ModelLoader.ParameterTypes;
using Xunit;

namespace BehaviorTreeMainProject.Tests;

/// <summary>
/// The public ServicePDDLPlanning constructor hardcodes a
/// RestPlannerCommunicator against localhost:5000 with no seam
/// to substitute a fake - added an `internal` constructor overload
/// accepting any IPlannerCommunicator (see ServicePDDLPlanning.cs) purely
/// for this; no behavior change to the existing public constructor.
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
    /// Found while wiring up the tests below, then double-checked rather than
    /// assumed: with no owning flow node set, ServicePlanning.OnEvaluate
    /// (ServicePlanning.cs ~line 253) unconditionally evaluates
    /// `OwningFlowNode.ParentNode is PActionNode parentAction` - no null
    /// guard, right after a block that explicitly handles
    /// `OwningFlowNode == null` a few lines earlier (~line 223) and does NOT
    /// return/throw from that branch. So this line does throw a
    /// NullReferenceException when a NodeGraph comes back and no flow node
    /// was ever attached. It does NOT escape as a crash, though: the whole
    /// method (ServicePlanning.cs line ~160 onward) is wrapped in one big
    /// try/catch that treats any exception as a planning failure - so the
    /// net effect is a genuinely successful plan silently reported as
    /// `WasSuccessful=false, LastError="Object reference not set to an
    /// instance of an object."`, a confusing, non-actionable error instead
    /// of either a working headless mode or a clear message about the
    /// missing flow node. Confirmed below, not assumed.
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

        // CreateAndRegisterActionInstance (called deep inside ParseNodeGraph)
        // resolves each parameter name against the blackboard's registered
        // entities - e1/p1/r1/g1 must exist there first, or action creation
        // fails silently (caught, logged, skipped - see the
        // GarbagePlanContent_... finding below for what that looks like).
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

        // IsCommunicationError() matches "Planning request timed out" -> retry,
        // not a permanent failure - HasCompleted must stay false so the next
        // tick re-enters the planning path (ServicePlanning.cs's retry branch).
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

        // MAX_COMMUNICATION_RETRIES = 3 in ServicePlanning.cs - tick 4 times.
        for (int i = 0; i < 4; i++)
            service.OnEvaluate(0.1f);

        Assert.True(service.HasCompleted);
        Assert.False(service.WasSuccessful);
        Assert.Equal(4, comm.CallCount);
    }

    [Fact]
    public void PlannerRejection_FailsPermanently_WithoutRetrying()
    {
        // A real planner-level rejection (e.g. no valid plan found), not a
        // communication error - IsCommunicationError() should NOT match this,
        // so it must fail immediately with no retry.
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
    /// The real finding here, confirmed empirically (not the "same as empty"
    /// guess this test started from - see git history if curious): garbage
    /// Plan content that ParseRawOutput can't recognize makes
    /// TransformToAPTreeModel return "" (Planner.cs:
    /// `if (actions.Count == 0) return string.Empty`). That empty string then
    /// goes through ParsePlanStringToNodeGraph's OWN empty-string check
    /// (ServicePDDLPlanning.cs), which returns a fresh, non-null, EMPTY
    /// NodeGraph rather than null or an error. GenerateNodeGraphFromResult
    /// only sets success=false when result.Plan ITSELF was empty
    /// (ServicePDDLPlanning.cs) - it never checks whether the resulting
    /// graph actually contains anything - so a non-null empty NodeGraph
    /// reads as success. Net effect: garbage planner output is reported as
    /// WasSuccessful=true / HasPlanGenerated=true with a NodeGraph that has
    /// zero actions - the opposite of "handled correctly" the test-plan item
    /// asks for. The same silent-empty-success path is reachable for a
    /// well-formed plan whose parameters reference blackboard entities that
    /// were never registered (see SuccessfulPlan_... above for what
    /// registering them looks like) - action creation fails per-line,
    /// is logged, and is skipped rather than surfaced as an overall failure.
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
