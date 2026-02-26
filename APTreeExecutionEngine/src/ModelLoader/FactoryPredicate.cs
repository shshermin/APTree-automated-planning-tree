

using System;
using System.Collections.Generic;
using ModelLoader.PredicateTypes;
using BehaviorTreeMainProject.Services;
using BehaviorTreeMainProject.Log.Services;

public class FactoryPredicate
{
    private static FactoryPredicate instance;
   

    public static FactoryPredicate Instance
    {
        get
        {
            return instance ??= new FactoryPredicate();
        }
    }

   

    // Create a predicate instance by predicate name and parameter mappings
    public Predicate CreatePredicateInstance(string predicateName, List<ParameterMapping> parameterMappings, Blackboard<FastName> blackboard)
    {
        // Start timing
        var startTime = DateTime.Now;
        
        LoggingService.LogInfo($"FACTORY: Creating predicate instance for '{predicateName}'");
        LoggingService.LogInfo($"FACTORY: Parameter mappings: {string.Join(", ", parameterMappings.Select(pm => $"{pm.ParameterName}={pm.ParameterValue}"))}");
        
        // Dynamically find the predicate type
        Type predicateType = FindPredicateType(predicateName);
        
        if (predicateType == null)
        {
            LoggingService.LogError($"FACTORY: Unknown predicate type: {predicateName}");
            throw new ArgumentException($"Unknown predicate type: {predicateName}");
        }
        
        LoggingService.LogInfo($"FACTORY: Found predicate type: {predicateType.Name}");

        // Get the actual parameter values from blackboard
        var parameterValues = new List<object>();
        var parameterTypes = new List<Type>();
        
        // Get constructor parameters in order
        var constructors = predicateType.GetConstructors();
        if (constructors.Length == 0)
        {
            LoggingService.LogError($"❌ ERROR: No constructors found for predicate type {predicateName}");
            throw new InvalidOperationException($"No constructors found for predicate type {predicateName}");
        }
        
        LoggingService.LogError($"🔧 Found {constructors.Length} constructor(s)");
        
        // Use the first constructor (assuming it's the one with parameters)
        var constructor = constructors[0];
        var constructorParams = constructor.GetParameters();
        
        LoggingService.LogError($"🔧 Constructor parameters: {string.Join(", ", constructorParams.Select(p => $"{p.Name}:{p.ParameterType.Name}"))}");
        
        // Map parameter mappings to constructor parameters
        foreach (var param in constructorParams)
        {
            LoggingService.LogError($"\n🔍 Looking for constructor parameter: {param.Name} (type: {param.ParameterType.Name})");
            
            var mapping = parameterMappings.FirstOrDefault(m => 
                string.Equals(m.ParameterName, param.Name, StringComparison.OrdinalIgnoreCase));
            
            if (mapping != null)
            {
                LoggingService.LogError($"✅ Found mapping: {mapping.ParameterName} = {mapping.ParameterValue}");
                
                // Get the actual entity from blackboard
                var key = new FastName(mapping.ParameterValue);
                LoggingService.LogError($"🔍 Looking up entity in blackboard with key: {key}");
                
                try
                {
                    object value = GetEntityFromBlackboard(blackboard, key, param.ParameterType);
                    LoggingService.LogError($"✅ Retrieved entity: {value} (type: {value?.GetType().Name})");
                    parameterValues.Add(value);
                    parameterTypes.Add(param.ParameterType);
                }
                catch (Exception ex)
                {
                    LoggingService.LogError($"❌ ERROR retrieving entity: {ex.Message}");
                    throw;
                }
            }
            else
            {
                LoggingService.LogError($"❌ ERROR: Constructor parameter '{param.Name}' not found in parameter mappings");
                LoggingService.LogError($"Available mappings: {string.Join(", ", parameterMappings.Select(m => m.ParameterName))}");
                throw new ArgumentException($"Constructor parameter '{param.Name}' not found in parameter mappings for predicate {predicateName}");
            }
        }

        LoggingService.LogError($"\n🔧 Creating instance with parameters: {string.Join(", ", parameterValues.Select(v => $"{v}"))}");
        
        // Create instance using constructor with parameters (no predicatename needed)
        var instance = Activator.CreateInstance(predicateType, parameterValues.ToArray()) as Predicate;
        
        if (instance == null)
        {
            LoggingService.LogError($"❌ ERROR: Failed to create instance of predicate type {predicateName}");
            throw new InvalidOperationException($"Failed to create instance of predicate type {predicateName}");
        }
        
        LoggingService.LogError($"✅ FACTORY: Successfully created predicate instance: {instance.GetType().Name}");
        LoggingService.LogError($"🔑 FACTORY: Predicate unique key (PredicateName): {instance.PredicateName}");
        
        // Log the GetParameterValues result to see what's being generated
        var paramValues = instance.GetParameterValues();
        LoggingService.LogError($"📋 FACTORY: GetParameterValues result: {string.Join(", ", paramValues)}");
        
        // Log the unique key generation
        var uniqueKey = instance.PredicateName;
        LoggingService.LogError($"🔑 FACTORY: GetUniqueKey result: {uniqueKey}");
        LoggingService.LogError($"🔑 FACTORY: PredicateName vs GetUniqueKey match: {instance.PredicateName == uniqueKey}");

        // Set any additional properties that might not be in constructor
        foreach (var mapping in parameterMappings)
        {
            var property = predicateType.GetProperty(mapping.ParameterName);
            if (property != null && !parameterTypes.Contains(property.PropertyType))
            {
                LoggingService.LogError($"🔧 FACTORY: Setting additional property: {mapping.ParameterName} = {mapping.ParameterValue}");
                
                // Get the actual entity from blackboard using the parameter name
                var key = new FastName(mapping.ParameterValue);
                object value = GetEntityFromBlackboard(blackboard, key, property.PropertyType);

                // Set the property value
                property.SetValue(instance, value);
                LoggingService.LogError($"✅ FACTORY: Set predicate property {mapping.ParameterName} = {mapping.ParameterValue} (actual: {value})");
            }
        }
        
        // The predicate's PredicateName is already set to the unique key in the constructor
        LoggingService.LogError($"🔧 FACTORY: Final PredicateName (unique key): {instance.PredicateName}");
        
        // Register the predicate in the blackboard using the PredicateName (which is the unique key)
        LoggingService.LogError($"🔧 FACTORY: Registering predicate with blackboard using key: {instance.PredicateName}");       
        
        blackboard.SetPredicateSync(instance.PredicateName, instance);
        
        // Check blackboard state after registration
        var predicatesAfter = blackboard.GetAllPredicates();
        LoggingService.LogError($"🔧 FACTORY: Predicates in blackboard after registration: {predicatesAfter.Count}");
        
        var foundInBlackboard = predicatesAfter.Any(p => p.PredicateName == instance.PredicateName);
        LoggingService.LogError($"🔧 FACTORY: Predicate found in blackboard after registration: {foundInBlackboard}");
        
        if (!foundInBlackboard)
        {
            LoggingService.LogError($"⚠️ FACTORY WARNING: Predicate {instance.PredicateName} was not found in blackboard after SetPredicateSync!");
        }
        
        LoggingService.LogError($"✅ FACTORY: Successfully registered predicate with key: {instance.PredicateName}");
        
        // Calculate and track timing
        var endTime = DateTime.Now;
        var generationTime = endTime - startTime;
        
        // Track creation timing for blackboard summary
        BlackboardSummaryLogger.TrackCreation("PredicateInstances", predicateName, generationTime);
        
        LoggingService.LogError($"⏱️ FACTORY: Predicate creation took {generationTime.TotalMilliseconds:F2}ms");
        
        return instance;
    }

