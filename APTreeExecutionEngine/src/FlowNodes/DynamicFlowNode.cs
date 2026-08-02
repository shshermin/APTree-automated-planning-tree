using BehaviorTreeMainProject;
using BehaviorTreeMainProject.Services;
using System.Collections.Generic;
using System.Linq;
using BehaviorTreeMainProject.Log.Services;

public class DynamicFlowNode : FlowNode
{

    public override string TypeName => "DynamicFlowNode";
    private bool planningCompleted = false;

    // Track if we've completed the first planning cycle
    private bool firstPlanningCycleCompleted = false;

    // Track which executable nodes have been processed
    private HashSet<string> processedExecutableNodes = new HashSet<string>();

    // NEW: Track tick count to allow multiple rounds before failing
    private int tickCount = 0;
    private const int MAX_TICKS_BEFORE_FAILURE = 10;
    private bool hasCompletedFirstRound = false;

    private readonly FlowNodeGraph flowGraph = new();

    public bool IsAllFlow => flowGraph.Count > 0;

    public void AddFlowRelation(string fromName, string toName, TemporalType temporalType = TemporalType.MEETS)
    {
        var from = flowGraph.GetAllNodes().FirstOrDefault(node =>
            string.Equals(node.InstanceName.ToString(), fromName, StringComparison.OrdinalIgnoreCase));
        var to = flowGraph.GetAllNodes().FirstOrDefault(node =>
            string.Equals(node.InstanceName.ToString(), toName, StringComparison.OrdinalIgnoreCase));

        if (from != null && to != null)
            flowGraph.AddRelation(from, to, temporalType);
    }

    public void AddPlanningPhaseService()
    {
        AddService(new ServicePlanningPhaseManager(OwningTree, this), false);
        LoggingService.LogInfo($"🔧 DynamicFlowNode: Added PlanningPhaseManager service to {DebugDisplayName}");
    }

    public override string DebugDisplayName { get; protected set; } = "DynamicFlowNode";

    public DynamicFlowNode(
        FastName nodeName,
        IBehaviorTree owningTree,
        SuccessCriteria successCriteria = SuccessCriteria.ALL,
        float threshold = 1.0f,
        bool addLeafDecorators = true)
        : base(nodeName, successCriteria, threshold)
    {
        this.OwningTree = owningTree;
        DebugDisplayName = nodeName.ToString();

        // Track this flow node
        LoggingService.TrackNodeStart(nodeName.ToString(), "DynamicFlowNode", System.DateTime.Now);

        // Track flow node initialization (unified as "FlowNode")
        BehaviorTreeComponentLogger.TrackFlowNodeInitialization("FlowNode");

        if (addLeafDecorators)
        {
            // Automatically add PlanningComplete decorator to dynamic flow nodes
            AddDecorator(new DecoratorPlanningComplete());
            LoggingService.LogInfo($"🔧 DynamicFlowNode: Added PlanningComplete decorator to {nodeName.ToString()}");

            // Add RetryOnFailure decorator: when all children finish but success criteria not met,
            // this post-processing decorator resets failed children and converts Failure → InProgress
            AddDecorator(new DecoratorRetryOnFailure(this));
            LoggingService.LogInfo($"🔧 DynamicFlowNode: Added RetryOnFailure decorator to {nodeName.ToString()}");
        }
        
    }

    public override IBTNode AddChild(IBTNode childNode)
    {
        base.AddChild(childNode);

        if (childNode is FlowNode flowNode)
            flowGraph.AddNode(flowNode);
        else if (childNode is PActionNode actionNode)
            actionGraph.AddNode(actionNode);

        return childNode;
    }

    public override List<IBTNode> GetChildren()
    {
        return IsAllFlow
            ? flowGraph.GetAllNodes().Cast<IBTNode>().ToList()
            : base.GetChildren();
    }

