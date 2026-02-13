using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using BehaviorTreeMainProject.Services;
using BehaviorTreeMainProject.Log.Services;

public static class Parser
{
    /// <summary>
    /// Parses a NodeGraph from a list of action instance strings and relation strings
    /// </summary>
    /// <param name="actionInstanceStrings">List of action instance strings in MontiCore format</param>
    /// <param name="relationStrings">List of relation strings in the format "source --[TemporalType]--> target"</param>
    /// <param name="blackboard">The blackboard containing parameter instances</param>
    /// <returns>A populated NodeGraph instance</returns>
    public static NodeGraph ParseNodeGraph(List<string> actionInstanceStrings, List<string> relationStrings, Blackboard<FastName> blackboard)
    {
        LoggingService.LogInfo($"🔧 Parser: ParseNodeGraph called with {actionInstanceStrings?.Count ?? 0} actions and {relationStrings?.Count ?? 0} relations");
        
        var nodeGraph = new NodeGraph();
        var actionInstances = new Dictionary<string, PActionNode>();
        var blackboardWriter = new BlackboardWriter(blackboard);
        
        if (actionInstanceStrings == null || actionInstanceStrings.Count == 0)
        {
            LoggingService.LogError($"❌ Parser: No action instances provided");
            return nodeGraph;
        }
        
        // Step 1: Convert action instances to MontiCore format
        LoggingService.LogInfo($"🔧 Parser: Step 1 - Converting {actionInstanceStrings.Count} action instances to MontiCore format");
        var montiCoreActionStrings = ConvertToMontiCoreFormat(actionInstanceStrings);
        
        // Step 2: Create action instances
        LoggingService.LogInfo($"🔧 Parser: Step 2 - Creating action instances from {montiCoreActionStrings.Count} MontiCore action strings");
        
        foreach (var actionString in montiCoreActionStrings)
        {
            LoggingService.LogInfo($"🔧 Parser: Processing action string: {actionString}");
            
            try
            {
                // Use BlackboardWriter to create and register the action
                LoggingService.LogInfo($"🔧 Parser: Calling BlackboardWriter.CreateAndRegisterActionInstance...");
                var actionInstance = blackboardWriter.CreateAndRegisterActionInstance(actionString);
                LoggingService.LogInfo($"🔍 Parser: Action created: {actionInstance?.InstanceName.ToString() ?? "NULL"}");
                
                if (actionInstance != null)
                {
                    string actionKey = GetActionInstanceName(actionString);
                    actionInstances[actionKey] = actionInstance;
                    LoggingService.LogSuccess($"✅ Parser: Created action instance: {actionKey} -> {actionInstance.InstanceName.ToString()}");
                }
                else
                {
                    LoggingService.LogError($"❌ Parser: Failed to create action instance from: {actionString}");
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"❌ Parser: Exception creating action instance from '{actionString}': {ex.Message}");
            }
        }
        
        LoggingService.LogSuccess($"✅ Parser: Created {actionInstances.Count} action instances");
        
        // Step 3: Add all actions to the NodeGraph
        LoggingService.LogInfo($"🔧 Parser: Step 3 - Adding {actionInstances.Count} actions to NodeGraph");
        foreach (var kvp in actionInstances)
        {
            LoggingService.LogInfo($"🔧 Parser: Adding action to NodeGraph: {kvp.Key} -> {kvp.Value.InstanceName.ToString()}");
            nodeGraph.AddNode(kvp.Value);
        }
        
        LoggingService.LogSuccess($"✅ Parser: Added {actionInstances.Count} actions to NodeGraph");
        
        // Step 4: Create relations
        if (relationStrings != null && relationStrings.Count > 0)
        {
            LoggingService.LogInfo($"🔧 Parser: Step 4 - Creating {relationStrings.Count} relations");
            
            // Log all available action instances for relation parsing
            LoggingService.LogInfo($"🔍 Parser: Available action instances for relation parsing:");
            foreach (var kvp in actionInstances)
            {
                LoggingService.LogInfo($"   - {kvp.Key} -> {kvp.Value.InstanceName.ToString()}");
            }
            
            foreach (var relationString in relationStrings)
            {
                LoggingService.LogInfo($"🔧 Parser: Processing relation: {relationString}");
                try
                {
                    ParseRelation(relationString, actionInstances, nodeGraph);
                }
                catch (Exception ex)
                {
                    LoggingService.LogError($"❌ Parser: Exception processing relation '{relationString}': {ex.Message}");
                }
            }
            
            LoggingService.LogSuccess($"✅ Parser: Processed {relationStrings.Count} relations");
        }
        else
        {
            LoggingService.LogWarning($"⚠️ Parser: No relations provided, NodeGraph will have no dependencies");
        }
        
        LoggingService.LogSuccess($"✅ Parser: Successfully created NodeGraph with {nodeGraph.GetAllActionNodes().Count} nodes");
        return nodeGraph;
    }

