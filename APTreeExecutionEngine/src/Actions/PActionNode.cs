using System;
using System.Collections.Generic;
using System.Linq;
using BehaviorTreeMainProject.Services;
using BehaviorTreeMainProject;
using BehaviorTreeMainProject.Log.Services;


// Generic action class that will be created by the factory
public abstract class PActionNode : ActionNode
{
    public override string TypeName => "GenericBTAction";
    public readonly FastName actionType;
    private readonly Blackboard<FastName> blackboard;
    public int cost;

    // High-level action support
    public bool IsHighLevelAction { get; set; } = false;
    public DynamicFlowNode HighLevelSubtree { get; set; }
    public Service ServicePlanning { get; protected set; }

    // ServiceSubtreeInject access
    public ServiceSubtreeInject ServiceSubtreeInject => GetSubtreeInjectionService();

    // Abstract properties for preconditions and effects
    protected abstract State Preconditions { get; }
    protected abstract State Effects { get; }

    /// <summary>
    /// HL actions with a subtree report having children so the BT lifecycle
    /// routes subtree execution through the Children phase, not NodeLogic.
    /// </summary>
    public override bool HasChildren => IsHighLevelAction && HighLevelSubtree != null;

    public override string DebugDisplayName
    {
        get => debugDisplayName;
        protected set => debugDisplayName = InstanceName.ToString();
    }

    // Constructor for action instances
    public PActionNode(
        string actionType,
        string instanceName,
        Blackboard<FastName> blackboard
    ) : base(blackboard, instanceName)
    {
        LoggingService.LogInfo($"🔧 GenericBTAction: Constructor called for {instanceName} with actionType: {actionType}");
        
        // Dynamically set actionType to the actual class name
        string actualClassName = this.GetType().Name;
        this.actionType = new FastName(actualClassName);
        
        LoggingService.LogInfo($"🔧 GenericBTAction: Set actionType to actual class name: {actualClassName} (was: {actionType})");
        
        this.blackboard = blackboard;
        
        LoggingService.LogInfo($"🔧 GenericBTAction: About to call InitializeSubtreeInjectionService for {instanceName}");
        
        // Automatically add ServiceSubtreeInject to all actions
        InitializeSubtreeInjectionService();
        
        // Automatically add ServiceLLSubtreeInject (only acts on ML actions)
        InitializeLLSubtreeInjectionService();
        
        LoggingService.LogInfo($"🔧 GenericBTAction: Constructor completed for {instanceName}");
    }

    /// <summary>
    /// Set this action as a high-level action with a subtree and planning service
    /// </summary>
    public void SetAsHighLevelAction(DynamicFlowNode subtree, Service planningService)
    {
       
            LoggingService.LogInfo($"🧹 GenericBTAction: Cleaning up old subtree before setting new one");

            IsHighLevelAction = true;
            HighLevelSubtree = subtree;
            ServicePlanning = planningService;

            // NEW: Establish bidirectional parent-child relationship
            subtree.SetParentNode(this);

            // Attach decorator that handles planning state reset on subtree success
            AddDecorator(new DecoratorResetOnSubtreeSuccess(this));

            LoggingService.LogInfo($"🔧 GenericBTAction: Set {InstanceName.ToString()} as high-level action with subtree type: {subtree.GetType().Name}");
            LoggingService.LogInfo($"🔧 GenericBTAction: ServicePlanning type: {planningService?.GetType().Name ?? "None"}");
            LoggingService.LogInfo($"🔧 GenericBTAction: Established parent-child relationship: {InstanceName.ToString()} ↔ {subtree.DebugDisplayName}");
        
    }

    /// <summary>
    /// Remove the subtree and fall back to normal execution
    /// </summary>
    public void RemoveSubtree()
    {
        IsHighLevelAction = false;
        HighLevelSubtree = null;
        ServicePlanning = null;
        // Console.WriteLine($"🔧 GenericBTAction: Removed subtree from {InstanceName.ToString()}");
    }

    /// <summary>
    /// Get the effects of this action (for PDDL problem generation)
    /// </summary>
    public State GetEffects()
    {
        return Effects;
    }

