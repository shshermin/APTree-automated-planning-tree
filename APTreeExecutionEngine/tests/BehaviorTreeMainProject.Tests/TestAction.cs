namespace BehaviorTreeMainProject.Tests;

/// <summary>
/// Minimal PActionNode for scheduling tests. No HL/ML/LL suffix, so no
/// subtree-injection services get attached.
/// </summary>
public class TestAction : PActionNode
{
    protected override State Preconditions => null;
    protected override State Effects => null;

    public TestAction(string instanceName, Blackboard<FastName> blackboard)
        : base("TestAction", instanceName, blackboard)
    {
    }
}
