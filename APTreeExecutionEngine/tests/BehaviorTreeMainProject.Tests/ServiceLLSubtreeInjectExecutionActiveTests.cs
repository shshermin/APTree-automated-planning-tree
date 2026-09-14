using Xunit;

namespace BehaviorTreeMainProject.Tests;

public class ServiceLLSubtreeInjectExecutionActiveTests
{
    private const string ActionTypeName = "TestActionML";

    public ServiceLLSubtreeInjectExecutionActiveTests()
    {
        // RegisterTemplate writes into a static dictionary shared across all
        // tests in the process - always (re-)register before each test so
        // this test class doesn't depend on run order.
        var template = new ServiceLLSubtreeInject.LLSubtreeTemplate(ActionTypeName);
        template.Steps.Add(new ServiceLLSubtreeInject.LLStep("OpenGripperLL"));
        ServiceLLSubtreeInject.RegisterTemplate(ActionTypeName, template);
    }

    private static (TestActionML action, ServiceLLSubtreeInject service) NewMLAction()
    {
        var blackboard = new Blackboard<FastName>(new DictionaryPredicateStore());
        var tree = new BehaviorTree();
        tree.Initialise(blackboard, "Test");

        var action = new TestActionML("mlAction", blackboard);
        action.SetOwiningTree(tree);
        action.SetTreeForAllServices(tree);

        var service = action.GetLLSubtreeInjectionService();
        return (action, service);
    }

    [Fact]
    public void ExecutionActiveTrue_InjectsTheLLSubtree()
    {
        var (action, service) = NewMLAction();
        action.Blackboard.SetBool(new FastName("ExecutionActive"), true);

        service.OnEvaluate(0.1f);

        Assert.True(action.IsHighLevelAction);
        Assert.NotNull(action.HighLevelSubtree);
        Assert.True(action.HasChildren);
    }

    [Fact]
    public void ExecutionActiveFalse_SkipsInjection_PlanningOnly()
    {
        var (action, service) = NewMLAction();
        action.Blackboard.SetBool(new FastName("ExecutionActive"), false);

        service.OnEvaluate(0.1f);

        Assert.False(action.IsHighLevelAction);
        Assert.Null(action.HighLevelSubtree);
        Assert.False(action.HasChildren);
    }

    /// <summary>
    /// Not in the original test-plan wording, but a real edge case found
    /// while writing the two tests above: OnEvaluate's ExecutionActive read
    /// is wrapped in try/catch, and the catch treats "flag not set at all"
    /// the same as "flag set to false" (skip injection) - so a tree that
    /// never sets ExecutionActive at all silently stays in planning-only
    /// mode forever, with only a log line ("ExecutionActive not found on
    /// blackboard") as a trace, no error surfaced to the caller.
    /// </summary>
    [Fact]
    public void ExecutionActiveNeverSet_AlsoSkipsInjection_SameAsExplicitFalse()
    {
        var (action, service) = NewMLAction();
        // Deliberately not calling SetBool at all.

        service.OnEvaluate(0.1f);

        Assert.False(action.IsHighLevelAction);
        Assert.Null(action.HighLevelSubtree);
    }
}
