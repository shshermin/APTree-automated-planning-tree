using System.Diagnostics;
using System.Threading.Tasks;
using BehaviorTreeMainProject.ModelLoader;
using Xunit;

namespace BehaviorTreeMainProject.Tests;

/// <summary>
/// The runner's tick loop stops at MaxTicks, stops early when the tree finishes,
/// treats MaxTicks &lt;= 0 as unlimited and waits TickDelayMs between ticks.
/// </summary>
public class TreeExecutionBoundaryTests
{
    /// <summary>Root that reports InProgress until it has been ticked <c>finishAfter</c> times.</summary>
    private class CountingRoot : BTFlowNodeComposite
    {
        private readonly int _finishAfter;
        public int Ticks { get; private set; }

        public CountingRoot(IBehaviorTree tree, int finishAfter) : base(new FastName("Root"), tree)
        {
            _finishAfter = finishAfter;
        }

        protected override bool OnTick_NodeLogic(float inDeltaTime)
        {
            Ticks++;
            status = Ticks >= _finishAfter ? BTNodeResult.Success : BTNodeResult.InProgress;
            return true;
        }
    }

    private static (BehaviorTree tree, CountingRoot root) NewTree(int finishAfter)
    {
        var tree = new BehaviorTree();
        tree.Initialise(new Blackboard<FastName>(new DictionaryPredicateStore()), "Test");
        var root = new CountingRoot(tree, finishAfter);
        tree.root = root;
        return (tree, root);
    }

    private static Task<int> Execute(BehaviorTree tree, int maxTicks, int tickDelayMs = 0) =>
        new BehaviorTreeRunner("unused.json", new BehaviorTreeConfiguration { MaxTicks = maxTicks, TickDelayMs = tickDelayMs })
            .ExecuteTree(tree);

    [Fact]
    public async Task TreeExecution_StopsAtMaxTicks()
    {
        var (tree, root) = NewTree(finishAfter: int.MaxValue);

        int ticks = await Execute(tree, maxTicks: 3);

        Assert.Equal(3, ticks);
        Assert.Equal(3, root.Ticks);
    }

    [Fact]
    public async Task TreeExecution_StopsWhenTheTreeFinishes_BeforeMaxTicks()
    {
        var (tree, root) = NewTree(finishAfter: 2);

        int ticks = await Execute(tree, maxTicks: 10);

        Assert.Equal(2, ticks);
        Assert.Equal(2, root.Ticks);
    }

    [Fact]
    public async Task TreeExecution_WithMaxTicksZero_RunsUntilTheTreeFinishes()
    {
        var (tree, _) = NewTree(finishAfter: 5);

        int ticks = await Execute(tree, maxTicks: 0);

        Assert.Equal(5, ticks);
    }

    [Fact]
    public async Task TreeExecution_WaitsTickDelayBetweenTicks()
    {
        var (tree, _) = NewTree(finishAfter: int.MaxValue);
        var watch = Stopwatch.StartNew();

        await Execute(tree, maxTicks: 3, tickDelayMs: 100);

        // One delay after each of the 3 ticks; leave slack for timer resolution.
        Assert.True(watch.ElapsedMilliseconds >= 250, $"took only {watch.ElapsedMilliseconds} ms");
    }
}
