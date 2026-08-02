using BehaviorTreeMainProject.Log.Services;

public class FlowGraphNode
{
    public FlowNode Node { get; }
    public List<FlowRelation> Successors { get; } = new();
    public List<FlowRelation> Predecessors { get; } = new();
    public bool IsExecuting { get; set; }
    public bool IsCompleted { get; set; }

    public FlowGraphNode(FlowNode node)
    {
        Node = node;
    }
}

public class FlowRelation
{
    public FlowGraphNode From { get; }
    public FlowGraphNode To { get; }
    public TemporalType TemporalType { get; }

    public FlowRelation(FlowGraphNode from, FlowGraphNode to, TemporalType temporalType)
    {
        From = from;
        To = to;
        TemporalType = temporalType;
    }
}

public class FlowNodeGraph
{
    private readonly List<FlowGraphNode> nodes = new();
    private readonly Dictionary<FlowNode, FlowGraphNode> nodeMap = new();

    public int Count => nodes.Count;

    public void AddNode(FlowNode node)
    {
        if (nodeMap.ContainsKey(node))
            return;

        var graphNode = new FlowGraphNode(node);
        nodes.Add(graphNode);
        nodeMap[node] = graphNode;
    }

    public void AddRelation(FlowNode from, FlowNode to, TemporalType temporalType = TemporalType.MEETS)
    {
        if (!nodeMap.TryGetValue(from, out var fromNode) ||
            !nodeMap.TryGetValue(to, out var toNode))
        {
            LoggingService.LogError("FlowNodeGraph: Cannot add relation because one or both nodes are missing");
            return;
        }

        if (fromNode.Successors.Any(relation => relation.To == toNode))
            return;

        var relation = new FlowRelation(fromNode, toNode, temporalType);
        fromNode.Successors.Add(relation);
        toNode.Predecessors.Add(relation);
    }

    public List<FlowNode> GetExecutableNodes()
    {
        return nodes
            .Where(node => !node.IsCompleted && CanExecute(node))
            .Select(node => node.Node)
            .ToList();
    }

    public List<FlowNode> GetAllNodes()
    {
        return nodes.Select(node => node.Node).ToList();
    }

    public void MarkNodeStarted(FlowNode node)
    {
        if (nodeMap.TryGetValue(node, out var graphNode))
            graphNode.IsExecuting = true;
    }

    public void MarkNodeCompleted(FlowNode node)
    {
        if (!nodeMap.TryGetValue(node, out var graphNode))
            return;

        graphNode.IsCompleted = true;
        graphNode.IsExecuting = false;
    }

    private static bool CanExecute(FlowGraphNode node)
    {
        if (node.IsExecuting)
            return true;
        if (node.IsCompleted)
            return false;
        if (node.Predecessors.Count == 0)
            return true;

        return node.Predecessors.All(relation =>
            IsConstraintSatisfied(relation.From, relation.TemporalType));
    }

    private static bool IsConstraintSatisfied(FlowGraphNode predecessor, TemporalType temporalType)
    {
        return temporalType switch
        {
            TemporalType.MEETS => predecessor.IsCompleted && predecessor.Node.status == BTNodeResult.Success,
            TemporalType.PRECEDES => predecessor.IsCompleted,
            TemporalType.OVERLAPS => predecessor.IsExecuting || predecessor.IsCompleted,
            TemporalType.STARTS => predecessor.IsExecuting || predecessor.IsCompleted,
            TemporalType.FINISHES => predecessor.IsExecuting || predecessor.IsCompleted,
            TemporalType.CONTAINS => predecessor.IsExecuting,
            TemporalType.EQUALS => predecessor.IsExecuting || predecessor.IsCompleted,
            _ => true
        };
    }
}