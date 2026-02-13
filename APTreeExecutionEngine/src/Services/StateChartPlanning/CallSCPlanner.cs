using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDataStructures;
using AIPlanning;
using BehaviorTreeMainProject.Services;
using BehaviorTreeMainProject.Log.Services;

public class CallSCPlanner : BTServicePlanner
{
    private DateTime planningStartTime;
    private bool planningStarted = false;

    private readonly Blackboard<FastName> blackboard;
    private readonly FactoryAction actionFactory;
  
    public FastName PlannerName = new FastName("StateChartPlanner");

    public CallSCPlanner(BehaviorTreeInstance InOwningTree, StateChartPlanningRequest InPlanningRequest) 
        : base(InOwningTree, new RestPlannerCommunicator("http://localhost:5001"), InPlanningRequest) // Different port for SC planner
    {
        this.blackboard = InOwningTree.linkedBlackboard;
        this.actionFactory = FactoryAction.Instance;
    }
    
    public CallSCPlanner(BehaviorTreeInstance InOwningTree, IPlannerCommunicator customCommunicator, StateChartPlanningRequest InPlanningRequest) 
        : base(InOwningTree, customCommunicator, InPlanningRequest)
    {
        this.blackboard = InOwningTree.linkedBlackboard;
        this.actionFactory = FactoryAction.Instance;
    }

    public override bool OnEvaluate(float InDeltaTime)
    {
        if (!planningStarted)
        {
            planningStartTime = DateTime.Now;
            planningStarted = true;
            
            // Track planning service start
            LoggingService.TrackPlanningService(
                "CallSCPlanner", 
                "StateChart", 
                planningStartTime, 
                false, 
                0
            );
        }
        
        return base.OnEvaluate(InDeltaTime);
    }

    protected override NodeGraph GenerateNodeGraphFromResult(PlanningResult result)
    {
        var endTime = DateTime.Now;
        bool success = result.Success;
        int actionsGenerated = 0;

        LoggingService.LogInfo($"🔧 CallSCPlanner: Converting StateChart result to NodeGraph...");
        
        try
        {
            if (string.IsNullOrEmpty(result.Plan))
            {
                LoggingService.LogWarning($"⚠️ CallSCPlanner: No plan in planning result");
                success = false;
            }
            else
            {
                // Parse the plan string and create NodeGraph
                var nodeGraph = ParsePlanStringToNodeGraph(result.Plan);
                
                if (nodeGraph != null)
                {
                    actionsGenerated = nodeGraph.GetAllActionNodes().Count;
                    LoggingService.LogSuccess($"✅ CallSCPlanner: Generated NodeGraph with {actionsGenerated} actions");
                }
                else
                {
                    success = false;
                }
            }
        }
        catch (Exception ex)
        {
            LoggingService.LogError($"❌ CallSCPlanner: Error generating NodeGraph: {ex.Message}");
            success = false;
        }

        // Track planning service completion
        LoggingService.TrackPlanningService(
            "CallSCPlanner", 
            "StateChart", 
            planningStartTime, 
            success, 
            actionsGenerated,
            endTime
        );

        return success ? ParsePlanStringToNodeGraph(result.Plan) : null;
    }
    

    
    private NodeGraph ParsePlanStringToNodeGraph(string planString)
    {
        var nodeGraph = new NodeGraph();
        var actions = new List<PActionNode>();
        
        // Parse the plan string (assuming it's in a format like NodeGraphGenerated.txt)
        var lines = planString.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            
            // Skip comments and empty lines
            if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("//"))
                continue;
            
            // Parse ActionInstance lines
            if (trimmedLine.StartsWith("ActionInstance:"))
            {
                var actionString = trimmedLine.Substring("ActionInstance:".Length).Trim();
                var actionInstance = CreateActionFromString(actionString);
                if (actionInstance != null)
                {
                    actions.Add(actionInstance);
                    nodeGraph.AddNode(actionInstance);
                    Console.WriteLine($"🔧 CallSCPlanner: Added action {actionInstance.InstanceName.ToString()}");
                }
            }
            
