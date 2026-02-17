using BehaviorTreeMainProject.Log.Services;

public abstract class BTDecoratorBase : IBTDecorator
{
    // gets the responsible agent
   // public Agent self => LinkedBlackboard.GetAgent();
   public Agent? self  {get;  protected set;}

    public IBehaviorTree OwningTree { get;  protected set;} = null!;

    public Blackboard<FastName> LinkedBlackboard => OwningTree.linkedBlackboard;
    public PActionNode? AttachedAction { get; protected set; }
    public BTFlowNodeDynamic? AttachedNode { get; protected set; }
    public abstract bool CanPostProcessTickResult { get; }
    public abstract BTNodeResult PostProcessTickResult(BTNodeResult InResult);
  
    public bool bIsInverted { get; protected set; } = false;
    protected bool? bLastResult;

    // Decorator execution statistics tracking
    private int totalTickCount = 0;
    private int successCount = 0;
    private int failureCount = 0;
    
    // Public properties for accessing statistics
    public int TotalTickCount => totalTickCount;
    public int SuccessCount => successCount;
    public int FailureCount => failureCount;
    protected BTDecoratorBase(bool bInIsInverted = false)
    {
        this.bIsInverted = bInIsInverted;
    }



    // public virtual EBTNodeResult PostProcessTickresult(EBTNodeResult InResult)
    // {
    //     return InResult;
    // }

    public void SetOwiningTree(IBehaviorTree InOwningtree)
    {
        this.OwningTree = InOwningtree;
    }

    public bool Tick(float InDeltaTime)
    {
        // Increment tick count
        totalTickCount++;
        
        // Track decorator tick count in real-time
        BehaviorTreeComponentLogger.TrackDecoratorTick(this.GetType().Name);
        
        // Call the actual evaluation logic
        bool result = OnEvaluate(InDeltaTime);
        
        // Track success and failure counts
        if (result)
        {
            successCount++;
            // Log success tracking
            BehaviorTreeComponentLogger.TrackDecoratorSuccess(this.GetType().Name);
        }
        else
        {
            failureCount++;
            // Log failure tracking
            BehaviorTreeComponentLogger.TrackDecoratorFailure(this.GetType().Name);
        }
        
        return result;
    }
    protected abstract bool OnEvaluate(float InDeltaTime);
}