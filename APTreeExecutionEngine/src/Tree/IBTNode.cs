public interface IBTNode
{
    // who is responsible for handling this node
    Agent? self { get; }
    // which tree does this node belong to
    IBehaviorTree OwningTree { get; }
    
    // NEW: Reference to parent node for bidirectional access
    IBTNode? ParentNode { get;  }
    void SetParentNode(IBTNode parent);
    
    Blackboard<FastName> LinkedBlackboard { get; }  //= new();
    // what is the current state of this node
    BTNodeResult status{ get; }
    // is the node already finished?
    bool HasFinished { get; }
    bool HasChildren{ get; }
     // Add debug display name property
    string DebugDisplayName { get; }
    void SetOwiningTree(IBehaviorTree InOwningtree);
    void SetTreeForAllServices(IBehaviorTree InOwningtree);
bool DoDecoratorsNowPermitRunning(float InDeltaTime);
    void Reset();
    BTNodeResult Tick(float InDeltaTime);
    IBTNode AddService(Service InService, bool InIsAlwaysOn = false);
    IBTNode AddDecorator(IBTDecorator InDecorator);


}