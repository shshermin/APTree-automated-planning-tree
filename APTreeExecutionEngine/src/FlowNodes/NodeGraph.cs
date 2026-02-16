using System.Collections.Generic;
using System.Linq;
using BehaviorTreeMainProject.Services;
using System;
using BehaviorTreeMainProject.Log.Services;


/// <summary>
/// Manages a graph of behavior tree action nodes with order relations and temporal constraints
/// </summary>
public class NodeGraph
{
    private List<GraphNode> nodes = new();
    private Dictionary<ActionNode, GraphNode> nodeMap = new();
    private float elapsedTime = 0f;
    
    public NodeGraph()
    {
        LoggingService.LogInfo($"🔧 NodeGraph: New NodeGraph instance created (HashCode: {this.GetHashCode()})");
    }

    /// <summary>
    /// Add an action node to the graph
    /// </summary>
    public void AddNode(PActionNode actionNode)
    {
        if (!nodeMap.ContainsKey(actionNode))
        {
            // Reset the action node to readyToTick status so it can be executed
            actionNode.Reset();
            
            var graphNode = new GraphNode(actionNode);
            nodes.Add(graphNode);
            nodeMap[actionNode] = graphNode;
            
            // Console.WriteLine($"   ✅ NodeGraph: Added action {actionNode.InstanceName.ToString()} with status: {actionNode.LastStatus}");
        }
    }

    /// <summary>
    /// Add an order relation between two nodes (like Hasse diagram)
    /// </summary>
    public void AddOrderRelation(ActionNode from, ActionNode to)
    {
        LoggingService.LogInfo($"🔧 NodeGraph: AddOrderRelation called: {from.InstanceName.ToString()} → {to.InstanceName.ToString()}");
        
        // Check for self-reference (circular dependency)
        if (from == to)
        {
            LoggingService.LogError($"❌ NodeGraph: Cannot add order relation - self-reference detected: {from.InstanceName.ToString()} → {to.InstanceName.ToString()}");
            LoggingService.LogError($"❌ NodeGraph: Self-reference detected! Action {from.InstanceName.ToString()} is trying to be its own predecessor");
            
            // Add stack trace to help identify where this is being called from
            var stackTrace = Environment.StackTrace;
            LoggingService.LogError($"❌ NodeGraph: Stack trace for self-reference:");
            var stackLines = stackTrace.Split('\n');
            for (int i = 0; i < Math.Min(10, stackLines.Length); i++)
            {
                LoggingService.LogError($"   {stackLines[i].Trim()}");
            }
            return;
        }
        
        if (!nodeMap.ContainsKey(from) || !nodeMap.ContainsKey(to))
        {
            LoggingService.LogError($"❌ NodeGraph: Cannot add order relation - nodes not found in graph");
            LoggingService.LogError($"   From node exists: {nodeMap.ContainsKey(from)}");
            LoggingService.LogError($"   To node exists: {nodeMap.ContainsKey(to)}");
            return;
        }
        var fromNode = nodeMap[from];
        var toNode = nodeMap[to];
        
        // Check if relation already exists
        if (fromNode.Successors.Any(r => r.To == toNode))
        {
            LoggingService.LogWarning($"⚠️ NodeGraph: Order relation already exists: {from.InstanceName.ToString()} → {to.InstanceName.ToString()}");
            return;
        }
        
        // Check for potential circular dependency by checking if 'to' is already a predecessor of 'from'
        if (toNode.Successors.Any(r => r.To == fromNode))
        {
            LoggingService.LogError($"❌ NodeGraph: Circular dependency detected: {from.InstanceName.ToString()} ↔ {to.InstanceName.ToString()}");
            LoggingService.LogError($"❌ NodeGraph: {to.InstanceName.ToString()} is already a successor of {from.InstanceName.ToString()}");
            LoggingService.LogError($"❌ NodeGraph: Cannot add reverse relation without creating a cycle");
            
            // Add stack trace
            var stackTrace = Environment.StackTrace;
            LoggingService.LogError($"❌ NodeGraph: Stack trace for circular dependency:");
            var stackLines = stackTrace.Split('\n');
            for (int i = 0; i < Math.Min(10, stackLines.Length); i++)
            {
                LoggingService.LogError($"   {stackLines[i].Trim()}");
            }
            return;
        }
        
        // Log the current state before adding the relation
        LoggingService.LogInfo($"🔍 NodeGraph: Before adding relation - {from.InstanceName.ToString()} has {fromNode.Successors.Count} successors");
        LoggingService.LogInfo($"🔍 NodeGraph: Before adding relation - {to.InstanceName.ToString()} has {toNode.Predecessors.Count} predecessors");
        
        // Create and add the relation with a default temporal constraint (PRECEDES)
        var relation = new Relation(fromNode, toNode, TemporalConstraint.PRECEDES);
        fromNode.Successors.Add(relation);
        toNode.Predecessors.Add(relation);
        
        LoggingService.LogInfo($"✅ NodeGraph: Added order relation: {from.InstanceName.ToString()} → {to.InstanceName.ToString()}");
        LoggingService.LogInfo($"🔍 NodeGraph: {from.InstanceName.ToString()} now has {fromNode.Successors.Count} successors");
        LoggingService.LogInfo($"🔍 NodeGraph: {to.InstanceName.ToString()} now has {toNode.Predecessors.Count} predecessors");
        
        // Log all predecessors of the target node after adding the relation
        LoggingService.LogInfo($"🔍 NodeGraph: {to.InstanceName.ToString()} predecessors after adding relation:");
        foreach (var predRelation in toNode.Predecessors)
        {
            LoggingService.LogInfo($"   - {predRelation.From.ActionNode.InstanceName.ToString()}");
        }
    }