    /// <summary>
    /// Parses planner output string and extracts action instances and relations into separate lists
    /// </summary>
    /// <param name="plannerOutput">Raw planner output string containing both actions and relations</param>
    /// <returns>Tuple containing list of action instances and list of relations</returns>
    public static (List<string> ActionInstances, List<string> Relations) ParsePlannerOutput(string plannerOutput)
    {
        LoggingService.LogInfo($"🔧 Parser: ParsePlannerOutput called");
        LoggingService.LogInfo($"🔧 Parser: Planner output length: {plannerOutput?.Length ?? 0}");
        
        var actionInstances = new List<string>();
        var relations = new List<string>();
        
        if (string.IsNullOrEmpty(plannerOutput))
        {
            LoggingService.LogError($"❌ Parser: Planner output is null or empty");
            return (actionInstances, relations);
        }
        
        // Split the planner output into lines
        string[] lines = plannerOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        LoggingService.LogInfo($"🔧 Parser: Processing {lines.Length} lines from planner output");
        
        foreach (string line in lines)
        {
            string trimmedLine = line.Trim();
            if (string.IsNullOrEmpty(trimmedLine))
                continue;
                
            LoggingService.LogInfo($"🔧 Parser: Processing line: {trimmedLine}");
            
            // Check if this is a relation line FIRST (contains "--[")
            // This must be checked before action instances to avoid conflicts
            if (trimmedLine.Contains("--["))
            {
                relations.Add(trimmedLine);
                LoggingService.LogInfo($"🔧 Parser: Added relation: {trimmedLine}");
            }
            // Check if this is an action instance line
            else if (trimmedLine.StartsWith("ActionInstance:"))
            {
                actionInstances.Add(trimmedLine);
                LoggingService.LogInfo($"🔧 Parser: Added action instance: {trimmedLine}");
            }
            else
            {
                LoggingService.LogWarning($"⚠️ Parser: Ignoring unrecognized line: {trimmedLine}");
            }
        }
        
        LoggingService.LogSuccess($"✅ Parser: Extracted {actionInstances.Count} action instances and {relations.Count} relations");
        return (actionInstances, relations);
    }

    /// <summary>
    /// Extracts the instance name from an ActionInstance definition
    /// </summary>
    private static string GetActionInstanceName(string actionInstanceLine)
    {
        // Return the full MontiCore format string as the key
        LoggingService.LogInfo($"🔧 Parser: GetActionInstanceName called with: {actionInstanceLine}");
        string fullActionName = actionInstanceLine;
        LoggingService.LogInfo($"🔧 Parser: GetActionInstanceName returning: {fullActionName}");
        
        return fullActionName;
    }

