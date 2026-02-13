public interface IBTDecorator 
{

    // which tree does this node belong to
    IBehaviorTree OwningTree { get; }
    Blackboard<FastName> LinkedBlackboard{ get; }
    bool CanPostProcessTickResult { get; }
    BTNodeResult PostProcessTickResult(BTNodeResult InResult);

     void SetOwiningTree(IBehaviorTree InOwningtree);
    // it returns a bool that will let us know if we can continue running
    bool Tick(float InDeltaTime);
    // decorators can modify the return value of a node
    //EBTNodeResult PostProcessTickresult(EBTNodeResult InResult);
}