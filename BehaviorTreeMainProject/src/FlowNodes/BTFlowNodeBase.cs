using System.Collections;
using System.ComponentModel.DataAnnotations;
using BehaviorTreeMainProject.Services;
using BehaviorTreeMainProject.Log.Services;

public abstract class BTFlowNodeBase : BTNodeBase, IEnumerable
{
    // is this node allowed to have children?
    public override bool HasChildren => true;
  
    // public override string DebugDisplayName { get; protected set; } = "FlowNode";
    public SuccessCriteria successCriteria { get; protected set; }
    // needed if success criteria is count or percentage
    protected float successThreshold;
    // childResults is no longer needed since we use actionGraph for evaluation

    // Replace simple list with node graph structure
    protected NodeGraph actionGraph = new();

    // Property to check if NodeGraph is locked (has been set and cannot be replaced)
    public bool IsNodeGraphLocked => actionGraph != null;

    int maxCount = 0;
    int currentCount = 0;

    // Planning service for high-level actions
    public BTServicePlanner PlanningService { get; protected set; }
    private readonly IBehaviorTree owningTree;

    // Node name property
    public FastName InstanceName { get; protected set; }

    public abstract IEnumerator<IBTNode> GetEnumerator();

    // Explicit implementation for non-generic IEnumerable
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }



    public BTFlowNodeBase(FastName nodeName, SuccessCriteria criteria = SuccessCriteria.ALL, float threshold = 1.0f)
    {
        this.InstanceName = nodeName;
        this.DebugDisplayName = nodeName.ToString();
        this.successCriteria = criteria;
        this.successThreshold = threshold;
    }
    /// <summary>
    /// Evaluates the ALL success criteria - continues ticking remaining children even if first child failed
    /// </summary>
    /// <returns>True if all actions succeeded, false if any action failed</returns>
    protected bool EvaluateAllSuccessCriteria()
    {
        var actionNodes = actionGraph.GetAllActionNodes();
        if (actionNodes.Count == 0) return false;

        // Check if all actions have succeeded
        var succeededActions = actionNodes.Where(node => node.status == BTNodeResult.Succeeded).ToList();
        var failedActions = actionNodes.Where(node => node.status == BTNodeResult.failed).ToList();
        var inProgressActions = actionNodes.Where(node => node.status != BTNodeResult.Succeeded && node.status != BTNodeResult.failed).ToList();

        LoggingService.LogInfo($"🔍 FlowNode: ALL criteria check - Total: {actionNodes.Count}, Succeeded: {succeededActions.Count}, InProgress: {inProgressActions.Count}, Failed: {failedActions.Count}");

        // If some actions are still in progress, we can't make a final decision yet - continue ticking
        if (inProgressActions.Any())
        {
            LoggingService.LogInfo($"🔍 FlowNode: ALL criteria - {inProgressActions.Count} actions still in progress, waiting for completion");
            return false;
        }

        // All actions have completed, now evaluate final result
        if (succeededActions.Count == actionNodes.Count)
        {
            LoggingService.LogSuccess($"✅ FlowNode: ALL success criteria met - all {actionNodes.Count} actions succeeded");
            return true;
        }
        else if (failedActions.Any())
        {
            LoggingService.LogWarning($"❌ FlowNode: ALL success criteria failed - {failedActions.Count} actions failed:");
            foreach (var failedAction in failedActions)
            {
                LoggingService.LogWarning($"   ❌ Failed action: {failedAction.InstanceName.ToString()}");
            }
            return false;
        }

        // This should not happen, but just in case
        LoggingService.LogWarning($"⚠️ FlowNode: ALL criteria - unexpected state, returning false");
        return false;
    }

    /// <summary>
    /// this function evaluates the success criteria to see if a flow node is successful or not
    /// </summary>
    /// <returns></returns>
    protected bool EvaluateSuccessCriteria()
    {
        // For ALL criteria, use the specialized function that fails immediately on any failure
        if (successCriteria == SuccessCriteria.ALL)
        {
            return EvaluateAllSuccessCriteria();
        }

        var actionNodes = actionGraph.GetAllActionNodes();
        if (actionNodes.Count == 0) return false;

        int successCount = actionNodes.Count(node => node.status == BTNodeResult.Succeeded);
        int failedCount = actionNodes.Count(node => node.status == BTNodeResult.failed);
        int totalCount = actionNodes.Count;
        int inProgressCount = totalCount - successCount - failedCount;

        LoggingService.LogInfo($"🔍 FlowNode: Success criteria evaluation - Total: {totalCount}, Succeeded: {successCount}, Failed: {failedCount}, InProgress: {inProgressCount}");

        // If any actions are still in progress, we can't make a final decision yet
        if (inProgressCount > 0)
        {
            LoggingService.LogInfo($"🔍 FlowNode: {inProgressCount} actions still in progress, cannot evaluate final success yet");
            return false;
        }

        // All actions have completed (either succeeded or failed), now evaluate based on criteria
        var result = successCriteria switch
        {
            SuccessCriteria.ALL => successCount == totalCount, // All must succeed, any failure means overall failure
            SuccessCriteria.ANY => successCount > 0, // At least one must succeed
            SuccessCriteria.COUNT => successCount >= (int)successThreshold, // Must have at least threshold number of successes
            SuccessCriteria.PERCENTAGE => successCount >= (totalCount * successThreshold), // Must have at least threshold percentage of successes
            _ => false
        };
        
        // Track flow node execution for execution summary
        ExecutionSummaryLogger.TrackFlowNode(DebugDisplayName, GetType().Name, result);
        
        return result;
    }

    /// <summary>
    /// Creates a NodeGraph from a list of action nodes with default relations
    /// Default: MEETS temporal constraint and sequential order (left-to-right)
    /// </summary>
    /// <param name="actionNodes">List of action nodes to add to the graph</param>
    /// <returns>The created NodeGraph</returns>
    public NodeGraph CreateNodeGraphFromActions(List<PActionNode> actionNodes)
    {
        var graph = new NodeGraph();

        Console.WriteLine($"🔧 CreateNodeGraphFromActions: Input actionNodes count: {actionNodes?.Count ?? 0}");

        if (actionNodes == null || actionNodes.Count == 0)
        {
            Console.WriteLine("🔧 CreateNodeGraphFromActions: No action nodes provided, returning empty graph");
            return graph;
        }

        // Add all action nodes to the graph
        foreach (var action in actionNodes)
        {
            Console.WriteLine($"🔧 CreateNodeGraphFromActions: Adding action {action.InstanceName.ToString()} to graph");
            graph.AddNode(action);
        }

        Console.WriteLine($"🔧 CreateNodeGraphFromActions: Added {actionNodes.Count} nodes to graph");

        // Create default relations: sequential order with MEETS temporal constraint
        for (int i = 0; i < actionNodes.Count - 1; i++)
        {
            var currentAction = actionNodes[i];
            var nextAction = actionNodes[i + 1];

            Console.WriteLine($"🔧 CreateNodeGraphFromActions: Creating relation {currentAction.InstanceName.ToString()} → {nextAction.InstanceName.ToString()}");

            // Add order relation (sequential execution)
            graph.AddOrderRelation(currentAction, nextAction);

            // Add temporal constraint (MEETS - next action starts when current ends)
            graph.AddTemporalConstraint(currentAction, nextAction, TemporalConstraint.MEETS);
        }

        Console.WriteLine($"🔧 CreateNodeGraphFromActions: Created {actionNodes.Count - 1} relations");
        Console.WriteLine($"🔧 CreateNodeGraphFromActions: Final graph has {graph.GetAllActionNodes().Count} nodes");

        // Debug: Show final graph structure
        Console.WriteLine("\n🔍 DEBUG: Final Graph Structure:");
        var allNodes = graph.GetAllActionNodes();
        for (int i = 0; i < allNodes.Count; i++)
        {
            var node = allNodes[i];
            Console.WriteLine($"   Node {i}: {node.InstanceName.ToString()}");
        }

        return graph;
    }

    /// <summary>
    /// Creates a NodeGraph from a list of action nodes with custom relations
    /// </summary>
    /// <param name="actionNodes">List of action nodes to add to the graph</param>
    /// <param name="useOrderRelations">Whether to create sequential order relations</param>
    /// <param name="defaultTemporalConstraint">Default temporal constraint between consecutive actions</param>
    /// <returns>The created NodeGraph</returns>
    protected NodeGraph CreateNodeGraphFromActions(List<PActionNode> actionNodes, bool useOrderRelations, TemporalConstraint defaultTemporalConstraint)
    {
        var graph = new NodeGraph();

        if (actionNodes == null || actionNodes.Count == 1)
            return graph;

        // Add all action nodes to the graph
        foreach (var action in actionNodes)
        {
            graph.AddNode(action);
        }

        // Create relations based on parameters
        for (int i = 0; i < actionNodes.Count - 1; i++)
        {
            var currentAction = actionNodes[i];
            var nextAction = actionNodes[i + 1];

            // Add order relation if requested
            if (useOrderRelations)
            {
                graph.AddOrderRelation(currentAction, nextAction);
            }

            // Add temporal constraint
            graph.AddTemporalConstraint(currentAction, nextAction, defaultTemporalConstraint);
        }

        return graph;
    }

    /// <summary>
    /// Sets the action graph for this flow node
    /// </summary>
    /// <param name="graph">The NodeGraph to use</param>
    public void SetActionGraph(NodeGraph graph)
    {
        LoggingService.LogInfo($"🔧 BTFlowNodeBase: SetActionGraph called for {DebugDisplayName} - New NodeGraph HashCode: {graph?.GetHashCode()}");
        LoggingService.LogInfo($"🔧 BTFlowNodeBase: New NodeGraph has {graph?.GetAllActionNodes().Count ?? 0} actions");

        // Simplified tracking - child count tracking removed

        // Prevent NodeGraph replacement once it's been set, UNLESS the new graph has actions and current is empty
        if (actionGraph != null)
        {
            int currentActionCount = actionGraph.GetAllActionNodes().Count;
            int newActionCount = graph?.GetAllActionNodes().Count ?? 0;

            LoggingService.LogInfo($"🔧 BTFlowNodeBase: Current NodeGraph has {currentActionCount} actions");
            LoggingService.LogInfo($"🔧 BTFlowNodeBase: New NodeGraph has {newActionCount} actions");

            // Allow replacement if current graph is empty and new graph has actions
            if (currentActionCount == 0 && newActionCount > 0)
            {
                LoggingService.LogInfo($"🔧 BTFlowNodeBase: Allowing replacement - current graph is empty, new graph has {newActionCount} actions");
                LoggingService.LogInfo($"🔧 BTFlowNodeBase: Replacing empty NodeGraph (HashCode: {actionGraph.GetHashCode()}) with populated NodeGraph (HashCode: {graph?.GetHashCode()})");
                actionGraph = graph;
                return;
            }

            // Prevent replacement if current graph has actions (to preserve completion statuses)
            if (currentActionCount > 0)
            {
                LoggingService.LogWarning($"🔒 BTFlowNodeBase: NodeGraph already set with {currentActionCount} actions (HashCode: {actionGraph.GetHashCode()}), preventing replacement with new NodeGraph (HashCode: {graph?.GetHashCode()})");
                LoggingService.LogWarning($"🔒 BTFlowNodeBase: This prevents loss of completion statuses. New NodeGraph will be ignored.");
                return; // Don't replace the existing NodeGraph
            }
        }
        else
        {
            LoggingService.LogInfo($"🔧 BTFlowNodeBase: No existing NodeGraph, setting initial NodeGraph");
        }

        LoggingService.LogInfo($"🔧 BTFlowNodeBase: Setting initial NodeGraph (HashCode: {graph?.GetHashCode()})");
        actionGraph = graph;
        
        // Note: Action node final count is now tracked in BTServicePlanner after all actions are set up
    }

    /// <summary>
    /// Clears the action graph (for reset scenarios)
    /// </summary>
    public void ClearActionGraph()
    {
        if (actionGraph != null)
        {
            LoggingService.LogWarning($"🔄 BTFlowNodeBase: Clearing action graph (HashCode: {actionGraph.GetHashCode()}) for {DebugDisplayName}");
            
            // Use the new Clear() method to actually remove actions from the NodeGraph
            actionGraph.Clear();
            actionGraph = null;
        }
    }

    /// <summary>
    /// Force sets the action graph (bypasses the lock - use with caution)
    /// </summary>
    /// <param name="graph">The NodeGraph to use</param>
    public void ForceSetActionGraph(NodeGraph graph)
    {
        LoggingService.LogWarning($"⚠️ BTFlowNodeBase: ForceSetActionGraph called - This bypasses the NodeGraph lock!");
        LoggingService.LogWarning($"⚠️ BTFlowNodeBase: Previous HashCode: {actionGraph?.GetHashCode()}, New HashCode: {graph?.GetHashCode()}");
        actionGraph = graph;
    }

    /// <summary>
    /// Gets the action graph for this flow node
    /// </summary>
    /// <returns>The NodeGraph</returns>
    public NodeGraph GetActionGraph()
    {
        return actionGraph;
    }

    /// <summary>
    /// Set the planning service for this flow node
    /// </summary>
    /// <param name="service">The planning service to use</param>
    public void SetPlanningService(BTServicePlanner service)
    {
        LoggingService.LogInfo($"🔧 BTFlowNodeBase: SetPlanningService called for {DebugDisplayName} with service {service.GetType().Name}");
        PlanningService = service;

        // If this is a BTServicePlanner, set the bidirectional reference
        if (service is BTServicePlanner plannerService)
        {
            LoggingService.LogInfo($"🔧 BTFlowNodeBase: Setting bidirectional reference with planning service {service.GetType().Name}");
            plannerService.SetOwningFlowNode(this);
            LoggingService.LogInfo($"🔧 BTFlowNodeBase: Bidirectional reference set - {DebugDisplayName} ↔ {service.GetType().Name}");
        }
        else
        {
            LoggingService.LogWarning($"⚠️ BTFlowNodeBase: Service {service.GetType().Name} is not a BTServicePlanner, cannot set bidirectional reference");
        }

        // Add the planning service to the general services list so it gets ticked
        AddService(service, false); // false = not always on

        LoggingService.LogInfo($"🔧 BTFlowNodeBase: Added planning service {service.GetType().Name} to services list for {DebugDisplayName}");
    }

    /// <summary>
    /// Get the node name as a string
    /// </summary>
    /// <returns>The node name as a string</returns>
    public string GetNodeName()
    {
        return InstanceName?.ToString() ?? "Unnamed";
    }

    public void SetMaxCount(int count)
    {
        maxCount = count;
    }

    public void IncrementCurrentCount()
    {
        currentCount++;
    }

    public void ResetCurrentCount()
    {
        currentCount = 0;
    }

    public int GetCurrentCount()
    {
        return currentCount;
    }

    public int GetMaxCount()
    {
        return maxCount;
    }
}