    /// <summary>
    /// Add a temporal constraint between two nodes (based on Allen's theory)
    /// </summary>
    public void AddTemporalConstraint(ActionNode from, ActionNode to, TemporalConstraint constraint)
    {
        LoggingService.LogInfo($"🔧 NodeGraph: AddTemporalConstraint called: {from.InstanceName.ToString()} {constraint} {to.InstanceName.ToString()}");
        
        if (!nodeMap.ContainsKey(from) || !nodeMap.ContainsKey(to))
        {
            LoggingService.LogError($"❌ NodeGraph: Cannot add temporal constraint - nodes not found in graph");
            return;
        }

        var fromNode = nodeMap[from];
        var toNode = nodeMap[to];
        
        // Find the existing relation
        var existingRelation = fromNode.Successors.FirstOrDefault(r => r.To == toNode);
        
        if (existingRelation != null)
        {
            // Update the constraint on existing relation
            existingRelation.tempType = constraint;
            LoggingService.LogInfo($"✅ NodeGraph: Updated temporal constraint on existing relation: {from.InstanceName.ToString()} {constraint} {to.InstanceName.ToString()}");
        }
        else
        {
            // Create a new relation with this constraint
            var newRelation = new Relation(fromNode, toNode, constraint);
            fromNode.Successors.Add(newRelation);
            toNode.Predecessors.Add(newRelation);
            LoggingService.LogInfo($"✅ NodeGraph: Created new relation with constraint: {from.InstanceName.ToString()} {constraint} {to.InstanceName.ToString()}");
        }
        
        LoggingService.LogInfo($"🔍 NodeGraph: {from.InstanceName.ToString()} now has {fromNode.Successors.Count} relations");
    }

    /// <summary>
    /// Get all nodes in the graph in execution order (preserving order relations)
    /// </summary>
    public List<PActionNode> GetAllActionNodes()
    {
        return GetExecutionOrder();
    }

    /// <summary>
    /// Get node info for debugging purposes
    /// </summary>
    public GraphNode GetNodeInfo(ActionNode actionNode)
    {
        if (nodeMap.TryGetValue(actionNode, out var graphNode))
        {
            return graphNode;
        }
        return null;
    }

    /// <summary>
    /// Get nodes that can be executed at the current time based on order and temporal constraints
    /// </summary>
    public List<PActionNode> GetExecutableNodes(float deltaTime)
    {
        elapsedTime += deltaTime;
        return GetExecutableNodesInternal();
    }