    /// <summary>
    /// Parses a relation definition like "source --[MEETS]--> target"
    /// Updated to handle simplified action names (without ActionInstance: prefix)
    /// </summary>
    private static void ParseRelation(string relationLine, Dictionary<string, PActionNode> actionInstances, NodeGraph nodeGraph)
    {
        LoggingService.LogInfo($"🔧 Parser: ParseRelation called with line: {relationLine}");
        
        // Expected format: sourceAction --[CONSTRAINT]--> targetAction
        // Example: PickUpHL_lp1_fp1_r1 --[MEETS]--> PlaceHL_lp1_pr1_r1 (simplified format)
        // OR: ActionInstance: PickUpHL_lp1_fp1_r1 --[MEETS]--> ActionInstance: PlaceHL_lp1_pr1_r1 (full format)
        
        // Find the arrow pattern "--[CONSTRAINT]-->"
        int arrowStart = relationLine.IndexOf("--[");
        if (arrowStart == -1)
        {
            LoggingService.LogError($"❌ Parser: No arrow pattern '--[' found in relation: {relationLine}");
            return;
        }
        
        int arrowEnd = relationLine.IndexOf("]-->", arrowStart);
        if (arrowEnd == -1)
        {
            LoggingService.LogError($"❌ Parser: No closing arrow pattern ']-->' found in relation: {relationLine}");
            return;
        }
        
        // Extract source action name
        string sourceActionName = relationLine.Substring(0, arrowStart).Trim();
        
        // Extract temporal constraint
        string constraintStr = relationLine.Substring(arrowStart + 3, arrowEnd - arrowStart - 3).Trim();
        TemporalConstraint temporalConstraint = ParseTemporalConstraint(constraintStr);
        
        // Extract target action name
        string targetActionName = relationLine.Substring(arrowEnd + 4).Trim();
        
        LoggingService.LogInfo($"🔧 Parser: Parsed relation - Source: '{sourceActionName}' -> Target: '{targetActionName}' [Constraint: {temporalConstraint}]");
        
        // Find the action instances by matching simplified names to full action instance names
        var sourceAction = FindActionInstanceBySimplifiedName(sourceActionName, actionInstances);
        if (sourceAction == null)
        {
            LoggingService.LogError($"❌ Parser: Source action not found: {sourceActionName}");
            LoggingService.LogInfo($"🔍 Parser: Available action instances:");
            foreach (var kvp in actionInstances)
            {
                LoggingService.LogInfo($"   - {kvp.Key} -> {kvp.Value.InstanceName.ToString()}");
            }
            return;
        }
        
        var targetAction = FindActionInstanceBySimplifiedName(targetActionName, actionInstances);
        if (targetAction == null)
        {
            LoggingService.LogError($"❌ Parser: Target action not found: {targetActionName}");
            LoggingService.LogInfo($"🔍 Parser: Available action instances:");
            foreach (var kvp in actionInstances)
            {
                LoggingService.LogInfo($"   - {kvp.Key} -> {kvp.Value.InstanceName.ToString()}");
            }
            return;
        }
        
        LoggingService.LogInfo($"🔧 Parser: Found action instances:");
        LoggingService.LogInfo($"   Source: {sourceAction.InstanceName.ToString()} (type: {sourceAction.GetType().Name})");
        LoggingService.LogInfo($"   Target: {targetAction.InstanceName.ToString()} (type: {targetAction.GetType().Name})");
        
        // Check for self-reference before adding
        if (sourceAction == targetAction)
        {
            LoggingService.LogError($"❌ Parser: SELF-REFERENCE DETECTED! {sourceAction.InstanceName.ToString()} is trying to relate to itself");
            LoggingService.LogError($"❌ Parser: This will create a circular dependency!");
            return;
        }
        
        // Add the relation to the NodeGraph
        LoggingService.LogInfo($"🔧 Parser: Adding order relation: {sourceAction.InstanceName.ToString()} → {targetAction.InstanceName.ToString()}");
        nodeGraph.AddOrderRelation(sourceAction, targetAction);
        
        LoggingService.LogInfo($"🔧 Parser: Adding temporal constraint: {sourceAction.InstanceName.ToString()} {temporalConstraint} {targetAction.InstanceName.ToString()}");
        nodeGraph.AddTemporalConstraint(sourceAction, targetAction, temporalConstraint);
        
        LoggingService.LogSuccess($"✅ Parser: Successfully added relation: {sourceAction.InstanceName.ToString()} -> {targetAction.InstanceName.ToString()} [{temporalConstraint}]");
    }

