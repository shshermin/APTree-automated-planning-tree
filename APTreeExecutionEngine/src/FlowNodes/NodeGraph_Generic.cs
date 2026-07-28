using System;
using System.Collections.Generic;
using System.Linq;
using BehaviorTreeMainProject.Log.Services;

/// <summary>
/// Graph node wrapper for a FlowNode. Tracks execution state.
/// </summary>
public class FlowGraphNode
{
    public FlowNode Node { get; private set; }
    public List<FlowRelation> Successors { get; set; } = new();
    public List<FlowRelation> Predecessors { get; set; } = new();
    public bool IsExecuting { get; set; } = false;
    public bool IsCompleted { get; set; } = false;

    public FlowGraphNode(FlowNode node) => Node = node;
}

/// <summary>
/// Relation between two flow graph nodes with a temporal constraint.
/// </summary>
public class FlowRelation
{
    public FlowGraphNode From { get; set; }
    public FlowGraphNode To { get; set; }
    public TemporalType TempType { get; set; }

    public FlowRelation(FlowGraphNode from, FlowGraphNode to, TemporalType constraint)
    {
        From = from;
        To = to;
        TempType = constraint;
    }
}

/// <summary>
/// A node graph that stores FlowNode children with temporal relations.
/// Mirrors the NodeGraph API used for PActionNode.
/// </summary>
public class FlowNodeGraph
{
    private readonly List<FlowGraphNode> _nodes = new();
    private readonly Dictionary<FlowNode, FlowGraphNode> _nodeMap = new();

    public void AddNode(FlowNode node)
    {
        if (_nodeMap.ContainsKey(node)) return;
        var graphNode = new FlowGraphNode(node);
        _nodes.Add(graphNode);
        _nodeMap[node] = graphNode;
    }

    public void AddRelation(FlowNode from, FlowNode to, TemporalType constraint = TemporalType.MEETS)
    {
        if (!_nodeMap.TryGetValue(from, out var fromNode) ||
            !_nodeMap.TryGetValue(to, out var toNode))
        {
            LoggingService.LogError($"❌ FlowNodeGraph: Cannot add relation — node(s) not found");
            return;
        }

        if (fromNode.Successors.Any(r => r.To == toNode)) return;

        var relation = new FlowRelation(fromNode, toNode, constraint);
        fromNode.Successors.Add(relation);
        toNode.Predecessors.Add(relation);
    }

    /// <summary>
    /// Returns flow nodes whose predecessors have all completed successfully,
    /// that are not themselves already completed.
    /// Same eligibility logic as NodeGraph.GetExecutableNodes.
    /// </summary>
    public List<FlowNode> GetExecutableNodes()
    {
        var result = new List<FlowNode>();
        foreach (var gn in _nodes)
        {
            if (gn.IsCompleted) continue;
            if (CanExecute(gn))
                result.Add(gn.Node);
        }
        return result;
    }

    public List<FlowNode> GetAllNodes() => _nodes.Select(n => n.Node).ToList();

    public int Count => _nodes.Count;

    public void MarkNodeStarted(FlowNode node)
    {
        if (_nodeMap.TryGetValue(node, out var gn))
            gn.IsExecuting = true;
    }

    public void MarkNodeCompleted(FlowNode node)
    {
        if (_nodeMap.TryGetValue(node, out var gn))
        {
            gn.IsCompleted = true;
            gn.IsExecuting = false;
        }
    }

    private bool CanExecute(FlowGraphNode node)
    {
        if (node.IsExecuting) return true;
        if (node.IsCompleted) return false;

        if (!node.Predecessors.Any()) return true;

        foreach (var rel in node.Predecessors)
        {
            if (!IsConstraintSatisfied(rel.From, rel.TempType))
                return false;
        }
        return true;
    }

    private static bool IsConstraintSatisfied(FlowGraphNode pred, TemporalType constraint)
    {
        return constraint switch
        {
            TemporalType.MEETS    => pred.IsCompleted && pred.Node.status == BTNodeResult.Success,
            TemporalType.PRECEDES => pred.IsCompleted,
            TemporalType.OVERLAPS => pred.IsExecuting || pred.IsCompleted,
            TemporalType.STARTS   => pred.IsExecuting || pred.IsCompleted,
            TemporalType.FINISHES => pred.IsExecuting || pred.IsCompleted,
            TemporalType.CONTAINS => pred.IsExecuting,
            TemporalType.EQUALS   => pred.IsExecuting || pred.IsCompleted,
            _                     => true,
        };
    }
}
