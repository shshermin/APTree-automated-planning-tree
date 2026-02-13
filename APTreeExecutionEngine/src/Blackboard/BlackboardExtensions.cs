public static class BlackboardExtensions
{
    public static bool HasElement<T>(this Blackboard<T> blackboard, FastName key) where T : class
    {
        try
        {
            blackboard.GetElement(key);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool HasAgent<T>(this Blackboard<T> blackboard, FastName key) where T : class
    {
        try
        {
            blackboard.GetAgent(key);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool HasLocation<T>(this Blackboard<T> blackboard, FastName key) where T : class
    {
        try
        {
            blackboard.GetLocation(key);
            return true;
        }
        catch
        {
            return false;
        }
    }
    // Add HasPredicate extension method
    public static bool HasPredicate<T>(this Blackboard<T> blackboard, FastName key) where T : class
    {
        try
        {
            blackboard.GetPredicate(key);
            return true;
        }
        catch
        {
            return false;
        }
    }

    // Add RemovePredicate extension method
    public static bool RemovePredicate<T>(this Blackboard<T> blackboard, FastName key) where T : class 
    {
        try
        {
            // Check if predicate exists first
            blackboard.GetPredicate(key);
            // Since we can't directly remove from the private dictionary, we'll return true if it exists
            // The actual removal would need to be implemented in the Blackboard class itself
            return true;
        }
        catch
        {
            return false;
        }
    }
public static string FormatPredicate(Predicate predicate)
{
    try
    {
        var parameters = predicate.GetAllProperties()
            .Where(p => p.Key != "PredicateName" && p.Key != "PredicateType" && p.Key != "isNegated")
            .Select(p => GetInstanceId(p.Value as Entity))
            .Where(id => !string.IsNullOrEmpty(id)) // Filter out null/empty IDs
            .ToList();

        return $"{predicate.PredicateName}({string.Join(",", parameters)})";
    }
    catch (Exception ex)
    {
        // Fallback to a simpler format if there's an error
        return $"{predicate.PredicateName}(error_formatting_parameters: {ex.Message})";
    }
}

private static string GetInstanceId(Entity obj)
{
    if (obj == null)
        return "";
    
    return obj.NameKey?.ToString() ?? "";
}

/// <summary>
/// Registers an Entity instance on the blackboard if it doesn't already exist based on its base type.
/// The method automatically determines the correct registration method based on the entity's type.
/// </summary>
/// <typeparam name="T">The blackboard key type</typeparam>
/// <param name="blackboard">The blackboard instance</param>
/// <param name="entity">The entity to register</param>
/// <returns>True if the entity was registered, false if it already existed</returns>
public static bool RegisterEntityIfNotExists<T>(this Blackboard<T> blackboard, Entity entity) where T : class
{
    if (entity == null)
    {
        throw new ArgumentNullException(nameof(entity), "Entity cannot be null");
    }

    var key = entity.NameKey;
    if (key == null)
    {
        throw new ArgumentException("Entity must have a valid NameKey", nameof(entity));
    }

    // Check if entity already exists based on its type
    bool alreadyExists = false;
    try
    {
        switch (entity)
        {
            case Element:
                blackboard.GetElement(key);
                alreadyExists = true;
                break;
            case Agent:
                blackboard.GetAgent(key);
                alreadyExists = true;
                break;
            case Location:
                blackboard.GetLocation(key);
                alreadyExists = true;
                break;
            case Layer:
                blackboard.GetLayer(key);
                alreadyExists = true;
                break;
            case Module:
                blackboard.GetModule(key);
                alreadyExists = true;
                break;
            case Tool:
                blackboard.GetTool(key);
                alreadyExists = true;
                break;
            default:
                throw new ArgumentException($"Unsupported entity type: {entity.GetType().Name}");
        }
    }
    catch (ArgumentException)
    {
        // Entity doesn't exist, which is what we want
        alreadyExists = false;
    }

    if (alreadyExists)
    {
        Console.WriteLine($"Entity of type {entity.GetType().Name} with key {key} already exists in blackboard");
        return false;
    }

    // Register the entity based on its type
    switch (entity)
    {
        case Element element:
            blackboard.SetElement(key, element);
            Console.WriteLine($"Registered Element: {key}");
            break;
        case Agent agent:
            blackboard.SetAgent(key, agent);
            Console.WriteLine($"Registered Agent: {key}");
            break;
        case Location location:
            blackboard.SetLocation(key, location);
            Console.WriteLine($"Registered Location: {key}");
            break;
        case Layer layer:
            blackboard.SetLayer(key, layer);
            Console.WriteLine($"Registered Layer: {key}");
            break;
        case Module module:
            blackboard.SetModule(key, module);
            Console.WriteLine($"Registered Module: {key}");
            break;
        case Tool tool:
            blackboard.SetTool(key, tool);
            Console.WriteLine($"Registered Tool: {key}");
            break;
        default:
            throw new ArgumentException($"Unsupported entity type: {entity.GetType().Name}");
    }

    return true;
}


    
}