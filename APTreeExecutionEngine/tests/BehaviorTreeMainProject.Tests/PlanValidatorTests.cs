using System.Collections.Generic;
using System.Linq;
using AIPlanning;
using BehaviorTreeMainProject.Services.AIPlanning;
using ModelLoader.ParameterTypes;
using ModelLoader.PredicateTypes;
using Xunit;

namespace BehaviorTreeMainProject.Tests;

/// <summary>ML action with preconditions and effects set by the test.</summary>
public class ValidatorStepML : PActionNode
{
    private readonly State _preconditions = new(StateType.Precondition, new FastName("pre"));
    private readonly State _effects = new(StateType.Effect, new FastName("eff"));

    public ValidatorStepML(string instanceName, Blackboard<FastName> blackboard)
        : base("ValidatorStepML", instanceName, blackboard)
    {
    }

    public ValidatorStepML Requires(Predicate predicate)
    {
        _preconditions.AddPredicate(new FastName($"pre_{_preconditions.GetAllPredicates().Count()}"), predicate);
        return this;
    }

    public ValidatorStepML Causes(Predicate predicate)
    {
        _effects.AddPredicate(new FastName($"eff_{_effects.GetAllPredicates().Count()}"), predicate);
        return this;
    }

    protected override State Preconditions => _preconditions;
    protected override State Effects => _effects;
}

/// <summary>HL action whose preconditions are never met.</summary>
public class ValidatorStepHL : PActionNode
{
    private readonly State _preconditions = new(StateType.Precondition, new FastName("pre"));

    public ValidatorStepHL(string instanceName, Blackboard<FastName> blackboard, Predicate unmet)
        : base("ValidatorStepHL", instanceName, blackboard)
    {
        _preconditions.AddPredicate(new FastName("pre_0"), unmet);
    }

    protected override State Preconditions => _preconditions;
    protected override State Effects => null;
}

public class PlanValidatorTests
{
    private static readonly Beam Top = new() { NameKey = new FastName("top") };
    private static readonly Beam Below = new() { NameKey = new FastName("below") };
    private static readonly FirstPos P1 = new() { NameKey = new FastName("p1") };
    private static readonly Robot R1 = new() { NameKey = new FastName("r1") };
    private static readonly Gripper G1 = new() { NameKey = new FastName("g1") };
    private static readonly RobotPosition Rp1 = new() { NameKey = new FastName("rp1") };
    private static readonly RobotPosition Rp2 = new() { NameKey = new FastName("rp2") };

    private static Blackboard<FastName> NewBlackboard(params Predicate[] facts)
    {
        var blackboard = new Blackboard<FastName>(new DictionaryPredicateStore());
        foreach (var fact in facts)
            blackboard.SetPredicateSync(fact.PredicateName, fact);
        return blackboard;
    }

    private static NodeGraph Sequence(params PActionNode[] steps)
    {
        var graph = new NodeGraph();
        foreach (var step in steps)
            graph.AddNode(step);
        for (int i = 0; i + 1 < steps.Length; i++)
        {
            graph.AddOrderRelation(steps[i], steps[i + 1]);
            graph.AddTemporalConstraint(steps[i], steps[i + 1], TemporalType.MEETS);
        }
        return graph;
    }

    private static PlanValidationResult Validate(NodeGraph plan, Blackboard<FastName> blackboard)
        => new SymbolicPlanValidator().ValidatePlan(plan, blackboard);

    // ── Planner answer ──

    [Theory]
    [InlineData(PlanStatus.Unsolvable, false)]
    [InlineData(PlanStatus.Error, false)]
    [InlineData(PlanStatus.Solved, true)]
    public void ValidateResult_OnlyAcceptsASolvedResult(PlanStatus status, bool success)
    {
        var check = new SymbolicPlanValidator().ValidateResult(new PlanningResult { Success = success, Status = status });

        Assert.Equal(status == PlanStatus.Solved, check.IsValid);
    }

    [Fact]
    public void ValidateResult_RejectsASolvedStatusThatIsNotSuccessful()
    {
        Assert.False(new SymbolicPlanValidator().ValidateResult(new PlanningResult { Success = false, Status = PlanStatus.Solved }).IsValid);
    }

