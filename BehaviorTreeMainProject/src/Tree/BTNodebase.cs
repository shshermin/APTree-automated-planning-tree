using System.Reflection.PortableExecutable;
using BehaviorTreeMainProject.Services;
using BehaviorTreeMainProject.Log.Services;

public abstract class BTNodeBase : IBTNode
{
    //public  string DebugDisplayName { get; protected set; } = "Unnamed Node";
    //who is responsible for doing this action

    public Agent? self { get; protected set; }
    // which tree does this node belong to

    public abstract string TypeName { get; }
    public IBehaviorTree OwningTree { get; protected set; } = null!;

    // NEW: Reference to parent node for bidirectional access
    public IBTNode? ParentNode { get; set; }
    
    // NEW: Public method to set parent reference (for external use)
    public void SetParentNode(IBTNode parent)
    {
        ParentNode = parent;
        LoggingService.LogInfo($"🔧 BTNodeBase: {DebugDisplayName} - Parent reference set to: {parent?.DebugDisplayName ?? "null"}");
    }

    public Blackboard<FastName> LinkedBlackboard => OwningTree.linkedBlackboard;
    // to keep track of the last status of the node
    public BTNodeResult status { get; protected set; } = BTNodeResult.Uninitialized;
    // to keep track of the tick phase of each node
    protected EBTNodeTickPhase CurrentTickPhase { get; set; } = EBTNodeTickPhase.WaitingForNextTick;
    // to store the list of services of this node
    protected List<BTServiceBase>? AlwaysOnServices;
    protected List<BTServiceBase>? GenrealServices; 
    // to store the list of decorators of this node
    protected List<IBTDecorator>? Decorators;
    
// to know if a know has finished or not. (succeeded or failed)
    public bool HasFinished => (status == BTNodeResult.Success || status == BTNodeResult.Failure);
    // to store if all the decorators allow for running this node

    protected bool bDecoratorsAllowRunning = true;

    // Tick timing tracking
    private DateTime tickStartTime;
    private DateTime servicesEndTime;
    private DateTime decoratorsEndTime;
    private DateTime nodeLogicEndTime;
    private DateTime childrenEndTime;
    private DateTime tickEndTime;
    
    // Tick timing properties
    public TimeSpan ServicesDuration => servicesEndTime - tickStartTime;
    public TimeSpan DecoratorsDuration => decoratorsEndTime - servicesEndTime;
    public TimeSpan NodeLogicDuration => nodeLogicEndTime - decoratorsEndTime;
    public TimeSpan ChildrenDuration => childrenEndTime - nodeLogicEndTime;
    public TimeSpan TotalTickDuration => tickEndTime - tickStartTime;
    public bool HasCompletedFullTick { get; private set; } = false;

    // Node execution statistics tracking
    private int totalTickCount = 0;
    private int successCount = 0;
    private int failureCount = 0;
    
    // Public properties for accessing statistics
    public int TotalTickCount => totalTickCount;
    public int SuccessCount => successCount;
    public int FailureCount => failureCount;

// to diffrentiate between flow nodes and action nodes
    public abstract bool HasChildren { get; }

    public abstract string DebugDisplayName { get; protected set; }

    protected bool bCanSendExitNotification = false;
/// <summary>
/// Adds the decorator nodes to a node
/// </summary>
/// <param name="InDecorator"></param>
/// <returns></returns>
    public IBTNode AddDecorator(IBTDecorator InDecorator)
    {
        if (Decorators == null)
            Decorators = new();
        InDecorator.SetOwiningTree(OwningTree);
        Decorators.Add(InDecorator);
        
        // Track decorator addition
        BehaviorTreeComponentLogger.TrackDecoratorAddition(InDecorator.GetType().Name);
        
        return this;
    }
    /// <summary>
    /// Adds services to each node
    /// </summary>
    /// <param name="InService"></param>
    /// <param name="InIsAlwaysOn"></param>
    /// <returns></returns>