    /// <summary>
    /// this function creates a plan with the planner and adds the action nodes to the graph
    /// </summary>
    /// <returns></returns>
    public override IEnumerator<IBTNode> GetEnumerator()
    {
        return GetChildren().GetEnumerator();
    }

    protected override bool OnTick_NodeLogic(float inDeltaTime)
    {
        if (IsAllFlow)
        {
            var children = flowGraph.GetAllNodes();
            int successCount = children.Count(child => child.status == BTNodeResult.Success);
            int failedCount = children.Count(child => child.status == BTNodeResult.Failure);
            bool anyInProgress = children.Any(child => !child.HasFinished);

            bool criteriaMet = successCriteria switch
            {
                SuccessCriteria.ALL => successCount == children.Count,
                SuccessCriteria.ANY => successCount > 0,
                SuccessCriteria.COUNT => successCount >= (int)successThreshold,
                SuccessCriteria.PERCENTAGE => successCount >= children.Count * successThreshold,
                _ => false
            };

            if (criteriaMet)
            {
                status = BTNodeResult.Success;
                return true;
            }

            if (anyInProgress)
            {
                status = BTNodeResult.InProgress;
                return true;
            }

            foreach (var failedChild in children.Where(child => child.status == BTNodeResult.Failure))
                failedChild.Reset();

            LoggingService.LogInfo($"DynamicFlowNode[AllFlow]: Retrying {failedCount} failed children because criteria were not met");
            status = BTNodeResult.InProgress;
            return true;
        }

        LoggingService.LogInfo($"🚨 DEBUG: DynamicFlowNode.OnTick_NodeLogic called for {DebugDisplayName}");
        LoggingService.LogInfo($"🔍 FlowNode: Current LastStatus: {status}");
        LoggingService.LogInfo($"🔍 FlowNode: HasChildren: {HasChildren}");

        // Track child count for average branching factor calculation
        var childCount = actionGraph.GetAllActionNodes().Count;

        // Check if success criteria is met based on children's status
        bool successCriteriaMet = EvaluateSuccessCriteria();
        
        if (successCriteriaMet)
        {
            // All children completed successfully according to success criteria
            LoggingService.LogInfo($"   ✅ FlowNode: Success criteria met, setting status to Success");
            status = BTNodeResult.Success;
            LoggingService.TrackNodeCompletion(DebugDisplayName, System.DateTime.Now, true);
            return true; // Return true so parent can detect completion
        }
        else
        {
            // Success criteria not met - check if all children are done executing
            var allNodes = actionGraph.GetAllActionNodes();
            bool anyInProgress = allNodes.Any(node => node.status == BTNodeResult.InProgress || 
                                                       node.status == BTNodeResult.ReadyToTick);
            
            if (anyInProgress)
            {
                // Children still executing - keep waiting
                LoggingService.LogInfo($"   🔄 FlowNode: Success criteria not met yet, children still executing, keeping InProgress");
                status = BTNodeResult.InProgress;
                return true; // Continue ticking
            }
            else
            {
                // All children finished (succeeded or failed) but success criteria not met - this is a failure
                LoggingService.LogError($"   ❌ FlowNode: All children finished but success criteria not met, setting status to Failure");
                status = BTNodeResult.Failure;
                LoggingService.TrackNodeCompletion(DebugDisplayName, System.DateTime.Now, false);
                return false; // Return false to stop execution
            }
        }
    }



    /// <summary>
    /// Get the action graph for debugging and monitoring purposes
    /// </summary>
    public NodeGraph GetActionGraph()
    {
        return actionGraph;
    }