    [Fact]
    public void ValidateResult_WithoutAStatus_FallsBackToSuccess()
    {
        Assert.True(new SymbolicPlanValidator().ValidateResult(new PlanningResult { Success = true }).IsValid);
        Assert.False(new SymbolicPlanValidator().ValidateResult(new PlanningResult { Success = false }).IsValid);
    }

    [Theory]
    [InlineData("solved", PlanStatus.Solved)]
    [InlineData("unsolvable", PlanStatus.Unsolvable)]
    [InlineData("error", PlanStatus.Error)]
    public void TheServiceStatusString_IsParsedIntoPlanStatus(string json, PlanStatus expected)
    {
        var result = RestPlannerCommunicator.ParseResult($"{{\"success\": false, \"status\": \"{json}\"}}");

        Assert.Equal(expected, result.Status);
    }

    // ── Plan simulation ──

    [Fact]
    public void APlanWhoseStepsEstablishEachOthersPreconditions_IsValid()
    {
        var blackboard = NewBlackboard(new Clear(Top, false));
        var first = new ValidatorStepML("first", blackboard).Requires(new Clear(Top, false)).Causes(new Accessible(Top, false));
        var second = new ValidatorStepML("second", blackboard).Requires(new Accessible(Top, false));

        var check = Validate(Sequence(first, second), blackboard);

        Assert.True(check.IsValid, check.Reason);
    }

    [Fact]
    public void AStepThatNeedsAFactAnEarlierStepRemoved_IsRejected()
    {
        var blackboard = NewBlackboard(new Clear(Top, false));
        var first = new ValidatorStepML("first", blackboard).Requires(new Clear(Top, false)).Causes(new Clear(Top, true));
        var second = new ValidatorStepML("second", blackboard).Requires(new Clear(Top, false));

        var check = Validate(Sequence(first, second), blackboard);

        Assert.False(check.IsValid);
        Assert.Contains("Step 2 (second)", check.Reason);
    }

    [Fact]
    public void ANegatedPreconditionThatIsTrue_IsRejected()
    {
        var blackboard = NewBlackboard(new Fixed(Top, false));
        var step = new ValidatorStepML("step", blackboard).Requires(new Fixed(Top, true));

        Assert.False(Validate(Sequence(step), blackboard).IsValid);
    }

    [Fact]
    public void Validation_DoesNotChangeTheBlackboard()
    {
        var blackboard = NewBlackboard(new Clear(Top, false));
        var step = new ValidatorStepML("step", blackboard).Requires(new Clear(Top, false))
            .Causes(new Clear(Top, true)).Causes(new Accessible(Top, false));

        Validate(Sequence(step), blackboard);

        var facts = blackboard.GetAllPredicates();
        Assert.Single(facts);
        Assert.False(facts[0].not);
    }

    [Fact]
    public void MovingARobot_RemovesItsOldLocation_LikeTheBlackboardDoes()
    {
        var blackboard = NewBlackboard(new AtAgent(R1, Rp1, false));
        var move = new ValidatorStepML("move", blackboard).Causes(new AtAgent(R1, Rp2, false));
        var stillThere = new ValidatorStepML("stillThere", blackboard).Requires(new AtAgent(R1, Rp1, false));

        Assert.False(Validate(Sequence(move, stillThere), blackboard).IsValid);
    }

    [Fact]
    public void HLStepPreconditions_AreNotChecked_SameAsAtRuntime()
    {
        var blackboard = NewBlackboard();
        var hl = new ValidatorStepHL("hl", blackboard, new Clear(Top, false));

        Assert.True(Validate(Sequence(hl), blackboard).IsValid);
    }

    [Fact]
    public void AnUnresolvedParameter_IsRejected()
    {
        var blackboard = NewBlackboard();
        var step = new ValidatorStepML("step", blackboard).Causes(new Clear(null!, false));

        var check = Validate(Sequence(step), blackboard);

        Assert.False(check.IsValid);
        Assert.Contains("unresolved parameter", check.Reason);
    }

