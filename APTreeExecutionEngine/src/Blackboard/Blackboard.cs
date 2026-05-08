using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Neo4j.Driver;
using System.Linq;
using System.Reflection;
using BehaviorTreeMainProject.Services;
using BehaviorTreeMainProject.Log.Services;
using ModelLoader.ParameterTypes;


//we need to fix the query so that the parent types are also added to the graph
public class Blackboard<T> : IDisposable where T : class
{
    // built-in types
    Dictionary<FastName, int>           IntValues =            new ();
    Dictionary<FastName, double>        DoubleValues =         new ();
    Dictionary<FastName, bool>          BoolValues =           new (); 
    Dictionary<FastName, string>        StringValues =         new ();
 
    
    // registered types as lists
    List<FastName> AvailableEntityTypes = new();
    List<FastName> AvailablePredicateTypes = new();
    List<FastName> AvailableActionTypes = new(); 
     // registered instances
     Dictionary<FastName, Layer> LayerValues = new();
     Dictionary<FastName, Module> ModuleValues = new();
     Dictionary<FastName, Tool> ToolValues = new();
    Dictionary<FastName, Element>   ElementValues =    new ();
    Dictionary<FastName, Location>   LocationValues =    new ();
    Dictionary<FastName, Agent>   AgentValues =    new ();
    private Dictionary<FastName, Predicate> InitialStatePredicates = new();
    private Dictionary<FastName, Predicate> GoalStatePredicates = new();
    Dictionary<FastName, PActionNode> ActionValues = new();
    Dictionary<FastName, IBTNode> FlowNodeValues = new();
     Dictionary<FastName, State> StateValues = new();
    Dictionary<FastName, NodeGraph> NodeGraphValues = new();
    Dictionary<FastName, DynamicFlowNode> InjectedSubtreesValues = new();

    /// <summary>
    /// Global counter per base action key. Tracks how many instances of each
    /// action signature have been created across all cassettes, so that
    /// cross-cassette duplicates get unique _dup suffixes.
    /// Key = base action key (e.g. "ChangeEndeffectorHL_robot1_staplergun1_gripper1")
    /// Value = number of instances created so far
    /// </summary>
    public Dictionary<string, int> ActionInstanceCounts { get; } = new Dictionary<string, int>();
   
    private readonly EnvironmentGraph? _envGraph;

    /// <summary>
    /// Controls whether the system is in planning phase (true) or execution phase (false)
    /// During planning phase, HL actions only generate NodeGraphs without executing ML actions
    /// </summary>
    public bool PlanningPhase { get; set; } = true;
    public int LowestCost { get; set; } = 0;

    /// <summary>
    /// The currently chosen dynamic flow node that must finish executing all its children
    /// before any other branch is allowed. Set by LowestCostExecution decorator, enforced by ExclusiveBranchGate.
    /// Only cleared when the chosen branch reaches Success.
    /// </summary>
    public DynamicFlowNode? ChosenExecutingBranch { get; set; } = null;

    /// <summary>
    /// Array to track when each cassette has generated and inserted its subtree
    /// Index 0 = cassette1, Index 1 = cassette2, Index 2 = cassette3, Index 3 = cassette4
    /// </summary>
    public bool[] CassetteSubtreeCompleted { get; set; } = new bool[4] { false, false, false, false };

    public Blackboard(string uri, string user, string password)
    {
        _envGraph = new EnvironmentGraph(uri, user, password);
    }

    /// <summary>
    /// Constructor without Neo4j connection. The blackboard will work without graph database support.
    /// </summary>
    public Blackboard()
    {
        _envGraph = null;
    }

    public bool TryGet(FastName key, out int value, int defaultvalue = 0)
    {
        if(IntValues.ContainsKey(key))
        {
                value = IntValues[key];
                return true;
        }
            value = defaultvalue;
            return false;
    }
    public int GetInt(FastName key)
    {
        if( !IntValues.ContainsKey(key))
        {
                throw new System.ArgumentException($"could not find a value for {key} this key");
               
        }
         return IntValues[key];
    }
    public double GetDouble(FastName key)
    {
        if (!DoubleValues.ContainsKey(key))
        {
            throw new System.ArgumentException($"could not find a value for {key} this key");
        }
        return DoubleValues[key]; 
    }

