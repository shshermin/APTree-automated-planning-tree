
/// <summary>
/// Represents a relation between two nodes with temporal constraints
/// </summary>
public class Relation
{
    public GraphNode From { get; set; }
    public GraphNode To { get; set; }
    public TemporalConstraint tempType { get; set; }

    public Relation(GraphNode from, GraphNode to, TemporalConstraint constraint)
    {
        From = from;
        To = to;
        tempType = constraint;
    }
}