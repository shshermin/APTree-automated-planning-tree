
using ModelLoader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;


/// <summary>
/// This class is responsible for initializing the system by registering entity types, action types, and predicate types.
/// It also creates initial entities and adds them to the blackboard.
/// </summary>
public class BlackboardWriter
{
    private Blackboard<FastName> blackboard;
    private FactoryParameter entityFactory;
    private FactoryAction actionFactory;
    private readonly FactoryPredicate predicateFactory;

    public BlackboardWriter(Blackboard<FastName> blackboard)
    {
        this.blackboard = blackboard;
        this.entityFactory = FactoryParameter.Instance;
        this.actionFactory = FactoryAction.Instance;
        this.predicateFactory = FactoryPredicate.Instance;
    }

    /// <summary>
    /// Registers all parameter types from the ParameterTypes folder into the blackboard
    /// </summary>
    public void RegisterParameterTypes()
    {
        Console.WriteLine("Registering parameter types...");
        
        try
        {
            // Get the path to the ParameterTypes folder
            string parameterTypesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "src", "ModelLoader", "ParameterTypes");
            
            if (Directory.Exists(parameterTypesPath))
            {
                // Get all .cs files in the ParameterTypes folder
                string[] csFiles = Directory.GetFiles(parameterTypesPath, "*.cs");
                
                foreach (string file in csFiles)
                {
                    string fileName = Path.GetFileNameWithoutExtension(file);
                    Console.WriteLine($"Processing parameter type: {fileName}");
                    
                    try
                    {
                        // Register the entity type
                        blackboard.RegisterEntityType(new FastName(fileName));
                        Console.WriteLine($"Registered entity type: {fileName}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing parameter type {fileName}: {ex.Message}");
                    }
                }
                
                Console.WriteLine("Parameter types registration completed");
            }
            else
            {
                Console.WriteLine($"Warning: ParameterTypes folder not found at {parameterTypesPath}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error registering parameter types: {ex.Message}");
        }
    }

    /// <summary>
    /// Registers all predicate types from the PredicateTypes folder into the blackboard
    /// </summary>
    public void RegisterPredicateTypes()
    {
        Console.WriteLine("Registering predicate types...");
        
        try
        {
            // Get the path to the PredicateTypes folder
            string predicateTypesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "src", "ModelLoader", "PredicateTypes");
            
            if (Directory.Exists(predicateTypesPath))
            {
                // Get all .cs files in the PredicateTypes folder
                string[] csFiles = Directory.GetFiles(predicateTypesPath, "*.cs");
                
                foreach (string file in csFiles)
                {
                    string fileName = Path.GetFileNameWithoutExtension(file);
                    Console.WriteLine($"Processing predicate type: {fileName}");
                    
                    try
                    {
                        // Register the predicate type
                        blackboard.RegisterPredicateType(new FastName(fileName));
                        Console.WriteLine($"Registered predicate type: {fileName}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing predicate type {fileName}: {ex.Message}");
                    }
                }
                
                Console.WriteLine("Predicate types registration completed");
            }
            else
            {
                Console.WriteLine($"Warning: PredicateTypes folder not found at {predicateTypesPath}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error registering predicate types: {ex.Message}");
        }
    }

    /// <summary>
    /// Registers all action types from the ActionTypes folder into the blackboard
    /// </summary>
    public void RegisterActionTypes()
    {
        Console.WriteLine("Registering action types...");
        
        try
        {
            // Get the path to the ActionTypes folder
            string actionTypesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "src", "ModelLoader", "ActionTypes");
            
            if (Directory.Exists(actionTypesPath))
            {
                // Get all .cs files in the ActionTypes folder
                string[] csFiles = Directory.GetFiles(actionTypesPath, "*.cs");
                
                foreach (string file in csFiles)
                {
                    string fileName = Path.GetFileNameWithoutExtension(file);
                    Console.WriteLine($"Processing action type: {fileName}");
                    
                    try
                    {
                        // Register the action type
                        blackboard.RegisterActionType(new FastName(fileName));
                        Console.WriteLine($"Registered action type: {fileName}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing action type {fileName}: {ex.Message}");
                    }
                }
                
                Console.WriteLine("Action types registration completed");
            }
            else
            {
                Console.WriteLine($"Warning: ActionTypes folder not found at {actionTypesPath}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error registering action types: {ex.Message}");
        }
    }

    /// <summary>
    /// Registers all types (parameter, predicate, and action types) into the blackboard
    /// </summary>
    public void RegisterAllTypes()
    {
        Console.WriteLine("Starting registration of all types...");
        
        RegisterParameterTypes();
        RegisterPredicateTypes();
        RegisterActionTypes();
        
        Console.WriteLine("All types registration completed");
    }

    /// <summary>
    /// Registers parameter instances from a MontiCore grammar file
    /// </summary>
    /// <param name="parameterInstancesFile">Path to the parameter instances file</param>
    public void RegisterParameterInstances(string parameterInstancesFile)
    {
        Console.WriteLine("\n=== REGISTERING PARAMETER INSTANCES ===");
        var parameterInstances = ParseMontiCoreGrammarFile(parameterInstancesFile);
        foreach (var instance in parameterInstances)
        {
            RegisterParameterInstanceByBaseType(instance);
        }
    }