    public override void Reset()
    {
        bool wasUninitialized = (status == BTNodeResult.Uninitialized);
        base.Reset();

        // Don't reset planning service during initialization if other planning is in progress
        if (wasUninitialized)
        {
            LoggingService.LogInfo($"🔄 FlowNode: Initialization reset - preserving existing planning services");
            LoggingService.LogInfo($"🔄 FlowNode: Node {DebugDisplayName} was uninitialized, checking if planning is already completed");

            // Check if planning has already been completed by checking if NodeGraph exists
            if (ServicePlanning is ServicePlanning initPlannerService && initPlannerService.GetGeneratedNodeGraph() != null)
            {
                LoggingService.LogInfo($"🔄 FlowNode: Planning already completed (NodeGraph exists), setting planningCompleted = true");
                planningCompleted = true;
            }
            else
            {
                LoggingService.LogInfo($"🔄 FlowNode: Planning not completed yet (no NodeGraph), keeping planningCompleted = false");
                planningCompleted = false;
            }

            return;
        }

        // Only reset planning if it hasn't completed yet (success or failure)
        if (ServicePlanning is ServicePlanning plannerService)
        {
            // If planning has already completed (success or failure), don't reset it
            if (plannerService.HasCompleted)
            {
                if (plannerService.HasPlanningSucceeded())
                {
                    LoggingService.LogInfo($"🔄 FlowNode: Planning already completed successfully, preserving NodeGraph (HashCode: {plannerService.GetGeneratedNodeGraph()?.GetHashCode()})");
                    // Keep the planning completed flag true to preserve the NodeGraph
                    planningCompleted = true;
                }
                else
                {
                    LoggingService.LogInfo($"🔄 FlowNode: Planning already completed and failed, preserving failure state - {plannerService.LastError}");
                    // Keep the planning completed flag false to maintain failure state
                    planningCompleted = false;
                }
            }
            else
            {
                LoggingService.LogInfo($"🔄 FlowNode: Resetting planning service (not completed yet)");
                // Clear action graph BEFORE ResetPlanningService to get accurate node count for PRR
                ClearActionGraph();
                plannerService.ResetPlanningService();
                planningCompleted = false;
            }
        }
        else
        {
            planningCompleted = false;
            // Clear the action graph when no planning service
            ClearActionGraph();
        }
    }

    /// <summary>
    /// Reset the flow node for the next round of execution
    /// This preserves the planning service but resets the execution state
    /// </summary>
    public void ResetForNextRound()
    {
        LoggingService.LogInfo($"🔄 FlowNode: ResetForNextRound called for {DebugDisplayName}");
        
        // NEW: Reset the parent action if it exists
        var parentAction = GetParentAction();
        if (parentAction != null)
        {
            LoggingService.LogInfo($"🔄 FlowNode: Resetting parent action {parentAction.InstanceName.ToString()}");
            parentAction.Reset();
            LoggingService.LogInfo($"🔄 FlowNode: Parent action reset complete - LastStatus: {parentAction.status}");
        }
        else
        {
            LoggingService.LogWarning($"⚠️ FlowNode: No parent action found to reset");
        }
        
        // Reset the planning completed flag to allow new planning
        planningCompleted = false;
        
        // Reset the node status to allow it to continue ticking
        status = BTNodeResult.ReadyToTick;
        
        // Reset the tick counter for the next round
        tickCount = 0;
        LoggingService.LogInfo($"🔄 FlowNode: Reset tick counter to 0 for next round");
        
        // Clear the action graph to allow new plans to be set
        if (actionGraph != null)
        {
            LoggingService.LogInfo($"🔄 FlowNode: Clearing action graph for next round");
            ClearActionGraph();
        }
        else
        {
            LoggingService.LogInfo($"🔄 FlowNode: No action graph found");
            
        }
        
        LoggingService.LogInfo($"🔄 FlowNode: Reset completed - ready for next round of execution");
    }

