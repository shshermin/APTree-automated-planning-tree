public class BehaviorTree : IBehaviorTree
{
    public string DebugDisplayName { get; set; } = "Behavior Tree";

    public Blackboard<FastName> linkedBlackboard { get; protected set; }

    public BTFlowNodeBase root { get;  set; }

    public BehaviorTree()
    {
        DebugDisplayName = "Default Tree";
        linkedBlackboard = null;
        root = null;
    }

   
    public void Initialise( Blackboard<FastName> InBlackboard, string InRootNodeName = "Root")
    {
        if (!string.IsNullOrEmpty(InRootNodeName))
            DebugDisplayName = InRootNodeName;
        linkedBlackboard = InBlackboard;
       
        // Use composite flow node as root to support hierarchical structure
        root = new BTFlowNode_Composite(new FastName(InRootNodeName), this);
        root.SetOwiningTree(this);
    }

     public IBTNode AddChildToRootNode<NodeType>(IBTNode InNode) 
    {
        InNode.SetOwiningTree(this);
        
        // Set the tree for all services that don't have it set yet
        InNode.SetTreeForAllServices(this);
        
        // If this is a GenericBTAction, also set the tree for its SubtreeInjectionService
        if (InNode is PActionNode action)
        {
            action.SetTreeForSubtreeInjectionService(this);
        }
        
        return (root as BTFlowNode_Composite).AddChild(InNode);
        
    }

     public bool HasFinished()
    {
        return root?.HasFinished ?? true;
    }

    public void Reset()
    {
        root.Reset();
    }

    public BTNodeResult Tick(float InDeltaTime)
    {
       return root.Tick(InDeltaTime);
    }
}