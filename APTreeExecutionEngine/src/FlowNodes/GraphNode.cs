/// <summary>
/// Represents a node in the behavior tree graph with order and temporal constraints
/// </summary>
public class GraphNode
{
    public PActionNode ActionNode { get; set; }
    public List<Relation> Successors { get; set; } = new();
    public List<Relation> Predecessors { get; set; } = new();
    public float StartTime { get; set; } = 0f;
    public float EndTime { get; set; } = 0f;
    public bool IsExecuting { get; set; } = false;
    public bool IsCompleted { get; set; } = false;

    public GraphNode(PActionNode actionNode)
    {
        ActionNode = actionNode;
    }
}