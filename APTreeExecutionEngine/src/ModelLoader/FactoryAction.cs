using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using BehaviorTreeMainProject.Log.Services;


public class FactoryAction : Singleton<FactoryAction>
{
    /// <summary>
    /// Creates an action instance from an ActionInstance definition like:
    /// ActionInstance: pickUp(pickedObject : b1, rob : r1, loc : fp1, robTool : vg1)
    /// </summary>
    public PActionNode CreateActionInstance(
        string actionInstanceDefinition, 
        Blackboard<FastName> blackboard)
    {
        // Start timing
        var startTime = DateTime.Now;
        
        // Parse the action instance definition
        var (actionTypeName, instanceName, parameterValues) = ParseActionInstanceDefinition(actionInstanceDefinition, blackboard);
        
        LoggingService.LogError($"🔧 Creating action instance: {actionTypeName} with name: {instanceName}");
        
        // Dynamically find the action type
        Type actionType = FindActionType(actionTypeName);
        
        if (actionType == null)
        {
            throw new ArgumentException($"Unknown action type: {actionTypeName}");
        }
        
        LoggingService.LogError($"\u2705 Found action type: {actionType.Name}");

        // Get the constructor that matches our expected signature
        var constructors = actionType.GetConstructors();
        var targetConstructor = constructors.FirstOrDefault(c => 
        {
            var parameters = c.GetParameters();
            // Check if this constructor has the expected signature: (string, string, Blackboard, ...)
            return parameters.Length >= 3 && 
                   parameters[0].ParameterType == typeof(string) &&
                   parameters[1].ParameterType == typeof(string) &&
                   parameters[2].ParameterType == typeof(Blackboard<FastName>);
        });

        if (targetConstructor == null)
        {
            throw new ArgumentException($"No suitable constructor found for action type {actionTypeName}");
        }

        LoggingService.LogError($"✅ Found constructor with {targetConstructor.GetParameters().Length} parameters");

        // Build constructor arguments in the correct order
        var constructorArgs = new List<object> { actionTypeName, instanceName, blackboard };
        
        // Get constructor parameters (skip the first 3: actionType, instanceName, blackboard)
        var constructorParams = targetConstructor.GetParameters().Skip(3).ToArray();
        
        // Match parameters by name and add them in constructor order
        foreach (var param in constructorParams)
        {
            LoggingService.LogError($"🔍 Looking for constructor parameter: {param.Name} of type {param.ParameterType.Name}");
            
            // Find the corresponding parameter value from the action definition
            if (parameterValues.TryGetValue(param.Name, out string paramValue))
            {
                LoggingService.LogError($"  📋 Found parameter value: {param.Name} = {paramValue}");
                
                // Get the parameter instance from blackboard
                object parameterInstance = GetParameterInstanceFromBlackboard(blackboard, paramValue, actionType, param.Name);
                
                if (parameterInstance == null)
                {
                    throw new ArgumentException($"Parameter instance '{paramValue}' not found in blackboard");
                }
                
                LoggingService.LogError($"  ✅ Retrieved from blackboard: {paramValue} -> {parameterInstance.GetType().Name}");
                constructorArgs.Add(parameterInstance);
            }
            else
            {
                throw new ArgumentException($"Required parameter '{param.Name}' not found in action definition");
            }
        }

        // Create the action instance
        LoggingService.LogError($"🔍 Creating instance with constructor arguments...");
        PActionNode instance;
        try
        {
            instance = targetConstructor.Invoke(constructorArgs.ToArray()) as PActionNode;
            
            if (instance == null)
            {
                throw new InvalidOperationException($"Failed to create instance of type {actionTypeName}");
            }
            
            LoggingService.LogError($"✅ Successfully created action instance: {instance.GetType().Name}");
            
            // Calculate and track timing
            var endTime = DateTime.Now;
            var generationTime = endTime - startTime;
            
            // Track creation timing for blackboard summary
            BlackboardSummaryLogger.TrackCreation("ActionInstances", actionTypeName, generationTime);
            
            LoggingService.LogError($"⏱️ FACTORY: Action creation took {generationTime.TotalMilliseconds:F2}ms");
            
            return instance;
        }
        catch (Exception ex)
        {
            LoggingService.LogError($"❌ ERROR during instance creation: {ex.Message}");
            throw;
        }
    }
    