    public string GetString(FastName key)
    {
        if (!StringValues.ContainsKey(key))
        {
            throw new System.ArgumentException($"could not find a value for {key} this key");
        }
        return StringValues[key];
    }

    public bool GetBool(FastName key)
    {
        if (!BoolValues.ContainsKey(key))
        {
            throw new System.ArgumentException($"could not find a value for {key} this key");
        }
        return BoolValues[key];
    }

    public IBTNode GetFlowNode(FastName key)
    {
        if (!FlowNodeValues.ContainsKey(key))
        {
            throw new System.ArgumentException($"could not find a value for {key} this key");
        }
        return FlowNodeValues[key];
    }
    public List<IBTNode> GetAllFlowNodes()
    {
        return FlowNodeValues.Values.ToList();
    }
    public void SetFlowNodeInstance(FastName key, FlowNode value)
    {
        if (!FlowNodeValues.ContainsKey(key))
        {
            FlowNodeValues[key] = value;
            // Log new flow node instance created
            BlackboardTrackingLogger.LogNewInstance(key.ToString(), value.GetType().Name, "Blackboard", $"Flow node instance: {value.GetType().Name}");
            
            // Track for blackboard summary
            var startTime = DateTime.Now;
            // Simulate generation time (since we don't have actual timing)
            var generationTime = DateTime.Now - startTime;
            BlackboardSummaryLogger.TrackCreation("ActionInstances", value.GetType().Name, generationTime);
        }
    }

    public Element GetElement(FastName key)
    {
        if (!ElementValues.ContainsKey(key))
        {
            throw new System.ArgumentException($"could not find a value for {key} this key");
        }
        return ElementValues[key];
    }

    public Location GetLocation(FastName key)
    {
        if (!LocationValues.ContainsKey(key))
        {
            throw new System.ArgumentException($"could not find a value for {key} this key");
        }
        return LocationValues[key];
    }

    public Agent GetAgent(FastName key)
    {
        if (!AgentValues.ContainsKey(key))
        {
            throw new System.ArgumentException($"could not find a value for {key} this key");
        }
        return AgentValues[key];
    }

    // Get methods for predicates
    public Predicate GetPredicate(FastName key)
    {
        if (!InitialStatePredicates.ContainsKey(key))
        {
            throw new ArgumentException($"Could not find predicate for {key}");
        }
        return InitialStatePredicates[key];
    }

     // Update corresponding Get methods




// Get methods


    // Set methods for all types
    public void SetInt(FastName key, int value)
    {
        IntValues[key] = value;
    }

    public void SetDouble(FastName key, double value)
    {
        DoubleValues[key] = value;
    }

    public void SetBool(FastName key, bool value)
    {
        BoolValues[key] = value;
    }

    public void SetString(FastName key, string value)
    {
        StringValues[key] = value;
    }

    

    public void SetElement(FastName key, Element value)
    {
        // Store the element with its instance ID as the key
        ElementValues[key] = value;
        // Ensure the element's NameKey matches its instance ID
        value.NameKey = key;  // This ensures the element keeps its instance ID
        LoggingService.LogInfo($"Successfully added {value.GetType().Name} to Blackboard with key: {key}");
        
        // Log new element instance created
        BlackboardTrackingLogger.LogNewInstance(key.ToString(), value.GetType().Name, "Blackboard", $"Element instance: {value.GetType().Name}");
    }

    public void SetLocation(FastName key, Location value)
    {
        LocationValues[key] = value;
        value.NameKey = key;  // Set the instance ID
        
        // Log new location instance created
        BlackboardTrackingLogger.LogNewInstance(key.ToString(), value.GetType().Name, "Blackboard", $"Location instance: {value.GetType().Name}");
    }