    /// <summary>
    /// Dynamically find predicate type by name
    /// </summary>
    /// <param name="predicateName"></param>
    /// <returns></returns>
    private Type FindPredicateType(string predicateName)
    {
        LoggingService.LogError($"🔍 FindPredicateType: searching for '{predicateName}'");
        
        // Get the assembly containing Predicate types
        var assembly = typeof(Predicate).Assembly;
        
        // Search for types that inherit from Predicate
        var predicateTypes = assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(Predicate)) && !t.IsAbstract)
            .ToList();
        
        LoggingService.LogError($"📋 Found {predicateTypes.Count} predicate types: {string.Join(", ", predicateTypes.Select(t => t.Name))}");
        
        // Try exact match first (case-insensitive)
        var exactMatch = predicateTypes.FirstOrDefault(t => 
            string.Equals(t.Name, predicateName, StringComparison.OrdinalIgnoreCase));
        
        if (exactMatch != null)
        {
            LoggingService.LogError($"✅ Found exact match: {exactMatch.Name}");
            return exactMatch;
        }
        
        // Try partial match (e.g., "isat" matches "IsAt")
        var partialMatch = predicateTypes.FirstOrDefault(t => 
            string.Equals(t.Name.Replace(" ", ""), predicateName.Replace(" ", ""), StringComparison.OrdinalIgnoreCase));
        
        if (partialMatch != null)
        {
            LoggingService.LogError($"✅ Found partial match: {partialMatch.Name}");
            return partialMatch;
        }
        
        LoggingService.LogError($"❌ No match found for predicate name: {predicateName}");
        // If no match found, return null
        return null;
    }



    /// <summary>
    /// Get entity from blackboard based on type
    /// </summary>
    /// <param name="blackboard"></param>
    /// <param name="key"></param>
    /// <param name="entityType"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    private object GetEntityFromBlackboard(Blackboard<FastName> blackboard, FastName key, Type entityType)
    {
        LoggingService.LogError($"🔍 GetEntityFromBlackboard: key={key}, expectedType={entityType.Name}");
        
        // Handle primitive types first
        if (entityType == typeof(bool))
        {
            // For boolean values, parse the string value directly
            bool boolValue = bool.Parse(key.ToString());
            LoggingService.LogError($"✅ GetEntityFromBlackboard result: {boolValue} (type: Boolean)");
            return boolValue;
        }
        else if (entityType == typeof(int))
        {
            // For integer values, parse the string value directly
            int intValue = int.Parse(key.ToString());
            LoggingService.LogError($"✅ GetEntityFromBlackboard result: {intValue} (type: Int32)");
            return intValue;
        }
        else if (entityType == typeof(double))
        {
            // For double values, parse the string value directly
            double doubleValue = double.Parse(key.ToString());
            LoggingService.LogError($"✅ GetEntityFromBlackboard result: {doubleValue} (type: Double)");
            return doubleValue;
        }
        else if (entityType == typeof(string))
        {
            // For string values, return the key as string
            string stringValue = key.ToString();
            LoggingService.LogError($"✅ GetEntityFromBlackboard result: {stringValue} (type: String)");
            return stringValue;
        }
        
        // Use a simple switch expression to map entity types to blackboard methods
        object result = entityType.Name switch
        {
            "Element" => blackboard.GetElement(key),
            "Agent" => blackboard.GetAgent(key),
            "Location" => blackboard.GetLocation(key),
            "Tool" => blackboard.GetTool(key),
            "Layer" => blackboard.GetLayer(key),
            "Module" => blackboard.GetModule(key),
            _ => throw new ArgumentException($"Unsupported entity type: {entityType.Name}")
        };
        
        LoggingService.LogError($"✅ GetEntityFromBlackboard result: {result} (type: {result?.GetType().Name})");
        return result;
    }

    

   

  

}