using BehaviorTreeMainProject.Log.Services;

/// <summary>
/// Service attached to a batch composite. On the first tick after the batch becomes active
/// (or after a Reset), it (re)initializes planning-related blackboard state so the batch's
/// planners and decorators behave as if starting from a fresh tree:
///   - PlanningPhase = true (re-arms ServicePlanningPhaseManager for this batch)
///   - ChosenExecutingBranch = null (clears any stale lock from a previous batch)
///   - CassetteSubtreeCompleted[i] = false for the cassette indices this batch owns
/// After running once, it no-ops until <see cref="Rearm"/> is called.
/// </summary>
public class ServiceBatchEntry : Service
{
    public string DebugDisplayName { get; protected set; } = "BatchEntry";

    private readonly int[] _ownedCassetteIndices;
    private readonly string? _objectsFilePath;
    private bool _done = false;

    public ServiceBatchEntry(IBehaviorTree owningTree, FlowNode owningFlowNode, int[] ownedCassetteIndices, string? objectsFilePath = null) : base(owningTree)
    {
        AttachedNode = owningFlowNode;
        _ownedCassetteIndices = ownedCassetteIndices ?? new int[0];
        _objectsFilePath = objectsFilePath;
    }

    public void Rearm() => _done = false;

    public override bool OnEvaluate(float inDeltaTime)
    {
        if (_done) return true;

        if (linkedBlackboard != null)
        {
            linkedBlackboard.PlanningPhase = true;
            linkedBlackboard.ChosenExecutingBranch = null;

            if (linkedBlackboard.CassetteSubtreeCompleted != null)
            {
                foreach (var idx in _ownedCassetteIndices)
                {
                    if (idx >= 0 && idx < linkedBlackboard.CassetteSubtreeCompleted.Length)
                        linkedBlackboard.CassetteSubtreeCompleted[idx] = false;
                }
            }

            if (!string.IsNullOrEmpty(_objectsFilePath))
            {
                BehaviorTreeMainProject.Services.AIPlanning.ServicePDDLPlanning.CurrentObjectsFile = _objectsFilePath;
                LoggingService.LogInfo($"📁 ServiceBatchEntry: ServicePDDLPlanning.CurrentObjectsFile = {_objectsFilePath}");
            }

            LoggingService.LogInfo($"🚦 ServiceBatchEntry: re-armed batch '{AttachedNode?.DebugDisplayName}' — PlanningPhase=true, ChosenExecutingBranch=null, reset flags for cassettes [{string.Join(",", _ownedCassetteIndices)}]");
        }

        _done = true;
        return true;
    }
}