    public void SetAgent(FastName key, Agent value)
    {
        AgentValues[key] = value;
        value.NameKey = key;  // Set the instance ID
        
        // Log new agent instance created
        BlackboardTrackingLogger.LogNewInstance(key.ToString(), value.GetType().Name, "Blackboard", $"Agent instance: {value.GetType().Name}");
    }

    
/// <summary>
/// Sets the entity type for a given key
/// </summary>
/// <param name="key"></param>
/// <param name="elementType"></param>
/// <exception cref="ArgumentException"></exception>
   public void SetEntityType(FastName key, CustomProperty elementType)
{
    if (!typeof(CustomProperty).IsAssignableFrom(elementType.GetType()))
    {
        throw new ArgumentException($"Type {elementType.GetType().Name} is not an CustomProperty type");
    }

    if (!AvailableEntityTypes.Contains(key))
    {
        AvailableEntityTypes.Add(key);
        // Log new entity type added
        BlackboardTrackingLogger.LogNewType(key.ToString(), "CustomProperty", $"CustomProperty type: {elementType.GetType().Name}");
    }
    AvailableEntityTypes.Add(key);
}

/// <summary>
/// Registers an entity type
/// </summary>
/// <param name="typeName"></param>
public void RegisterEntityType(FastName typeName)
{
    if (!AvailableEntityTypes.Contains(typeName))
    {
        AvailableEntityTypes.Add(typeName);
        // Log new entity type registered
        BlackboardTrackingLogger.LogNewType(typeName.ToString(), "CustomProperty", "Registered entity type");
    }
}

/// <summary>
/// Checks if an entity type is available
/// </summary>
/// <param name="typeName"></param>
/// <returns></returns>
public bool HasEntityType(FastName typeName)
{
    return AvailableEntityTypes.Contains(typeName);
}

    /// <summary>
    /// Gets all available entity types
    /// </summary>
    /// <returns></returns>
    public List<FastName> GetAllEntityTypes()
    {
        return AvailableEntityTypes.ToList();
    }
    public List<Element> GetAllElements()
    {
        return ElementValues.Values.ToList();
    }
public List<PActionNode> GetAllActions()
{
    return ActionValues.Values.ToList();
}
public List<Location> GetAllLocations()
{
    return LocationValues.Values.ToList();
}
public List<Agent> GetAllAgents()
{
    return AgentValues.Values.ToList();
}
public List<Layer> GetAllLayers()
{
    return LayerValues.Values.ToList();
}
public List<Module> GetAllModules()
{
    return ModuleValues.Values.ToList();
}
public List<Tool> GetAllTools()
{
    return ToolValues.Values.ToList();
}

/// <summary>
/// Registers a predicate type
/// </summary>
/// <param name="typeName"></param>
public void RegisterPredicateType(FastName typeName)
{
    if (!AvailablePredicateTypes.Contains(typeName))
    {
        AvailablePredicateTypes.Add(typeName);
        // Log new predicate type registered
        BlackboardTrackingLogger.LogNewType(typeName.ToString(), "Predicate", "Registered predicate type");
    }
}

/// <summary>
/// Checks if a predicate type is available
/// </summary>
/// <param name="typeName"></param>
/// <returns></returns>
public bool HasPredicateType(FastName typeName)
{
    return AvailablePredicateTypes.Contains(typeName);
}

/// <summary>
/// Gets all available predicate types
/// </summary>
/// <returns></returns>
public List<FastName> GetAllPredicateTypes()
{
    return AvailablePredicateTypes.ToList();
}

/// <summary>
/// Registers an action type
/// </summary>
/// <param name="typeName"></param>
public void RegisterActionType(FastName typeName)
{
    if (!AvailableActionTypes.Contains(typeName))
    {
        AvailableActionTypes.Add(typeName);
        // Log new action type registered
        BlackboardTrackingLogger.LogNewType(typeName.ToString(), "Action", "Registered action type");
    }
}

/// <summary>
/// Checks if an action type is available
/// </summary>
/// <param name="typeName"></param>
/// <returns></returns>
public bool HasActionType(FastName typeName)
{
    return AvailableActionTypes.Contains(typeName);
}

/// <summary>
/// Gets all available action types
/// </summary>
/// <returns></returns>
public List<FastName> GetAllActionTypes()
{
    return AvailableActionTypes.ToList();
}


// Predicate type methods
public void SetPredicateType(FastName key, Predicate predicateType)
{
    if (!typeof(Predicate).IsAssignableFrom(predicateType.GetType()))
    {
        throw new ArgumentException($"Type {predicateType.GetType().Name} is not a Predicate type");
    }

    // Check if this is a new predicate type (based on the actual type, not the key)
    var predicateTypeName = predicateType.GetType().Name;
    var typeKey = new FastName(predicateTypeName);
    
    if (!AvailablePredicateTypes.Contains(typeKey))
    {
        AvailablePredicateTypes.Add(typeKey);
        // Log new predicate type added (only for the actual type name, not instance key)
        BlackboardTrackingLogger.LogNewType(predicateTypeName, "Predicate", "Registered predicate type");
    }
}