    /// <summary>
    /// Get nodes that can be executed without incrementing elapsed time (for subsequent calls in same tick)
    /// </summary>
    public List<PActionNode> GetExecutableNodesInternal()
    {
        var executableNodes = new List<PActionNode>();

        LoggingService.LogInfo($"   🔍 NodeGraph: GetExecutableNodesInternal called - Total nodes in graph: {nodes.Count}");
        LoggingService.LogInfo($"   🔍 NodeGraph: Elapsed time: {elapsedTime}");

        // Check all nodes to see which ones can execute
        foreach (var node in nodes)
        {
            LoggingService.LogInfo($"   🔍 NodeGraph: ===== Checking node {node.ActionNode.InstanceName.ToString()} =====");
            LoggingService.LogInfo($"   🔍 NodeGraph: Node completed: {node.IsCompleted}, executing: {node.IsExecuting}");
            LoggingService.LogInfo($"   🔍 NodeGraph: Node LastStatus: {node.ActionNode.status}");
            LoggingService.LogInfo($"   🔍 NodeGraph: Has predecessors: {node.Predecessors.Any()}");
            LoggingService.LogInfo($"   🔍 NodeGraph: Has successors: {node.Successors.Any()}");
            
            if (node.Predecessors.Any())
            {
                LoggingService.LogInfo($"   🔍 NodeGraph: All predecessors completed: {AllPredecessorsCompleted(node)}");
                LoggingService.LogInfo($"   🔍 NodeGraph: Predecessor details:");
                foreach (var predRelation in node.Predecessors)
                {
                    var pred = predRelation.From;
                    LoggingService.LogInfo($"     - {pred.ActionNode.InstanceName.ToString()}: IsCompleted={pred.IsCompleted}, IsExecuting={pred.IsExecuting}, LastStatus={pred.ActionNode.status}");
                }
            }
            
            if (node.Successors.Any())
            {
                LoggingService.LogInfo($"   🔍 NodeGraph: Successor details:");
                foreach (var succRelation in node.Successors)
                {
                    var succ = succRelation.To;
                    LoggingService.LogInfo($"     - {succ.ActionNode.InstanceName.ToString()}: IsCompleted={succ.IsCompleted}, IsExecuting={succ.IsExecuting}, LastStatus={succ.ActionNode.status}");
                }
            }
            
            LoggingService.LogInfo($"   🔍 NodeGraph: Relations:");
            foreach (var relation in node.Successors)
            {
                LoggingService.LogInfo($"     - {node.ActionNode.InstanceName.ToString()} --[{relation.tempType}]--> {relation.To.ActionNode.InstanceName.ToString()}");
            }
            
            // A node can execute if:
            // 1. It's not completed and not already executing
            // 2. Either it has no predecessors (first in sequence) OR all its predecessors are completed
            // 3. Any temporal constraints are satisfied
            bool canExecuteNode = CanExecuteNode(node);
            
            LoggingService.LogInfo($"   🔍 NodeGraph: CanExecuteNode result: {canExecuteNode}");
            
            if (canExecuteNode)
            {
                LoggingService.LogInfo($"   ✅ NodeGraph: Node {node.ActionNode.InstanceName.ToString()} can be executed");
                executableNodes.Add(node.ActionNode as PActionNode);
                // Don't set IsExecuting here - let the BTFLowNode_Dynamic handle that when it actually starts executing
            }
            else
            {
                LoggingService.LogInfo($"   ❌ NodeGraph: Node {node.ActionNode.InstanceName.ToString()} cannot be executed");
                LoggingService.LogInfo($"   ❌ NodeGraph: Reason: CanExecuteNode returned false");
            }
            LoggingService.LogInfo($"   🔍 NodeGraph: ===== End checking node {node.ActionNode.InstanceName.ToString()} =====");
        }

        LoggingService.LogInfo($"   🔍 NodeGraph: Returning {executableNodes.Count} executable nodes");
        if (executableNodes.Count == 0)
        {
            LoggingService.LogWarning($"   ⚠️ NodeGraph: No executable nodes found! This might indicate:");
            LoggingService.LogWarning($"   ⚠️ NodeGraph: - All nodes have uncompleted predecessors");
            LoggingService.LogWarning($"   ⚠️ NodeGraph: - All nodes are already completed or executing");
            LoggingService.LogWarning($"   ⚠️ NodeGraph: - Temporal constraints are not satisfied");
            LoggingService.LogWarning($"   ⚠️ NodeGraph: - Node states are incorrect");
        }
        return executableNodes;
    }

