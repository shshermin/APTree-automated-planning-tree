public class BlackboardInitializer
{
    private readonly Blackboard<FastName> blackboard;
    private readonly Dictionary<string, FastName> createdKeys;

    public BlackboardInitializer(Blackboard<FastName> blackboard)
    {
        this.blackboard = blackboard;
        this.createdKeys = new Dictionary<string, FastName>();
    }
    /// <summary>
    /// Initialize the blackboard with the given entities
    /// </summary>
    /// <param name="agents"></param>
    /// <param name="elements"></param>
    /// <param name="locations"></param>
    /// <param name="layers"></param>
    /// <param name="modules"></param>

    public void Initialize(
        IEnumerable<Agent> agents = null,
        IEnumerable<Element> elements = null,
        IEnumerable<Location> locations = null,
        IEnumerable<Layer> layers = null,
        IEnumerable<Module> modules = null)
    {
        Console.WriteLine("Starting blackboard initialization...");

        if (agents != null)
        {
            foreach (var agent in agents)
            {
                var key = agent.NameKey;
                blackboard.SetAgent(key, agent);
                Console.WriteLine($"Added agent: {agent.NameKey.ToString()}");
            }
        }

        if (elements != null)
        {
            foreach (var element in elements)
            {
                var key = element.NameKey;
                blackboard.SetElement(key, element);
                Console.WriteLine($"Added element: {element.NameKey.ToString()}");
            }
        }

        if (locations != null)
        {
            foreach (var location in locations)
            {
                var key = location.NameKey;
                blackboard.SetLocation(key, location);
                Console.WriteLine($"Added location: {location.NameKey.ToString()}");
            }
        }
        if(layers != null)
        {
            foreach (var layer in layers)
            {
                var key = layer.NameKey;
                blackboard.SetLayer(key, layer);
                Console.WriteLine($"Added layer: {layer.NameKey.ToString()}");
            }
        }
        if(modules != null)
        {
            foreach (var module in modules)
            {
                var key = module.NameKey;
                blackboard.SetModule(key, module);
                Console.WriteLine($"Added module: {module.NameKey.ToString()}");
            }
        }
        Console.WriteLine("Blackboard initialization completed.");
    }

    // Get existing key or create new one
    private FastName CreateOrGetKey(string name)
    {
        if (!createdKeys.TryGetValue(name, out FastName key))
        {
            key = new FastName(name);
            createdKeys[name] = key;
        }
        return key;
    }

    // Get a previously created key
    public FastName GetKey(string name)
    {
        if (!createdKeys.TryGetValue(name, out FastName key))
        {
            throw new KeyNotFoundException($"No FastName key exists for '{name}'. Make sure the object was added to the blackboard first.");
        }
        return key;
    }

    // Verify if all objects were added successfully
    public void VerifyInitialization()
    {
        Console.WriteLine("\nVerifying blackboard contents:");
        foreach (var keyPair in createdKeys)
        {
            try
            {
                if (blackboard.GetAgent(keyPair.Value) != null)
                    Console.WriteLine($"{keyPair.Key} (Agent) exists: True");
            }
            catch
            {
                try
                {
                    if (blackboard.GetElement(keyPair.Value) != null)
                        Console.WriteLine($"{keyPair.Key} (Element) exists: True");
                }
                catch
                {
                    try
                    {
                        if (blackboard.GetLocation(keyPair.Value) != null)
                            Console.WriteLine($"{keyPair.Key} (Location) exists: True");
                    }
                    catch
                    {
                        Console.WriteLine($"{keyPair.Key} exists: False");
                    }
                }
            }
        }
    }
}