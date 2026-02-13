using System;
using System.Collections.Generic;
using System.Linq;
using BehaviorTreeMainProject.Services;
using BehaviorTreeMainProject.Log.Services;

/// <summary>
/// the way to define a predicate is explained here.
/// for defining a predicate one needs to input a name and some input object types. for example to build the predicate at(r1 -robot, l1 -location) , we would need a name and the types robot and location. then multiple instances of this predicate can be built with different objects.
/// </summary>
public abstract class Predicate
{
    public bool not { get; set; }
    public FastName PredicateName { get; protected set; }
    
    // The type of the predicate (e.g., "IsAtLocation", "IsHolding", etc.)
    protected FastName PredicateType { get; set; }
    
    // Public access to predicate type name
    public string PredicateTypeName => PredicateType?.ToString() ?? "unknown";
    
    // Abstract method that all predicates must implement
  //  public abstract bool Evaluate(Blackboard<FastName> blackboard);
    
    // Method to negate the predicate
    public void Negate()
    {
        not = !not;
    }

    // Override ToString for better debugging
    public override string ToString()
    {
        return $"{(not ? "NOT " : "")}{PredicateType}";
    }

    // Add this method to expose parameters
    // public abstract Dictionary<string, IThings> GetParameters();

    public Predicate(bool isNegated)
    {
        this.not = isNegated;
        
        
    }
    public string GetPredicateType()
    {
        return PredicateType.ToString();
    }

    public Dictionary<string, object> GetAllProperties()
    {
        var properties = new Dictionary<string, object>();
        //get all properties of the predicate
        var propertyInfos = this.GetType().GetProperties();

        foreach (var prop in propertyInfos)
        {
            properties[prop.Name] = prop.GetValue(this);
        }

        return properties;
    }

    public Predicate Clone()
    {
        return (Predicate)this.MemberwiseClone();
    }
    
    /// <summary>
    /// Generates a unique key for this predicate based on its type and parameters
    /// This ensures consistent key generation across the system for proper negation handling
    /// </summary>
    /// <returns>A unique FastName key that excludes the isNegated property</returns>
    public virtual FastName GetUniqueKey()
    {
        LoggingService.LogInfo($"🔑 UNIQUE_KEY: Starting GetUniqueKey() for predicate type: {PredicateType}");
        
        // Get parameter values in the correct order
        var parameterValues = GetParameterValues();
        LoggingService.LogInfo($"🔑 UNIQUE_KEY: Parameter values in order: {string.Join(", ", parameterValues)}");
        
        // Create unique key: {PredicateType}_{param1}_{param2}_{param3}...
        string uniqueKeyString = $"{PredicateType}_{string.Join("_", parameterValues)}";
        LoggingService.LogInfo($"🔑 UNIQUE_KEY: Generated unique key string: '{uniqueKeyString}'");
        
        var uniqueKey = new FastName(uniqueKeyString);
        LoggingService.LogInfo($"🔑 UNIQUE_KEY: Final unique key: {uniqueKey}");
        
        return uniqueKey;
    }
    
    /// <summary>
    /// Override this method in derived classes to provide parameter values for key generation
    /// </summary>
    /// <returns>List of parameter values in the correct order</returns>
    public virtual List<string> GetParameterValues()
    {
        // Default implementation returns empty list
        // Derived classes should override this to provide their parameter values in correct order
        return new List<string>();
    }
}
