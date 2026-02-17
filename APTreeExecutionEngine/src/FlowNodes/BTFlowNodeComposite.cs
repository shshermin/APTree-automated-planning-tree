using System.Collections;
using BehaviorTreeMainProject.Services;
using System.Linq;
using BehaviorTreeMainProject.Log.Services;

/// <summary>
/// Defines when a composite node should stop trying to achieve its success criteria
/// </summary>
public enum CompositeTerminationPolicy
{
    StopOnFirstFailure,         // Stop immediately when any child fails
    StopWhenCriteriaImpossible, // Stop when success criteria can no longer be met
    NeverStop,                  // Keep trying indefinitely (useful for services)
    StopAfterMaxAttempts        // Stop after N attempts/passes
}

public class BTFlowNodeComposite : FlowNode
{
    public override string TypeName => "BTFlowNodeComposite";


    // List to store flow nodes (since NodeGraph is designed for action nodes)
    public override string DebugDisplayName { get; protected set; } = "CompositeFlowNode";
    private List<IBTNode> flowNodes = new List<IBTNode>();

    // Termination policy configuration
    public CompositeTerminationPolicy TerminationPolicy { get; set; } = CompositeTerminationPolicy.NeverStop;
    public int MaxAttempts { get; set; } = 3;           // For StopAfterMaxAttempts policy
    private int currentAttempt = 0;                     // Track current attempt number
    
    public BTFlowNodeComposite(
        FastName nodeName,
        IBehaviorTree owningTree,
        SuccessCriteria successCriteria = SuccessCriteria.ALL,
        float threshold = 1.0f,
        CompositeTerminationPolicy terminationPolicy = CompositeTerminationPolicy.NeverStop)
        : base(nodeName, successCriteria, threshold)
    {
        this.OwningTree = owningTree;
        this.TerminationPolicy = terminationPolicy;
        DebugDisplayName = $"CompositeFlow({nodeName.ToString()})";
        
        // Track flow node initialization
        BehaviorTreeComponentLogger.TrackFlowNodeInitialization(this.GetType().Name);
        
        LoggingService.LogInfo($"🔧 CompositeFlow: Created with SuccessCriteria: {successCriteria}, TerminationPolicy: {terminationPolicy}");
    }
    
    /// <summary>
    /// Add a child node (can be any IBTNode, including other flow nodes)
    /// </summary>
    public override IBTNode AddChild(IBTNode childNode)
    {
        childNode.SetOwiningTree(OwningTree);
        
        // Set the tree for all services that don't have it set yet
        childNode.SetTreeForAllServices(OwningTree);
        
        // If this is a GenericBTAction, also set the tree for its ServiceSubtreeInject
        if (childNode is PActionNode action)
        {
            action.SetTreeForSubtreeInjectionService(OwningTree);
        }

        // Store flow nodes in a separate list since NodeGraph is designed for action nodes
        // We'll use the actionGraph from the base class for action nodes and a separate list for flow nodes
        if (childNode is PActionNode actionNode)
        {
            actionGraph.AddNode(actionNode);
            LinkedBlackboard.SetActionInstance(actionNode.InstanceName, actionNode);
            // Console.WriteLine($"✅ Added action node: {childNode.DebugDisplayName} to composite flow node actionGraph");
        }
        else if (childNode is FlowNode flowNode)
        {
            // For flow nodes, we'll store them in a separate list for now
            // In the future, we could extend NodeGraph to handle flow nodes
            flowNodes.Add(childNode);
            LinkedBlackboard.SetFlowNodeInstance(flowNode.InstanceName, flowNode);
            // Console.WriteLine($"✅ Added flow node: {childNode.DebugDisplayName} to composite flow node flowNodes list");
        }
        
        // Track child count for average branching factor calculation
        var totalChildCount = actionGraph.GetAllActionNodes().Count + flowNodes.Count;

        
        return childNode;
    }
    
    /// <summary>
    /// Get all child nodes (both action nodes and flow nodes)
    /// </summary>
    public List<IBTNode> GetChildren()
    {
        var allChildren = new List<IBTNode>();
        
        // Add action nodes from actionGraph
        var actionNodes = actionGraph.GetAllActionNodes();
        allChildren.AddRange(actionNodes.Cast<IBTNode>());
        
        // Add flow nodes from flowNodes list
        allChildren.AddRange(flowNodes);
        
        return allChildren;
    }
    
    /// <summary>
    /// Get the number of child nodes
    /// </summary>
    public int ChildCount => actionGraph.GetAllActionNodes().Count + flowNodes.Count;
    
    /// <summary>
    /// Enumerate through child nodes
    /// </summary>
    public override IEnumerator<IBTNode> GetEnumerator()
    {
        var allChildren = new List<IBTNode>();
        
        // Add action nodes from actionGraph
        var actionNodes = actionGraph.GetAllActionNodes();
        allChildren.AddRange(actionNodes.Cast<IBTNode>());
        
        // Add flow nodes from flowNodes list
        allChildren.AddRange(flowNodes);
        
        return allChildren.GetEnumerator();
    }
    
