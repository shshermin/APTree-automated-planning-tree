using Xunit;

namespace BehaviorTreeMainProject.Tests;

/// <summary>
/// Tree execution should stop at MaxTicks and respect TickDelayMs. Not testable
/// yet: the tick loop is private to BehaviorTreeRunner and Run() exposes neither
/// the tick count nor the tree, so the loop needs extracting first.
/// </summary>
public class TreeExecutionBoundaryTests
{
    [Fact(Skip = "BehaviorTreeRunner.ExecuteTree is private and exposes no tick count - see class comment")]
    public void TreeExecution_StopsAtMaxTicks_AndRespectsTickDelay()
    {
    }
}
