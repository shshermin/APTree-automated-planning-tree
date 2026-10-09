using Xunit;

namespace BehaviorTreeMainProject.Tests;

public class ServiceLLSubtreeInjectExecutionActiveTests
{
    private const string ActionTypeName = "TestActionML";

    public ServiceLLSubtreeInjectExecutionActiveTests()
    {
        // RegisterTemplate writes to a process-wide static; re-register so tests don't depend on order.
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
    /// A missing ExecutionActive flag is treated like false: injection is
    /// skipped with only a log line, no error.
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
