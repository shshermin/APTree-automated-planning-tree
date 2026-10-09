namespace BehaviorTreeMainProject.Tests;

/// <summary>
/// Minimal ML-level PActionNode. The name must end in "ML": that's how
/// PActionNode decides to attach ServiceLLSubtreeInject.
/// </summary>
public class TestActionML : PActionNode
{
    protected override State Preconditions => null;
    protected override State Effects => null;

    public TestActionML(string instanceName, Blackboard<FastName> blackboard)
        : base("TestActionML", instanceName, blackboard)
    {
    }
}