    public void applyEffects()
    {
        LoggingService.LogInfo($"🔧 APPLY_EFFECTS: Starting applyEffects() for action: {InstanceName.ToString()}");
        LoggingService.LogInfo($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        
        // Apply effects to the blackboard
        if (Effects != null)
        {
            var effectsCount = Effects.GetAllPredicates().Count();
            LoggingService.LogInfo($"🔧 APPLY_EFFECTS: Effects collection is not null, processing {effectsCount} effects");
            var predicates = GetActionEffects();
            LoggingService.LogInfo($"🔧 APPLY_EFFECTS: Retrieved {predicates.Count} predicates from GetActionEffects()");
            
            // Log blackboard state before applying effects
            var blackboardBeforeCount = blackboard.GetAllPredicates().Count;
            LoggingService.LogInfo($"🔧 APPLY_EFFECTS: Blackboard predicate count BEFORE applying effects: {blackboardBeforeCount}");
            
            foreach (var predicate in predicates)
            {
                LoggingService.LogInfo($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                LoggingService.LogInfo($"🔧 APPLY_EFFECTS: Processing predicate #{predicates.IndexOf(predicate) + 1} of {predicates.Count}");
                LoggingService.LogInfo($"🔧 APPLY_EFFECTS: Predicate type: {predicate.GetType().Name}");
                LoggingService.LogInfo($"🔧 APPLY_EFFECTS: Predicate.PredicateName (unique key): {predicate.PredicateName}");
                LoggingService.LogInfo($"🔧 APPLY_EFFECTS: Predicate.isNegated: {predicate.not}");
                
                // Log predicate parameters if available
                try
                {
                    var parameters = predicate.GetParameterValues();
                    if (parameters != null && parameters.Count > 0)
                    {
                        LoggingService.LogInfo($"🔧 APPLY_EFFECTS: Predicate parameters: {string.Join(", ", parameters)}");
                    }
                    else
                    {
                        LoggingService.LogInfo($"🔧 APPLY_EFFECTS: Predicate has no parameters");
                    }
                }
                catch (Exception ex)
                {
                    LoggingService.LogWarning($"🔧 APPLY_EFFECTS: Could not extract predicate parameters: {ex.Message}");
                }
                
                // Check if predicate already exists in blackboard (we'll check after setting)
                LoggingService.LogInfo($"🔧 APPLY_EFFECTS: About to set predicate in blackboard");
                
                // PredicateName already contains the unique key generated in the constructor
                LoggingService.LogInfo($"🔧 APPLY_EFFECTS: Calling blackboard.SetPredicateSync with key: {predicate.PredicateName}");
                blackboard.SetPredicateSync(predicate.PredicateName, predicate);
                LoggingService.LogSuccess($"🔧 APPLY_EFFECTS: Successfully called SetPredicateSync for predicate: {predicate.PredicateName}");
                
                // Verify the predicate was actually set by checking if it's in GetAllPredicates
                var allPredicates = blackboard.GetAllPredicates();
                var storedPredicate = allPredicates.FirstOrDefault(p => p.PredicateName == predicate.PredicateName);
                if (storedPredicate != null)
                {
                    LoggingService.LogSuccess($"🔧 APPLY_EFFECTS: ✅ VERIFIED: Predicate stored successfully in blackboard");
                    LoggingService.LogInfo($"🔧 APPLY_EFFECTS: Stored predicate.isNegated: {storedPredicate.not}");
                }
                else
                {
                    LoggingService.LogError($"🔧 APPLY_EFFECTS: ❌ ERROR: Predicate was NOT stored in blackboard!");
                }
            }
            
            // Log blackboard state after applying effects
            var blackboardAfterCount = blackboard.GetAllPredicates().Count;
            LoggingService.LogInfo($"🔧 APPLY_EFFECTS: Blackboard predicate count AFTER applying effects: {blackboardAfterCount}");
            LoggingService.LogInfo($"🔧 APPLY_EFFECTS: Net change in blackboard predicates: {blackboardAfterCount - blackboardBeforeCount}");
            
            // Log summary of all predicates in blackboard
            LoggingService.LogInfo($"🔧 APPLY_EFFECTS: Final blackboard predicate summary:");
            var finalPredicates = blackboard.GetAllPredicates();
            foreach (var pred in finalPredicates)
            {
                LoggingService.LogInfo($"   📋 {pred.PredicateName}: {pred.GetType().Name} (isNegated: {pred.not})");
            }
        }
        else
        {
            LoggingService.LogWarning($"🔧 APPLY_EFFECTS: Effects collection is null, no effects to apply");
        }
        
        LoggingService.LogInfo($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        LoggingService.LogInfo($"🔧 APPLY_EFFECTS: Completed applyEffects() for action: {InstanceName.ToString()}");
    }

    /// <summary>
    /// Called when the node enters (starts). Checks if preconditions are met from the blackboard.
    /// If preconditions are not met, sets status to Failure so the node short-circuits before NodeLogic.
    /// </summary>
    protected override void OnEnter()
    {
        base.OnEnter();

        // Only check preconditions for mid-level (non-HL) actions.
        // HL actions run in parallel cassettes with a shared blackboard, so their preconditions
        // may reflect a planned state that no longer matches the current blackboard (e.g. after
        // another cassette applied its effects). The PDDL planner already ensures validity when
        // it creates the ML-level subtree for each HL action based on the current blackboard state.
        if (!IsHighLevelAction)
        {
            LoggingService.LogInfo($"🔍 GenericBTAction: {InstanceName.ToString()} OnEnter - checking preconditions (ML action)");

            if (!CheckPreconditions())
            {
                LoggingService.LogWarning($"❌ GenericBTAction: {InstanceName.ToString()} preconditions NOT met, setting status to Failure");
                status = BTNodeResult.Failure;
            }
            else
            {
                LoggingService.LogSuccess($"✅ GenericBTAction: {InstanceName.ToString()} all preconditions met");
            }
        }
        else
        {
            LoggingService.LogInfo($"🔍 GenericBTAction: {InstanceName.ToString()} OnEnter - skipping precondition check (HL action, PDDL planner handles validity)");
        }
    }

    /// <summary>
    /// Checks whether all preconditions of this action are satisfied by the current blackboard state.
    /// For each precondition predicate:
    ///   - If the predicate is positive (not negated), the blackboard must contain a matching predicate that is also not negated.
    ///   - If the predicate is negative (negated), the blackboard must either not contain it, or contain it as negated.
    /// </summary>
    /// <returns>True if all preconditions are met, false otherwise.</returns>
    private bool CheckPreconditions()
    {
        if (Preconditions == null)
        {
            LoggingService.LogInfo($"🔍 PRECONDITIONS: No preconditions defined for {InstanceName.ToString()}, passing by default");
            return true;
        }

        var preconditionPredicates = Preconditions.GetAllPredicates();
        if (!preconditionPredicates.Any())
        {
            LoggingService.LogInfo($"🔍 PRECONDITIONS: Preconditions state is empty for {InstanceName.ToString()}, passing by default");
            return true;
        }

        var blackboardPredicates = blackboard.GetAllPredicates();
        LoggingService.LogInfo($"🔍 PRECONDITIONS: Checking {preconditionPredicates.Count()} preconditions against {blackboardPredicates.Count} blackboard predicates for {InstanceName.ToString()}");

        foreach (var precondition in preconditionPredicates)
        {
            LoggingService.LogInfo($"🔍 PRECONDITIONS: Checking precondition: {precondition.PredicateName} (negated: {precondition.not})");

            // Find matching predicate in blackboard by PredicateName
            var matchingPredicate = blackboardPredicates.FirstOrDefault(p => p.PredicateName == precondition.PredicateName);

            if (precondition.not)
            {
                // Negated precondition: satisfied if blackboard doesn't have it, or has it as negated
                if (matchingPredicate != null && !matchingPredicate.not)
                {
                    LoggingService.LogWarning($"❌ PRECONDITIONS: Negated precondition FAILED - {precondition.PredicateName} exists as positive in blackboard");
                    return false;
                }
                LoggingService.LogInfo($"✅ PRECONDITIONS: Negated precondition passed - {precondition.PredicateName}");
            }
            else
            {
                // Positive precondition: satisfied if blackboard has it and it's not negated
                if (matchingPredicate == null)
                {
                    LoggingService.LogWarning($"❌ PRECONDITIONS: Positive precondition FAILED - {precondition.PredicateName} not found in blackboard");
                    return false;
                }
                if (matchingPredicate.not)
                {
                    LoggingService.LogWarning($"❌ PRECONDITIONS: Positive precondition FAILED - {precondition.PredicateName} is negated in blackboard");
                    return false;
                }
                LoggingService.LogInfo($"✅ PRECONDITIONS: Positive precondition passed - {precondition.PredicateName}");
            }
        }

        return true;
    }

    /// <summary>
    /// NodeLogic: Only handles action-level logic.
    /// For HL actions this is a pass-through (subtree execution is handled in OnTick_Children).
    /// For ML actions this executes the actual action logic.
    /// </summary>
    protected override bool OnTick_NodeLogic(float InDeltaTime)
    {
        LoggingService.LogInfo($"🚨 DEBUG: OnTick_NodeLogic called for {InstanceName.ToString()}");
        LoggingService.LogInfo($"🔍 GenericBTAction: {InstanceName.ToString()} OnTick_NodeLogic - IsHighLevelAction: {IsHighLevelAction}, HighLevelSubtree: {(HighLevelSubtree != null ? "exists" : "null")}");
        LoggingService.LogInfo($"🔍 GenericBTAction: {InstanceName.ToString()} LastStatus before: {status}");

        if (IsHighLevelAction && HighLevelSubtree != null)
        {
            // HL action: NodeLogic is a pass-through.
            // The subtree will be ticked in the Children phase.
            LoggingService.LogInfo($"🔧 GenericBTAction: {InstanceName.ToString()} is HL action — NodeLogic pass-through, subtree handled in Children phase");
            return true;
        }
        else
        {
            // ML action: execute the actual action logic
            var actionName = this.GetType().Name;
            var instanceName = InstanceName.ToString();
            ActionExecutionLogger.Instance.LogActionStarted(actionName, instanceName, $"ML action execution, DeltaTime: {InDeltaTime:F3}");

            LoggingService.LogInfo($"🔧 GenericBTAction: {InstanceName.ToString()} executing ML action logic");
            var result = ExecuteActionLogic(InDeltaTime);
            LoggingService.LogInfo($"📊 GenericBTAction: {InstanceName.ToString()} ML action result: {result}, LastStatus: {status}");
            return result;
        }
    }

    /// <summary>
    /// Children phase: Ticks the HL subtree. This is the BT-native way to delegate
    /// execution to a child node, keeping subtree execution separate from action logic.
    /// Only called when HasChildren is true (i.e. IsHighLevelAction && HighLevelSubtree != null).
    /// </summary>
    protected override bool OnTick_Children(float InDeltaTime)
    {
        var actionName = this.GetType().Name;
        var instanceName = InstanceName.ToString();
        ActionExecutionLogger.Instance.LogActionStarted(actionName, instanceName, $"HL subtree tick: {HighLevelSubtree.DebugDisplayName}, DeltaTime: {InDeltaTime:F3}");

        LoggingService.LogInfo($"🔧 GenericBTAction: {InstanceName.ToString()} Children phase — ticking subtree {HighLevelSubtree.DebugDisplayName}");

        // Tick the subtree
        var subtreeResult = HighLevelSubtree.Tick(InDeltaTime);

        // Propagate subtree status to this action
        status = HighLevelSubtree.status;

        LoggingService.LogInfo($"📊 GenericBTAction: {InstanceName.ToString()} Subtree result: {subtreeResult}, Status: {status}");

        if (subtreeResult == BTNodeResult.Failure)
        {
            ActionExecutionLogger.Instance.LogActionFailed(actionName, instanceName, "Subtree execution failed");
            LoggingService.LogWarning($"❌ GenericBTAction: {InstanceName.ToString()} subtree failed");
            return false;
        }
        else if (subtreeResult == BTNodeResult.Success)
        {
            ActionExecutionLogger.Instance.LogActionCompleted(actionName, instanceName, "Subtree execution completed successfully");

            // Run the HL action's own logic (sets status to Success)
            LoggingService.LogInfo($"🔧 GenericBTAction: {InstanceName.ToString()} subtree succeeded — executing post-subtree action logic");
            var postResult = ExecuteActionLogic(InDeltaTime);
            LoggingService.LogInfo($"� GenericBTAction: {InstanceName.ToString()} post-subtree result: {postResult}, LastStatus: {status}");
            return postResult;
        }
        else
        {
            // Subtree still in progress
            LoggingService.LogSuccess($"✅ GenericBTAction: {InstanceName.ToString()} subtree in progress");
            return true;
        }
    }
    

    /// <summary>
    /// Execute the actual action logic (to be implemented by derived classes)
    /// </summary>
    protected bool ExecuteActionLogic(float InDeltaTime)
    {
        // Log action execution start to separate file
        var actionName = this.GetType().Name;
        var instanceName = InstanceName.ToString();
        ActionExecutionLogger.Instance.LogActionStarted(actionName, instanceName, $"DeltaTime: {InDeltaTime:F3}");
        
        LoggingService.LogInfo($"🔧 GenericBTAction: {InstanceName.ToString()} executing ExecuteActionLogic");
        
        // Log successful completion
        ActionExecutionLogger.Instance.LogActionCompleted(actionName, instanceName, "Action logic completed successfully");
        LoggingService.LogInfo($"🔧 GenericBTAction: {InstanceName.ToString()} setting status to Succeeded");

        return SetStatusAndCalculateReturnvalue(BTNodeResult.Success);
    }

    /// <summary>
    /// Called when the node exits (finishes). If successful, apply effects to the blackboard.
    /// </summary>
    protected override void OnExit()
    {
        if (status == BTNodeResult.Success)
        {
            LoggingService.LogInfo($"🔧 GenericBTAction: {InstanceName.ToString()} OnExit - applying effects to blackboard");
            try
            {
                applyEffects();
                LoggingService.LogInfo($"🔧 GenericBTAction: {InstanceName.ToString()} effects applied successfully on exit");
                ActionExecutionLogger.Instance.LogActionCompleted(this.GetType().Name, InstanceName.ToString(), "Effects applied on exit");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"❌ GenericBTAction: {InstanceName.ToString()} failed to apply effects on exit: {ex.Message}");
                ActionExecutionLogger.Instance.LogActionFailed(this.GetType().Name, InstanceName.ToString(), $"Effect application failed on exit: {ex.Message}");
            }
        }

        base.OnExit();
    }

    /// <summary>
    /// Initialize and add ServiceSubtreeInject to this action
    /// </summary>
    private void InitializeSubtreeInjectionService()
    {
        LoggingService.LogInfo($"🔧 GenericBTAction: InitializeSubtreeInjectionService called for {InstanceName.ToString()}");
        
        try
        {
            LoggingService.LogInfo($"🔧 GenericBTAction: InitializeSubtreeInjectionService called for {InstanceName.ToString()}");
            
            // Create a new ServiceSubtreeInject instance for this action without the tree initially
            var subtreeService = new ServiceSubtreeInject(this);
            LoggingService.LogInfo($"🔧 GenericBTAction: Created ServiceSubtreeInject instance for {InstanceName.ToString()}");
            LoggingService.LogInfo($"🔧 GenericBTAction: Created ServiceSubtreeInject instance for {InstanceName.ToString()}");
            
            // Add it to the GeneralServices (not AlwaysOnServices since it should only run when needed)
            LoggingService.LogInfo($"🔧 GenericBTAction: About to call AddService for {InstanceName.ToString()}");
            AddService(subtreeService, false); // false = not always on
            LoggingService.LogInfo($"🔧 GenericBTAction: Called AddService for {InstanceName.ToString()}");
            LoggingService.LogInfo($"🔧 GenericBTAction: Called AddService for {InstanceName.ToString()}");
            
            // Verify the service was added
            var addedService = GetSubtreeInjectionService();
            if (addedService != null)
            {
                LoggingService.LogSuccess($"✅ GenericBTAction: Successfully added ServiceSubtreeInject to {InstanceName.ToString()}");
                LoggingService.LogInfo($"✅ GenericBTAction: Successfully added ServiceSubtreeInject to {InstanceName.ToString()}");
            }
            else
            {
                LoggingService.LogWarning($"❌ GenericBTAction: Failed to add ServiceSubtreeInject to {InstanceName.ToString()} - GetSubtreeInjectionService returned null");
                LoggingService.LogWarning($"❌ GenericBTAction: Failed to add ServiceSubtreeInject to {InstanceName.ToString()} - GetSubtreeInjectionService returned null");
            }
        }
        catch (Exception ex)
        {
            LoggingService.LogError($"❌ GenericBTAction: Exception in InitializeSubtreeInjectionService for {InstanceName.ToString()}: {ex.Message}");
            LoggingService.LogError($"❌ GenericBTAction: Failed to initialize ServiceSubtreeInject for {InstanceName.ToString()}: {ex.Message}");
        }
    }

    /// <summary>
    /// Set the tree for the ServiceSubtreeInject after the action is added to the tree
    /// This should be called after SetOwiningTree is called on the action
    /// </summary>
    public void SetTreeForSubtreeInjectionService(IBehaviorTree InOwningtree)
    {
        var subtreeService = GetSubtreeInjectionService();
        if (subtreeService != null)
        {
            subtreeService.SetOwiningTree(InOwningtree);
        }
        var llService = GetLLSubtreeInjectionService();
        if (llService != null)
        {
            llService.SetOwiningTree(InOwningtree);
        }
    }

    /// <summary>
    /// Initialize and add ServiceLLSubtreeInject to this action.
    /// Only acts on ML-level actions (checked at tick time in OnEvaluate).
    /// </summary>
    private void InitializeLLSubtreeInjectionService()
    {
        try
        {
            var llService = new ServiceLLSubtreeInject(this);
            AddService(llService, false);
            LoggingService.LogInfo($"✅ GenericBTAction: Added ServiceLLSubtreeInject to {InstanceName.ToString()}");
        }
        catch (Exception ex)
        {
            LoggingService.LogError($"❌ GenericBTAction: Failed to add ServiceLLSubtreeInject for {InstanceName.ToString()}: {ex.Message}");
        }
    }

    /// <summary>
    /// Get the ServiceLLSubtreeInject associated with this action
    /// </summary>
    public ServiceLLSubtreeInject GetLLSubtreeInjectionService()
    {
        if (GenrealServices != null)
        {
            foreach (var service in GenrealServices)
            {
                if (service is ServiceLLSubtreeInject llService)
                    return llService;
            }
        }
        return null;
    }

    /// <summary>
    /// Get the ServiceSubtreeInject associated with this action
    /// </summary>
    public ServiceSubtreeInject GetSubtreeInjectionService()
    {
        if (GenrealServices != null)
        {
            foreach (var service in GenrealServices)
            {
                if (service is ServiceSubtreeInject subtreeService)
                {
                    return subtreeService;
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Get action effects for goals
    /// </summary>
    public List<Predicate> GetActionEffects()
    {
        var effects = new List<Predicate>();

        try
        {
            // Access the Effects property from the action using the public method
            foreach (var predicate in Effects.GetAllPredicates())
            {
                effects.Add(predicate); // This line was missing!
            }

            // Console.WriteLine($"🎯 ServiceSubtreeInject: Retrieved {effects.Count} effects from action");
        }
        catch (Exception ex)
        {
            // Console.WriteLine($"❌ ServiceSubtreeInject: Error getting action effects: {ex.Message}");
        }

        return effects;
    }

    /// <summary>
    /// Get the current status of this action
    /// </summary>
    public BTNodeResult GetCurrentStatus()
    {
        return status;
    }

   
}

