using BehaviorTreeMainProject.Services;
using BehaviorTreeMainProject.Services.AIPlanning;
using BehaviorTreeMainProject.Log.Services;

/// <summary>
/// Fires once when its owning batch composite is first ticked.
/// Re-arms the global PlanningPhase flag, resets the CassetteSubtreeCompleted
/// entries owned by this batch, and points ServicePDDLPlanning.CurrentObjectsFile
/// at this batch's ParameterInstances objects file so its planners use the
/// right object range (C1-C4 / C5-C8 / C9-C12).
/// </summary>
public class ServiceBatchEntry : Service
{
    public string DebugDisplayName { get; protected set; } = "BatchEntry";

    private readonly int[] _ownedCassetteIndices;
    private readonly string? _objectsFilePath;
    private bool _hasFired = false;

    public ServiceBatchEntry(IBehaviorTree owningTree, FlowNode owningFlowNode, int[] ownedCassetteIndices, string? objectsFilePath = null)
        : base(owningTree)
    {
        AttachedNode = owningFlowNode;
        _ownedCassetteIndices = ownedCassetteIndices ?? new int[0];
        _objectsFilePath = objectsFilePath;
    }

    public override bool OnEvaluate(float inDeltaTime)
    {
        var flags = linkedBlackboard.CassetteSubtreeCompleted;

        // Re-assert non-owned flags as already-completed on EVERY tick.
        // ServiceSubtreeInject.resetAfterSuccessFullExecution() (fired by
        // DecoratorResetOnSubtreeSuccess after a successful HL action) clears
        // all 12 flags blindly, which would otherwise re-trigger the global
        // BLOCK in DecoratorDynamicPlanningComplete and stall this batch.
        // Also re-pin owned cassettes that have already reached Success — the
        // shared SetFlagForSuccessfulCassetteNodes in DecoratorDynamicPlanningComplete
        // is hardcoded to cassette1..4, so for batches 2 and 3 the flag of the
        // first cassette to finish would otherwise stay false after each reset
        // and BLOCK the remaining cassettes' MoveCassetteToStack indefinitely.
        if (flags != null)
        {
            for (int i = 0; i < flags.Length; i++)
            {
                if (!System.Linq.Enumerable.Contains(_ownedCassetteIndices, i))
                    flags[i] = true;
            }

            foreach (var idx in _ownedCassetteIndices)
            {
                if (idx < 0 || idx >= flags.Length) continue;
                var cassetteNode = linkedBlackboard.GetFlowNode(new FastName($"cassette{idx + 1}")) as DynamicFlowNode;
                if (cassetteNode != null && cassetteNode.status == BTNodeResult.Success)
                    flags[idx] = true;
            }
        }

        if (_hasFired) return true;
        _hasFired = true;

        linkedBlackboard.PlanningPhase = true;

        if (flags != null)
        {
            foreach (var idx in _ownedCassetteIndices)
            {
                if (idx >= 0 && idx < flags.Length)
                    flags[idx] = false;
            }
        }

        // Clear any stale chosen branch carried over from a previous batch so
        // DecoratorLowestCostExecution re-picks from this batch's 4 cassettes.
        linkedBlackboard.ChosenExecutingBranch = null;

        if (!string.IsNullOrEmpty(_objectsFilePath))
        {
            ServicePDDLPlanning.CurrentObjectsFile = _objectsFilePath;
            LoggingService.LogInfo($"ServiceBatchEntry: ServicePDDLPlanning.CurrentObjectsFile = {_objectsFilePath}");
        }

        LoggingService.LogInfo($"ServiceBatchEntry: re-armed PlanningPhase=true, owns CassetteSubtreeCompleted[{string.Join(',', _ownedCassetteIndices)}] (others pre-marked true) for {AttachedNode?.GetNodeName()}");
        return true;
    }
}