    /// <summary>
    /// Execute the composite flow node logic
    /// Children are ticked strictly sequentially: each child is ticked and its result
    /// is fully returned before the next child is ticked. This eliminates race conditions
    /// where decorators on different children could see inconsistent blackboard state.
    /// </summary>
    protected override bool OnTick_NodeLogic(float inDeltaTime)
    {
        var allChildren = GetChildren();
        
        // If no children, fail immediately
        if (allChildren.Count == 0)
        {
            // Let the base class handle failure tracking through OnTickReturn
            return false; // This will trigger OnTickReturn with failed status
        }
        
        // Tick children SEQUENTIALLY: tick child i, wait for its result, then tick child i+1.
        // Each child's Tick() fully completes (including all decorator evaluation, node logic,
        // and post-processing) before the next child begins. This ensures that blackboard
        // state changes made by one child's decorators (e.g. LowestCostExecution writing
        // ChosenExecutingBranch) are fully visible to the next child's decorators
        // (e.g. ExclusiveBranchGate reading ChosenExecutingBranch).
        for (int i = 0; i < allChildren.Count; i++)
        {
            var child = allChildren[i];
            
            var previousStatus = child.status;
            
            LoggingService.LogInfo($"🎯 CompositeFlow: [{i + 1}/{allChildren.Count}] Ticking child: {child.DebugDisplayName} (current status: {previousStatus})");
            
            // Tick the child - this call blocks until the child's full tick cycle completes
            child.Tick(inDeltaTime);
            
            LoggingService.LogInfo($"📊 CompositeFlow: [{i + 1}/{allChildren.Count}] Child {child.DebugDisplayName}: {previousStatus} → {child.status}");
        }
        
        // Check if any child is still running
        bool anyRunning = allChildren.Any(node => node.status != BTNodeResult.Success && node.status != BTNodeResult.Failure);
        if (anyRunning)
        {
            status = BTNodeResult.InProgress;
            LoggingService.LogInfo($"⏳ CompositeFlow: Some children still running, continuing next tick");
            return true;
        }

        // All children finished (succeeded or failed) - evaluate using success criteria and termination policy
        currentAttempt++;
        
        // First check if we've achieved success criteria
        bool criteriaAchieved = EvaluateSuccessCriteria(allChildren);
        if (criteriaAchieved)
        {
            status = BTNodeResult.Success;
            LoggingService.LogSuccess($"✅ CompositeFlow: Success criteria achieved on attempt {currentAttempt}");
            return true;
        }
        
        // Success not achieved - check termination policy to decide if we should continue
        bool shouldTerminate = ShouldTerminate(allChildren);
        if (shouldTerminate)
        {
            LoggingService.LogError($"❌ CompositeFlow: Termination policy triggered after {currentAttempt} attempts");
            
            // Let the base class handle failure tracking through OnTickReturn
            return false; // This will trigger OnTickReturn with failed status
        }
        else
        {
            // Reset failed children and try again
            ResetFailedChildren(allChildren);
            status = BTNodeResult.InProgress;
            LoggingService.LogInfo($"🔄 CompositeFlow: Retrying - attempt {currentAttempt}, continuing execution");
            return true;
        }
    }
    
    /// <summary>
    /// Evaluate success criteria based on child node results
    /// </summary>
    private bool EvaluateSuccessCriteria(List<IBTNode> children)
    {
        if (children.Count == 0) return false;
        
        int successCount = children.Count(node => node.status == BTNodeResult.Success);
        int totalCount = children.Count;
        
        LoggingService.LogInfo($"📊 CompositeFlow: Success evaluation - {successCount}/{totalCount} children succeeded");
        
        return successCriteria switch
        {
            SuccessCriteria.ALL => successCount == totalCount,
            SuccessCriteria.ANY => successCount > 0,
            SuccessCriteria.COUNT => successCount >= (int)successThreshold,
            SuccessCriteria.PERCENTAGE => successCount >= (totalCount * successThreshold),
            _ => false
        };
    }
    
    /// <summary>
    /// Determine if execution should terminate based on termination policy
    /// </summary>
    private bool ShouldTerminate(List<IBTNode> children)
    {
        return TerminationPolicy switch
        {
            CompositeTerminationPolicy.StopOnFirstFailure => 
                children.Any(node => node.status == BTNodeResult.Failure),
                
            CompositeTerminationPolicy.StopWhenCriteriaImpossible => 
                IsCriteriaImpossible(children),
                
            CompositeTerminationPolicy.NeverStop => 
                false,
                
            CompositeTerminationPolicy.StopAfterMaxAttempts => 
                currentAttempt >= MaxAttempts,
                
            _ => true
        };
    }
    
