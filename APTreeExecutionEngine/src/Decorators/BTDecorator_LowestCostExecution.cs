using BehaviorTreeMainProject.Services;
using System.Collections.Generic;
using System.Linq;
using BehaviorTreeMainProject.Log.Services;

/// <summary>
/// Decorator that ensures only the executable node with the lowest cost is allowed to execute.
/// This decorator compares costs from all dynamic flow nodes and blocks execution for higher-cost nodes.
/// </summary>
public class BTDecorator_LowestCostExecution : BTDecoratorBase
{
    public override bool CanPostProcessTickResult => false;
    public override BTNodeResult PostProcessTickResult(BTNodeResult InResult) => InResult;
    public int lowestCost;

    private FastName nodeName;

    public BTDecorator_LowestCostExecution(BTFlowNode_Dynamic AttachedNode) : base(false)
    {
        this.AttachedNode = AttachedNode;
    }


    protected override bool OnEvaluate(float InDeltaTime)
    {
        // First, find and update the lowest cost on the blackboard
        FindLowestCost();
        
        // Check if the current flow node's action count matches the blackboard's lowest cost
        if (AttachedNode is BTFlowNode_Dynamic dynamicNode)
        {
            var actionGraph = dynamicNode.GetActionGraph();
            if (actionGraph != null)
            {
                var actionCount = actionGraph.GetAllActionNodes().Count;
                var blackboardLowestCost = LinkedBlackboard.LowestCost;
                
                LoggingService.LogInfo($"🔍 LowestCostExecutionDecorator: Current node has {actionCount} actions, blackboard lowest cost is {blackboardLowestCost}");
                
                if (actionCount == blackboardLowestCost)
                {
                    LoggingService.LogSuccess($"✅ LowestCostExecutionDecorator: Action count ({actionCount}) matches lowest cost ({blackboardLowestCost}), allowing execution");
                    return true;
                }
                else
                {
                    LoggingService.LogInfo($"⏳ LowestCostExecutionDecorator: Action count ({actionCount}) doesn't match lowest cost ({blackboardLowestCost}), blocking execution");
                    return false;
                }
            }
        }
        
        // If we can't determine the action count, allow execution
        LoggingService.LogWarning($"⚠️ LowestCostExecutionDecorator: Cannot determine action count, allowing execution");
        return true;
    }

    private void FindLowestCost()
    {
        try
        {
            // Get all injected subtrees from the blackboard
            var injectedSubtrees = LinkedBlackboard.GetAllInjectedSubtrees();
            LoggingService.LogInfo($"🔍 FindLowestCost: Found {injectedSubtrees.Count} injected subtrees");
            
            var allCosts = new Dictionary<string, int>();
            
            // Process each injected subtree
            foreach (var subtree in injectedSubtrees)
            {
                var subtreeName = subtree.InstanceName.ToString();
                var actionGraph = subtree.GetActionGraph();
                
                if (actionGraph != null)
                {
                    // Count the number of children (actions) in the node graph
                    var actionNodes = actionGraph.GetAllActionNodes();
                    var childCount = actionNodes.Count;
                    
                    // Use child count as the cost (more children = higher cost)
                    allCosts[subtreeName] = childCount;
                }
                else
                {
                    LoggingService.LogWarning($"⚠️ FindLowestCost: Subtree {subtreeName} has no action graph");
                    allCosts[subtreeName] = 0; // Default cost for subtrees without graphs
                }
            }
            
            if (allCosts.Count > 0)
            {
                // Find the lowest cost
                var lowestCost = allCosts.Values.Min();
                var lowestCostNodes = allCosts.Where(kvp => kvp.Value == lowestCost).Select(kvp => kvp.Key).ToList();
                
                // NEW: Consolidated log block showing all costs and selection
                LoggingService.LogInfo("📊 COST ANALYSIS SUMMARY:");
                LoggingService.LogInfo("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                
                // Show each node and its cost
                foreach (var kvp in allCosts.OrderBy(x => x.Value))
                {
                    var nodeName = kvp.Key;
                    var cost = kvp.Value;
                    var isLowest = cost == lowestCost;
                    var marker = isLowest ? "🏆 LOWEST" : "   ";
                    LoggingService.LogInfo($"   {marker} {nodeName}: {cost} actions");
                }
                
                LoggingService.LogInfo("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                LoggingService.LogSuccess($"🎯 SELECTED: {string.Join(", ", lowestCostNodes)} with cost {lowestCost}");
                LoggingService.LogInfo("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                
                // Set the lowest cost on the blackboard's existing variable
                LinkedBlackboard.LowestCost = lowestCost;
                LoggingService.LogSuccess($"✅ FindLowestCost: Set blackboard.LowestCost to {lowestCost}");
                
            }
            else
            {
                LoggingService.LogWarning($"⚠️ FindLowestCost: No costs found, setting default lowest cost to 0");
                LinkedBlackboard.LowestCost = 0;
                this.lowestCost = 0;
            }
        }
        catch (System.Exception ex)
        {
            LoggingService.LogError($"❌ FindLowestCost: Error finding lowest cost: {ex.Message}");
            // Set default values on error
            LinkedBlackboard.LowestCost = 0;
            this.lowestCost = 0;
        }
    }
    
   
    
    
}
