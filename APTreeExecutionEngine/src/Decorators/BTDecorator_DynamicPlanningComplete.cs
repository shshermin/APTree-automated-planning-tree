using BehaviorTreeMainProject.Services;
using BehaviorTreeMainProject.Log.Services;

/// <summary>
/// Decorator that ensures dynamic planning is completed before allowing node execution.
/// This decorator simply checks the PlanningPhaseDynamic flag on the blackboard.
/// </summary>
public class BTDecorator_DynamicPlanningComplete : BTDecoratorBase
{
    public override bool CanPostProcessTickResult => false;
    public override BTNodeResult PostProcessTickResult(BTNodeResult InResult) => InResult;
    public BTDecorator_DynamicPlanningComplete() : base(false)
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
                var cassetteNode = LinkedBlackboard.GetFlowNode(new FastName(cassetteName)) as BTFlowNode_Dynamic;
                
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
