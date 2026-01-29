using BehaviorTreeMainProject;
using BehaviorTreeMainProject.Services;
using System.Collections.Generic;
using System.Linq;
using BehaviorTreeMainProject.Log.Services;

public class BTFlowNode_Dynamic : BTFlowNodeBase
{

    public override string TypeName => "BTFlowNode_Dynamic";
    private bool planningCompleted = false;

    // Track if we've completed the first planning cycle
    private bool firstPlanningCycleCompleted = false;

    // Track which executable nodes have been processed
    private HashSet<string> processedExecutableNodes = new HashSet<string>();

    // NEW: Track tick count to allow multiple rounds before failing
    private int tickCount = 0;
    private const int MAX_TICKS_BEFORE_FAILURE = 10;
    private bool hasCompletedFirstRound = false;

    public override string DebugDisplayName { get; protected set; } = "DynamicFlowNode";

    public BTFlowNode_Dynamic(
        FastName nodeName,
        IBehaviorTree owningTree,
        SuccessCriteria successCriteria = SuccessCriteria.ALL,
        float threshold = 1.0f,
        bool addLowestCostDecorator = false)  // New parameter to control decorator addition
        : base(nodeName, successCriteria, threshold)
    {
        this.OwningTree = owningTree;
        DebugDisplayName = nodeName.ToString();

        // Track this flow node
        LoggingService.TrackNodeStart(nodeName.ToString(), "BTFlowNode_Dynamic", System.DateTime.Now);

        // Track flow node initialization
        BehaviorTreeComponentLogger.TrackFlowNodeInitialization(this.GetType().Name);

        // Automatically add PlanningComplete decorator to dynamic flow nodes
        AddDecorator(new BTDecorator_PlanningComplete());
        LoggingService.LogInfo($"🔧 BTFlowNode_Dynamic: Added PlanningComplete decorator to {nodeName.ToString()}");
        
    }

    /// <summary>
    /// this function creates a plan with the planner and adds the action nodes to the graph
    /// </summary>
    /// <returns></returns>
    public override IEnumerator<IBTNode> GetEnumerator()
    {
        // Console.WriteLine($"   🔍 FlowNode: GetEnumerator called - Current actionGraph has {actionGraph.GetAllActionNodes().Count} nodes");

        // Return the current actionGraph nodes
        return actionGraph.GetAllActionNodes().Cast<IBTNode>().GetEnumerator();
    }