    public IBTNode AddService(BTServiceBase InService, bool InIsAlwaysOn = false)
    {
        // Only set the tree if it's already available
        if (OwningTree != null)
        {
            InService.SetOwiningTree(OwningTree);
        }
        
        if (InIsAlwaysOn)
        {
            if (AlwaysOnServices == null)
                AlwaysOnServices = new();
            AlwaysOnServices.Add(InService);
        }
        else 
        {
            if (GenrealServices == null)
                GenrealServices = new();
            GenrealServices.Add(InService);
        }
        
        // Track service addition
        BehaviorTreeComponentLogger.TrackServiceAddition(InService.GetType().Name);
        
        return this;

    }

    /// <summary>
    /// Set the tree for all services that don't have it set yet
    /// This should be called after SetOwiningTree is called on the node
    /// </summary>
    public void SetTreeForAllServices(IBehaviorTree InOwningtree)
    {
        if (AlwaysOnServices != null)
        {
            foreach (var service in AlwaysOnServices)
            {
                if (service.OwningTree == null)
                {
                    service.SetOwiningTree(InOwningtree);
                }
            }
        }
        
        if (GenrealServices != null)
        {
            foreach (var service in GenrealServices)
            {
                if (service.OwningTree == null)
                {
                    service.SetOwiningTree(InOwningtree);
                }
            }
        }
    }
/// <summary>
/// 
/// </summary>
/// <param name="InDeltaTime"></param>
/// <returns></returns>
    public bool DoDecoratorsNowPermitRunning(float InDeltaTime)
    {
        // if the decorators already allow running then no need to check
        if (bDecoratorsAllowRunning)
            return false;

        // update always on services on services
        if (!OnTick_AlwaysOnServices(InDeltaTime))
            return false;

        // check decorators 
        if (!OnTick_Decorators(InDeltaTime))
            return false;

        return true;

    }

    public virtual void Reset()
    {
        status = BTNodeResult.ReadyToTick;
        
       
    }

    public void SetOwiningTree(IBehaviorTree InOwningtree)
    {
        this.OwningTree = InOwningtree;
        
      
        
    }
    
    
    
   