    // Action type methods
    public void SetActionType(FastName key, PActionNode actionType)
    {
        if (!typeof(PActionNode).IsAssignableFrom(actionType.GetType()))
        {
            throw new ArgumentException($"Type {actionType.GetType().Name} is not an Action type");
        }

        // Check if this is a new action type (based on the actual type, not the key)
        var actionTypeName = actionType.GetType().Name;
        var typeKey = new FastName(actionTypeName);

        if (!AvailableActionTypes.Contains(typeKey))
        {
            AvailableActionTypes.Add(typeKey);
            // Log new action type added (only for the actual type name, not instance key)
            BlackboardTrackingLogger.LogNewType(actionTypeName, "Action", "Registered action type");
        }

        // // Store the action instance
        // ActionValues[key] = actionType;

        // // Log new action instance created
        // BlackboardTrackingLogger.LogNewInstance(key.ToString(), actionTypeName, "Blackboard", $"Action instance: {actionTypeName}");
    }

public void SetActionInstance(FastName key, PActionNode actionInstance)
{
    if (!ActionValues.ContainsKey(key))
    {
        ActionValues[key] = actionInstance;
        // Log new action instance created
        BlackboardTrackingLogger.LogNewInstance(key.ToString(), actionInstance.GetType().Name, "Blackboard", $"Action instance: {actionInstance.GetType().Name}");
        
        // Track for blackboard summary
        var startTime = DateTime.Now;
        // Simulate generation time (since we don't have actual timing)
        var generationTime = DateTime.Now - startTime;
        BlackboardSummaryLogger.TrackCreation("ActionInstances", actionInstance.GetType().Name, generationTime);
    }
}

/// <summary>
/// Checks whether an action instance with the given key exists on the blackboard.
/// </summary>
public bool HasActionInstance(FastName key)
{
    return ActionValues.ContainsKey(key);
}

public ActionNode GetAction(FastName key)
    {
        if (!ActionValues.ContainsKey(key))
        {
            throw new ArgumentException($"Could not find action for {key}");
        }
        return ActionValues[key];
    }

/// <summary>
/// Gets all action instances from the blackboard
/// </summary>
/// <returns>List of all action instances</returns>
public List<PActionNode> GetAllActionInstances()
{
    return ActionValues.Values.ToList();
}


