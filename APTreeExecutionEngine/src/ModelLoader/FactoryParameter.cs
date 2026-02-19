using System;
using System.Collections.Generic;
using ModelLoader.ParameterTypes;
using BehaviorTreeMainProject.Log.Services;

public class FactoryParameter : Singleton<FactoryParameter>
{
    // Create a parameter instance by type name and instance name only
    public CustomProperty CreateParameter(string typeName, string instanceName)
    {
        // Start timing
        var startTime = DateTime.Now;
        
        // Dynamically find the parameter type
        Type parameterType = FindParameterType(typeName);
        
        if (parameterType == null)
        {
            throw new ArgumentException($"Unknown parameter type: {typeName}");
        }

        // Create instance using empty constructor
        var instance = Activator.CreateInstance(parameterType) as CustomProperty;
        
        if (instance == null)
        {
            throw new InvalidOperationException($"Failed to create instance of type {typeName}");
        }

        // Set the name using the NameKey property
        instance.NameKey = new FastName(instanceName);
        instance.ID = instanceName;

        // Calculate and track timing
        var endTime = DateTime.Now;
        var generationTime = endTime - startTime;
        
        // Track creation timing for blackboard summary
        BlackboardSummaryLogger.TrackCreation("ParameterInstances", typeName, generationTime);
        
        LoggingService.LogError($"⏱️ FACTORY: Parameter creation took {generationTime.TotalMilliseconds:F2}ms");

        return instance;
    }

    // Create a parameter instance with parameter values
    public CustomProperty CreateParameter(string typeName, string instanceName, Dictionary<string, object> parameters)
    {
        // Create the base instance
        var instance = CreateParameter(typeName, instanceName);
        
        // Set the parameter values using the abstract method
        instance.SetParameters(parameters);
        
        return instance;
    }
    
    // Dynamically find parameter type by name
    private Type FindParameterType(string typeName)
    {
        // Get the assembly containing CustomProperty types
        var assembly = typeof(CustomProperty).Assembly;
        
        // Search for types that inherit from CustomProperty
        var entityTypes = assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(CustomProperty)) && !t.IsAbstract)
            .ToList();
        
        // Try exact match first (case-insensitive)
        var exactMatch = entityTypes.FirstOrDefault(t => 
            string.Equals(t.Name, typeName, StringComparison.OrdinalIgnoreCase));
        
        if (exactMatch != null)
        {
            return exactMatch;
        }
        
        // Try partial match (e.g., "firstlocation" matches "FirstLocation")
        var partialMatch = entityTypes.FirstOrDefault(t => 
            string.Equals(t.Name.Replace(" ", ""), typeName.Replace(" ", ""), StringComparison.OrdinalIgnoreCase));
        
        if (partialMatch != null)
        {
            return partialMatch;
        }
        
        // If no match found, return null
        return null;
    }

    
}