    /// <summary>
    /// Registers predicate instances from a MontiCore grammar file
    /// </summary>
    /// <param name="predicateInstancesFile">Path to the predicate instances file</param>
    public void RegisterPredicateInstances(string predicateInstancesFile)
    {
        Console.WriteLine("\n=== REGISTERING PREDICATE INSTANCES ===");
        ParseAndRegisterMontiCorePredicateFile(predicateInstancesFile, blackboard);
    }

    /// <summary>
    /// Registers action instances from action definition strings
    /// </summary>
    /// <param name="actionDefinitionStrings">Array of action definition strings</param>
    public void RegisterActionInstances(string[] actionDefinitionStrings)
    {
        Console.WriteLine("\n=== REGISTERING ACTION INSTANCES ===");
        CreateAndRegisterActionInstances(actionDefinitionStrings);
    }

    /// <summary>
    /// Registers action instances from a file
    /// </summary>
    /// <param name="actionInstancesFile">Path to the action instances file</param>
    public void RegisterActionInstancesFromFile(string actionInstancesFile)
    {
        Console.WriteLine("\n=== REGISTERING ACTION INSTANCES FROM FILE ===");
        string[] actionDefinitionStrings = ReadActionDefinitionsFromFile(actionInstancesFile);
        RegisterActionInstances(actionDefinitionStrings);
    }

    /// <summary>
    /// Registers all instances (parameters, predicates, and actions) using file paths
    /// </summary>
    /// <param name="parameterInstancesFile">Path to the parameter instances file</param>
    /// <param name="predicateInstancesFile">Path to the predicate instances file</param>
    /// <param name="actionInstancesFile">Path to the action instances file</param>
    public void RegisterAllInstances(string parameterInstancesFile, string predicateInstancesFile, string actionInstancesFile)
    {
        Console.WriteLine("Starting registration of all instances...");
        
        RegisterParameterInstances(parameterInstancesFile);
        RegisterPredicateInstances(predicateInstancesFile);
        RegisterActionInstancesFromFile(actionInstancesFile);
        
        Console.WriteLine("All instances registration completed");
    }