    // Set methods for predicates
    private void SetPredicateSecondary(FastName key, Predicate predicate)
    {
        LoggingService.LogInfo($"🔧 BLACKBOARD_SECONDARY: SetPredicateSecondary called with key: {key}");
        
        string newPredicateStr = BlackboardExtensions.FormatPredicate(predicate);
        LoggingService.LogInfo($"🔧 BLACKBOARD_SECONDARY: Formatted predicate string: {newPredicateStr}");
        
        LoggingService.LogInfo($"🔧 BLACKBOARD_SECONDARY: Current PredicateValues count: {InitialStatePredicates.Count}");
        
        // Check if identical predicate exists
        if (!InitialStatePredicates.ContainsKey(key))
        {
            LoggingService.LogInfo($"🔧 BLACKBOARD_SECONDARY: Key {key} not found in PredicateValues, adding new predicate");
            InitialStatePredicates[key] = predicate;
            LoggingService.LogInfo($"🔧 BLACKBOARD_SECONDARY: PredicateValues count after adding: {InitialStatePredicates.Count}");
            LoggingService.LogSuccess($"🔧 BLACKBOARD_SECONDARY: Successfully added predicate with key: {key}");
        }
        else if (BlackboardExtensions.FormatPredicate(InitialStatePredicates[key]) != newPredicateStr)
        {
            LoggingService.LogInfo($"🔧 BLACKBOARD_SECONDARY: Key {key} exists but predicate content differs, updating predicate");
            LoggingService.LogInfo($"🔧 BLACKBOARD_SECONDARY: Old predicate: {BlackboardExtensions.FormatPredicate(InitialStatePredicates[key])}");
            LoggingService.LogInfo($"🔧 BLACKBOARD_SECONDARY: New predicate: {newPredicateStr}");
            InitialStatePredicates[key] = predicate;
            LoggingService.LogInfo($"🔧 BLACKBOARD_SECONDARY: PredicateValues count after updating: {InitialStatePredicates.Count}");
            LoggingService.LogSuccess($"🔧 BLACKBOARD_SECONDARY: Successfully updated predicate with key: {key}");
        }
        else
        {
            LoggingService.LogInfo($"🔧 BLACKBOARD_SECONDARY: Key {key} exists and predicate content is identical, no update needed");
        }
        
        // Final verification
        var finalCount = InitialStatePredicates.Count;
        var keyExists = InitialStatePredicates.ContainsKey(key);
        LoggingService.LogInfo($"🔧 BLACKBOARD_SECONDARY: Final verification - Count: {finalCount}, Key exists: {keyExists}");
    }
    