    /// <summary>
    /// Finds an action instance by simplified name using the InstanceName property
    /// </summary>
    /// <param name="simplifiedName">Simplified action name (e.g., "PickUpHL_lp1_fp1_r1")</param>
    /// <param name="actionInstances">Dictionary of action instances</param>
    /// <returns>The matching action instance or null if not found</returns>
    private static PActionNode FindActionInstanceBySimplifiedName(string simplifiedName, Dictionary<string, PActionNode> actionInstances)
    {
        LoggingService.LogInfo($"🔧 Parser: FindActionInstanceBySimplifiedName called with: {simplifiedName}");
        
        // Search through all action instances and match by InstanceName property
        foreach (var kvp in actionInstances)
        {
            var actionInstance = kvp.Value;
            string instanceName = actionInstance.InstanceName.ToString();
            
            if (instanceName == simplifiedName)
            {
                LoggingService.LogInfo($"🔧 Parser: Found exact match: {simplifiedName}");
                return actionInstance;
            }
        }
        
        // Try case-insensitive matching as fallback
        foreach (var kvp in actionInstances)
        {
            var actionInstance = kvp.Value;
            string instanceName = actionInstance.InstanceName.ToString();
            
            if (string.Equals(instanceName, simplifiedName, StringComparison.OrdinalIgnoreCase))
            {
                LoggingService.LogInfo($"🔧 Parser: Found case-insensitive match: {simplifiedName} -> {instanceName}");
                return actionInstance;
            }
        }
        
        LoggingService.LogError($"❌ Parser: No action instance found for simplified name: {simplifiedName}");
        return null;
    }
    
    public static string ConvertMultiplePredicatesToPDDL(List<Predicate> predicates)
    {
        var pddlPredicates = new List<string>();
        foreach (var predicate in predicates)
        {
            var pddlPredicate = ConvertPredicateToPDDL(predicate);
            if (!string.IsNullOrEmpty(pddlPredicate))
            {
                pddlPredicates.Add(pddlPredicate);
            }
        }
        return string.Join("\n", pddlPredicates);
    }

    /// <summary>
    /// Converts a single predicate to PDDL format
    /// </summary>
    /// <param name="predicate">The predicate to convert</param>
    /// <returns>PDDL formatted predicate string</returns>
    private static string ConvertPredicateToPDDL(Predicate predicate)
    {
        try
        {
            if (predicate == null)
                return string.Empty;

            // Get the predicate type name (not the unique key)
            string predicateName = predicate.PredicateTypeName;
            
            // Use the GetParameterValues method to get clean parameter values in correct order
            var parameterValues = predicate.GetParameterValues();

            // Create PDDL format
            string pddlFormat = $"({predicateName} {string.Join(" ", parameterValues)})";
            
            // Handle negation
            if (predicate.not)
            {
                pddlFormat = $"(not {pddlFormat})";
            }

            return pddlFormat;
        }
        catch (Exception ex)
        {
            LoggingService.LogError($"❌ Parser: Error converting predicate to PDDL: {ex.Message}");
            return string.Empty;
        }
    }

    private static TemporalConstraint ParseTemporalConstraint(string constraintStr)
    {
        // Convert temporal constraint string to enum
        if (Enum.TryParse<TemporalConstraint>(constraintStr, true, out var temporalConstraint))
        {
            return temporalConstraint;
        }
        
        // Handle common variations
        switch (constraintStr.ToUpper())
        {
            case "PRECEDES":
            case "BEFORE":
                return TemporalConstraint.PRECEDES;
            case "MEETS":
            case "SEQUENTIAL":
                return TemporalConstraint.MEETS;
            case "OVERLAPS":
            case "PARALLEL":
                return TemporalConstraint.OVERLAPS;
            case "STARTS":
                return TemporalConstraint.STARTS;
            case "FINISHES":
                return TemporalConstraint.FINISHES;
            case "CONTAINS":
                return TemporalConstraint.CONTAINS;
            case "EQUALS":
                return TemporalConstraint.EQUALS;
            default:
                LoggingService.LogWarning($"⚠️ Parser: Unknown temporal constraint '{constraintStr}', defaulting to MEETS");
                return TemporalConstraint.MEETS;
        }
    }