    /// <summary>
    /// Add a child node and set up proper parent-child relationship
    /// This method should be overridden by derived classes that can have children
    /// </summary>
    public virtual IBTNode AddChild(IBTNode childNode)
    {
        LoggingService.LogInfo($"🔧 BTNodeBase: AddChild called for {DebugDisplayName} - adding child: {childNode.DebugDisplayName}");
        
        // NEW: Set the parent reference for bidirectional access
        childNode.SetParentNode(this);
        LoggingService.LogInfo($"🔧 BTNodeBase: Set ParentNode for child {childNode.DebugDisplayName}");
        
        // Set the owning tree for the child
        childNode.SetOwiningTree(OwningTree);
        LoggingService.LogInfo($"🔧 BTNodeBase: Set OwningTree for child {childNode.DebugDisplayName}");
        
        // Set the tree for all services that don't have it set yet
        childNode.SetTreeForAllServices(OwningTree);
        LoggingService.LogInfo($"🔧 BTNodeBase: Set tree for all services of child {childNode.DebugDisplayName}");
        
        // If this is a GenericBTAction, also set the tree for its SubtreeInjectionService
        if (childNode is PActionNode action)
        {
            action.SetTreeForSubtreeInjectionService(OwningTree);
            LinkedBlackboard.SetActionInstance(action.InstanceName, action);
            LoggingService.LogInfo($"🔧 BTNodeBase: Set tree for SubtreeInjectionService of {childNode.DebugDisplayName}");
            
            // Track action addition
            BehaviorTreeComponentLogger.TrackActionAddition("GenericBTAction");
        }
        else if (childNode is BTFlowNodeBase flowNode)
        {
            LinkedBlackboard.SetFlowNodeInstance(flowNode.InstanceName, flowNode);
            LoggingService.LogInfo($"🔧 BTNodeBase: Set tree for all services of {childNode.DebugDisplayName}");
            
            // Flow node counting now handled in constructors via TrackFlowNodeInitialization
        }
        
        LoggingService.LogInfo($"🔧 BTNodeBase: AddChild completed for {childNode.DebugDisplayName}");
        return childNode;
    }
/// <summary>
/// main logic of the ticks. ticks decide which nodes are gonna be executed
/// </summary>
/// <param name="InDeltaTime"></param>
/// <returns></returns>
    public BTNodeResult Tick(float InDeltaTime)
    {
        // Initialize timing tracking
        tickStartTime = DateTime.Now;
        HasCompletedFullTick = false;
        totalTickCount++;
        
        LogTickStart();

        // First time running, reset the node
        if (status == BTNodeResult.Uninitialized)
        {
            // sets the status to ready to tick
            Reset();
        }

        // Run AlwaysOnServices phase
        CurrentTickPhase = EBTNodeTickPhase.AlwaysOnServices;
        if (!OnTick_AlwaysOnServices(InDeltaTime))
        {
            status = BTNodeResult.Failure;
            LogPhaseFailure("AlwaysOnServices");
            return OnTickReturn(status);
        }
        LogPhaseSuccess("AlwaysOnServices");

        // Run GeneralServices phase
        CurrentTickPhase = EBTNodeTickPhase.GeneralServices;
        if (!OnTick_GeneralServices(InDeltaTime))
        {
            status = BTNodeResult.Failure;
            LogPhaseFailure("GeneralServices");
            return OnTickReturn(status);
        }
        LogPhaseSuccess("GeneralServices");
        servicesEndTime = DateTime.Now;

        // Run Decorators phase
        CurrentTickPhase = EBTNodeTickPhase.Decorators;
        if (!OnTick_Decorators(InDeltaTime))
        {
            status = BTNodeResult.Failure;
            if (bDecoratorsAllowRunning && bCanSendExitNotification)
                OnExit();
            bDecoratorsAllowRunning = false;
            LogDecoratorBlocked();
            return OnTickReturn(status);
        }
        LogPhaseSuccess("Decorators");
        decoratorsEndTime = DateTime.Now;

        // Handle decorator state transition
        if (!bDecoratorsAllowRunning)
        {
            Reset();
        }
        bDecoratorsAllowRunning = true;

        // Check if node has already finished
        if (HasFinished)
        {
            return OnTickReturn(status);
        }

        // Call OnEnter if needed
        if (status == BTNodeResult.ReadyToTick)
        {
            OnEnter();
            if (HasFinished)
                return OnTickReturn(status);
        }


        // Tick children if this node has them
        if (HasChildren)
        {
            CurrentTickPhase = EBTNodeTickPhase.Children;
            if (!OnTick_Children(InDeltaTime))
            {
                return OnTickReturn(status);
            }
            LogPhaseSuccess("Children");
        }
        childrenEndTime = DateTime.Now;

         // Run NodeLogic phase
        CurrentTickPhase = EBTNodeTickPhase.NodeLogic;
        if (!OnTick_NodeLogic(InDeltaTime))
        {
            status = BTNodeResult.Failure;
            LogPhaseFailure("NodeLogic");
            return OnTickReturn(status);
        }
        LogPhaseSuccess("NodeLogic");
        nodeLogicEndTime = DateTime.Now;

        // Record tick completion
        tickEndTime = DateTime.Now;
        HasCompletedFullTick = true;
        LogTickCompletion();

        return OnTickReturn(status);
    }

    /// <summary>
    /// Logs the start of the tick operation with tracking information
    /// </summary>
    private void LogTickStart()
    {
        // Track flow node tick if this is a flow node
        if (this is BTFlowNodeBase)
        {
            BehaviorTreeComponentLogger.TrackFlowNodeTick(this.GetType().Name);
        }
        else if (this is PActionNode)
        {
            BehaviorTreeComponentLogger.TrackActionTick("GenericBTAction");
        }
        
        ExecutionFlowLogger.LogNodeTick(DebugDisplayName, GetType().Name, "START", status.ToString());
        LoggingService.LogInfo($"🔄 BTNodeBase: {DebugDisplayName} - Tick started");
    }

    /// <summary>
    /// Logs a phase failure and tracks it for statistics
    /// </summary>
    private void LogPhaseFailure(string phaseName)
    {
        LoggingService.LogWarning($"❌ BTNodeBase: {phaseName} failed for {DebugDisplayName}");
        ExecutionFlowLogger.LogPhaseTransition(DebugDisplayName, phaseName, "EXIT");
        BehaviorTreeComponentLogger.TrackNodeFailure(this.GetType().Name, DebugDisplayName);
    }