            // Parse Relation lines (new format: action1 --[CONSTRAINT]--> action2)
            if (trimmedLine.Contains("--[") && trimmedLine.Contains("]-->"))
            {
                ParseRelationString(trimmedLine, actions, nodeGraph);
            }
        }
        
        // If no relations were parsed, add default parallel ordering (StateChart actions often run in parallel)
        if (actions.Count > 1 && nodeGraph.GetAllActionNodes().Count == actions.Count)
        {
            for (int i = 0; i < actions.Count - 1; i++)
            {
                // StateChart actions can often run in parallel, so use OVERLAPS constraint
                nodeGraph.AddTemporalConstraint(actions[i], actions[i + 1], TemporalConstraint.OVERLAPS);
                Console.WriteLine($"🔧 CallSCPlanner: Added default parallel relation {i} || {i + 1}");
            }
        }
        
        return nodeGraph;
    }
    
    private PActionNode CreateActionFromString(string actionString)
    {
        try
        {
            // Try to find existing action instance in blackboard
            var actionInstances = blackboard.GetAllActionInstances();
            
            // Look for action with matching instance name
            foreach (var action in actionInstances)
            {
                if (action.InstanceName.ToString().Equals(actionString, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"🔧 CallSCPlanner: Found matching action {action.InstanceName.ToString()}");
                    return action;
                }
            }
            
            // If no exact match found, create a new instance
            Console.WriteLine($"⚠️ CallSCPlanner: No exact match found for {actionString}, creating new instance");
            return CreateNewActionInstance(actionString);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ CallSCPlanner: Error creating action from string: {ex.Message}");
            return null;
        }
    }
    
    private PActionNode CreateNewActionInstance(string actionString)
    {
        try
        {
            // Create action instance using factory
            var actionInstance = actionFactory.CreateActionInstance(actionString, blackboard);
            
            if (actionInstance != null)
            {
                // Register in blackboard using the correct method
                blackboard.SetActionType(new FastName(actionString), actionInstance);
                Console.WriteLine($"🔧 CallSCPlanner: Created new action instance {actionString}");
                return actionInstance;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ CallSCPlanner: Error creating new action instance: {ex.Message}");
        }
        
        return null;
    }
    
    private void ParseRelationString(string relationString, List<PActionNode> actions, NodeGraph nodeGraph)
    {
        try
        {
            // Parse relation string like "action1 --[OVERLAPS]--> action2"
            // Find the arrow pattern "--[CONSTRAINT]-->"
            int arrowStart = relationString.IndexOf("--[");
            if (arrowStart == -1)
            {
                LoggingService.LogError($"❌ CallSCPlanner: No arrow pattern '--[' found in relation: {relationString}");
                return;
            }
            
            int arrowEnd = relationString.IndexOf("]-->", arrowStart);
            if (arrowEnd == -1)
            {
                LoggingService.LogError($"❌ CallSCPlanner: No closing arrow pattern ']-->' found in relation: {relationString}");
                return;
            }
            
            // Extract action names and constraint
            string action1Name = relationString.Substring(0, arrowStart).Trim();
            string constraintStr = relationString.Substring(arrowStart + 3, arrowEnd - arrowStart - 3).Trim();
            string action2Name = relationString.Substring(arrowEnd + 4).Trim();
            
            LoggingService.LogInfo($"🔧 CallSCPlanner: Parsed relation: {action1Name} --[{constraintStr}]--> {action2Name}");
            
            // Find the corresponding actions
            var action1 = actions.FirstOrDefault(a => a.InstanceName.ToString().Equals(action1Name, StringComparison.OrdinalIgnoreCase));
            var action2 = actions.FirstOrDefault(a => a.InstanceName.ToString().Equals(action2Name, StringComparison.OrdinalIgnoreCase));
            
            if (action1 != null && action2 != null)
            {
                // Add order relation and temporal constraint
                nodeGraph.AddOrderRelation(action1, action2);
                
                // Add temporal constraint (StateChart focuses on temporal relationships)
                var constraintType = ParseTemporalConstraint(constraintStr);
                nodeGraph.AddTemporalConstraint(action1, action2, constraintType);
                
                LoggingService.LogInfo($"🔧 CallSCPlanner: Added relation {action1Name} {constraintStr} {action2Name}");
            }
            else
            {
                LoggingService.LogError($"❌ CallSCPlanner: Action not found - action1: {action1Name}, action2: {action2Name}");
            }
        }
        catch (Exception ex)
        {
            LoggingService.LogError($"❌ CallSCPlanner: Error parsing relation string: {ex.Message}");
        }
    }
    
    private TemporalConstraint ParseTemporalConstraint(string relationType)
    {
        return relationType?.ToUpper() switch
        {
            "MEETS" => TemporalConstraint.MEETS,
            "PRECEDES" => TemporalConstraint.PRECEDES,
            "OVERLAPS" => TemporalConstraint.OVERLAPS,
            "PARALLEL" => TemporalConstraint.OVERLAPS, // StateChart parallel = overlaps
            _ => TemporalConstraint.OVERLAPS // Default to OVERLAPS for StateChart
        };
    }
    
    // StateChart-specific extraction methods
    private string ExtractCurrentStateForStateChart()
    {
        try
        {
            // Extract current state machine state from blackboard
            // StateChart uses state names as strings
            var currentState = "Idle"; // Default state
            
            LoggingService.LogInfo($"🔧 CallSCPlanner: Extracted current state: {currentState}");
            return currentState;
        }
        catch (Exception ex)
        {
            LoggingService.LogError($"❌ CallSCPlanner: Error extracting current state: {ex.Message}");
            return "Error";
        }
    }
    
    private string ExtractTargetStateForStateChart()
    {
        try
        {
            // Extract target state from blackboard
            var targetState = "Completed"; // Default target state
            
            LoggingService.LogInfo($"🔧 CallSCPlanner: Extracted target state: {targetState}");
            return targetState;
        }
        catch (Exception ex)
        {
            LoggingService.LogError($"❌ CallSCPlanner: Error extracting target state: {ex.Message}");
            return "Error";
        }
    }
    
    private List<string> ExtractAvailableTransitions()
    {
        var transitions = new List<string>();
        
        try
        {
            // Extract available state transitions from blackboard
            transitions.Add("Idle -> Working");
            transitions.Add("Working -> Completed");
            transitions.Add("Working -> Error");
            transitions.Add("Error -> Idle");
            
            Console.WriteLine($"🔧 CallSCPlanner: Extracted {transitions.Count} available transitions");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ CallSCPlanner: Error extracting transitions: {ex.Message}");
        }
        
        return transitions;
    }
    
    // Legacy methods for backward compatibility
    public List<ActionNode> GetPlan()
    {
        if (generatedNodeGraph != null)
        {
            return generatedNodeGraph.GetAllActionNodes().Cast<ActionNode>().ToList();
        }
        return new List<ActionNode>();
    }
    
    public (List<IBTNode> Actions, List<OrderType> Orders) CreatePlanWithOrders()
    {
        if (generatedNodeGraph != null)
        {
            var actions = generatedNodeGraph.GetAllActionNodes().Cast<IBTNode>().ToList();
            var orders = new List<OrderType>();
            
            // Generate orders based on NodeGraph structure (StateChart often uses parallel)
            for (int i = 0; i < actions.Count - 1; i++)
            {
                orders.Add(OrderType.Parallel); // StateChart actions often run in parallel
            }
            
            return (actions, orders);
        }
        
        return (new List<IBTNode>(), new List<OrderType>());
    }
}