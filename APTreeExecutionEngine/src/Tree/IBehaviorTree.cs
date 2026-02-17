
public interface IBehaviorTree
{
   
    Blackboard<FastName> linkedBlackboard{ get; }
    FlowNode root{ get; }

    // function for initializing a tree
    void Initialise( Blackboard<FastName> InBlackboard, string InRootNodeName = "Root");

    // adding nodes to the tree
    public IBTNode AddChildToRootNode<NodeType>(IBTNode InNode);

    // to interrupt and reset the behavior tree
    void Reset();

    BTNodeResult Tick(float Indeltatime);


}