    /// <summary>
    /// Check if a node can be executed based on temporal constraints
    /// </summary>
    private bool CanExecuteNode(GraphNode node)
    {
        LoggingService.LogInfo($"   🔍 NodeGraph: CanExecuteNode called for {node.ActionNode.InstanceName.ToString()}");
        
        // Allow nodes that are already executing to continue executing
        if (node.IsExecuting)
        {
            LoggingService.LogInfo($"   ✅ NodeGraph: Node {node.ActionNode.InstanceName.ToString()} can continue executing (already executing)");
            return true;
        }
        
        // Prevent completed nodes from executing again
        if (node.IsCompleted)
        {
            LoggingService.LogInfo($"   ❌ NodeGraph: Node {node.ActionNode.InstanceName.ToString()} cannot execute - already completed");
            return false;
        }

        // For nodes with no order predecessors (first in sequence), don't check temporal constraints
        // They should be able to start execution immediately
        if (!node.Predecessors.Any())
        {
            LoggingService.LogInfo($"   ✅ NodeGraph: First node {node.ActionNode.InstanceName.ToString()} can execute (no predecessors)");
            return true;
        }

        // Check temporal constraints from predecessor nodes
        // Each constraint type determines what state the predecessor must be in
        // (e.g., PRECEDES/MEETS require completion, OVERLAPS/STARTS only require execution started)
        LoggingService.LogInfo($"   🔍 NodeGraph: Checking temporal constraints for {node.ActionNode.InstanceName.ToString()}");
        foreach (var relation in node.Predecessors)
        {
            var otherNode = relation.From;
            var temporalConstraint = relation.tempType;
            
            LoggingService.LogInfo($"   🔍 NodeGraph: Checking temporal constraint {temporalConstraint} from {otherNode.ActionNode.InstanceName.ToString()} to {node.ActionNode.InstanceName.ToString()}");
            LoggingService.LogInfo($"   🔍 NodeGraph: Other node status - IsCompleted: {otherNode.IsCompleted}, IsExecuting: {otherNode.IsExecuting}");

            if (!IsTemporalConstraintSatisfied(otherNode, node, temporalConstraint))
            {
                LoggingService.LogInfo($"   ❌ NodeGraph: Temporal constraint {temporalConstraint} not satisfied");
                return false;
            }
            else
            {
                LoggingService.LogInfo($"   ✅ NodeGraph: Temporal constraint {temporalConstraint} satisfied");
            }
        }

        LoggingService.LogInfo($"   ✅ NodeGraph: Node {node.ActionNode.InstanceName.ToString()} can execute");
        return true;
    }

    /// <summary>
    /// Check if all order predecessors have completed
    /// </summary>
    private bool AllPredecessorsCompleted(GraphNode node)
    {
        LoggingService.LogInfo($"🔍 DEBUG: AllPredecessorsCompleted called for {node.ActionNode.InstanceName.ToString()}");
        LoggingService.LogInfo($"🔍 DEBUG: Number of predecessors: {node.Predecessors.Count}");
        
        bool allCompleted = true;
        foreach (var predRelation in node.Predecessors)
        {
            var pred = predRelation.From;
            LoggingService.LogInfo($"🔍 DEBUG: Predecessor {pred.ActionNode.InstanceName.ToString()}: IsCompleted={pred.IsCompleted}");
            if (!pred.IsCompleted)
            {
                allCompleted = false;
            }
        }
        
        LoggingService.LogInfo($"🔍 DEBUG: AllPredecessorsCompleted result for {node.ActionNode.InstanceName.ToString()}: {allCompleted}");
        return allCompleted;
    }