    /// <summary>
    /// Registers all instances using default file paths
    /// </summary>
    /// <param name="actionInstancesFile">Path to the action instances file</param>
    public void RegisterAllInstances(string actionInstancesFile)
    {
        // Use default file paths for parameters and predicates
        string parameterInstancesFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "src", "InputInstances", "ParameterInstances.txt");
        string predicateInstancesFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "src", "InputInstances", "PredicateInstances.txt");
        
        RegisterAllInstances(parameterInstancesFile, predicateInstancesFile, actionInstancesFile);
    }

    /// <summary>
    /// Creates and registers all instances using default file paths and action definitions
    /// </summary>
    /// <param name="actionDefinitionStrings">Array of action definition strings</param>
    public void CreateAndRegisterAllInstances(string parameterInstancesFile, string predicateInstancesFile, string[] actionDefinitionStrings)
    {
        Console.WriteLine("Starting creation and registration of all instances...");
        
        RegisterParameterInstances(parameterInstancesFile);
         RegisterPredicateInstances(predicateInstancesFile);
        RegisterActionInstances(actionDefinitionStrings);
        
        Console.WriteLine("All instances creation and registration completed");
    }

    /// <summary>
    /// Creates and registers all instances using default file paths and action definitions
    /// </summary>
    /// <param name="actionDefinitionStrings">Array of action definition strings</param>
    public void CreateAndRegisterAllInstances(string[] actionDefinitionStrings)
    {
        // Use default file paths
        string parameterInstancesFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "src", "InputInstances", "ParameterInstances.txt");
        string predicateInstancesFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "src", "InputInstances", "PredicateInstances.txt");
        
        CreateAndRegisterAllInstances(parameterInstancesFile, predicateInstancesFile, actionDefinitionStrings);
    }

    /// <summary>
    /// Creates and registers all instances using default file paths and action definitions from a file
    /// </summary>
    /// <param name="actionInstancesFile">Path to the action instances file</param>
    public void CreateAndRegisterAllInstancesFromFiles(string actionInstancesFile)
    {
        // Use default file paths for parameters and predicates
        string parameterInstancesFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "src", "InputInstances", "ParameterInstances.txt");
        string predicateInstancesFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "src", "InputInstances", "PredicateInstances.txt");
        
        // Read action definitions from file
        string[] actionDefinitionStrings = ReadActionDefinitionsFromFile(actionInstancesFile);
        
        CreateAndRegisterAllInstances(parameterInstancesFile, predicateInstancesFile, actionDefinitionStrings);
    }

    /// <summary>
    /// Creates and registers all instances including NodeGraphs using file paths
    /// </summary>
    /// <param name="parameterInstancesFile">Path to the parameter instances file</param>
    /// <param name="predicateInstancesFile">Path to the predicate instances file</param>
    /// <param name="actionInstancesFile">Path to the action instances file</param>
    /// <param name="nodeGraphFile">Path to the NodeGraph file</param>
    public void CreateAndRegisterAllInstancesWithNodeGraph(string parameterInstancesFile, string predicateInstancesFile, string actionInstancesFile, string nodeGraphFile)
    {
        Console.WriteLine("Starting creation and registration of all instances with NodeGraph...");
        
        // Register parameters, predicates, and actions first
        RegisterParameterInstances(parameterInstancesFile);
        RegisterPredicateInstances(predicateInstancesFile);
        RegisterActionInstancesFromFile(actionInstancesFile);
        
        // Parse and register the NodeGraph
        ParseAndRegisterNodeGraph(nodeGraphFile);
        
        Console.WriteLine("All instances and NodeGraph registration completed");
    }

    /// <summary>
    /// Creates and registers all instances including NodeGraphs using default file paths
    /// </summary>
    /// <param name="actionInstancesFile">Path to the action instances file</param>
    /// <param name="nodeGraphFile">Path to the NodeGraph file</param>
    public void CreateAndRegisterAllInstancesWithNodeGraph(string actionInstancesFile, string nodeGraphFile)
    {
        // Use default file paths for parameters and predicates
        string parameterInstancesFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "src", "InputInstances", "ParameterInstances.txt");
        string predicateInstancesFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "src", "InputInstances", "PredicateInstances.txt");
        
        CreateAndRegisterAllInstancesWithNodeGraph(parameterInstancesFile, predicateInstancesFile, actionInstancesFile, nodeGraphFile);
    }

    /// <summary>
    /// Reads action definition strings from a file
    /// Expected format: one action definition per line
    /// </summary>
    /// <param name="filePath">Path to the action instances file</param>
    /// <returns>Array of action definition strings</returns>
    private string[] ReadActionDefinitionsFromFile(string filePath)
    {
        List<string> actionDefinitions = new List<string>();
        
        try
        {
            Console.WriteLine($"Reading action definitions from: {filePath}");
            
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Warning: Action instances file not found at {filePath}");
                return actionDefinitions.ToArray();
            }
            
            string[] lines = File.ReadAllLines(filePath);
            
            foreach (string line in lines)
            {
                // Skip comments and empty lines
                if (string.IsNullOrWhiteSpace(line) || line.Trim().StartsWith("#"))
                    continue;
                
                // Add the action definition
                actionDefinitions.Add(line.Trim());
            }
            
            Console.WriteLine($"Read {actionDefinitions.Count} action definitions from file");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading action instances file {filePath}: {ex.Message}");
        }
        
        return actionDefinitions.ToArray();
    }

    /// <summary>
    /// Takes a single parameter instance, determines its base type, and registers it in the appropriate blackboard dictionary
    /// </summary>
    /// <param name="parameterInstance">The parameter instance to register</param>
    public void RegisterParameterInstanceByBaseType(Entity parameterInstance)
    {
        try
        {
            // Get the base type from the entity
            string baseTypeName = GetBaseTypeName(parameterInstance);
            
            Console.WriteLine($"Registering {parameterInstance.GetType().Name} instance '{parameterInstance.ID}' as base type '{baseTypeName}'");
            
            // Register the instance in the appropriate blackboard dictionary based on base type
            switch (baseTypeName.ToLower())
            {
                case "element":
                    blackboard.RegisterEntityIfNotExists(parameterInstance);
                    Console.WriteLine($"  ✅ Registered as Element: {parameterInstance.ID}");
                    break;
                    
                case "agent":
                    blackboard.RegisterEntityIfNotExists(parameterInstance);
                    Console.WriteLine($"  ✅ Registered as Agent: {parameterInstance.ID}");
                    break;
                    
                case "location":
                    blackboard.RegisterEntityIfNotExists(parameterInstance);
                    Console.WriteLine($"  ✅ Registered as Location: {parameterInstance.ID}");
                    break;
                    
                case "tool":
                    blackboard.RegisterEntityIfNotExists(parameterInstance);
                    Console.WriteLine($"  ✅ Registered as Tool: {parameterInstance.ID}");
                    break;
                    
                case "layer":
                    blackboard.RegisterEntityIfNotExists(parameterInstance);
                    Console.WriteLine($"  ✅ Registered as Layer: {parameterInstance.ID}");
                    break;
                    
                case "module":
                    blackboard.RegisterEntityIfNotExists(parameterInstance);
                    Console.WriteLine($"  ✅ Registered as Module: {parameterInstance.ID}");
                    break;
                    
                default:
                    Console.WriteLine($"  ⚠️ Unknown base type '{baseTypeName}' for instance '{parameterInstance.ID}', registering as generic Entity");
                    blackboard.RegisterEntityIfNotExists(parameterInstance);
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ❌ Error registering instance '{parameterInstance.ID}': {ex.Message}");
        }
    }

    /// <summary>
    /// Determines the base type name of an entity instance
    /// </summary>
    /// <param name="instance">The entity instance</param>
    /// <returns>The base type name (e.g., "Element", "Agent", "Location")</returns>
    private string GetBaseTypeName(Entity instance)
    {
        // Get the base type from the entity's BaseType property
        if (instance.BaseType != null)
        {
            return instance.BaseType.ToString();
        }
        
        // Fallback: determine base type from inheritance hierarchy
        Type currentType = instance.GetType();
        
        // Check inheritance hierarchy to find the base type
        while (currentType != null && currentType != typeof(Entity))
        {
            if (currentType == typeof(Element))
                return "Element";
            if (currentType == typeof(Agent))
                return "Agent";
            if (currentType == typeof(Location))
                return "Location";
            if (currentType == typeof(Tool))
                return "Tool";
            if (currentType == typeof(Layer))
                return "Layer";
            if (currentType == typeof(Module))
                return "Module";
                
            currentType = currentType.BaseType;
        }
        
        // If no specific base type found, return the actual type name
        return instance.GetType().Name;
    }

    /// <summary>
    /// Parses a MontiCore grammar text file and generates parameter instances
    /// Expected format: ParameterInstance: typeName {instanceName}
    /// </summary>
    /// <param name="filePath">Path to the MontiCore grammar text file</param>
    /// <returns>List of created parameter instances</returns>
    public List<Entity> ParseMontiCoreGrammarFile(string filePath)
    {
        List<Entity> createdInstances = new List<Entity>();
        
        try
        {
            Console.WriteLine($"Parsing MontiCore grammar file: {filePath}");
            
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"❌ Error: File not found at {filePath}");
                return createdInstances;
            }
            
            string[] lines = File.ReadAllLines(filePath);
            int lineNumber = 0;
            int successCount = 0;
            int errorCount = 0;
            
            foreach (string line in lines)
            {
                lineNumber++;
                
                // Skip empty lines and comments
                if (string.IsNullOrWhiteSpace(line) || line.Trim().StartsWith("#"))
                {
                    continue;
                }
                
                try
                {
                    // Parse the line: ParameterInstance: typeName {instanceName}
                    var instance = ParseParameterInstanceLine(line.Trim());
                    
                    if (instance != null)
                    {
                        createdInstances.Add(instance);
                        successCount++;
                        Console.WriteLine($"  ✅ Line {lineNumber}: Created {instance.GetType().Name} instance '{instance.ID}'");
                    }
                }
                catch (Exception ex)
                {
                    errorCount++;
                    Console.WriteLine($"  ❌ Line {lineNumber}: Error parsing '{line.Trim()}': {ex.Message}");
                }
            }
            
            Console.WriteLine($"\n📊 Parsing Summary:");
            Console.WriteLine($"  ✅ Successfully created: {successCount} instances");
            Console.WriteLine($"  ❌ Errors: {errorCount}");
            Console.WriteLine($"  📄 Total lines processed: {lineNumber}");
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error reading file {filePath}: {ex.Message}");
        }
        
        return createdInstances;
    }
    
    /// <summary>
    /// Parses a single line in MontiCore grammar format
    /// Expected format: ParameterInstance: typeName {instanceName}
    /// </summary>
    /// <param name="line">The line to parse</param>
    /// <returns>Created Entity instance or null if parsing failed</returns>
    private Entity ParseParameterInstanceLine(string line)
    {
        // Expected format: ParameterInstance: typeName {instanceName}
        const string prefix = "ParameterInstance:";
        
        if (!line.StartsWith(prefix))
        {
            throw new ArgumentException($"Line does not start with '{prefix}'");
        }
        
        // Remove the prefix and trim
        string content = line.Substring(prefix.Length).Trim();
        
        // Find the opening and closing braces
        int openBraceIndex = content.IndexOf('{');
        int closeBraceIndex = content.LastIndexOf('}');
        
        if (openBraceIndex == -1 || closeBraceIndex == -1 || openBraceIndex >= closeBraceIndex)
        {
            throw new ArgumentException("Invalid brace format. Expected: typeName {instanceName}");
        }
        
        // Extract type name and instance name
        string typeName = content.Substring(0, openBraceIndex).Trim();
        string instanceName = content.Substring(openBraceIndex + 1, closeBraceIndex - openBraceIndex - 1).Trim();
        
        if (string.IsNullOrWhiteSpace(typeName))
        {
            throw new ArgumentException("Type name cannot be empty");
        }
        
        if (string.IsNullOrWhiteSpace(instanceName))
        {
            throw new ArgumentException("Instance name cannot be empty");
        }
        
        // Create the parameter instance using the factory
        return entityFactory.CreateParameter(typeName, instanceName);
    }
    
    /// <summary>
    /// Parses a MontiCore grammar text file and registers all parameter instances in the blackboard
    /// </summary>
    /// <param name="filePath">Path to the MontiCore grammar text file</param>
    public void ParseAndRegisterMontiCoreGrammarFile(string filePath)
    {
        Console.WriteLine($"\n=== PARSING AND REGISTERING MONTICORE GRAMMAR FILE ===");
        
        var instances = ParseMontiCoreGrammarFile(filePath);
        
        Console.WriteLine($"\n=== REGISTERING {instances.Count} INSTANCES IN BLACKBOARD ===");
        
        int registeredCount = 0;
        int skippedCount = 0;
        
        foreach (var instance in instances)
        {
            try
            {
                // Register the instance by its base type
                RegisterParameterInstanceByBaseType(instance);
                registeredCount++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ❌ Error registering instance '{instance.ID}': {ex.Message}");
                skippedCount++;
            }
        }
        
        Console.WriteLine($"\n📊 Registration Summary:");
        Console.WriteLine($"  ✅ Successfully registered: {registeredCount} instances");
        Console.WriteLine($"  ⚠️ Skipped: {skippedCount} instances");
    }

    /// <summary>
    /// Parses a MontiCore grammar text file and generates predicate instances
    /// Expected format: PredicateInstance: predicateName(parameterName = value, isNegated = false/true)
    /// </summary>
    /// <param name="filePath">Path to the MontiCore grammar text file</param>
    /// <param name="blackboard">The blackboard to get entities from</param>
    /// <returns>List of created predicate instances</returns>
    public  List<Predicate> ParseMontiCorePredicateFile(string filePath, Blackboard<FastName> blackboard)
    {
        List<Predicate> createdInstances = new List<Predicate>();
        
        try
        {
            Console.WriteLine($"Parsing MontiCore predicate file: {filePath}");
            
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"❌ Error: File not found at {filePath}");
                return createdInstances;
            }
            
            string[] lines = File.ReadAllLines(filePath);
            int lineNumber = 0;
            int successCount = 0;
            int errorCount = 0;
            
            Console.WriteLine($"📄 Found {lines.Length} lines in file");
            
            foreach (string line in lines)
            {
                lineNumber++;
                
                // Skip empty lines and comments
                if (string.IsNullOrWhiteSpace(line) || line.Trim().StartsWith("#"))
                {
                    Console.WriteLine($"  ⏭️ Line {lineNumber}: Skipping (empty or comment)");
                    continue;
                }
                
                Console.WriteLine($"\n🔍 Line {lineNumber}: Processing '{line.Trim()}'");
                
                try
                {
                    // Parse the line: PredicateInstance: predicateName(parameterName = value, isNegated = false/true)
                    Predicate instance =  ParsePredicateInstanceLine(line.Trim(), blackboard);
                    
                    if (instance != null)
                    {
                        createdInstances.Add(instance);
                        successCount++;
                        Console.WriteLine($"  ✅ Line {lineNumber}: Created {instance.GetType().Name} instance '{instance.PredicateName}'");
                        
                        // Verify the instance was actually registered
                        var allPredicates = blackboard.GetAllPredicates();
                        var foundInBlackboard = allPredicates.Any(p => p.PredicateName == instance.PredicateName);
                        Console.WriteLine($"  🔍 Line {lineNumber}: Predicate in blackboard: {foundInBlackboard}");
                        
                        if (!foundInBlackboard)
                        {
                            Console.WriteLine($"  ⚠️ WARNING: Predicate {instance.PredicateName} was created but not found in blackboard!");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"  ❌ Line {lineNumber}: ParsePredicateInstanceLine returned null");
                        errorCount++;
                    }
                }
                catch (Exception ex)
                {
                    errorCount++;
                    Console.WriteLine($"  ❌ Line {lineNumber}: Error parsing '{line.Trim()}': {ex.Message}");
                    Console.WriteLine($"  📋 Exception details: {ex}");
                }
            }
            
            Console.WriteLine($"\n📊 Parsing Summary:");
            Console.WriteLine($"  ✅ Successfully created: {successCount} instances");
            Console.WriteLine($"  ❌ Errors: {errorCount}");
            Console.WriteLine($"  📄 Total lines processed: {lineNumber}");
            
            // Final verification
            var finalPredicates = blackboard.GetAllPredicates();
            Console.WriteLine($"  📊 Final predicate count in blackboard: {finalPredicates.Count}");
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error reading file {filePath}: {ex.Message}");
            Console.WriteLine($"📋 Exception details: {ex}");
        }
        
        return createdInstances;
    }
    
    /// <summary>
    /// Parses a single line in MontiCore predicate grammar format
    /// Expected format: PredicateInstance: predicateName(parameterName = value, isNegated = false/true)
    /// </summary>
    /// <param name="line">The line to parse</param>
    /// <param name="blackboard">The blackboard to get entities from</param>
    /// <returns>Created Predicate instance or null if parsing failed</returns>
    private Predicate ParsePredicateInstanceLine(string line, Blackboard<FastName> blackboard)
    {
        Console.WriteLine($"  🔧 ParsePredicateInstanceLine: Starting to parse line: '{line}'");
        
        // Expected format: PredicateInstance: predicateName(parameterName = value, isNegated = false/true)
        const string prefix = "PredicateInstance:";
        
        if (!line.StartsWith(prefix))
        {
            throw new ArgumentException($"Line does not start with '{prefix}'");
        }
        
        // Remove the prefix and trim
        string content = line.Substring(prefix.Length).Trim();
        Console.WriteLine($"  🔧 ParsePredicateInstanceLine: Content after prefix: '{content}'");
        
        // Find the opening and closing parentheses
        int openParenIndex = content.IndexOf('(');
        int closeParenIndex = content.LastIndexOf(')');
        
        Console.WriteLine($"  🔧 ParsePredicateInstanceLine: Parentheses positions - open: {openParenIndex}, close: {closeParenIndex}");
        
        if (openParenIndex == -1 || closeParenIndex == -1 || openParenIndex >= closeParenIndex)
        {
            throw new ArgumentException("Invalid parentheses format. Expected: predicateName(parameterName = value, isNegated = false/true)");
        }
        
        // Extract predicate name and parameters
        string predicateName = content.Substring(0, openParenIndex).Trim();
        string parametersContent = content.Substring(openParenIndex + 1, closeParenIndex - openParenIndex - 1).Trim();
        
        Console.WriteLine($"  🔧 ParsePredicateInstanceLine: Predicate name: '{predicateName}'");
        Console.WriteLine($"  🔧 ParsePredicateInstanceLine: Parameters content: '{parametersContent}'");
        
        if (string.IsNullOrWhiteSpace(predicateName))
        {
            throw new ArgumentException("Predicate name cannot be empty");
        }
        
        // Parse parameters
        var parameters = ParsePredicateParameters(parametersContent);
        Console.WriteLine($"  🔧 ParsePredicateInstanceLine: Parsed parameters: {string.Join(", ", parameters.Select(kvp => $"{kvp.Key}={kvp.Value}"))}");
        
        // Convert to ParameterMapping list
        var parameterMappings = ConvertToParameterMappingList(parameters);
        Console.WriteLine($"  🔧 ParsePredicateInstanceLine: Parameter mappings: {string.Join(", ", parameterMappings.Select(pm => $"{pm.ParameterName}={pm.ParameterValue}"))}");
        
        Console.WriteLine($"  🔧 ParsePredicateInstanceLine: Calling predicateFactory.CreatePredicateInstance...");
        
        // Create the predicate instance using the factory
        var result = predicateFactory.CreatePredicateInstance(predicateName, parameterMappings, blackboard);
        
        Console.WriteLine($"  🔧 ParsePredicateInstanceLine: Factory returned: {result?.GetType().Name} with PredicateName: {result?.PredicateName}");
        
        return result;
    }
    
    /// <summary>
    /// Parses predicate parameters from a string
    /// Expected format: parameterName = value, isNegated = false/true
    /// </summary>
    /// <param name="parametersContent">The parameters string to parse</param>
    /// <returns>Dictionary of parameter names and values</returns>
    private Dictionary<string, object> ParsePredicateParameters(string parametersContent)
    {
        var parameters = new Dictionary<string, object>();
        
        if (string.IsNullOrWhiteSpace(parametersContent))
        {
            return parameters;
        }
        
        // Split by comma, but be careful about commas inside parentheses
        var parameterPairs = parametersContent.Split(',');
        
        foreach (var pair in parameterPairs)
        {
            var trimmedPair = pair.Trim();
            if (string.IsNullOrWhiteSpace(trimmedPair))
                continue;
                
            // Split by equals sign
            var equalIndex = trimmedPair.IndexOf('=');
            if (equalIndex == -1)
            {
                throw new ArgumentException($"Invalid parameter format: {trimmedPair}. Expected: parameterName = value");
            }
            
            string paramName = trimmedPair.Substring(0, equalIndex).Trim();
            string paramValue = trimmedPair.Substring(equalIndex + 1).Trim();
            
            if (string.IsNullOrWhiteSpace(paramName))
            {
                throw new ArgumentException($"Parameter name cannot be empty in: {trimmedPair}");
            }
            
            if (string.IsNullOrWhiteSpace(paramValue))
            {
                throw new ArgumentException($"Parameter value cannot be empty in: {trimmedPair}");
            }
            
            // Convert value to appropriate type
            object convertedValue = ConvertParameterValue(paramValue);
            parameters[paramName] = convertedValue;
        }
        
        return parameters;
    }
    
    /// <summary>
    /// Converts a parameter value string to the appropriate type
    /// </summary>
    /// <param name="value">The value string to convert</param>
    /// <returns>The converted value</returns>
    private object ConvertParameterValue(string value)
    {
        // Handle boolean values
        if (value.Equals("true", StringComparison.OrdinalIgnoreCase))
            return true;
        if (value.Equals("false", StringComparison.OrdinalIgnoreCase))
            return false;
            
        // Handle numeric values
        if (int.TryParse(value, out int intValue))
            return intValue;
        if (double.TryParse(value, out double doubleValue))
            return doubleValue;
            
        // Handle string values (remove quotes if present)
        if (value.StartsWith("\"") && value.EndsWith("\""))
            return value.Substring(1, value.Length - 2);
            
        // Default to string
        return value;
    }
    
    /// <summary>
    /// Converts a Dictionary<string, object> to a List<ParameterMapping>
    /// </summary>
    /// <param name="parameters">The parameters dictionary to convert</param>
    /// <returns>List of ParameterMapping</returns>
    private List<ParameterMapping> ConvertToParameterMappingList(Dictionary<string, object> parameters)
    {
        var parameterMappings = new List<ParameterMapping>();
        foreach (var kvp in parameters)
        {
            parameterMappings.Add(new ParameterMapping(kvp.Key, kvp.Value?.ToString() ?? ""));
        }
        return parameterMappings;
    }
    
    /// <summary>
    /// Parses a MontiCore predicate grammar text file and registers all predicate instances in the blackboard
    /// </summary>
    /// <param name="filePath">Path to the MontiCore predicate grammar text file</param>
    /// <param name="blackboard">The blackboard to register predicates in</param>
    public void ParseAndRegisterMontiCorePredicateFile(string filePath, Blackboard<FastName> blackboard)
    {
        Console.WriteLine($"\n=== PARSING AND REGISTERING MONTICORE PREDICATE FILE ===");
        Console.WriteLine($"📁 File: {filePath}");
        
        List<Predicate> instances = ParseMontiCorePredicateFile(filePath, blackboard);
        
        Console.WriteLine($"\n=== PREDICATE INSTANCES CREATED AND REGISTERED ===");
        Console.WriteLine($"  ✅ Successfully created: {instances.Count} predicates");
        
        // Verify predicates are actually in blackboard
        var allPredicates = blackboard.GetAllPredicates();
        Console.WriteLine($"  📊 Total predicates in blackboard: {allPredicates.Count}");
        
        if (allPredicates.Count > 0)
        {
            Console.WriteLine($"  📋 Predicates in blackboard:");
            foreach (var pred in allPredicates)
            {
                Console.WriteLine($"    - {pred.PredicateName} ({pred.GetType().Name}) - isNegated: {pred.not}");
            }
        }
        else
        {
            Console.WriteLine($"  ⚠️ WARNING: No predicates found in blackboard after registration!");
        }
        
        Console.WriteLine($"  📝 Note: Predicates are automatically registered by the factory");
    }

    /// <summary>
    /// Creates and registers multiple action instances from action definition strings
    /// Expected format: ActionInstance: actionType(parameter1 : value1, parameter2 : value2, ...)
    /// </summary>
    /// <param name="actionDefinitionStrings">Array of action definition strings</param>
    /// <returns>List of created and registered action instances</returns>
    public List<PActionNode> CreateAndRegisterActionInstances(string[] actionDefinitionStrings)
    {
        List<PActionNode> createdActions = new List<PActionNode>();
        
        Console.WriteLine($"\n=== CREATING AND REGISTERING {actionDefinitionStrings.Length} ACTION INSTANCES ===");
        
        int successCount = 0;
        int errorCount = 0;
        
        foreach (string actionDefinition in actionDefinitionStrings)
        {
            try
            {
                Console.WriteLine($"\n🔧 Processing action definition: {actionDefinition}");
                
                // Create the action instance using FactoryAction
                var actionInstance = actionFactory.CreateActionInstance(actionDefinition, blackboard);
                
                if (actionInstance != null)
                {
                    // Generate a unique key for the action instance
                    string actionKey = GenerateActionInstanceKey(actionDefinition);
                    var fastNameKey = new FastName(actionKey);
                    
                    // Register the action instance on the blackboard
                    blackboard.SetActionType(fastNameKey, actionInstance);
                    
                    createdActions.Add(actionInstance);
                    successCount++;
                    
                    Console.WriteLine($"  ✅ Successfully created and registered action: {actionInstance.GetType().Name}");
                    Console.WriteLine($"  🔑 Registered with key: {actionKey}");
                    Console.WriteLine($"  📝 Debug Display Name: {actionInstance.DebugDisplayName}");
                }
                else
                {
                    errorCount++;
                    Console.WriteLine($"  ❌ Failed to create action instance for: {actionDefinition}");
                }
            }
            catch (Exception ex)
            {
                errorCount++;
                Console.WriteLine($"  ❌ Error processing action definition '{actionDefinition}': {ex.Message}");
            }
        }
        
        Console.WriteLine($"\n📊 Action Instance Creation Summary:");
        Console.WriteLine($"  ✅ Successfully created and registered: {successCount} actions");
        Console.WriteLine($"  ❌ Errors: {errorCount}");
        Console.WriteLine($"  📄 Total definitions processed: {actionDefinitionStrings.Length}");
        
        return createdActions;
    }
    
    /// <summary>
    /// Creates and registers a single action instance from an action definition string
    /// </summary>
    /// <param name="actionDefinition">Action definition string</param>
    /// <returns>Created and registered action instance</returns>
    public PActionNode CreateAndRegisterActionInstance(string actionDefinition)
    {
        var actions = CreateAndRegisterActionInstances(new string[] { actionDefinition });
        return actions.Count > 0 ? actions[0] : null;
    }
    
    /// <summary>
    /// Generates a unique key for an action instance based on its definition
    /// </summary>
    /// <param name="actionDefinition">The action definition string</param>
    /// <returns>A unique key string</returns>
    public string GenerateActionInstanceKey(string actionDefinition)
    {
        // Expected format: ActionInstance: actionType(parameter1 : value1, parameter2 : value2, ...)
        const string prefix = "ActionInstance:";
        
        if (!actionDefinition.StartsWith(prefix))
        {
            throw new ArgumentException($"Action definition does not start with '{prefix}'");
        }
        
        // Remove the prefix and trim
        string content = actionDefinition.Substring(prefix.Length).Trim();
        
        // Find the opening and closing parentheses
        int openParenIndex = content.IndexOf('(');
        int closeParenIndex = content.LastIndexOf(')');
        
        if (openParenIndex == -1 || closeParenIndex == -1 || openParenIndex >= closeParenIndex)
        {
            throw new ArgumentException("Invalid parentheses format. Expected: actionType(parameter1 : value1, parameter2 : value2, ...)");
        }
        
        // Extract action type and parameters
        string actionType = content.Substring(0, openParenIndex).Trim();
        string parametersContent = content.Substring(openParenIndex + 1, closeParenIndex - openParenIndex - 1).Trim();
        
        // Parse parameters to extract instance names
        var parameterInstances = ParseActionParameters(parametersContent);
        
        // Create a unique key: actionType_instance1_instance2_...
        string key = actionType;
        foreach (var instance in parameterInstances)
        {
            key += "_" + instance;
        }
        
        return key;
    }

    /// <summary>
    /// Parses a NodeGraph from a file and stores it in the blackboard
    /// Expected format: Nodegraph Name { ActionInstance: ... Relations: ... }
    /// </summary>
    /// <param name="nodeGraphFile">Path to the NodeGraph file</param>
    /// <returns>The created NodeGraph instance</returns>
    public NodeGraph ParseAndRegisterNodeGraph(string nodeGraphFile)
    {
        Console.WriteLine($"\n=== PARSING AND REGISTERING NODEGRAPH FROM FILE ===");
        Console.WriteLine($"📁 File: {nodeGraphFile}");
        
        try
        {
            if (!File.Exists(nodeGraphFile))
            {
                throw new FileNotFoundException($"NodeGraph file not found: {nodeGraphFile}");
            }
            
            // Read the file content
            string content = File.ReadAllText(nodeGraphFile);
            Console.WriteLine($"📄 File content length: {content.Length} characters");
            
            // Extract the NodeGraph name from the first line
            string nodeGraphName = ExtractNodeGraphName(content);
            Console.WriteLine($"🔍 Extracted NodeGraph name: {nodeGraphName}");
            
            // Use the existing Parser to create the NodeGraph
            var (actionInstances, relations) = Parser.ParsePlannerOutput(content);
            var nodeGraph = Parser.ParseNodeGraph(actionInstances, relations, blackboard);
            
            // Register the NodeGraph in the blackboard
            var fastNameKey = new FastName(nodeGraphName);
            blackboard.SetNodeGraph(fastNameKey, nodeGraph);
            
            Console.WriteLine($"✅ Successfully created and registered NodeGraph: {nodeGraphName}");
            Console.WriteLine($"📊 NodeGraph contains {nodeGraph.GetAllActionNodes().Count} action nodes");
            Console.WriteLine($"📊 Execution order: {string.Join(" → ", nodeGraph.GetExecutionOrder().Select(a => a.InstanceName.ToString()))}");
            
            return nodeGraph;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error parsing NodeGraph file {nodeGraphFile}: {ex.Message}");
            throw;
        }
    }
    
    /// <summary>
    /// Extracts the NodeGraph name from the file content
    /// Expected format: Nodegraph Name { ... }
    /// </summary>
    /// <param name="content">The file content</param>
    /// <returns>The NodeGraph name</returns>
    private string ExtractNodeGraphName(string content)
    {
        // Split into lines and find the first line that starts with "Nodegraph"
        string[] lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (string line in lines)
        {
            string trimmedLine = line.Trim();
            if (trimmedLine.StartsWith("Nodegraph", StringComparison.OrdinalIgnoreCase))
            {
                // Extract the name between "Nodegraph" and "{"
                int startIndex = trimmedLine.IndexOf(' ');
                if (startIndex != -1)
                {
                    int endIndex = trimmedLine.IndexOf('{');
                    if (endIndex != -1 && endIndex > startIndex)
                    {
                        return trimmedLine.Substring(startIndex + 1, endIndex - startIndex - 1).Trim();
                    }
                }
            }
        }
        
        // If no name found, generate a default name
        return "DefaultNodeGraph";
    }

    /// <summary>
    /// Parses action parameters to extract instance names
    /// Expected format: parameter1 : value1, parameter2 : value2, ...
    /// </summary>
    /// <param name="parametersContent">The parameters string to parse</param>
    /// <returns>List of instance names</returns>
    private List<string> ParseActionParameters(string parametersContent)
    {
        var instances = new List<string>();
        
        if (string.IsNullOrWhiteSpace(parametersContent))
        {
            return instances;
        }
        
        // Split by comma, but be careful about commas inside parentheses
        var parameterPairs = parametersContent.Split(',');
        
        foreach (var pair in parameterPairs)
        {
            var trimmedPair = pair.Trim();
            if (string.IsNullOrWhiteSpace(trimmedPair))
                continue;
                
            // Split by colon
            var colonIndex = trimmedPair.IndexOf(':');
            if (colonIndex == -1)
            {
                throw new ArgumentException($"Invalid parameter format: {trimmedPair}. Expected: parameter : value");
            }
            
            string paramValue = trimmedPair.Substring(colonIndex + 1).Trim();
            
            if (string.IsNullOrWhiteSpace(paramValue))
            {
                throw new ArgumentException($"Parameter value cannot be empty in: {trimmedPair}");
            }
            
            instances.Add(paramValue);
        }
        
        return instances;
    }


}