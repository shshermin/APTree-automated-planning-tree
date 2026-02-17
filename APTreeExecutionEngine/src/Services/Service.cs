using BehaviorTreeMainProject.Log.Services;

public abstract class Service
{

    public IBehaviorTree OwningTree { get; protected set; } = null!;
    public FlowNode AttachedNode { get; protected set; } = null!;

    // Service execution statistics tracking
    private int totalTickCount = 0;
    private int successCount = 0;
    private int failureCount = 0;
    
    // Public properties for accessing statistics
    public int TotalTickCount => totalTickCount;
    public int SuccessCount => successCount;
    public int FailureCount => failureCount;

    protected Service(IBehaviorTree InOwningTree)

    {
        this.OwningTree = InOwningTree;
    }
    public Blackboard<FastName> linkedBlackboard => OwningTree.linkedBlackboard;

    public void SetOwiningTree(IBehaviorTree InOwningtree)
    {
        this.OwningTree = InOwningtree;
    }

    public bool Tick(float InDeltaTime)
    {
        // Increment tick count
        totalTickCount++;
        
        // Track service tick count in real-time
        BehaviorTreeComponentLogger.TrackServiceTick(this.GetType().Name);
        
        // Call the actual evaluation logic
        bool result = OnEvaluate(InDeltaTime);
        
        // Track success and failure counts
        if (result)
        {
            successCount++;
            // Log success tracking
            BehaviorTreeComponentLogger.TrackServiceSuccess(this.GetType().Name);
        }
        else
        {
            failureCount++;
            // Log failure tracking
            BehaviorTreeComponentLogger.TrackServiceFailure(this.GetType().Name);
        }
        
        return result;
    }
    
   public abstract bool OnEvaluate(float InDeltaTime);
  
    
}