    /// <summary>
    /// Check if a temporal constraint is satisfied based on Allen's theory
    /// </summary>
    private bool IsTemporalConstraintSatisfied(GraphNode from, GraphNode to, TemporalConstraint constraint)
    {
        bool result = false;
        
        switch (constraint)
        {
            case TemporalConstraint.PRECEDES:
                result = from.IsCompleted && !to.IsExecuting;
                // Console.WriteLine($"   🔍 NodeGraph: PRECEDES constraint - from completed: {from.IsCompleted}, to executing: {to.IsExecuting}, result: {result}");
                break;
            
            case TemporalConstraint.MEETS:
                // MEETS: the next action starts immediately after the previous one ends
                // For initial execution, we just need the previous action to be completed
                // For timing precision, we check that the next action hasn't started yet or starts at the right time
                if (from.IsCompleted)
                {
                    // Previous action is completed, next action can start
                    // Simplified logic: if previous is completed and next hasn't started yet, allow it
                    result = !to.IsExecuting && !to.IsCompleted;
                    LoggingService.LogInfo($"   🔍 NodeGraph: MEETS constraint - from completed: {from.IsCompleted}, to executing: {to.IsExecuting}, to completed: {to.IsCompleted}, result: {result}");
                }
                else
                {
                    // Previous action is not completed yet, so MEETS constraint is not satisfied
                    result = false;
                    LoggingService.LogInfo($"   🔍 NodeGraph: MEETS constraint - from not completed yet, result: false");
                }
                break;
            
            case TemporalConstraint.OVERLAPS:
                // OVERLAPS: actions can run in parallel
                // For parallel execution, we allow the second action to start while the first is still executing
                if (from.IsExecuting || from.IsCompleted)
                {
                    // First action is either executing or completed, so second action can start
                    result = !to.IsCompleted; // Second action should not be completed yet
                }
                else
                {
                    // First action hasn't started yet, so OVERLAPS constraint is not satisfied
                    result = false;
                }
                // Console.WriteLine($"   🔍 NodeGraph: OVERLAPS constraint - from executing: {from.IsExecuting}, from completed: {from.IsCompleted}, to completed: {to.IsCompleted}, result: {result}");
                break;
            
            case TemporalConstraint.STARTS:
                // STARTS: From and To start at the same time (From ends first)
                // Scheduling: To can start when From has started (executing or completed)
                result = from.IsExecuting || from.IsCompleted;
                LoggingService.LogInfo($"   🔍 NodeGraph: STARTS constraint - from executing: {from.IsExecuting}, from completed: {from.IsCompleted}, result: {result}");
                break;
            
            case TemporalConstraint.FINISHES:
                // FINISHES: From starts after To but they end at the same time
                // Scheduling: To can start when From is executing or completed (they need to overlap)
                result = from.IsExecuting || from.IsCompleted;
                LoggingService.LogInfo($"   🔍 NodeGraph: FINISHES constraint - from executing: {from.IsExecuting}, from completed: {from.IsCompleted}, result: {result}");
                break;
            
            case TemporalConstraint.CONTAINS:
                // CONTAINS: From starts before To and ends after To (From fully contains To)
                // Scheduling: To can start when From is currently executing (started but not finished)
                result = from.IsExecuting;
                LoggingService.LogInfo($"   🔍 NodeGraph: CONTAINS constraint - from executing: {from.IsExecuting}, result: {result}");
                break;
            
            case TemporalConstraint.EQUALS:
                // EQUALS: From and To have the same start and end times
                // Scheduling: To can start when From has started (executing or completed)
                result = from.IsExecuting || from.IsCompleted;
                LoggingService.LogInfo($"   🔍 NodeGraph: EQUALS constraint - from executing: {from.IsExecuting}, from completed: {from.IsCompleted}, result: {result}");
                break;
            
            default:
                result = true;
                // Console.WriteLine($"   🔍 NodeGraph: Default constraint - result: {result}");
                break;
        }
        
        return result;
    }

