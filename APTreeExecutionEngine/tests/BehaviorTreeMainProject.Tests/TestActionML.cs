namespace BehaviorTreeMainProject.Tests;

/// <summary>
/// Minimal ML-level PActionNode test double. The class name must end in
/// "ML" - PActionNode's constructor only wires up ServiceLLSubtreeInject for
/// action types ending in "ML" (see PActionNode.cs), and
/// ServiceLLSubtreeInject.OnEvaluate itself re-checks actionType.EndsWith("ML").
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