    /// <summary>
    /// Parses an ActionInstance definition string like:
    /// "ActionInstance: pickUp(pickedObject : b1, rob : r1, loc : fp1, robTool : vg1)"
    /// </summary>
    private (string actionTypeName, string instanceName, Dictionary<string, string> parameters) ParseActionInstanceDefinition(string definition, Blackboard<FastName> blackboard)
    {
        // Use the existing BlackboardWriter to generate the proper instance name
        var blackboardWriter = new BlackboardWriter(blackboard);
        string instanceName = blackboardWriter.GenerateActionInstanceKey(definition);
        
        // Remove "ActionInstance: " prefix for parsing
        string content = definition.Replace("ActionInstance: ", "").Trim();
        
        // Find the opening parenthesis
        int openParen = content.IndexOf('(');
        int closeParen = content.LastIndexOf(')');
        
        if (openParen == -1 || closeParen == -1)
        {
            throw new ArgumentException($"Invalid ActionInstance format: {definition}");
        }
        
        // Extract action type name
        string actionTypeName = content.Substring(0, openParen).Trim();
        
        // Extract parameters
        string paramsString = content.Substring(openParen + 1, closeParen - openParen - 1);
        
        // Parse parameters into dictionary
        var parameters = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(paramsString))
        {
            string[] paramPairs = paramsString.Split(',');
            foreach (string pair in paramPairs)
            {
                string trimmedPair = pair.Trim();
                if (trimmedPair.Contains(":"))
                {
                    string[] parts = trimmedPair.Split(':');
                    if (parts.Length == 2)
                    {
                        string paramName = parts[0].Trim();
                        string paramValue = parts[1].Trim();
                        parameters[paramName] = paramValue;
                    }
                }
            }
        }
        
        return (actionTypeName, instanceName, parameters);
    }
    
    /// <summary>
    /// Dynamically finds an action type by name
    /// </summary>
    private Type FindActionType(string actionTypeName)
    {
        LoggingService.LogError($"🔍 Finding action type: '{actionTypeName}'");
        
        // Get the assembly containing GenericBTAction types
        var assembly = typeof(PActionNode).Assembly;
        
        // Search for types that inherit from GenericBTAction
        var actionTypes = assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(PActionNode)) && !t.IsAbstract)
            .ToList();
        
        LoggingService.LogError($"📋 Found {actionTypes.Count} action types: {string.Join(", ", actionTypes.Select(t => t.Name))}");
        
        // Try exact match first (case-insensitive)
        var exactMatch = actionTypes.FirstOrDefault(t => 
            string.Equals(t.Name, actionTypeName, StringComparison.OrdinalIgnoreCase));
        
        if (exactMatch != null)
        {
            LoggingService.LogError($"✅ Found exact match: {exactMatch.Name}");
            return exactMatch;
        }
        
        // Try partial match (e.g., "pickup" matches "PickUp")
        var partialMatch = actionTypes.FirstOrDefault(t => 
            string.Equals(t.Name.Replace(" ", ""), actionTypeName.Replace(" ", ""), StringComparison.OrdinalIgnoreCase));
        
        if (partialMatch != null)
        {
            LoggingService.LogError($"✅ Found partial match: {partialMatch.Name}");
            return partialMatch;
        }
        
        LoggingService.LogError($"❌ No match found for action name: {actionTypeName}");
        return null;
    }

    /// <summary>
    /// Retrieves a parameter instance from the blackboard
    /// </summary>
    private object GetParameterInstanceFromBlackboard(Blackboard<FastName> blackboard, string instanceName, Type actionType, string parameterName)
    {
        LoggingService.LogError($"🔍 Getting parameter instance: '{instanceName}' (parameter '{parameterName}' in action '{actionType.Name}')");
        
        // Get the parameter type from the action class
        var parameterProperty = actionType.GetProperty(parameterName);
        if (parameterProperty == null)
        {
            throw new ArgumentException($"Parameter '{parameterName}' not found in action class '{actionType.Name}'");
        }
        
        Type parameterType = parameterProperty.PropertyType;
        LoggingService.LogError($"📋 Parameter type: {parameterType.Name}");
        
        // Find the parent entity type
        Type parentType = GetParentEntityType(parameterType);
        LoggingService.LogError($"📋 Parent entity type: {parentType?.Name ?? "null"}");
        
        if (parentType == null)
        {
            throw new ArgumentException($"Could not determine parent entity type for parameter type '{parameterType.Name}'");
        }
        
        // Get the instance from the correct blackboard dictionary
        var key = new FastName(instanceName);
        object result = parentType.Name switch
        {
            "Element" => blackboard.GetElement(key),
            "Agent" => blackboard.GetAgent(key),
            "Location" => blackboard.GetLocation(key),
            "Tool" => blackboard.GetTool(key),
            "Layer" => blackboard.GetLayer(key),
            "Module" => blackboard.GetModule(key),
            _ => throw new ArgumentException($"Unsupported parent entity type: {parentType.Name}")
        };
        
        if (result != null)
        {
            LoggingService.LogError($"✅ Found instance '{instanceName}' as {parentType.Name}: {result.GetType().Name}");
        }
        else
        {
            LoggingService.LogError($"❌ Instance '{instanceName}' not found in {parentType.Name} dictionary");
        }
        
        return result;
    }
    
    /// <summary>
    /// Determines the parent entity type for a given parameter type
    /// </summary>
    private Type GetParentEntityType(Type parameterType)
    {
        // Check if the parameter type directly inherits from one of our entity types
        var entityTypes = new[] { typeof(Element), typeof(Agent), typeof(Location), typeof(Tool), typeof(Layer), typeof(Module) };
        
        foreach (var entityType in entityTypes)
        {
            if (parameterType.IsSubclassOf(entityType) || parameterType == entityType)
            {
                return entityType;
            }
        }
        
        return null;
    }
}


