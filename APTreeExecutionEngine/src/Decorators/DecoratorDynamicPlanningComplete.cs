using BehaviorTreeMainProject.Services;
using BehaviorTreeMainProject.Log.Services;

/// <summary>
/// Decorator that ensures dynamic planning is completed before allowing node execution.
/// This decorator simply checks the PlanningPhaseDynamic flag on the blackboard.
/// </summary>
public class DecoratorDynamicPlanningComplete : Decorator
{
    public override bool CanPostProcessTickResult => false;
    public override BTNodeResult PostProcessTickResult(BTNodeResult InResult) => InResult;
    public DecoratorDynamicPlanningComplete() : base(false)
    {

    }



    protected override bool OnEvaluate(float InDeltaTime)
    {
        // Check if LinkedBlackboard is available
        if (LinkedBlackboard == null)
        {
            LoggingService.LogWarning($"⚠️ DynamicPlanningCompleteDecorator: LinkedBlackboard is null, allowing execution");
            ExecutionFlowLogger.LogDecoratorTick("DynamicPlanningComplete", "LinkedBlackboard", "Null", "ALLOW_NULL");
            BehaviorTreeComponentLogger.TrackDecoratorEvaluation("DecoratorDynamicPlanningComplete", true);
            return true; // Allow execution when blackboard is not available
        }
        SetFlagForSuccessfulCassetteNodes();
        // Check if all cassettes have completed their subtree injection
        if (LinkedBlackboard.CassetteSubtreeCompleted == null)
        {
            LoggingService.LogWarning($"⚠️ DynamicPlanningCompleteDecorator: CassetteSubtreeCompleted array is null, allowing execution");
            ExecutionFlowLogger.LogDecoratorTick("DynamicPlanningComplete", "CassetteSubtreeCompleted", "Null", "ALLOW_NULL");
            return true; // Allow execution when array is not available
        }

        // Check if all cassettes have completed subtree injection
        bool allCassettesCompleted = true;
        foreach (bool completed in LinkedBlackboard.CassetteSubtreeCompleted)
        {
            if (!completed)
            {
                allCassettesCompleted = false;
                break;
            }
        }

        if (!allCassettesCompleted)
        {
            // Log which cassettes are still pending
            var pendingCassettes = new List<int>();
            for (int i = 0; i < LinkedBlackboard.CassetteSubtreeCompleted.Length; i++)
            {
                if (!LinkedBlackboard.CassetteSubtreeCompleted[i])
                {
                    pendingCassettes.Add(i + 1); // +1 for human-readable cassette numbers
                }
            }

            LoggingService.LogInfo($"⏳ DynamicPlanningCompleteDecorator: Waiting for cassettes {string.Join(", ", pendingCassettes)} to complete subtree injection");
            ExecutionFlowLogger.LogDecoratorTick("DynamicPlanningComplete", "CassetteSubtreeCompleted", "Pending", "BLOCK_FOR_RE_EVAL");
            BehaviorTreeComponentLogger.TrackDecoratorEvaluation("DecoratorDynamicPlanningComplete", false);
            return false; // Block execution until all cassettes complete
        }
        else
        {
            LoggingService.LogInfo($"✅ DynamicPlanningCompleteDecorator: All cassettes have completed subtree injection, allowing execution");
            ExecutionFlowLogger.LogDecoratorTick("DynamicPlanningComplete", "CassetteSubtreeCompleted", "Complete", "ALLOW");
            BehaviorTreeComponentLogger.TrackDecoratorEvaluation("DecoratorDynamicPlanningComplete", true);
            return true; // Allow execution when all cassettes are complete
        }
    }

    // ───────────────────────────────────────────────────────────────
    //  Static helpers – cassette-index lookup via tree traversal
    // ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Set the cassette subtree completion flag for the cassette that contains the given action.
    /// Called from ServiceSubtreeInject after a subtree is injected.
    /// </summary>
    public static void SetCassetteSubtreeCompletedFlag(IBTNode root, PActionNode action, Blackboard<FastName> blackboard)
    {
        if (blackboard == null)
        {
            LoggingService.LogWarning("⚠️ DynamicPlanningCompleteDecorator: Blackboard is null, cannot set cassette flag");
            return;
        }

        int cassetteIndex = FindCassetteIndexForAction(root, action);

        if (cassetteIndex >= 0 && cassetteIndex < blackboard.CassetteSubtreeCompleted.Length)
        {
            blackboard.CassetteSubtreeCompleted[cassetteIndex] = true;
            LoggingService.LogSuccess($"✅ DynamicPlanningCompleteDecorator: Set cassette{cassetteIndex + 1} subtree completion flag to true");
        }
        else
        {
            LoggingService.LogWarning($"⚠️ DynamicPlanningCompleteDecorator: Could not determine cassette index for action '{action.InstanceName.ToString()}'");
        }
    }