    /// <summary>
    /// PickUpML's conditional effect (picking up what was stacked clears the element below)
    /// must be simulated too, or a valid rearrangement plan would be rejected.
    /// </summary>
    [Fact]
    public void PickUpMLsConditionalEffect_IsSimulated_WithoutTouchingTheBlackboard()
    {
        var blackboard = NewBlackboard(
            new HasTool(R1, G1, false), new AtPlace(Top, P1, false), new AtAgent(R1, Rp1, false),
            new GripperEmpty(R1, false), new Clear(Top, false), new Stacked(Top, Below, false),
            new Clear(Below, true));
        var pickUp = new PickUpML("PickUpML", "pickTop", blackboard, Top, P1, R1, G1, Rp1);
        var needsBelowClear = new ValidatorStepML("useBelow", blackboard).Requires(new Clear(Below, false));

        var check = Validate(Sequence(pickUp, needsBelowClear), blackboard);

        Assert.True(check.IsValid, check.Reason);
        Assert.Contains(blackboard.GetAllPredicates(), p => p is Stacked && !p.not);
        Assert.Contains(blackboard.GetAllPredicates(), p => p is Clear c && c.obj == Below && c.not);
    }

    // ── Hooked into ServicePlanning ──

    private sealed class RejectEverything : PlanValidator
    {
        public int Calls { get; private set; }

        public override PlanValidationResult ValidatePlan(NodeGraph plan, Blackboard<FastName> blackboard)
        {
            Calls++;
            return PlanValidationResult.Invalid("test says no");
        }
    }

    private static (ServicePDDLPlanning service, DynamicFlowNode flowNode) NewService(PlanningResult result)
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
        var service = new ServicePDDLPlanning(tree, request, new FakePlannerCommunicator(result));
        var flowNode = new DynamicFlowNode(new FastName("Main"), tree, SuccessCriteria.ALL, 1.0f, false);
        service.SetOwningFlowNode(flowNode);
        return (service, flowNode);
    }

    private static PlanningResult OneStepPlan(PlanStatus? status = null) => new()
    {
        Success = true,
        Status = status,
        Plan = "0.0: (pickuphl e1 p1 r1 g1)",
        PlannerUsed = "Enhsp"
    };

    [Fact]
    public void EveryPDDLPlanningService_HasASymbolicValidator()
    {
        var (service, _) = NewService(OneStepPlan());

        Assert.IsType<SymbolicPlanValidator>(service.Validator);
        Assert.Throws<System.ArgumentNullException>(() => service.Validator = null!);
    }

    [Fact]
    public void ARejectedPlan_FailsPlanning_AndIsNeverAttachedToTheTree()
    {
        var (service, flowNode) = NewService(OneStepPlan());
        var validator = new RejectEverything();
        service.Validator = validator;

        bool ok = service.OnEvaluate(0.1f);

        Assert.False(ok);
        Assert.Equal(1, validator.Calls);
        Assert.True(service.HasCompleted);
        Assert.False(service.WasSuccessful);
        Assert.Null(service.GeneratedNodeGraph);
        Assert.Empty(flowNode.GetActionGraph()?.GetAllActionNodes() ?? new List<PActionNode>());
        Assert.Contains("test says no", service.LastError);
    }

    [Fact]
    public void AResultMarkedUnsolvable_IsRejectedBeforeAnyGraphIsBuilt()
    {
        var (service, flowNode) = NewService(OneStepPlan(PlanStatus.Unsolvable));

        service.OnEvaluate(0.1f);

        Assert.False(service.WasSuccessful);
        Assert.Empty(flowNode.GetActionGraph()?.GetAllActionNodes() ?? new List<PActionNode>());
        Assert.Contains("unsolvable", service.LastError);
    }

    [Fact]
    public void ASolvedHLPlan_StillGoesThrough()
    {
        var (service, _) = NewService(OneStepPlan(PlanStatus.Solved));

        service.OnEvaluate(0.1f);

        Assert.True(service.WasSuccessful, service.LastError);
        Assert.Single(service.GeneratedNodeGraph.GetAllActionNodes());
    }
}
