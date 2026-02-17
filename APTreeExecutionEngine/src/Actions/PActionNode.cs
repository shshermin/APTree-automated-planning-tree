using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
    public bool IsHighLevelAction { get; protected set; } = false;
    public BTFlowNode_Dynamic HighLevelSubtree { get; protected set; }
    public BTServiceBase PlanningService { get; protected set; }

    // SubtreeInjectionService access
    public SubtreeInjectionService SubtreeInjectionService => GetSubtreeInjectionService();

    // Abstract properties for preconditions and effects
    protected abstract State Preconditions { get; }
    protected abstract State Effects { get; }

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
        
        // Automatically add SubtreeInjectionService to all actions
        InitializeSubtreeInjectionService();
        
        LoggingService.LogInfo($"🔧 GenericBTAction: Constructor completed for {instanceName}");
        
        // Track action node creation for execution summary
        ExecutionSummaryLogger.TrackNodeCreation("GenericBTAction");
    }

    /// <summary>
    /// Set this action as a high-level action with a subtree and planning service
    /// </summary>
    public void SetAsHighLevelAction(BTFlowNode_Dynamic subtree, BTServiceBase planningService)
    {
       
            LoggingService.LogInfo($"🧹 GenericBTAction: Cleaning up old subtree before setting new one");

            IsHighLevelAction = true;
            HighLevelSubtree = subtree;
            PlanningService = planningService;

            // NEW: Establish bidirectional parent-child relationship
            subtree.SetParentNode(this);
            LoggingService.LogInfo($"🔧 GenericBTAction: Set {InstanceName.ToString()} as high-level action with subtree type: {subtree.GetType().Name}");
            LoggingService.LogInfo($"🔧 GenericBTAction: PlanningService type: {planningService.GetType().Name}");
            LoggingService.LogInfo($"🔧 GenericBTAction: Established parent-child relationship: {InstanceName.ToString()} ↔ {subtree.DebugDisplayName}");
        
    }

    /// <summary>
    /// Remove the subtree and fall back to normal execution
    /// </summary>
    public void RemoveSubtree()
    {
        IsHighLevelAction = false;
        HighLevelSubtree = null;
        PlanningService = null;
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
    /// Override the base OnTick_NodeLogic to handle high-level actions with planning phase support
    /// </summary>
    protected override bool OnTick_NodeLogic(float InDeltaTime)
    {
        LoggingService.LogInfo($"🚨 DEBUG: OnTick_NodeLogic called for {InstanceName.ToString()}");
        LoggingService.LogInfo($"🔍 GenericBTAction: {InstanceName.ToString()} OnTick_NodeLogic - IsHighLevelAction: {IsHighLevelAction}, HighLevelSubtree: {(HighLevelSubtree != null ? "exists" : "null")}");
        LoggingService.LogInfo($"🔍 GenericBTAction: {InstanceName.ToString()} LastStatus before: {status}");
        LoggingService.LogInfo($"🔍 GenericBTAction: {InstanceName.ToString()} ActionType: {actionType.ToString()}");
        LoggingService.LogInfo($"🔍 GenericBTAction: {InstanceName.ToString()} PlanningPhase: {blackboard.PlanningPhase}");
        
        // Component execution will be tracked by the base class through OnTickReturn
        
        // Check general services
        LoggingService.LogInfo($"🔍 GenericBTAction: {InstanceName.ToString()} GeneralServices count: {GenrealServices?.Count ?? 0}");
        if (GenrealServices != null && GenrealServices.Count > 0)
        {
            LoggingService.LogInfo($"🔍 GenericBTAction: {InstanceName.ToString()} GeneralServices list:");
            for (int i = 0; i < GenrealServices.Count; i++)
            {
                var service = GenrealServices[i];
                LoggingService.LogInfo($"   📋 Service {i+1}: {service.GetType().Name}");
            }
        }
        else
        {
            LoggingService.LogInfo($"🔍 GenericBTAction: {InstanceName.ToString()} No GeneralServices found");
        }

       
            // Execution phase - normal behavior
            if (IsHighLevelAction && HighLevelSubtree != null)
            {
                // Log high-level action execution start to separate file
                var actionName = this.GetType().Name;
                var instanceName = InstanceName.ToString();
                ActionExecutionLogger.Instance.LogActionStarted(actionName, instanceName, $"High-level execution with subtree: {HighLevelSubtree.DebugDisplayName}, DeltaTime: {InDeltaTime:F3}");
                
                LoggingService.LogInfo($"🔧 GenericBTAction: {InstanceName.ToString()} is high-level action, delegating to subtree");
                LoggingService.LogInfo($"🔧 GenericBTAction: {InstanceName.ToString()} Subtree type: {HighLevelSubtree.GetType().Name}");
                LoggingService.LogInfo($"🔧 GenericBTAction: {InstanceName.ToString()} PlanningService: {(PlanningService != null ? PlanningService.GetType().Name : "null")}");

            // Delegate execution to the subtree
              
                var subtreeResult = HighLevelSubtree.Tick(InDeltaTime);

                // Propagate subtree status to this action
                status = HighLevelSubtree.status;

                LoggingService.LogInfo($"📊 GenericBTAction: Subtree result: {subtreeResult}, Status: {status}");

                // Return true to continue ticking if subtree is in progress, false if it failed
                if (subtreeResult == BTNodeResult.Failure)
                {
                    // Log high-level action failure
                    ActionExecutionLogger.Instance.LogActionFailed(actionName, instanceName, "Subtree execution failed");

                    LoggingService.LogWarning($"❌ GenericBTAction: {InstanceName.ToString()} returning false (subtree failed)");
                    // Let the base class handle failure tracking through OnTickReturn
                    return false;
                }
                else if (subtreeResult == BTNodeResult.Success)
                {
                    // Log high-level action completion
                    ActionExecutionLogger.Instance.LogActionCompleted(actionName, instanceName, "Subtree execution completed successfully");

                    // Reset planning state after successful HL action completion
                    ResetPlanningStateAfterSuccess();

                    // After a successful subtree, execute this action's logic (apply effects)
                    LoggingService.LogInfo($"🔧 GenericBTAction: {InstanceName.ToString()} executing post-subtree action logic");
                    var postResult = ExecuteActionLogic(InDeltaTime);
                    LoggingService.LogInfo($"📊 GenericBTAction: {InstanceName.ToString()} post-subtree action result: {postResult}, LastStatus: {status}");
                    return postResult;
                }
                else
                {
                    // Subtree still in progress
                    LoggingService.LogSuccess($"✅ GenericBTAction: {InstanceName.ToString()} returning true (subtree in progress)");
                    return true;
                }
            }
            else
            {
                // Log action execution start to separate file
                var actionName = this.GetType().Name;
                var instanceName = InstanceName.ToString();
                ActionExecutionLogger.Instance.LogActionStarted(actionName, instanceName, $"Normal execution, DeltaTime: {InDeltaTime:F3}");
                
                LoggingService.LogInfo($"🔧 GenericBTAction: {InstanceName.ToString()} executing normal action logic (not high-level)");
                LoggingService.LogInfo($"🔧 GenericBTAction: {InstanceName.ToString()} IsHighLevelAction: {IsHighLevelAction}, HighLevelSubtree: {(HighLevelSubtree != null ? "exists" : "null")}");
                
                // Execute normal action logic
                var result = ExecuteActionLogic(InDeltaTime);
                LoggingService.LogInfo($"📊 GenericBTAction: {InstanceName.ToString()} normal action result: {result}, LastStatus: {status}");
                return result;
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
        LoggingService.LogInfo($"🔧 GenericBTAction: {InstanceName.ToString()} applying effects to blackboard");
        
        try
        {
            applyEffects();
            
            LoggingService.LogInfo($"🔧 GenericBTAction: {InstanceName.ToString()} effects applied, setting status to Succeeded");
            
            // Log successful completion
            ActionExecutionLogger.Instance.LogActionCompleted(actionName, instanceName, "Effects applied successfully");
            
            

            return SetStatusAndCalculateReturnvalue(BTNodeResult.Success);
        }
        catch (Exception ex)
        {
            // Log failure
            ActionExecutionLogger.Instance.LogActionFailed(actionName, instanceName, $"Exception: {ex.Message}");
            
            LoggingService.LogError($"❌ GenericBTAction: {InstanceName.ToString()} ExecuteActionLogic failed: {ex.Message}");
            return SetStatusAndCalculateReturnvalue(BTNodeResult.Failure);
        }
    }

    /// <summary>
    /// Initialize and add SubtreeInjectionService to this action
    /// </summary>
    private void InitializeSubtreeInjectionService()
    {
        LoggingService.LogInfo($"🔧 GenericBTAction: InitializeSubtreeInjectionService called for {InstanceName.ToString()}");
        
        try
        {
            LoggingService.LogInfo($"🔧 GenericBTAction: InitializeSubtreeInjectionService called for {InstanceName.ToString()}");
            
            // Create a new SubtreeInjectionService instance for this action without the tree initially
            var subtreeService = new SubtreeInjectionService(this);
            LoggingService.LogInfo($"🔧 GenericBTAction: Created SubtreeInjectionService instance for {InstanceName.ToString()}");
            LoggingService.LogInfo($"🔧 GenericBTAction: Created SubtreeInjectionService instance for {InstanceName.ToString()}");
            
            // Add it to the GeneralServices (not AlwaysOnServices since it should only run when needed)
            LoggingService.LogInfo($"🔧 GenericBTAction: About to call AddService for {InstanceName.ToString()}");
            AddService(subtreeService, false); // false = not always on
            LoggingService.LogInfo($"🔧 GenericBTAction: Called AddService for {InstanceName.ToString()}");
            LoggingService.LogInfo($"🔧 GenericBTAction: Called AddService for {InstanceName.ToString()}");
            
            // Verify the service was added
            var addedService = GetSubtreeInjectionService();
            if (addedService != null)
            {
                LoggingService.LogSuccess($"✅ GenericBTAction: Successfully added SubtreeInjectionService to {InstanceName.ToString()}");
                LoggingService.LogInfo($"✅ GenericBTAction: Successfully added SubtreeInjectionService to {InstanceName.ToString()}");
            }
            else
            {
                LoggingService.LogWarning($"❌ GenericBTAction: Failed to add SubtreeInjectionService to {InstanceName.ToString()} - GetSubtreeInjectionService returned null");
                LoggingService.LogWarning($"❌ GenericBTAction: Failed to add SubtreeInjectionService to {InstanceName.ToString()} - GetSubtreeInjectionService returned null");
            }
        }
        catch (Exception ex)
        {
            LoggingService.LogError($"❌ GenericBTAction: Exception in InitializeSubtreeInjectionService for {InstanceName.ToString()}: {ex.Message}");
            LoggingService.LogError($"❌ GenericBTAction: Failed to initialize SubtreeInjectionService for {InstanceName.ToString()}: {ex.Message}");
        }
    }

    /// <summary>
    /// Set the tree for the SubtreeInjectionService after the action is added to the tree
    /// This should be called after SetOwiningTree is called on the action
    /// </summary>
    public void SetTreeForSubtreeInjectionService(IBehaviorTree InOwningtree)
    {
        var subtreeService = GetSubtreeInjectionService();
        if (subtreeService != null)
        {
            subtreeService.SetOwiningTree(InOwningtree);
        }
    }

    /// <summary>
    /// Get the SubtreeInjectionService associated with this action
    /// </summary>
    public SubtreeInjectionService GetSubtreeInjectionService()
    {
        if (GenrealServices != null)
        {
            foreach (var service in GenrealServices)
            {
                if (service is SubtreeInjectionService subtreeService)
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

            // Console.WriteLine($"🎯 SubtreeInjectionService: Retrieved {effects.Count} effects from action");
        }
        catch (Exception ex)
        {
            // Console.WriteLine($"❌ SubtreeInjectionService: Error getting action effects: {ex.Message}");
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

    /// <summary>
    /// Reset planning state after successful HL action completion to force fresh planning on next cycle
    /// </summary>
    private void ResetPlanningStateAfterSuccess()
    {
        try
        {
            LoggingService.LogInfo($"🔄 GenericBTAction: Starting planning state reset after successful HL action completion: {InstanceName.ToString()}");
            
            // Get the SubtreeInjectionService from this action
            
            if (SubtreeInjectionService != null)
            {
                LoggingService.LogInfo($"🔄 GenericBTAction: Found SubtreeInjectionService, calling reset for {InstanceName.ToString()}");
                SubtreeInjectionService.resetAfterSuccessFullExecution();
                
            }
            else
            {
                LoggingService.LogWarning($"⚠️ GenericBTAction: SubtreeInjectionService not found for {InstanceName.ToString()}");
            }
        }
        catch (Exception ex)
        {
            LoggingService.LogError($"❌ GenericBTAction: Error during planning state reset: {ex.Message}");
        }
    }

   
}