    protected override bool OnTick_Children(float inDeltaTime)
    {
        if (IsAllFlow)
            return OnTickFlowChildren(inDeltaTime);

        // Increment tick counter
        tickCount++;
        LoggingService.LogInfo($"🚨 DEBUG: DynamicFlowNode.OnTick_Children called for {DebugDisplayName} - Tick #{tickCount}");
        LoggingService.LogInfo($"🔍 FlowNode: Planning completed: {planningCompleted}");
        LoggingService.LogInfo($"🔍 FlowNode: ActionGraph exists: {actionGraph != null}");
        LoggingService.LogInfo($"🔍 FlowNode: Tick progress: {tickCount}/{MAX_TICKS_BEFORE_FAILURE} ({(MAX_TICKS_BEFORE_FAILURE - tickCount)} attempts remaining)");

        // Get current executable nodes from NodeGraph based on order relations and temporal constraints
        var executableNodes = actionGraph.GetExecutableNodes(inDeltaTime);
        LoggingService.LogInfo($"   📊 FlowNode: Found {executableNodes.Count} executable nodes");

        if (executableNodes.Count == 0)
        {
            // No nodes are executable at this time - just wait
            LoggingService.LogInfo($"   ⏳ FlowNode: No executable nodes at this time (waiting for predecessors)");
            return true; // Continue ticking
        }

        // Reset unsuccessful children to readyToTick for next round
        ResetUnsuccessfulChildrenForNextRound();

        // Execute each executable node
        LoggingService.LogInfo($"   🔍 DEBUG: Starting to execute {executableNodes.Count} executable nodes");
        
        foreach (var node in executableNodes)
        {
            LoggingService.LogInfo($"   ⚡ Executing node: {node.InstanceName.ToString()}");
            LoggingService.LogInfo($"   🔍 Node status before tick: {node.status}");

            // Mark node as started if it's the first time executing
            if (node.status == BTNodeResult.ReadyToTick)
            {
                actionGraph.MarkNodeStarted(node);
                LoggingService.LogInfo($"   🚀 Marked {node.InstanceName.ToString()} as started");
            }

            var previousStatus = node.status;
            node.Tick(inDeltaTime);
            LoggingService.LogInfo($"   📊 Node {node.InstanceName.ToString()}: {previousStatus} → {node.status}");

            // Mark completed nodes in the graph
            if (node.status == BTNodeResult.Success)
            {
                actionGraph.MarkNodeCompleted(node);
                LoggingService.LogInfo($"   ✅ Marked {node.InstanceName.ToString()} as completed in graph");
                LoggingService.TrackNodeCompletion(node.InstanceName.ToString(), System.DateTime.Now, true);
            }
            else if (node.status == BTNodeResult.Failure)
            {
                LoggingService.LogError($"   ❌ Node {node.InstanceName.ToString()} failed");
                LoggingService.TrackNodeCompletion(node.InstanceName.ToString(), System.DateTime.Now, false);
            }
            else
            {
                LoggingService.LogInfo($"   ⏳ Node {node.InstanceName.ToString()} still in progress");
            }
        }
        
        LoggingService.LogInfo($"   🔍 DEBUG: Finished executing {executableNodes.Count} executable nodes");

        // Continue ticking - OnTick_NodeLogic will evaluate success criteria
        return true;
    }

    public List<FlowNode> GetExecutableFlowNodes()
    {
        return flowGraph.GetExecutableNodes();
    }

    private bool OnTickFlowChildren(float inDeltaTime)
    {
        tickCount++;
        var allChildren = flowGraph.GetAllNodes();
        if (allChildren.Count == 0)
            return false;

        var executableNodes = flowGraph.GetExecutableNodes();
        if (executableNodes.Count == 0)
            return true;

        int deprioritizedIndex = LinkedBlackboard.DeprioritizedBranchIndex;
        var orderedNodes = executableNodes
            .Where(node => allChildren.IndexOf(node) != deprioritizedIndex)
            .ToList();

        if (deprioritizedIndex >= 0 && deprioritizedIndex < allChildren.Count)
        {
            var deprioritizedNode = allChildren[deprioritizedIndex];
            if (executableNodes.Contains(deprioritizedNode))
                orderedNodes.Add(deprioritizedNode);
        }

        foreach (var child in orderedNodes)
        {
            if (child.status == BTNodeResult.ReadyToTick)
                flowGraph.MarkNodeStarted(child);

            child.Tick(inDeltaTime);

            if (child.status == BTNodeResult.Success)
                flowGraph.MarkNodeCompleted(child);
        }

        return true;
    }
    