    /// <summary>
    /// Mark a node as completed
    /// </summary>
    public void MarkNodeCompleted(ActionNode actionNode)
    {
        LoggingService.LogInfo($"🔍 DEBUG: MarkNodeCompleted called for {actionNode.InstanceName.ToString()}");
        LoggingService.LogInfo($"🔍 DEBUG: Node status before marking: {actionNode.status}");
        
        if (nodeMap.TryGetValue(actionNode, out var graphNode))
        {
            LoggingService.LogInfo($"🔍 DEBUG: Found graphNode for {actionNode.InstanceName.ToString()}");
            LoggingService.LogInfo($"🔍 DEBUG: GraphNode.IsCompleted before: {graphNode.IsCompleted}");
            LoggingService.LogInfo($"🔍 DEBUG: GraphNode.IsExecuting before: {graphNode.IsExecuting}");
            
            graphNode.IsCompleted = true;
            graphNode.IsExecuting = false;
            graphNode.EndTime = elapsedTime;
            
            LoggingService.LogInfo($"🔍 DEBUG: GraphNode.IsCompleted after: {graphNode.IsCompleted}");
            LoggingService.LogInfo($"🔍 DEBUG: GraphNode.IsExecuting after: {graphNode.IsExecuting}");
            LoggingService.LogInfo($"   ✅ NodeGraph: Marked {actionNode.InstanceName.ToString()} as completed (EndTime: {elapsedTime})");
        }
        else
        {
            LoggingService.LogInfo($"❌ DEBUG: Could not find graphNode for {actionNode.InstanceName.ToString()} in nodeMap!");
            LoggingService.LogInfo($"🔍 DEBUG: nodeMap contains {nodeMap.Count} entries");
            LoggingService.LogInfo($"🔍 DEBUG: Available keys in nodeMap:");
            foreach (var kvp in nodeMap)
            {
                LoggingService.LogInfo($"   - {kvp.Key.InstanceName.ToString()}");
            }
        }
    }

    /// <summary>
    /// Mark a node as started executing
    /// </summary>
    public void MarkNodeStarted(ActionNode actionNode)
    {
        if (nodeMap.TryGetValue(actionNode, out var graphNode))
        {
            graphNode.IsExecuting = true;
            graphNode.StartTime = elapsedTime;
            LoggingService.LogInfo($"   🚀 NodeGraph: Marked {actionNode.InstanceName.ToString()} as started (StartTime: {elapsedTime})");
        }
    }

    /// <summary>
    /// Reset the graph state
    /// </summary>
    public void Reset()
    {
        LoggingService.LogWarning($"🔄 NodeGraph: RESET called! This will clear all completion statuses!");
        LoggingService.LogWarning($"🔄 NodeGraph: Stack trace for Reset call:");
        var stackTrace = Environment.StackTrace;
        var stackLines = stackTrace.Split('\n');
        for (int i = 0; i < Math.Min(10, stackLines.Length); i++)
        {
            LoggingService.LogWarning($"   {stackLines[i].Trim()}");
        }
        
        // Track action deletion when resetting NodeGraph
        var actionCount = nodes.Count;
        if (actionCount > 0)
        {
            BehaviorTreeComponentLogger.TrackActionDeletion("NodeGraphReset", actionCount, "NodeGraph reset - all actions reset");
        }
        
        elapsedTime = 0f;
        foreach (var node in nodes)
        {
            LoggingService.LogWarning($"🔄 NodeGraph: Resetting node {node.ActionNode.InstanceName.ToString()} - IsCompleted: {node.IsCompleted} → false");
            node.IsExecuting = false;
            node.IsCompleted = false;
            node.StartTime = 0f;
            node.EndTime = 0f;
        }
        LoggingService.LogWarning($"🔄 NodeGraph: Reset completed - all {nodes.Count} nodes reset");
    }

    /// <summary>
    /// Clear all actions from the NodeGraph (actually removes them)
    /// </summary>
    public void Clear()
    {
        LoggingService.LogWarning($"🗑️ NodeGraph: CLEAR called! This will remove all actions from the graph!");
        LoggingService.LogWarning($"🗑️ NodeGraph: Stack trace for Clear call:");
        var stackTrace = Environment.StackTrace;
        var stackLines = stackTrace.Split('\n');
        for (int i = 0; i < Math.Min(10, stackLines.Length); i++)
        {
            LoggingService.LogWarning($"   {stackLines[i].Trim()}");
        }
        
        // Track action deletion when clearing NodeGraph
        var actionCount = nodes.Count;
        if (actionCount > 0)
        {
            BehaviorTreeComponentLogger.TrackActionDeletion("NodeGraphClear", actionCount, "NodeGraph clear - all actions removed");
        }
        
        // Actually remove all actions from the graph
        nodes.Clear();
        nodeMap.Clear();
        elapsedTime = 0f;
        
        LoggingService.LogWarning($"🗑️ NodeGraph: Clear completed - removed {actionCount} actions from graph");
    }