    /// <summary>
    /// Converts action instances from planner format to MontiCore format
    /// </summary>
    /// <param name="actionInstanceStrings">List of action instance strings in planner format</param>
    /// <returns>List of action instance strings in MontiCore format</returns>
    public static List<string> ConvertToMontiCoreFormat(List<string> actionInstanceStrings)
    {
        LoggingService.LogInfo($"🔧 Parser: ConvertToMontiCoreFormat called with {actionInstanceStrings?.Count ?? 0} action instances");
        
        var montiCoreActions = new List<string>();
        
        if (actionInstanceStrings == null || actionInstanceStrings.Count == 0)
        {
            LoggingService.LogWarning($"⚠️ Parser: No action instances to convert");
            return montiCoreActions;
        }
        
        foreach (var actionString in actionInstanceStrings)
        {
            try
            {
                string montiCoreAction = ConvertSingleActionToMontiCore(actionString);
                montiCoreActions.Add(montiCoreAction);
                LoggingService.LogInfo($"🔧 Parser: Converted: {actionString} -> {montiCoreAction}");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"❌ Parser: Error converting action '{actionString}': {ex.Message}");
                // Keep the original format if conversion fails
                montiCoreActions.Add(actionString);
            }
        }
        
        LoggingService.LogSuccess($"✅ Parser: Successfully converted {montiCoreActions.Count} action instances to MontiCore format");
        return montiCoreActions;
    }

    /// <summary>
    /// Converts a single action instance from planner format to MontiCore format
    /// </summary>
    /// <param name="actionString">Action string in format "ActionInstance: ActionType_param1_param2_param3"</param>
    /// <returns>Action string in MontiCore format "ActionInstance: ActionType(paramName1 : value1, paramName2 : value2, ...)"</returns>
    private static string ConvertSingleActionToMontiCore(string actionString)
    {
        LoggingService.LogInfo($"🔧 Parser: ConvertSingleActionToMontiCore called with: {actionString}");
        
        // Remove "ActionInstance: " prefix
        if (!actionString.StartsWith("ActionInstance:"))
        {
            LoggingService.LogError($"❌ Parser: Action string doesn't start with 'ActionInstance:': {actionString}");
            return actionString;
        }
        
        string actionPart = actionString.Substring("ActionInstance:".Length).Trim();
        
        // Split by underscore to get action type and parameters
        string[] parts = actionPart.Split('_');
        if (parts.Length < 1)
        {
            LoggingService.LogError($"❌ Parser: Invalid action format (no parts after split): {actionString}");
            return actionString;
        }
        
        string actionType = parts[0];
        string[] parameters = parts.Skip(1).ToArray();
        
        LoggingService.LogInfo($"🔧 Parser: Parsed action type: {actionType}, parameters: [{string.Join(", ", parameters)}]");
        
        // Get parameter names for this action type
        string[] paramNames = GetParameterNamesForAction(actionType);
        
        // Create MontiCore format
        var paramPairs = new List<string>();
        for (int i = 0; i < parameters.Length; i++)
        {
            string paramName = i < paramNames.Length ? paramNames[i] : $"param{i + 1}";
            paramPairs.Add($"{paramName} : {parameters[i]}");
        }
        
        string montiCoreFormat = $"{actionType}({string.Join(", ", paramPairs)})";
        string result = $"ActionInstance: {montiCoreFormat}";
        
        LoggingService.LogInfo($"🔧 Parser: Converted to MontiCore format: {result}");
        return result;
    }

    /// <summary>
    /// Gets parameter names for a given action type dynamically using reflection
    /// </summary>
    /// <param name="actionType">The action type (e.g., "PickUpHL", "PlaceHL")</param>
    /// <returns>Array of parameter names for this action type in constructor order</returns>
    private static string[] GetParameterNamesForAction(string actionType)
    {
        LoggingService.LogInfo($"🔧 Parser: GetParameterNamesForAction called with: {actionType}");
        
        try
        {
            // Dynamically find the action type using reflection
            Type actionTypeClass = FindActionTypeDynamically(actionType);
            
            if (actionTypeClass == null)
            {
                LoggingService.LogWarning($"⚠️ Parser: Could not find action type '{actionType}', using generic names");
                return new string[0];
            }
            
            LoggingService.LogInfo($"🔧 Parser: Found action type class: {actionTypeClass.Name}");
            
            // Get the constructor that matches our expected signature
            var constructors = actionTypeClass.GetConstructors();
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
                LoggingService.LogWarning($"⚠️ Parser: No suitable constructor found for action type '{actionType}', using generic names");
                return new string[0];
            }

            LoggingService.LogInfo($"🔧 Parser: Found constructor with {targetConstructor.GetParameters().Length} parameters");
            
            // Get constructor parameters (skip the first 3: actionType, instanceName, blackboard)
            var constructorParams = targetConstructor.GetParameters().Skip(3).ToArray();
            
            // Extract parameter names in constructor order
            var paramNames = new string[constructorParams.Length];
            for (int i = 0; i < constructorParams.Length; i++)
            {
                paramNames[i] = constructorParams[i].Name;
                LoggingService.LogInfo($"🔧 Parser: Parameter {i}: {paramNames[i]} (type: {constructorParams[i].ParameterType.Name})");
            }
            
            LoggingService.LogInfo($"🔧 Parser: Dynamic parameter mapping for {actionType}: [{string.Join(", ", paramNames)}]");
            return paramNames;
        }
        catch (Exception ex)
        {
            LoggingService.LogError($"❌ Parser: Error getting parameter names for action type '{actionType}': {ex.Message}");
            return new string[0];
        }
    }

    /// <summary>
    /// Dynamically finds an action type by name using reflection
    /// </summary>
    /// <param name="actionTypeName">The action type name to find</param>
    /// <returns>The Type object for the action class, or null if not found</returns>
    private static Type FindActionTypeDynamically(string actionTypeName)
    {
        LoggingService.LogInfo($"🔧 Parser: FindActionTypeDynamically called with: {actionTypeName}");
        
        try
        {
            // Get the assembly containing GenericBTAction types
            var assembly = typeof(PActionNode).Assembly;
            
            // Search for types that inherit from GenericBTAction
            var actionTypes = assembly.GetTypes()
                .Where(t => t.IsSubclassOf(typeof(PActionNode)) && !t.IsAbstract)
                .ToList();
            
            LoggingService.LogInfo($"🔧 Parser: Found {actionTypes.Count} action types: {string.Join(", ", actionTypes.Select(t => t.Name))}");
            
            // Try exact match first (case-insensitive)
            var exactMatch = actionTypes.FirstOrDefault(t => 
                string.Equals(t.Name, actionTypeName, StringComparison.OrdinalIgnoreCase));
            
            if (exactMatch != null)
            {
                LoggingService.LogInfo($"🔧 Parser: Found exact match: {exactMatch.Name}");
                return exactMatch;
            }
            
            // Try partial match (e.g., "pickup" matches "PickUp")
            var partialMatch = actionTypes.FirstOrDefault(t => 
                string.Equals(t.Name.Replace(" ", ""), actionTypeName.Replace(" ", ""), StringComparison.OrdinalIgnoreCase));
            
            if (partialMatch != null)
            {
                LoggingService.LogInfo($"🔧 Parser: Found partial match: {partialMatch.Name}");
                return partialMatch;
            }
            
            LoggingService.LogWarning($"⚠️ Parser: No match found for action name: {actionTypeName}");
            return null;
        }
        catch (Exception ex)
        {
            LoggingService.LogError($"❌ Parser: Error finding action type '{actionTypeName}': {ex.Message}");
            return null;
        }
    }
}
