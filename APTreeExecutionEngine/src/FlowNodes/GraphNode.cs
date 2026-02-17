using BehaviorTreeMainProject.Log.Services;

/// <summary>
/// Represents a node in the behavior tree graph with order and temporal constraints.
/// Owns its PActionNode via composition: when this GraphNode is destroyed,
/// the action node is cleaned up and should not be used elsewhere.
/// </summary>
public class GraphNode
{
    public PActionNode ActionNode { get; private set; }
    public List<Relation> Successors { get; set; } = new();
    public List<Relation> Predecessors { get; set; } = new();
    public float StartTime { get; set; } = 0f;
    public float EndTime { get; set; } = 0f;
    public bool IsExecuting { get; set; } = false;
    public bool IsCompleted { get; set; } = false;

    /// <summary>
    /// Creates a new GraphNode that takes ownership of the given action node.
    /// </summary>
    public GraphNode(PActionNode actionNode)
    {
        ActionNode = actionNode;
    }

    /// <summary>
    /// Composition cleanup: destroys the owned action node and detaches all relations.
    /// After calling this method, this GraphNode should not be reused.
    /// </summary>
    public void Destroy()
    {
        // Detach this node from other GraphNodes' relation lists
        foreach (var relation in Successors)
        {
            relation.To.Predecessors.RemoveAll(r => r.From == this);
        }
        foreach (var relation in Predecessors)
        {
            relation.From.Successors.RemoveAll(r => r.To == this);
        }
        Successors.Clear();
        Predecessors.Clear();

        // Destroy the owned action node (composition: GraphNode owns it)
        if (ActionNode != null)
        {
            LoggingService.LogInfo($"\U0001F5D1\uFE0F GraphNode: Destroying owned action {ActionNode.InstanceName}");
            ActionNode.RemoveSubtree();
            ActionNode.SetParentNode(null);
            ActionNode = null;
        }

        IsExecuting = false;
        IsCompleted = false;
    }
}