    /// <summary>
    /// Remove a specific action from the NodeGraph
    /// </summary>
    /// <param name="actionNode">The action to remove</param>
    /// <returns>True if the action was found and removed, false otherwise</returns>
    public bool RemoveAction(PActionNode actionNode)
    {
        if (nodeMap.TryGetValue(actionNode, out var graphNode))
        {
            LoggingService.LogInfo($"🗑️ NodeGraph: Removing action {actionNode.InstanceName.ToString()} from graph");
            
            // Remove from both collections
            nodes.Remove(graphNode);
            nodeMap.Remove(actionNode);
            
            // Track action deletion
            BehaviorTreeComponentLogger.TrackActionDeletion("SingleActionRemoval", 1, $"Removed action: {actionNode.InstanceName.ToString()}");
            
            LoggingService.LogInfo($"🗑️ NodeGraph: Successfully removed action {actionNode.InstanceName.ToString()}");
            return true;
        }
        else
        {
            LoggingService.LogWarning($"⚠️ NodeGraph: Action {actionNode.InstanceName.ToString()} not found in graph");
            return false;
        }
    }

    /// <summary>
    /// Get the execution order as a list (left-to-right like Hasse diagram)
    /// </summary>
    public List<PActionNode> GetExecutionOrder()
    {
        var result = new List<PActionNode>();
        var visited = new HashSet<GraphNode>();
        var tempVisited = new HashSet<GraphNode>();

        foreach (var node in nodes)
        {
            if (!visited.Contains(node))
            {
                TopologicalSort(node, visited, tempVisited, result);
            }
        }

        return result;
    }

    /// <summary>
    /// Log the complete graph structure for debugging
    /// </summary>
    public void LogGraphStructure()
    {
        LoggingService.LogInfo($"📊 NodeGraph: === GRAPH STRUCTURE SUMMARY ===");
        LoggingService.LogInfo($"📊 NodeGraph: Total nodes: {nodes.Count}");
        
        foreach (var node in nodes)
        {
            LoggingService.LogInfo($"📊 NodeGraph: Node: {node.ActionNode.InstanceName.ToString()}");
            LoggingService.LogInfo($"   - IsCompleted: {node.IsCompleted}");
            LoggingService.LogInfo($"   - IsExecuting: {node.IsExecuting}");
            LoggingService.LogInfo($"   - Predecessors ({node.Predecessors.Count}):");
            foreach (var predRelation in node.Predecessors)
            {
                LoggingService.LogInfo($"     * {predRelation.From.ActionNode.InstanceName.ToString()} [{predRelation.tempType}]");
            }
            LoggingService.LogInfo($"   - Successors ({node.Successors.Count}):");
            foreach (var succRelation in node.Successors)
            {
                LoggingService.LogInfo($"     * {succRelation.To.ActionNode.InstanceName.ToString()} [{succRelation.tempType}]");
            }
        }
        LoggingService.LogInfo($"📊 NodeGraph: === END GRAPH STRUCTURE ===");
    }

    /// <summary>
    /// Topological sort to determine execution order
    /// </summary>
    private void TopologicalSort(GraphNode node, HashSet<GraphNode> visited, HashSet<GraphNode> tempVisited, List<PActionNode> result)
    {
        if (tempVisited.Contains(node))
            return; // Cycle detected

        if (visited.Contains(node))
            return;

        tempVisited.Add(node);

        // Process all successors first (depth-first)
        foreach (var succRelation in node.Successors)
        {
            TopologicalSort(succRelation.To, visited, tempVisited, result);
        }

        tempVisited.Remove(node);
        visited.Add(node);
        
        // Add this node to the beginning of the result (reverse topological order)
        // This ensures that nodes with no successors come first
        result.Insert(0, node.ActionNode);
    }
}