    /// <summary>
    /// Check if success criteria can no longer be achieved
    /// </summary>
    private bool IsCriteriaImpossible(List<IBTNode> children)
    {
        int successCount = children.Count(node => node.status == BTNodeResult.Success);
        int failedCount = children.Count(node => node.status == BTNodeResult.Failure);
        int remainingCount = children.Count - successCount - failedCount;
        int maxPossibleSuccess = successCount + remainingCount;
        
        return successCriteria switch
        {
            SuccessCriteria.ALL => failedCount > 0, // Any failure makes ALL impossible
            SuccessCriteria.ANY => maxPossibleSuccess == 0, // No children can succeed
            SuccessCriteria.COUNT => maxPossibleSuccess < (int)successThreshold,
            SuccessCriteria.PERCENTAGE => maxPossibleSuccess < (children.Count * successThreshold),
            _ => false
        };
    }
    
    /// <summary>
    /// Reset failed children for retry
    /// </summary>
    private void ResetFailedChildren(List<IBTNode> children)
    {
        int resetCount = 0;
        foreach (var child in children)
        {
            if (child.status == BTNodeResult.Failure)
            {
                child.Reset();
                resetCount++;
                LoggingService.LogInfo($"🔄 CompositeFlow: Reset failed child: {child.DebugDisplayName}");
            }
        }
        LoggingService.LogInfo($"🔄 CompositeFlow: Reset {resetCount} failed children for retry");
    }
    
    /// <summary>
    /// Legacy method for backward compatibility
    /// </summary>
    private bool EvaluateCompositeSuccessCriteria()
    {
        return EvaluateSuccessCriteria(GetChildren());
    }
    
    /// <summary>
    /// Children are handled in OnTick_NodeLogic
    /// </summary>
    protected override bool OnTick_Children(float inDeltaTime)
    {
        // Children are handled in OnTick_NodeLogic
        return true;
    }
    
    /// <summary>
    /// Reset all child nodes and execution state
    /// </summary>
    public override void Reset()
    {
        base.Reset();
        
        // Reset the current count for maxCount mechanism
        ResetCurrentCount();
        
        // Reset the attempt counter for termination policy
        currentAttempt = 0;
        
        // Reset all child nodes
        var allChildren = GetChildren();
        foreach (var childNode in allChildren)
        {
            childNode.Reset();
        }
        
        LoggingService.LogInfo($"🔄 CompositeFlow: Reset execution state, child index, attempt counter, and all {allChildren.Count} children");
    }
    
    /// <summary>
    /// Configure termination policy with common settings
    /// </summary>
    public void ConfigureTerminationPolicy(CompositeTerminationPolicy policy, int maxAttempts = 3)
    {
        TerminationPolicy = policy;
        MaxAttempts = maxAttempts;
        LoggingService.LogInfo($"🔧 CompositeFlow: Configured termination policy: {policy} (max attempts: {maxAttempts})");
    }

    /// <summary>
    /// Add the planning phase management service to this composite node
    /// </summary>
    public void AddPlanningPhaseService()
    {
        var planningPhaseService = new ServicePlanningPhaseManager(OwningTree, this);
        AddService(planningPhaseService, false); // false = general service (runs during planning)
        LoggingService.LogInfo($"🔧 CompositeFlow: Added PlanningPhaseManager service to {DebugDisplayName}");
    }
    
   
   

    /// <summary>
    /// Set the maximum number of ticks before failing when success criteria is not met
    /// </summary>
    /// <param name="maxTicks">Maximum number of ticks before failing</param>
    public void SetMaxTicks(int maxTicks)
    {
        SetMaxCount(maxTicks);
        LoggingService.LogInfo($"🔧 CompositeFlow: Set max ticks to {maxTicks} for {DebugDisplayName}");
    }

    /// <summary>
    /// Checks if all planning services in child nodes have completed successfully
    /// </summary>
    /// <returns>True if all planning is complete, false otherwise</returns>
    public bool AreAllPlanningServicesComplete()
    {
        var children = GetChildren();
        
        foreach (var child in children)
        {
            if (child is DynamicFlowNode dynamicNode)
            {
                // Check if this dynamic node has a planning service
                if (dynamicNode.ServicePlanning is ServicePlanning plannerService)
                {
                    // Check if planning has generated a NodeGraph
                    if (!plannerService.HasGeneratedNodeGraph())
                    {
                        LoggingService.LogInfo($"⏳ Planning still in progress for {dynamicNode.GetNodeName()}");
                        return false; // Still planning
                    }
                }
                else
                {
                    LoggingService.LogWarning($"⚠️ Dynamic node {dynamicNode.GetNodeName()} has no planning service");
                    return false; // No planning service means not ready
                }
            }
            else if (child is BTFlowNodeComposite childCompositeNode)
            {
                // Recursively check composite nodes
                if (!childCompositeNode.AreAllPlanningServicesComplete())
                {
                    return false; // Child composite still planning
                }
            }
        }
        
        return true; // All planning complete
    }
}
