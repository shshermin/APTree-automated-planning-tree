﻿using BehaviorTreeMainProject.Log.Services;

using ModelLoader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using BehaviorTreeMainProject.Services.AIPlanning;


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
        LoggingService.LogInfo("Registering parameter types...");
        
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
                    LoggingService.LogInfo($"Processing parameter type: {fileName}");
                    
                    try
                    {
                        // Register the entity type
                        blackboard.RegisterEntityType(new FastName(fileName));
                        LoggingService.LogInfo($"Registered entity type: {fileName}");
                    }
                    catch (Exception ex)
                    {
                        LoggingService.LogInfo($"Error processing parameter type {fileName}: {ex.Message}");
                    }
                }
                
                LoggingService.LogInfo("Parameter types registration completed");
            }
            else
            {
                LoggingService.LogInfo($"Warning: ParameterTypes folder not found at {parameterTypesPath}");
            }
        }
        catch (Exception ex)
        {
            LoggingService.LogInfo($"Error registering parameter types: {ex.Message}");
        }
    }

    /// <summary>
    /// Registers all predicate types from the PredicateTypes folder into the blackboard
    /// </summary>
    public void RegisterPredicateTypes()
    {
        LoggingService.LogInfo("Registering predicate types...");
        
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
                    LoggingService.LogInfo($"Processing predicate type: {fileName}");
                    
                    try
                    {
                        // Register the predicate type
                        blackboard.RegisterPredicateType(new FastName(fileName));
                        LoggingService.LogInfo($"Registered predicate type: {fileName}");
                    }
                    catch (Exception ex)
                    {
                        LoggingService.LogInfo($"Error processing predicate type {fileName}: {ex.Message}");
                    }
                }
                
                LoggingService.LogInfo("Predicate types registration completed");
            }
            else
            {
                LoggingService.LogInfo($"Warning: PredicateTypes folder not found at {predicateTypesPath}");
            }
        }
        catch (Exception ex)
        {
            LoggingService.LogInfo($"Error registering predicate types: {ex.Message}");
        }
    }

    /// <summary>
    /// Registers all action types from the ActionTypes folder into the blackboard
    /// </summary>
    public void RegisterActionTypes()
    {
        LoggingService.LogInfo("Registering action types...");
        
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
                    LoggingService.LogInfo($"Processing action type: {fileName}");
                    
                    try
                    {
                        // Register the action type
                        blackboard.RegisterActionType(new FastName(fileName));
                        LoggingService.LogInfo($"Registered action type: {fileName}");
                    }
                    catch (Exception ex)
                    {
                        LoggingService.LogInfo($"Error processing action type {fileName}: {ex.Message}");
                    }
                }
                
                LoggingService.LogInfo("Action types registration completed");
            }
            else
            {
                LoggingService.LogInfo($"Warning: ActionTypes folder not found at {actionTypesPath}");
            }
        }
        catch (Exception ex)
        {
            LoggingService.LogInfo($"Error registering action types: {ex.Message}");
        }
    }

    /// <summary>
    /// Registers all types (parameter, predicate, and action types) into the blackboard
    /// </summary>
    public void RegisterAllTypes()
    {
        LoggingService.LogInfo("Starting registration of all types...");
        
        RegisterParameterTypes();
        RegisterPredicateTypes();
        RegisterActionTypes();
        
        LoggingService.LogInfo("All types registration completed");
    }

    /// <summary>
    /// Registers parameter instances from a MontiCore grammar file
    /// </summary>
    /// <param name="parameterInstancesFile">Path to the parameter instances file</param>
    public void RegisterParameterInstances(string parameterInstancesFile)
    {
        LoggingService.LogInfo("\n=== REGISTERING PARAMETER INSTANCES ===");
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
        LoggingService.LogInfo("\n=== REGISTERING PREDICATE INSTANCES ===");
        ParseAndRegisterMontiCorePredicateFile(predicateInstancesFile, blackboard);
    }

    /// <summary>
    /// Registers action instances from action definition strings
    /// </summary>
    /// <param name="actionDefinitionStrings">Array of action definition strings</param>
    public void RegisterActionInstances(string[] actionDefinitionStrings)
    {
        LoggingService.LogInfo("\n=== REGISTERING ACTION INSTANCES ===");
        CreateAndRegisterActionInstances(actionDefinitionStrings);
    }

    /// <summary>
    /// Registers action instances from a file
    /// </summary>
    /// <param name="actionInstancesFile">Path to the action instances file</param>
    public void RegisterActionInstancesFromFile(string actionInstancesFile)
    {
        LoggingService.LogInfo("\n=== REGISTERING ACTION INSTANCES FROM FILE ===");
        string[] actionDefinitionStrings = ReadActionDefinitionsFromFile(actionInstancesFile);
        RegisterActionInstances(actionDefinitionStrings);
    }

    /// <summary>
    /// Registers all instances using specified file paths
    /// </summary>
    /// <param name="parameterInstancesFile">Path to the setup objects JSON file</param>
    /// <param name="predicateInstancesFile">Path to the initial state predicates JSON file</param>
    /// <param name="actionInstancesFile">Path to the action instances file</param>
    public void RegisterAllInstances(string parameterInstancesFile, string predicateInstancesFile, string actionInstancesFile)
    {
        LoggingService.LogInfo("Starting registration of all instances...");
        
        RegisterParameterInstances(parameterInstancesFile);
        RegisterPredicateInstances(predicateInstancesFile);
        RegisterActionInstancesFromFile(actionInstancesFile);
        
        LoggingService.LogInfo("All instances registration completed");
    }

    /// <summary>
    /// Registers all instances using default file paths
    /// </summary>
    /// <param name="actionInstancesFile">Path to the action instances file</param>
    public void RegisterAllInstances(string actionInstancesFile)
    {
        // Use default file paths for parameters and predicates
        string parameterInstancesFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "src", "ModelLoader", "LiveMatSetupObjects.json");
        string predicateInstancesFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "src", "ModelLoader", "InitialStatePredicates.json");
        
        RegisterAllInstances(parameterInstancesFile, predicateInstancesFile, actionInstancesFile);
    }

    /// <summary>
    /// Creates and registers all instances using default file paths and action definitions
    /// </summary>
    /// <param name="actionDefinitionStrings">Array of action definition strings</param>
    public void CreateAndRegisterAllInstances(string parameterInstancesFile, string predicateInstancesFile, string[] actionDefinitionStrings)
    {
        LoggingService.LogInfo("Starting creation and registration of all instances...");
        
        RegisterParameterInstances(parameterInstancesFile);
         RegisterPredicateInstances(predicateInstancesFile);
        RegisterActionInstances(actionDefinitionStrings);
        
        LoggingService.LogInfo("All instances creation and registration completed");
    }

    /// <summary>
    /// Creates and registers all instances using default file paths and action definitions
    /// </summary>
    /// <param name="actionDefinitionStrings">Array of action definition strings</param>
    public void CreateAndRegisterAllInstances(string[] actionDefinitionStrings)
    {
        // Use default file paths
        string parameterInstancesFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "src", "ModelLoader", "LiveMatSetupObjects.json");
        string predicateInstancesFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "src", "ModelLoader", "InitialStatePredicates.json");
        
        CreateAndRegisterAllInstances(parameterInstancesFile, predicateInstancesFile, actionDefinitionStrings);
    }

    /// <summary>
    /// Creates and registers all instances using default file paths and action definitions from a file
    /// </summary>
    /// <param name="actionInstancesFile">Path to the action instances file</param>
    public void CreateAndRegisterAllInstancesFromFiles(string actionInstancesFile)
    {
        // Use default file paths for parameters and predicates
        string parameterInstancesFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "src", "ModelLoader", "LiveMatSetupObjects.json");
        string predicateInstancesFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "src", "ModelLoader", "InitialStatePredicates.json");
        
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
        LoggingService.LogInfo("Starting creation and registration of all instances with NodeGraph...");
        
        // Register parameters, predicates, and actions first
        RegisterParameterInstances(parameterInstancesFile);
        RegisterPredicateInstances(predicateInstancesFile);
        RegisterActionInstancesFromFile(actionInstancesFile);
        
        // Parse and register the NodeGraph
        ParseAndRegisterNodeGraph(nodeGraphFile);
        
        LoggingService.LogInfo("All instances and NodeGraph registration completed");
    }

    /// <summary>
    /// Creates and registers all instances including NodeGraphs using default file paths
    /// </summary>
    /// <param name="actionInstancesFile">Path to the action instances file</param>
    /// <param name="nodeGraphFile">Path to the NodeGraph file</param>
    public void CreateAndRegisterAllInstancesWithNodeGraph(string actionInstancesFile, string nodeGraphFile)
    {
        // Use default file paths for parameters and predicates
        string parameterInstancesFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "src", "ModelLoader", "LiveMatSetupObjects.json");
        string predicateInstancesFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "src", "ModelLoader", "InitialStatePredicates.json");
        
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
            LoggingService.LogInfo($"Reading action definitions from: {filePath}");
            
            if (!File.Exists(filePath))
            {
                LoggingService.LogInfo($"Warning: Action instances file not found at {filePath}");
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
            
            LoggingService.LogInfo($"Read {actionDefinitions.Count} action definitions from file");
        }
        catch (Exception ex)
        {
            LoggingService.LogInfo($"Error reading action instances file {filePath}: {ex.Message}");
        }
        
        return actionDefinitions.ToArray();
    }

    /// <summary>
    /// Takes a single parameter instance, determines its base type, and registers it in the appropriate blackboard dictionary
    /// </summary>
    /// <param name="parameterInstance">The parameter instance to register</param>
    public void RegisterParameterInstanceByBaseType(CustomProperty parameterInstance)
    {
        try
        {
            // Get the base type from the entity
            string baseTypeName = GetBaseTypeName(parameterInstance);
            
            LoggingService.LogInfo($"Registering {parameterInstance.GetType().Name} instance '{parameterInstance.ID}' as base type '{baseTypeName}'");
            
            // Register the instance in the appropriate blackboard dictionary based on base type
            switch (baseTypeName.ToLower())
            {
                case "element":
                    blackboard.RegisterEntityIfNotExists(parameterInstance);
                    LoggingService.LogInfo($"  ✅ Registered as Element: {parameterInstance.ID}");
                    break;
                    
                case "agent":
                    blackboard.RegisterEntityIfNotExists(parameterInstance);
                    LoggingService.LogInfo($"  ✅ Registered as Agent: {parameterInstance.ID}");
                    break;
                    
                case "location":
                    blackboard.RegisterEntityIfNotExists(parameterInstance);
                    LoggingService.LogInfo($"  ✅ Registered as Location: {parameterInstance.ID}");
                    break;
                    
                case "tool":
                    blackboard.RegisterEntityIfNotExists(parameterInstance);
                    LoggingService.LogInfo($"  ✅ Registered as Tool: {parameterInstance.ID}");
                    break;
                    
                case "layer":
                    blackboard.RegisterEntityIfNotExists(parameterInstance);
                    LoggingService.LogInfo($"  ✅ Registered as Layer: {parameterInstance.ID}");
                    break;
                    
                case "module":
                    blackboard.RegisterEntityIfNotExists(parameterInstance);
                    LoggingService.LogInfo($"  ✅ Registered as Module: {parameterInstance.ID}");
                    break;
                    
                default:
                    LoggingService.LogInfo($"  ⚠️ Unknown base type '{baseTypeName}' for instance '{parameterInstance.ID}', registering as generic CustomProperty");
                    blackboard.RegisterEntityIfNotExists(parameterInstance);
                    break;
            }
        }
        catch (Exception ex)
        {
            LoggingService.LogInfo($"  ❌ Error registering instance '{parameterInstance.ID}': {ex.Message}");
        }
    }

    /// <summary>
    /// Determines the base type name of an entity instance
    /// </summary>
    /// <param name="instance">The entity instance</param>
    /// <returns>The base type name (e.g., "Element", "Agent", "Location")</returns>
    private string GetBaseTypeName(CustomProperty instance)
    {
        // Get the base type from the entity's BaseType property
        if (instance.BaseType != null)
        {
            return instance.BaseType.ToString();
        }
        
        // Fallback: determine base type from inheritance hierarchy
        Type currentType = instance.GetType();
        
        // Check inheritance hierarchy to find the base type
        while (currentType != null && currentType != typeof(CustomProperty))
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
    /// Parses a JSON file generated by GeneratorJsonSetupObjects and creates parameter instances.
    /// Expected JSON format: {"instances": [{"type": "TypeName", "name": "instanceName", "extends": "BaseType"}, ...], "count": N}
    /// </summary>
    /// <param name="filePath">Path to the LiveMatSetupObjects.json file</param>
    /// <returns>List of created parameter instances</returns>
    public List<CustomProperty> ParseMontiCoreGrammarFile(string filePath)
    {
        List<CustomProperty> createdInstances = new List<CustomProperty>();
        
        try
        {
            LoggingService.LogInfo($"Parsing parameter instances JSON file: {filePath}");
            
            if (!File.Exists(filePath))
            {
                LoggingService.LogInfo($"❌ Error: File not found at {filePath}");
                return createdInstances;
            }
            
            string jsonContent = File.ReadAllText(filePath);
            using JsonDocument doc = JsonDocument.Parse(jsonContent);
            JsonElement root = doc.RootElement;
            
            if (!root.TryGetProperty("instances", out JsonElement instancesArray))
            {
                LoggingService.LogInfo($"❌ Error: JSON file does not contain an 'instances' array");
                return createdInstances;
            }
            
            int totalCount = instancesArray.GetArrayLength();
            int successCount = 0;
            int errorCount = 0;
            
            LoggingService.LogInfo($"📄 Found {totalCount} parameter entries in JSON");
            
            int index = 0;
            foreach (JsonElement instanceElement in instancesArray.EnumerateArray())
            {
                index++;
                
                try
                {
                    string typeName = instanceElement.GetProperty("type").GetString();
                    string instanceName = instanceElement.GetProperty("name").GetString();
                    string extendsType = instanceElement.TryGetProperty("extends", out JsonElement extendsElement) 
                        ? extendsElement.GetString() : "";
                    
                    LoggingService.LogInfo($"\n🔍 Entry {index}: {typeName} '{instanceName}' (extends {extendsType})");
                    
                    // Create the parameter instance using the factory
                    var instance = entityFactory.CreateParameter(typeName, instanceName);
                    
                    if (instance != null)
                    {
                        // Read "properties" dict and pass to SetParameters if present
                        if (instanceElement.TryGetProperty("properties", out JsonElement propsElement)
                            && propsElement.ValueKind == JsonValueKind.Object)
                        {
                            var paramDict = new Dictionary<string, object>();
                            foreach (var prop in propsElement.EnumerateObject())
                            {
                                paramDict[prop.Name] = prop.Value.ToString();
                            }
                            instance.SetParameters(paramDict);
                            LoggingService.LogInfo($"  📦 Entry {index}: Applied {paramDict.Count} properties to '{instanceName}'");
                        }

                        createdInstances.Add(instance);
                        successCount++;
                        LoggingService.LogInfo($"  ✅ Entry {index}: Created {instance.GetType().Name} instance '{instance.ID}'");
                    }
                }
                catch (Exception ex)
                {
                    errorCount++;
                    LoggingService.LogInfo($"  ❌ Entry {index}: Error: {ex.Message}");
                }
            }
            
            LoggingService.LogInfo($"\n📊 Parsing Summary:");
            LoggingService.LogInfo($"  ✅ Successfully created: {successCount} instances");
            LoggingService.LogInfo($"  ❌ Errors: {errorCount}");
            LoggingService.LogInfo($"  📄 Total entries processed: {index}");
            
        }
        catch (Exception ex)
        {
            LoggingService.LogInfo($"❌ Error reading file {filePath}: {ex.Message}");
        }
        
        return createdInstances;
    }
    
    /// <summary>
    /// Parses a MontiCore grammar text file and registers all parameter instances in the blackboard
    /// </summary>
    /// <param name="filePath">Path to the MontiCore grammar text file</param>
    public void ParseAndRegisterMontiCoreGrammarFile(string filePath)
    {
        LoggingService.LogInfo($"\n=== PARSING AND REGISTERING MONTICORE GRAMMAR FILE ===");
        
        var instances = ParseMontiCoreGrammarFile(filePath);
        
        LoggingService.LogInfo($"\n=== REGISTERING {instances.Count} INSTANCES IN BLACKBOARD ===");
        
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
                LoggingService.LogInfo($"  ❌ Error registering instance '{instance.ID}': {ex.Message}");
                skippedCount++;
            }
        }
        
        LoggingService.LogInfo($"\n📊 Registration Summary:");
        LoggingService.LogInfo($"  ✅ Successfully registered: {registeredCount} instances");
        LoggingService.LogInfo($"  ⚠️ Skipped: {skippedCount} instances");
    }

    /// <summary>
    /// Parses a JSON file generated by the DSL's InitialStateJsonGenerator and creates predicate instances.
    /// Expected JSON format: {"predicates": [{"type": "PredicateName", "properties": {"param": "value", ...}, "not": false}, ...], "count": N}
    /// </summary>
    /// <param name="filePath">Path to the InitialStatePredicates.json file</param>
    /// <param name="blackboard">The blackboard to get entities from</param>
    /// <returns>List of created predicate instances</returns>
    public  List<Predicate> ParseMontiCorePredicateFile(string filePath, Blackboard<FastName> blackboard)
    {
        List<Predicate> createdInstances = new List<Predicate>();
        
        try
        {
            LoggingService.LogInfo($"Parsing predicate JSON file: {filePath}");
            
            if (!File.Exists(filePath))
            {
                LoggingService.LogInfo($"❌ Error: File not found at {filePath}");
                return createdInstances;
            }
            
            string jsonContent = File.ReadAllText(filePath);
            using JsonDocument doc = JsonDocument.Parse(jsonContent);
            JsonElement root = doc.RootElement;
            
            if (!root.TryGetProperty("predicates", out JsonElement predicatesArray))
            {
                LoggingService.LogInfo($"❌ Error: JSON file does not contain a 'predicates' array");
                return createdInstances;
            }
            
            int totalCount = predicatesArray.GetArrayLength();
            int successCount = 0;
            int errorCount = 0;
            
            LoggingService.LogInfo($"📄 Found {totalCount} predicate entries in JSON");
            
            int index = 0;
            foreach (JsonElement predicateElement in predicatesArray.EnumerateArray())
            {
                index++;
                
                try
                {
                    // Extract predicate type name
                    string predicateName = predicateElement.GetProperty("type").GetString();
                    LoggingService.LogInfo($"\n🔍 Entry {index}: Processing predicate type '{predicateName}'");
                    
                    // Build parameter mappings from the "properties" object
                    var parameterMappings = new List<ParameterMapping>();
                    
                    if (predicateElement.TryGetProperty("properties", out JsonElement propertiesElement))
                    {
                        foreach (JsonProperty prop in propertiesElement.EnumerateObject())
                        {
                            string paramName = prop.Name;
                            if (paramName == "not") continue; // handled separately below
                            string paramValue = prop.Value.ToString();
                            parameterMappings.Add(new ParameterMapping(paramName, paramValue));
                            LoggingService.LogInfo($"  📝 Property: {paramName} = {paramValue}");
                        }
                    }
                    
                    // Extract the "not" (isNegated) property — check both top-level and inside "properties"
                    bool isNegated = false;
                    if (predicateElement.TryGetProperty("not", out JsonElement notElement))
                    {
                        isNegated = notElement.GetBoolean();
                    }
                    else if (predicateElement.TryGetProperty("properties", out JsonElement propsForNot)
                             && propsForNot.TryGetProperty("not", out JsonElement notInProps))
                    {
                        isNegated = notInProps.GetBoolean();
                    }
                    parameterMappings.Add(new ParameterMapping("isNegated", isNegated.ToString().ToLower()));
                    LoggingService.LogInfo($"  📝 isNegated: {isNegated}");
                    
                    // Create the predicate instance using the factory
                    Predicate instance = predicateFactory.CreatePredicateInstance(predicateName, parameterMappings, blackboard);
                    
                    if (instance != null)
                    {
                        createdInstances.Add(instance);
                        successCount++;
                        LoggingService.LogInfo($"  ✅ Entry {index}: Created {instance.GetType().Name} instance '{instance.PredicateName}'");
                        
                        // Verify the instance was actually registered
                        var allPredicates = blackboard.GetAllPredicates();
                        var foundInBlackboard = allPredicates.Any(p => p.PredicateName == instance.PredicateName);
                        LoggingService.LogInfo($"  🔍 Entry {index}: Predicate in blackboard: {foundInBlackboard}");
                        
                        if (!foundInBlackboard)
                        {
                            LoggingService.LogInfo($"  ⚠️ WARNING: Predicate {instance.PredicateName} was created but not found in blackboard!");
                        }
                    }
                    else
                    {
                        LoggingService.LogInfo($"  ❌ Entry {index}: Factory returned null");
                        errorCount++;
                    }
                }
                catch (Exception ex)
                {
                    errorCount++;
                    LoggingService.LogInfo($"  ❌ Entry {index}: Error: {ex.Message}");
                    LoggingService.LogInfo($"  📋 Exception details: {ex}");
                }
            }
            
            LoggingService.LogInfo($"\n📊 Parsing Summary:");
            LoggingService.LogInfo($"  ✅ Successfully created: {successCount} instances");
            LoggingService.LogInfo($"  ❌ Errors: {errorCount}");
            LoggingService.LogInfo($"  📄 Total entries processed: {index}");
            
            // Final verification
            var finalPredicates = blackboard.GetAllPredicates();
            LoggingService.LogInfo($"  📊 Final predicate count in blackboard: {finalPredicates.Count}");
            
        }
        catch (Exception ex)
        {
            LoggingService.LogInfo($"❌ Error reading file {filePath}: {ex.Message}");
            LoggingService.LogInfo($"📋 Exception details: {ex}");
        }
        
        return createdInstances;
    }
    
    /// <summary>
    /// Parses a MontiCore predicate grammar text file and registers all predicate instances in the blackboard
    /// </summary>
    /// <param name="filePath">Path to the MontiCore predicate grammar text file</param>
    /// <param name="blackboard">The blackboard to register predicates in</param>
    public void ParseAndRegisterMontiCorePredicateFile(string filePath, Blackboard<FastName> blackboard)
    {
        LoggingService.LogInfo($"\n=== PARSING AND REGISTERING MONTICORE PREDICATE FILE ===");
        LoggingService.LogInfo($"📁 File: {filePath}");
        
        List<Predicate> instances = ParseMontiCorePredicateFile(filePath, blackboard);
        
        LoggingService.LogInfo($"\n=== PREDICATE INSTANCES CREATED AND REGISTERED ===");
        LoggingService.LogInfo($"  ✅ Successfully created: {instances.Count} predicates");
        
        // Verify predicates are actually in blackboard
        var allPredicates = blackboard.GetAllPredicates();
        LoggingService.LogInfo($"  📊 Total predicates in blackboard: {allPredicates.Count}");
        
        if (allPredicates.Count > 0)
        {
            LoggingService.LogInfo($"  📋 Predicates in blackboard:");
            foreach (var pred in allPredicates)
            {
                LoggingService.LogInfo($"    - {pred.PredicateName} ({pred.GetType().Name}) - isNegated: {pred.not}");
            }
        }
        else
        {
            LoggingService.LogInfo($"  ⚠️ WARNING: No predicates found in blackboard after registration!");
        }
        
        LoggingService.LogInfo($"  📝 Note: Predicates are automatically registered by the factory");
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
        
        LoggingService.LogInfo($"\n=== CREATING AND REGISTERING {actionDefinitionStrings.Length} ACTION INSTANCES ===");
        
        int successCount = 0;
        int errorCount = 0;
        
        foreach (string actionDefinition in actionDefinitionStrings)
        {
            try
            {
                LoggingService.LogInfo($"\n🔧 Processing action definition: {actionDefinition}");
                
                // Create the action instance using FactoryAction
                var actionInstance = actionFactory.CreateActionInstance(actionDefinition, blackboard);
                
                if (actionInstance != null)
                {
                    // Generate the base key from the MontiCore definition
                    string baseKey = GenerateActionInstanceKey(actionDefinition);
                    
                    // Global dedup: use ActionInstanceCounts on the blackboard
                    // to assign a globally unique _dup suffix across all cassettes.
                    string uniqueKey;
                    if (blackboard.ActionInstanceCounts.TryGetValue(baseKey, out int count))
                    {
                        // Already seen this base key — increment and append _dup
                        blackboard.ActionInstanceCounts[baseKey] = count + 1;
                        uniqueKey = $"{baseKey}_dup{count + 1}";
                        LoggingService.LogInfo($"  🔄 Global dedup: {baseKey} seen {count} time(s) before -> {uniqueKey}");
                    }
                    else
                    {
                        // First time — no suffix needed
                        blackboard.ActionInstanceCounts[baseKey] = 1;
                        uniqueKey = baseKey;
                    }
                    
                    // Update the action's instance name if it was disambiguated
                    if (uniqueKey != baseKey)
                    {
                        actionInstance.UpdateInstanceName(uniqueKey);
                    }
                    
                    var fastNameKey = new FastName(uniqueKey);
                    
                    // Register the action instance on the blackboard
                    blackboard.SetActionType(fastNameKey, actionInstance);
                    
                    createdActions.Add(actionInstance);
                    successCount++;
                    
                    LoggingService.LogInfo($"  ✅ Successfully created and registered action: {actionInstance.GetType().Name}");
                    LoggingService.LogInfo($"  🔑 Registered with key: {uniqueKey}");
                    LoggingService.LogInfo($"  📝 Debug Display Name: {actionInstance.DebugDisplayName}");
                }
                else
                {
                    errorCount++;
                    LoggingService.LogInfo($"  ❌ Failed to create action instance for: {actionDefinition}");
                }
            }
            catch (Exception ex)
            {
                errorCount++;
                LoggingService.LogInfo($"  ❌ Error processing action definition '{actionDefinition}': {ex.Message}");
            }
        }
        
        LoggingService.LogInfo($"\n📊 Action Instance Creation Summary:");
        LoggingService.LogInfo($"  ✅ Successfully created and registered: {successCount} actions");
        LoggingService.LogInfo($"  ❌ Errors: {errorCount}");
        LoggingService.LogInfo($"  📄 Total definitions processed: {actionDefinitionStrings.Length}");
        
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
        LoggingService.LogInfo($"\n=== PARSING AND REGISTERING NODEGRAPH FROM FILE ===");
        LoggingService.LogInfo($"📁 File: {nodeGraphFile}");
        
        try
        {
            if (!File.Exists(nodeGraphFile))
            {
                throw new FileNotFoundException($"NodeGraph file not found: {nodeGraphFile}");
            }
            
            // Read the file content
            string content = File.ReadAllText(nodeGraphFile);
            LoggingService.LogInfo($"📄 File content length: {content.Length} characters");
            
            // Extract the NodeGraph name from the first line
            string nodeGraphName = ExtractNodeGraphName(content);
            LoggingService.LogInfo($"🔍 Extracted NodeGraph name: {nodeGraphName}");
            
            // Use the existing Parser to create the NodeGraph
            var (actionInstances, relations) = ServicePDDLPlanning.ParsePlannerOutput(content);
            var nodeGraph = ServicePDDLPlanning.ParseNodeGraph(actionInstances, relations, blackboard);
            
            // Register the NodeGraph in the blackboard
            var fastNameKey = new FastName(nodeGraphName);
            blackboard.SetNodeGraph(fastNameKey, nodeGraph);
            
            LoggingService.LogInfo($"✅ Successfully created and registered NodeGraph: {nodeGraphName}");
            LoggingService.LogInfo($"📊 NodeGraph contains {nodeGraph.GetAllActionNodes().Count} action nodes");
            LoggingService.LogInfo($"📊 Execution order: {string.Join(" → ", nodeGraph.GetExecutionOrder().Select(a => a.InstanceName.ToString()))}");
            
            return nodeGraph;
        }
        catch (Exception ex)
        {
            LoggingService.LogInfo($"❌ Error parsing NodeGraph file {nodeGraphFile}: {ex.Message}");
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