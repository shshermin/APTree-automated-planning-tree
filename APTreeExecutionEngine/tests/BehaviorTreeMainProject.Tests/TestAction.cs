namespace BehaviorTreeMainProject.Tests;

/// <summary>
/// Minimal PActionNode test double for NodeGraph scheduling tests: no
/// preconditions/effects, no HL/ML/LL suffix (so PActionNode's constructor
/// skips wiring up ServiceSubtreeInject/ServiceLLSubtreeInject - this is
/// purely about graph scheduling, not planning/injection).
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