    protected override bool OnTick_NodeLogic(float inDeltaTime)
    {
        LoggingService.LogInfo($"🚨 DEBUG: BTFlowNode_Dynamic.OnTick_NodeLogic called for {DebugDisplayName}");
        LoggingService.LogInfo($"🔍 FlowNode: Current LastStatus: {status}");
        LoggingService.LogInfo($"🔍 FlowNode: HasChildren: {HasChildren}");

        // Track child count for average branching factor calculation
        var childCount = actionGraph.GetAllActionNodes().Count;

        // Decorators handle all planning phase logic (PlanningPhase and PlanningPhaseDynamic flags)
        // This method only needs to set status for execution phase
        LoggingService.LogInfo($"   📋 FlowNode: Decorators handle planning phase, setting InProgress for execution");
        status = BTNodeResult.InProgress;
        LoggingService.LogInfo($"   🔄 FlowNode: Setting status to InProgress (OnTick_Children will handle execution)");
        LoggingService.LogInfo($"   🔄 FlowNode: Returning true to continue ticking (OnTick_Children will handle execution)");

        // Return true to continue ticking (let OnTick_Children handle the actual execution and final status)
        return true;
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
            if (PlanningService is BTServicePlanner initPlannerService && initPlannerService.GetGeneratedNodeGraph() != null)
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
        if (PlanningService is BTServicePlanner plannerService)
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
                plannerService.ResetPlanningService();
                planningCompleted = false;
                // Clear the action graph when resetting planning
                ClearActionGraph();
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
        status = BTNodeResult.readyToTick;
        
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
        // Increment tick counter
        tickCount++;
        LoggingService.LogInfo($"🚨 DEBUG: BTFlowNode_Dynamic.OnTick_Children called for {DebugDisplayName} - Tick #{tickCount}");
        LoggingService.LogInfo($"🔍 FlowNode: Planning completed: {planningCompleted}");
        LoggingService.LogInfo($"🔍 FlowNode: ActionGraph exists: {actionGraph != null}");
        LoggingService.LogInfo($"🔍 FlowNode: Tick progress: {tickCount}/{MAX_TICKS_BEFORE_FAILURE} ({(MAX_TICKS_BEFORE_FAILURE - tickCount)} attempts remaining)");

       

        // Step 1: Get current executable nodes from NodeGraph based on order relations and temporal constraints
        var executableNodes = actionGraph.GetExecutableNodes(inDeltaTime);

        LoggingService.LogInfo($"   📊 FlowNode: Found {executableNodes.Count} executable nodes");

        if (executableNodes.Count == 0)
        {
            // No nodes are executable at this time, but we're still in progress
            LoggingService.LogInfo($"   ⏳ FlowNode: No executable nodes at this time");
            return true; // Continue ticking
        }
          // NEW: Reset unsuccessful children to readyToTick for next round
            ResetUnsuccessfulChildrenForNextRound();

        LoggingService.LogInfo($"   🔍 FlowNode: Found {executableNodes.Count} executable nodes");

        // Step 2: Execute each executable node (only current ones, no dynamic updates within this tick)
        LoggingService.LogInfo($"   🔍 DEBUG: Starting to execute {executableNodes.Count} executable nodes in parallel");
        
        foreach (var node in executableNodes)
        {
            LoggingService.LogInfo($"   ⚡ Executing node: {node.InstanceName.ToString()}");
            LoggingService.LogInfo($"   🔍 Node type: {node.GetType().Name}");
            LoggingService.LogInfo($"   🔍 Node status before tick: {node.status}");
            LoggingService.LogInfo($"   🔍 Node cost: {node.cost}");

            // Mark node as started if it's the first time executing
            if (node.status == BTNodeResult.readyToTick)
            {
                actionGraph.MarkNodeStarted(node);
                LoggingService.LogInfo($"   🚀 Marked {node.InstanceName.ToString()} as started");
            }

            var previousStatus = node.status;
            LoggingService.LogInfo($"   🔄 Calling node.Tick() for {node.InstanceName.ToString()}");
            LoggingService.LogInfo($"   🔄 About to tick node with status: {previousStatus}");
            node.Tick(inDeltaTime);
            LoggingService.LogInfo($"   📊 Node {node.InstanceName.ToString()}: {previousStatus} → {node.status}");
            LoggingService.LogInfo($"   📊 Node tick result: {node.status}");

            // MODIFIED: Fail immediately if any child fails
            if (node.status == BTNodeResult.failed)
            {
                LoggingService.LogError($"   ❌ FlowNode: Action {node.InstanceName.ToString()} failed, failing immediately");
                status = BTNodeResult.failed;
                
                // Track completion of this flow node
                LoggingService.TrackNodeCompletion(DebugDisplayName, System.DateTime.Now, false);
                LoggingService.LogInfo($"   📊 FlowNode: Final status set to {status}");
                
                
                // Return false to stop the parent from ticking this node again
                LoggingService.LogInfo($"   🔄 FlowNode: OnTick_Children failed, returning false to stop ticking");
                return false; // Stop ticking this node - it has failed
            }

            // Mark completed nodes and track completion
            if (node.status == BTNodeResult.Succeeded)
            {
                actionGraph.MarkNodeCompleted(node);
                LoggingService.LogInfo($"   ✅ Marked {node.InstanceName.ToString()} as completed");
                LoggingService.LogInfo($"   ✅ DEBUG: Node {node.InstanceName.ToString()} is now COMPLETED with status: {node.status}");

                // Track node completion
                LoggingService.TrackNodeCompletion(node.InstanceName.ToString(), System.DateTime.Now, true);
            }
            else
            {
                LoggingService.LogInfo($"   ⏳ Node {node.InstanceName.ToString()} not completed yet (status: {node.status})");
                LoggingService.LogInfo($"   ⏳ DEBUG: Node {node.InstanceName.ToString()} is still IN PROGRESS - will continue ticking");
            }
        }
        
        LoggingService.LogInfo($"   🔍 DEBUG: Finished executing all {executableNodes.Count} executable nodes");

        // Step 3: Check if all nodes are completed
        var allNodes = actionGraph.GetAllActionNodes();
        bool allNodesProcessed = allNodes.All(node =>
            node.status == BTNodeResult.Succeeded); // MODIFIED: Only succeeded nodes, failed nodes cause immediate failure

        LoggingService.LogInfo($"   🔍 FlowNode: All nodes processed: {allNodesProcessed}");
        LoggingService.LogInfo($"   🔍 DEBUG: Total nodes in ActionGraph: {allNodes.Count}");
        
        // Debug: Show status of all nodes
        LoggingService.LogInfo($"   🔍 DEBUG: Status of all nodes:");
        foreach (var node in allNodes)
        {
            LoggingService.LogInfo($"      - {node.InstanceName}: {node.status}");
        }

        if (allNodesProcessed && allNodes.Count > 0)
        {
            // MODIFIED: Since we fail immediately on any failure, if we reach here all nodes must have succeeded
            LoggingService.LogInfo($"   🎯 FlowNode: All nodes processed successfully, setting status to Succeeded");
            status = BTNodeResult.Succeeded;
            LoggingService.LogSuccess($"   ✅ FlowNode: Setting status to Succeeded (all nodes completed successfully)");

            // Track completion of this flow node
            LoggingService.TrackNodeCompletion(DebugDisplayName, System.DateTime.Now, true);
            LoggingService.LogInfo($"   📊 FlowNode: Final status set to {status}");


            // Return true to continue ticking so the dynamic planning phase manager can detect completion
            LoggingService.LogInfo($"   🔄 FlowNode: OnTick_Children completed with final status {status}, returning true to allow manager to detect completion");
            return true; // Continue ticking so manager can detect completion and trigger reset
        }
        else
        {
            // Still processing nodes - keep in progress
            status = BTNodeResult.InProgress;
            LoggingService.LogInfo($"   🔄 FlowNode: Still processing nodes, keeping status as InProgress (tick #{tickCount})");
            LoggingService.LogInfo($"   🔄 DEBUG: FlowNode will continue ticking because not all nodes are processed yet");
        }

        // FIXED: Only return true if we're still processing nodes
        // This ensures the parent node continues to tick this flow node until completion
        LoggingService.LogInfo($"   🔄 FlowNode: OnTick_Children completed, returning true to continue ticking");
        LoggingService.LogInfo($"   🔄 DEBUG: FlowNode returning TRUE - will be ticked again by parent");
        return true; // Continue ticking children until all nodes are completed
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
        
        var completedActions = allActions.Count(a => a.status == BTNodeResult.Succeeded);
        var failedActions = allActions.Count(a => a.status == BTNodeResult.failed);
        var inProgressActions = allActions.Count(a => a.status == BTNodeResult.InProgress);
        var readyActions = allActions.Count(a => a.status == BTNodeResult.readyToTick);
        
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
                if (currentStatus == BTNodeResult.failed)
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