    public bool HasSimilarPredicate(Predicate newPredicate)
    {
        foreach (var existingPredicate in InitialStatePredicates.Values)
        {
            // First check if predicates have the same name
            if (existingPredicate.PredicateName == newPredicate.PredicateName)
            {
                LoggingService.LogDebug($"\nComparing predicates:");
                LoggingService.LogDebug($"New: {newPredicate.PredicateName}");
                LoggingService.LogDebug($"Existing: {existingPredicate.PredicateName}");
                
                // Get properties of both predicates
                var existingParams = existingPredicate.GetAllProperties();
                var newParams = newPredicate.GetAllProperties();
                
                // Check if all parameter names and values match exactly
                bool allParamsMatch = true;
                foreach (var param in newParams)
                {
                    // Skip metadata properties
                    if (param.Key == "PredicateName" || param.Key == "PredicateType" || param.Key == "isNegated")
                        continue;

                    // If parameter doesn't exist in existing predicate
                    if (!existingParams.ContainsKey(param.Key))
                    {
                        LoggingService.LogDebug($"Parameter {param.Key} not found in existing predicate");
                        allParamsMatch = false;
                        break;
                    }

                    // Compare the actual instance names
                    var existingValue = existingParams[param.Key];
                    var newValue = param.Value;

                    LoggingService.LogDebug($"\nComparing parameter {param.Key}:");
                    LoggingService.LogDebug($"Existing value: {existingValue}");
                    LoggingService.LogDebug($"New value: {newValue}");

                    // Get the instance identifiers for comparison
                    var existingId = existingValue?.GetType().GetProperty("InstanceId")?.GetValue(existingValue)?.ToString()
                        ?? existingValue?.ToString();
                    var newId = newValue?.GetType().GetProperty("InstanceId")?.GetValue(newValue)?.ToString()
                        ?? newValue?.ToString();

                    LoggingService.LogDebug($"Existing ID: {existingId}");
                    LoggingService.LogDebug($"New ID: {newId}");

                    if (existingId != newId)
                    {
                        LoggingService.LogInfo("IDs don't match");
                        allParamsMatch = false;
                        break;
                    }
                }
                
                if (allParamsMatch)
                {
                    LoggingService.LogInfo("Found similar predicate!");
                    return true;
                }
            }
        }
        return false;
    }
/// <summary>
/// Adds predicate to the graph
/// </summary>
/// <param name="key"></param>
/// <param name="predicate"></param>
/// <returns></returns>
/// <exception cref="InvalidOperationException"></exception>
    // Use it before adding new predicates
    public async Task SetPredicateOnGraph(FastName key, Predicate predicate)
    {
        LoggingService.LogInfo($"🔧 BLACKBOARD: SetPredicate called with key: {key}");
        LoggingService.LogInfo($"🔧 BLACKBOARD: Predicate type: {predicate.GetType().Name}");
        LoggingService.LogInfo($"🔧 BLACKBOARD: Predicate.PredicateName: {predicate.PredicateName}");
        LoggingService.LogInfo($"🔧 BLACKBOARD: Predicate.isNegated: {predicate.not}");
        
        if (_envGraph == null)
        {
            throw new InvalidOperationException("EnvironmentGraph not initialized");
        }

        await _envGraph.SetPredicateOnGraph(predicate);
    }

    
    public void SetPredicateSync(FastName key, Predicate predicate)
    {
        // NEW: Clear, prominent logging for predicate additions
        LoggingService.LogInfo("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        LoggingService.LogInfo($"➕ PREDICATE_ADDED: Adding predicate to blackboard");
        LoggingService.LogInfo($"   Key: {key}");
        LoggingService.LogInfo($"   Type: {predicate.GetType().Name}");
        LoggingService.LogInfo($"   PredicateName: {predicate.PredicateName}");
        LoggingService.LogInfo($"   isNegated: {predicate.not}");
        LoggingService.LogInfo($"   Current total predicates: {InitialStatePredicates.Count}");
        LoggingService.LogInfo("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        
        // NEW: Clean up conflicting atAgent predicates when updating location
        if (predicate.GetPredicateType() == "atAgent" && !predicate.not)
        {
            CleanupConflictingAtAgentPredicates(predicate);
        }
        // Check if a predicate with the same key already exists
        if (InitialStatePredicates.ContainsKey(key))
        {
            var existingPredicate = InitialStatePredicates[key];
            LoggingService.LogDebug($"🔍 PREDICATE_UPDATE: Key '{key}' already exists - updating negation");
            LoggingService.LogInfo($"   Old isNegated: {existingPredicate.not} → New isNegated: {predicate.not}");

            // Update the isNegated property of the existing predicate
            var oldNegationValue = existingPredicate.not;
            existingPredicate.not = predicate.not;

            // Log predicate negation change
            BlackboardTrackingLogger.LogPredicateNegation(key.ToString(), oldNegationValue, predicate.not, "Blackboard", "Updated existing predicate negation");

            LoggingService.LogSuccess($"✅ PREDICATE_UPDATE: Successfully updated negation for key: {key}");
            return;
        }

        // Check for identical predicate (different key but same content)
        string newPredicateStr = BlackboardExtensions.FormatPredicate(predicate);
        if (InitialStatePredicates.Values.Any(p => BlackboardExtensions.FormatPredicate(p) == newPredicateStr))
        {
            LoggingService.LogWarning($"⚠️ PREDICATE_DUPLICATE: Identical predicate content already exists: {newPredicateStr}");
            return;
        }

        // Store the predicate in the dictionary
        InitialStatePredicates[key] = predicate;
        
        // Log new predicate instance created
        BlackboardTrackingLogger.LogNewInstance(key.ToString(), predicate.GetType().Name, "Blackboard", $"Predicate instance: {predicate.PredicateName}");
        
        // Verify the predicate was actually added
        var foundInDict = InitialStatePredicates.ContainsKey(key);
        if (!foundInDict)
        {
            LoggingService.LogError($"❌ PREDICATE_ERROR: Failed to add predicate with key {key}!");
        }
        else
        {
            LoggingService.LogSuccess($"✅ PREDICATE_ADDED: Successfully stored predicate with key: {key} (Total: {InitialStatePredicates.Count})");
        }
    }



    // Implement IDisposable to properly close Neo4j connection
    public void Dispose()
    {
        _envGraph?.Dispose();
        
        // Close the blackboard tracking logger
        BlackboardTrackingLogger.Close();
    }
    
    public async Task<bool> TestNeo4jConnection()
    {
        try
        {
            return await _envGraph.TestConnection();
        }
        catch (Exception ex)
        {
            LoggingService.LogError($"Neo4j connection test failed: {ex.Message}");
            return false;
        }
    }

    // Set methods
   

    public void SetLayer(FastName key, Layer value)
    {
        LayerValues[key] = value;
        value.NameKey = key;  // Set the instance ID
        
        // Log new layer instance created
        BlackboardTrackingLogger.LogNewInstance(key.ToString(), value.GetType().Name, "Blackboard", $"Layer instance: {value.GetType().Name}");
    }

    public void SetModule(FastName key, Module value)
    {
        ModuleValues[key] = value;
        value.NameKey = key;  // Set the instance ID
        
        // Log new module instance created
        BlackboardTrackingLogger.LogNewInstance(key.ToString(), value.GetType().Name, "Blackboard", $"Module instance: {value.GetType().Name}");
    }

    public void SetTool(FastName key, Tool value)
    {
        ToolValues[key] = value;
        value.NameKey = key;  // Set the instance ID
        
        // Log new tool instance created
        BlackboardTrackingLogger.LogNewInstance(key.ToString(), value.GetType().Name, "Blackboard", $"Tool instance: {value.GetType().Name}");
    }

    public Layer GetLayer(FastName key)
    {
        if (!LayerValues.ContainsKey(key))
        {
            throw new ArgumentException($"Could not find a value for {key} this key");
        }
        return LayerValues[key];
    }

    public Module GetModule(FastName key)
    {
        if (!ModuleValues.ContainsKey(key))
        {
            throw new ArgumentException($"Could not find a value for {key} this key");
        }
        return ModuleValues[key];
    }

    public Tool GetTool(FastName key)
    {
        if (!ToolValues.ContainsKey(key))
        {
            throw new ArgumentException($"Could not find a value for {key} this key");
        }
        return ToolValues[key];
    }

    // Get and Set methods for States
    public State GetState(FastName key)
    {
        if (!StateValues.ContainsKey(key))
        {
            throw new ArgumentException($"Could not find state for key: {key}");
        }
        return StateValues[key];
    }

    public void SetState(FastName key, State value)
    {
        if (!StateValues.ContainsKey(key))
        {
            StateValues[key] = value;
            // Log new state instance created
            BlackboardTrackingLogger.LogNewInstance(key.ToString(), value.GetType().Name, "Blackboard", $"State instance: {value.GetType().Name}");
        }
        StateValues[key] = value;
        LoggingService.LogInfo($"Successfully added State to Blackboard with key: {key}");
    }

    // Get and Set methods for NodeGraphs
    public NodeGraph GetNodeGraph(FastName key)
    {
        if (!NodeGraphValues.ContainsKey(key))
        {
            throw new ArgumentException($"Could not find NodeGraph for key: {key}");
        }
        return NodeGraphValues[key];
    }

    public void SetNodeGraph(FastName key, NodeGraph value)
    {
        NodeGraphValues[key] = value;
        LoggingService.LogInfo($"Successfully added NodeGraph to Blackboard with key: {key}");
        
        // Log new node graph instance created
        BlackboardTrackingLogger.LogNewInstance(key.ToString(), value.GetType().Name, "Blackboard", $"NodeGraph instance: {value.GetType().Name}");
    }

    /// <summary>
    /// Gets all NodeGraph instances from the blackboard
    /// </summary>
    /// <returns>List of all NodeGraph instances</returns>
    public List<NodeGraph> GetAllNodeGraphs()
    {
        return NodeGraphValues.Values.ToList();
    }

    // Get and Set methods for Injected Subtrees
    public DynamicFlowNode GetInjectedSubtree(FastName key)
    {
        if (!InjectedSubtreesValues.ContainsKey(key))
        {
            throw new ArgumentException($"Could not find injected subtree for key: {key}");
        }
        return InjectedSubtreesValues[key];
    }

    public void SetInjectedSubtree(FastName key, DynamicFlowNode value)
    {
        InjectedSubtreesValues[key] = value;
        LoggingService.LogInfo($"Successfully added injected subtree to Blackboard with key: {key}");
        
        // Log new injected subtree instance created
        BlackboardTrackingLogger.LogNewInstance(key.ToString(), value.GetType().Name, "Blackboard", $"Injected subtree instance: {value.GetType().Name}");
    }

    /// <summary>
    /// Gets all injected subtrees from the blackboard
    /// </summary>
    /// <returns>List of all injected subtrees</returns>
    public List<DynamicFlowNode> GetAllInjectedSubtrees()
    {
        return InjectedSubtreesValues.Values.ToList();
    }

    /// <summary>
    /// Clears all injected subtrees from the blackboard
    /// </summary>
    public void ClearInjectedSubtrees()
    {
        InjectedSubtreesValues.Clear();
        LoggingService.LogInfo("Cleared all injected subtrees from Blackboard");
    }
     public List<Predicate> GetAllPredicates()
    {
        return InitialStatePredicates.Values.ToList();
    }

    public List<Predicate> GetGoalStatePredicates()
    {
        return GoalStatePredicates.Values.ToList();
    }

    public void SetGoalStatePredicate(FastName key, Predicate predicate)
    {
        GoalStatePredicates[key] = predicate;
    }

    /// <summary>
    /// Gets all non-negated (positive) predicates from the blackboard
    /// </summary>
    /// <returns>List of all predicates where isNegated is false</returns>
    public List<Predicate> GetTruePredicates()
    {
        var truePredicates = new List<Predicate>();

        foreach (var predicate in InitialStatePredicates.Values)
        {
            if (!predicate.not)
            {
                truePredicates.Add(predicate);
            }
        }

        return truePredicates;
    }
    /// <summary>
    /// Convenience method to retrieve a FinalLocation by name.
    /// Returns null if the key does not exist or is not a FinalLocation.
    /// </summary>
    public FinalLocation GetFinalLocationByName(string name)
    {
        var key = new FastName(name);
        if (LocationValues.ContainsKey(key) && LocationValues[key] is FinalLocation fl)
            return fl;
        return null;
    }

    private void CleanupConflictingAtAgentPredicates(Predicate newLocationPredicate)
{
    try
    {
        // Extract robot and location from the new predicate
        var newLocationStr = newLocationPredicate.GetPDDLParameterValues();
        if (newLocationStr.Count < 2) return;

        string robotName = newLocationStr[0];
        string newLocation = newLocationStr[1];

        LoggingService.LogInfo($"🧹 CLEANUP: Cleaning up ALL atAgent predicates for robot {robotName}");

        // Find ALL blackboard keys that contain "atAgent" for this robot
        var keysToRemove = new List<FastName>();
        
        foreach (var kvp in InitialStatePredicates)
        {
            string keyName = kvp.Key.ToString();
            
            // Check if this key contains "atAgent" and the robot name
            if (keyName.Contains("atAgent") && keyName.Contains(robotName))
            {
                keysToRemove.Add(kvp.Key);
                LoggingService.LogInfo($"   🗑️ Marking for removal: {keyName}");
            }
        }

        // Remove ALL conflicting atAgent predicates
        foreach (var key in keysToRemove)
        {
            InitialStatePredicates.Remove(key);
            LoggingService.LogInfo($"   ✅ Removed: {key}");
        }

        LoggingService.LogSuccess($"�� CLEANUP: Removed {keysToRemove.Count} atAgent predicates for robot {robotName}");
        LoggingService.LogInfo($"   📍 New location will be: {robotName} at {newLocation}");
    }
    catch (Exception ex)
    {
        LoggingService.LogError($"❌ CLEANUP: Error during cleanup: {ex.Message}");
    }
}



  
}