    // New method to get executable actions
    public List<PActionNode> GetExecutableActions()
    {
        var executableActions = new List<PActionNode>();
        
        // Get actions from NodeGraph that are ready to execute
        var readyActions = actionGraph.GetExecutableNodes(0.0f);
        
        foreach (var action in readyActions)
        {
            LoggingService.LogInfo($"🔍 FlowNode: Adding executable action: {action.InstanceName} (Cost: {action.cost})");
            executableActions.Add(action);
        }
        
        return executableActions;
    }
    
    // Method to get all actions (executable and non-executable) for monitoring
    public List<PActionNode> GetAllActions()
    {
        if (!planningCompleted || actionGraph == null)
        {
            return new List<PActionNode>();
        }
        
        return actionGraph.GetAllActionNodes();
    }
    
    // Method to check if this flow node has any executable actions
    public bool HasExecutableActions()
    {
        return GetExecutableActions().Count > 0;
    }
    
    // Method to get subtree status for agent monitoring
    public string GetSubtreeStatus()
    {
        if (!planningCompleted)
        {
            return "Planning";
        }
        
        var allActions = GetAllActions();
        if (allActions.Count == 0)
        {
            return "No Actions";
        }
		
        var completedActions = allActions.Count(a => a.status == BTNodeResult.Success);
        var failedActions = allActions.Count(a => a.status == BTNodeResult.Failure);
        var inProgressActions = allActions.Count(a => a.status == BTNodeResult.InProgress);
        var readyActions = allActions.Count(a => a.status == BTNodeResult.ReadyToTick);
        
        return $"Completed: {completedActions}, Failed: {failedActions}, InProgress: {inProgressActions}, Ready: {readyActions}";
    }

    /// <summary>
    /// Find the parent GenericBTAction that owns this subtree
    /// </summary>
    /// <returns>The parent action or null if not found</returns>
    public PActionNode? GetParentAction()
    {
        var current = ParentNode;
        
        // Traverse up the parent chain to find the GenericBTAction
        while (current != null)
        {
            if (current is PActionNode action)
            {
                LoggingService.LogInfo($"🔍 FlowNode: Found parent action: {action.InstanceName.ToString()}");
                return action;
            }
            
            // Move up to the next parent
            current = current.ParentNode;
        }
        
        LoggingService.LogWarning($"⚠️ FlowNode: No parent GenericBTAction found for subtree {DebugDisplayName}");
        return null;
    }

    /// <summary>
    /// Reset only unsuccessful children to readyToTick for the next round
    /// This is a minimal change that doesn't affect global cassette state
    /// </summary>
    private void ResetUnsuccessfulChildrenForNextRound()
    {
        if (actionGraph == null) return;
        
        var allNodes = actionGraph.GetAllActionNodes();
        int resetCount = 0;
        
        foreach (var node in allNodes)
        {
            // Check if this is a GenericBTAction and get its current status
            if (node is PActionNode action)
            {
                var currentStatus = action.GetCurrentStatus();
                
                // Only reset failed nodes, leave successful ones alone
                if (currentStatus == BTNodeResult.Failure)
                {
                    action.Reset();
                    resetCount++;
                    LoggingService.LogInfo($"   🔄 FlowNode: Reset failed node {node.InstanceName} to readyToTick for next round");
                }
                else
                {
                    LoggingService.LogInfo($"   ✅ FlowNode: Keeping successful node {node.InstanceName} with status {currentStatus}");
                }
            }
        }
        
        if (resetCount > 0)
        {
            LoggingService.LogInfo($"   🔄 FlowNode: Reset {resetCount} failed children to readyToTick for next round");
        }
        else
        {
            LoggingService.LogInfo($"   ✅ FlowNode: No failed children to reset - all nodes succeeded");
        }
    }
}