    /// <summary>
    /// Logs a phase success with phase transition
    /// </summary>
    private void LogPhaseSuccess(string phaseName)
    {
        LoggingService.LogInfo($"✅ BTNodeBase: {DebugDisplayName} - {phaseName} completed successfully");
        if (phaseName != "AlwaysOnServices")
        {
            string previousPhase = phaseName == "GeneralServices" ? "AlwaysOnServices" :
                                   phaseName == "Decorators" ? "GeneralServices" :
                                   phaseName == "NodeLogic" ? "Decorators" : "NodeLogic";
            ExecutionFlowLogger.LogPhaseTransition(DebugDisplayName, previousPhase, phaseName);
        }
    }

    /// <summary>
    /// Logs when decorators block execution
    /// </summary>
    private void LogDecoratorBlocked()
    {
        LoggingService.LogInfo($"⏳ BTNodeBase: Decorators blocked execution for {DebugDisplayName}");
        BehaviorTreeComponentLogger.TrackNodeFailure(this.GetType().Name, DebugDisplayName);
    }

    /// <summary>
    /// Logs the completion of the full tick with timing information
    /// </summary>
    private void LogTickCompletion()
    {
        LoggingService.LogInfo($"⏱️ BTNodeBase: {DebugDisplayName} - Tick timing: Services={ServicesDuration.TotalMilliseconds:F2}ms, Decorators={DecoratorsDuration.TotalMilliseconds:F2}ms, NodeLogic={NodeLogicDuration.TotalMilliseconds:F2}ms, Children={ChildrenDuration.TotalMilliseconds:F2}ms, Total={TotalTickDuration.TotalMilliseconds:F2}ms");
        TickTimingLogger.TrackTickTiming(this);
        LoggingService.LogInfo($"✅ BTNodeBase: {DebugDisplayName} - Tick method completed successfully, returning {status}");
    }
    /// <summary>
    ///  
    /// </summary>
    /// <param name="InProvisionalResult"></param>
    /// <returns></returns>
    protected virtual BTNodeResult OnTickReturn(BTNodeResult InProvisionalResult)
    {
        BTNodeResult FinalResult = InProvisionalResult;
        CurrentTickPhase = EBTNodeTickPhase.WaitingForNextTick;
        // if(Decorators != null)
        // {
        //     foreach(var Decorator in Decorators)
        //     {
        //         if (Decorator.CanPostProcessTickResult)
        //             FinalResult = Decorator.PostProcessTickResult(FinalResult);

        //     }
        // }
        
        // Track success and failure counts
        if (FinalResult == BTNodeResult.Success)
        {
            successCount++;
            // Track flow node success if this is a flow node
            if (this is BTFlowNodeBase)
            {
                BehaviorTreeComponentLogger.TrackFlowNodeSuccess(this.GetType().Name);
            }
            // Track action success if this is an action
            else if (this is PActionNode)
            {
                BehaviorTreeComponentLogger.TrackActionSuccess("GenericBTAction");
            }
            // Simplified tracking - detailed node success tracking removed
        }
        else if (FinalResult == BTNodeResult.Failure)
        {
            failureCount++;
            // Track flow node failure if this is a flow node
            if (this is BTFlowNodeBase)
            {
                BehaviorTreeComponentLogger.TrackFlowNodeFailure(this.GetType().Name);
            }
            // Track action failure if this is an action
            else if (this is PActionNode)
            {
                BehaviorTreeComponentLogger.TrackActionFailure("GenericBTAction");
            }
            // Simplified tracking - detailed node failure tracking removed
        }
        
        if (bCanSendExitNotification && HasFinished)
            OnExit();

        return FinalResult;
    }
    /// <summary>
    /// goes through the services, and if any of the services's thick return's false, then the function returns false
    /// </summary>
    /// <param name="InDeltaTime"></param>
    /// <returns></returns>
    protected virtual bool OnTick_AlwaysOnServices(float InDeltaTime)
    {
        if(AlwaysOnServices != null)
        {
            foreach(var service in AlwaysOnServices)
            {
                if (!service.Tick(InDeltaTime))
                    return false;
            }
        }
        return true;
    }
    protected virtual bool OnTick_GeneralServices(float InDeltaTime)
    {
        LoggingService.LogInfo($"🚨 DEBUG: BTNodeBase.OnTick_GeneralServices called for {DebugDisplayName}");
        LoggingService.LogInfo($"🔍 BTNodeBase: GeneralServices count: {GenrealServices?.Count ?? 0}");
        
        if(GenrealServices != null && GenrealServices.Count > 0)
        {
            LoggingService.LogInfo($"🔍 BTNodeBase: Executing {GenrealServices.Count} general services");
            foreach(var service in GenrealServices)
            {
                LoggingService.LogInfo($"   🔄 BTNodeBase: Calling service.Tick() for {service.GetType().Name}");
                
                // Log service tick start
                ExecutionFlowLogger.LogServiceTick(service.GetType().Name, "GeneralService", DebugDisplayName, "START");
                
                bool serviceResult = service.Tick(InDeltaTime);
                
                // Log service tick result
                ExecutionFlowLogger.LogServiceTick(service.GetType().Name, "GeneralService", DebugDisplayName, serviceResult ? "SUCCESS" : "FAILED");
                
                if (!serviceResult)
                {
                    LoggingService.LogWarning($"   ❌ BTNodeBase: Service {service.GetType().Name} returned false");
                    return false;
                }
                LoggingService.LogInfo($"   ✅ BTNodeBase: Service {service.GetType().Name} returned true");
            }
            LoggingService.LogInfo($"   ✅ BTNodeBase: All general services completed successfully");
        }
        else
        {
            LoggingService.LogInfo($"🔍 BTNodeBase: No general services to execute");
        }
        return true;
    }
    protected virtual bool OnTick_Decorators(float InDeltaTime)
    {
        LoggingService.LogInfo($"🔄 BTNodeBase: {DebugDisplayName} - OnTick_Decorators called, decorator count: {Decorators?.Count ?? 0}");
        
        if(Decorators != null)
        {
            LoggingService.LogInfo($"🔄 BTNodeBase: {DebugDisplayName} - Starting decorator evaluation for {Decorators.Count} decorators");
            
            foreach(var decorator in Decorators)
            {
                LoggingService.LogInfo($"🔄 BTNodeBase: {DebugDisplayName} - Evaluating decorator: {decorator.GetType().Name}");
                
                // Log decorator tick start
                ExecutionFlowLogger.LogDecoratorTick(decorator.GetType().Name, "Decorator", DebugDisplayName, "START");
                
                bool decoratorResult = decorator.Tick(InDeltaTime);
                
                // Log decorator tick result
                ExecutionFlowLogger.LogDecoratorTick(decorator.GetType().Name, "Decorator", DebugDisplayName, decoratorResult ? "ALLOW" : "BLOCK");
                
                LoggingService.LogInfo($"🔄 BTNodeBase: {DebugDisplayName} - Decorator {decorator.GetType().Name} result: {(decoratorResult ? "ALLOW" : "BLOCK")}");
                
                if (!decoratorResult)
                {
                    LoggingService.LogInfo($"🔄 BTNodeBase: {DebugDisplayName} - Decorator {decorator.GetType().Name} BLOCKED execution, returning false");
                    return false;
                }
            }
            
            LoggingService.LogInfo($"✅ BTNodeBase: {DebugDisplayName} - All {Decorators.Count} decorators evaluated successfully, returning true");
        }
        else
        {
            LoggingService.LogInfo($"🔄 BTNodeBase: {DebugDisplayName} - No decorators to evaluate, returning true");
        }
        return true;
    }
    // these ones are the ones that actually execute a node logic
    protected abstract bool OnTick_NodeLogic(float InDeltaTime);
    //this one is for the flow nodes 
    protected abstract bool OnTick_Children(float InDeltaTime);

    protected virtual void OnEnter()
    {
        bCanSendExitNotification = true;

    }
    protected virtual void OnExit()
    {
        bCanSendExitNotification = false;
    }
}

