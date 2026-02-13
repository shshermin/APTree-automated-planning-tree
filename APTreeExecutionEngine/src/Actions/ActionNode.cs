using System;
using System.Linq;
using System.Collections.Generic;

public abstract class ActionNode :BTNodeBase
{
    private readonly Func<Dictionary<string, object>, float, bool> actionLogic;
    protected readonly Blackboard<FastName> blackboard;
    protected FastName instanceName;
    protected string debugDisplayName;

    // Public property to access the instance name
    public FastName InstanceName => instanceName;

    public override string DebugDisplayName => debugDisplayName;

    protected ActionNode(Blackboard<FastName> blackboard, string instanceName)
    {
        this.blackboard = blackboard;
        this.instanceName = new FastName(instanceName);
        this.debugDisplayName = instanceName; // Set the debug display name
    }

    public override bool HasChildren => false;
   
    protected override bool OnTick_Children (float InDeltaTime)
    {
        return false;
    }
    protected bool SetStatusAndCalculateReturnvalue(BTNodeResult InResult, bool? bOverrideReturnValue = null)
    {
        status = InResult;
        if (bOverrideReturnValue.HasValue)
            return bOverrideReturnValue.Value;

        return InResult != BTNodeResult.Failure;
    }

}