    /// <summary>
    /// Find which cassette flow node contains the given action by traversing the tree structure.
    /// </summary>
    /// <returns>The cassette index (0-based) or -1 if not found</returns>
    public static int FindCassetteIndexForAction(IBTNode root, PActionNode action)
    {
        if (root == null)
        {
            LoggingService.LogWarning("⚠️ DynamicPlanningCompleteDecorator: Cannot traverse tree – root node is null");
            return -1;
        }

        LoggingService.LogInfo($"🔍 DynamicPlanningCompleteDecorator: Searching for action '{action.InstanceName.ToString()}' in tree structure");

        var cassetteIndex = TraverseTreeForAction(root, action);

        if (cassetteIndex >= 0)
        {
            LoggingService.LogInfo($"🔍 DynamicPlanningCompleteDecorator: Found action '{action.InstanceName.ToString()}' in cassette{cassetteIndex + 1}");
        }
        else
        {
            LoggingService.LogWarning($"⚠️ DynamicPlanningCompleteDecorator: Action '{action.InstanceName.ToString()}' not found in any cassette");
        }

        return cassetteIndex;
    }

    /// <summary>
    /// Recursively traverse the tree to find which cassette contains the given action.
    /// </summary>
    private static int TraverseTreeForAction(IBTNode node, PActionNode targetAction)
    {
        if (node == null) return -1;

        // Check if this is a cassette flow node
        if (node is DynamicFlowNode flowNode)
        {
            var nodeName = flowNode.GetNodeName().ToLower();
            if (nodeName.StartsWith("cassette"))
            {
                if (int.TryParse(nodeName.Substring("cassette".Length), out int cassetteNumber))
                {
                    int cassetteIndex = cassetteNumber - 1;

                    LoggingService.LogInfo($"🔍 DynamicPlanningCompleteDecorator: Checking cassette{cassetteNumber} (index {cassetteIndex}) for action '{targetAction.InstanceName.ToString()}'");

                    if (ContainsAction(flowNode, targetAction))
                    {
                        LoggingService.LogSuccess($"✅ DynamicPlanningCompleteDecorator: Found action '{targetAction.InstanceName.ToString()}' in cassette{cassetteNumber}");
                        return cassetteIndex;
                    }
                }
            }
        }

        // Recursively traverse both action-backed and flow-backed FlowNodes.
        if (node is FlowNode parentFlowNode)
        {
            foreach (var child in parentFlowNode.GetChildren())
            {
                var result = TraverseTreeForAction(child, targetAction);
                if (result >= 0)
                    return result;
            }
        }

        return -1;
    }

    /// <summary>
    /// Check if a flow node contains the given action.
    /// </summary>
    private static bool ContainsAction(DynamicFlowNode flowNode, PActionNode targetAction)
    {
        var actionGraph = flowNode.GetActionGraph();
        if (actionGraph != null)
        {
            var actionNodes = actionGraph.GetAllActionNodes();
            foreach (var actionNode in actionNodes)
            {
                if (actionNode == targetAction)
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Gets the specific cassette flow nodes by name (cassette1, cassette2, cassette3, cassette4)
    /// Returns null for cassettes that don't exist
    /// </summary>
    /// <returns>Dictionary with cassette names as keys and flow nodes as values</returns>
    public void SetFlagForSuccessfulCassetteNodes()
    {
        
        if (LinkedBlackboard == null)
        {
            LoggingService.LogWarning("⚠️ SetFlagForSuccessfulCassetteNodes: LinkedBlackboard is null");
            
        }
        
        var cassetteNames = new[] { "cassette1", "cassette2", "cassette3", "cassette4" };
        
        foreach (var cassetteName in cassetteNames)
        {
            try
            {
                var cassetteNode = LinkedBlackboard.GetFlowNode(new FastName(cassetteName)) as DynamicFlowNode;
                
                if (cassetteNode != null)
                {
                    // Check if the cassette node status is successful
                    if (cassetteNode.status == BTNodeResult.Success)
                    {
                        // Get the cassette index (cassette1=0, cassette2=1, cassette3=2, cassette4=3)
                        int cassetteIndex = Array.IndexOf(cassetteNames, cassetteName);
                        
                        // Set the corresponding flag on blackboard to true
                        LinkedBlackboard.CassetteSubtreeCompleted[cassetteIndex] = true;
                        
                        LoggingService.LogSuccess($"✅ Cassette {cassetteName} is successful - set flag[{cassetteIndex}] to true");
                    }
                    else
                    {
                        LoggingService.LogInfo($"ℹ️ Cassette {cassetteName} status is {cassetteNode.status} - no action taken");
                    }
                }
                else
                {
                    LoggingService.LogInfo($"ℹ️ Cassette flow node not found: {cassetteName}");
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogWarning($"⚠️ Error checking cassette {cassetteName}: {ex.Message}");
            }
        }
        
        LoggingService.LogInfo($"📊 SetFlagForSuccessfulCassetteNodes: Found /4 cassette flow nodes");
        
    }
}
