using System;
using System.Collections.Generic;
using System.Linq;
using BehaviorTreeMainProject.Log.Services;

/// <summary>
/// Self-discovering batch service. Attached to a batch flow node via "Service batchManager"
/// in the BT model. On first tick it auto-detects:
///   - Its batch index among sibling batches in the parent node
///   - Which global cassette indices it owns (from child count + preceding siblings)
///   - The PDDL objects file path (convention: ParameterInstances_PDDL{N}.txt)
///   - Total cassette count across all batches (initializes blackboard array)
///
/// On each activation it re-arms planning state, same as ServiceBatchEntry:
///   - PlanningPhase = true
///   - ChosenExecutingBranch = null
///   - CassetteSubtreeCompleted[i] = false for owned indices
/// </summary>
public class ServiceBatchManager : Service
{
    public string DebugDisplayName { get; protected set; } = "BatchManager";

    private bool _initialized = false;
    private bool _done = false;
    private int[] _ownedCassetteIndices = Array.Empty<int>();
    private string _objectsFilePath;

    public ServiceBatchManager(IBehaviorTree owningTree, FlowNode owningFlowNode) : base(owningTree)
    {
        AttachedNode = owningFlowNode;
    }

    public void Rearm() => _done = false;

    public override bool OnEvaluate(float inDeltaTime)
    {
        if (!_initialized)
            Initialize();

        MaintainCassetteCompletionFlags();

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
                LoggingService.LogInfo($"📁 BatchManager: CurrentObjectsFile = {_objectsFilePath}");
            }

            LoggingService.LogInfo($"🚦 BatchManager: re-armed batch '{AttachedNode?.DebugDisplayName}' — PlanningPhase=true, reset flags for cassettes [{string.Join(",", _ownedCassetteIndices)}]");
        }

        _done = true;
        return true;
    }

    private void MaintainCassetteCompletionFlags()
    {
        var flags = linkedBlackboard?.CassetteSubtreeCompleted;
        if (flags == null)
            return;

        for (int i = 0; i < flags.Length; i++)
        {
            if (!_ownedCassetteIndices.Contains(i))
                flags[i] = true;
        }

        foreach (var index in _ownedCassetteIndices)
        {
            if (index < 0 || index >= flags.Length)
                continue;

            var cassetteNode = linkedBlackboard.GetFlowNode(
                new FastName($"cassette{index + 1}")) as DynamicFlowNode;
            if (cassetteNode?.status == BTNodeResult.Success)
                flags[index] = true;
        }
    }

    private void Initialize()
    {
        _initialized = true;

        var batchNode = AttachedNode as FlowNode;
        if (batchNode == null) return;

        // Count this batch's cassette children
        var myChildren = batchNode.GetChildren();
        int myCassetteCount = myChildren.Count;

        // Walk parent's children to find batch index and global cassette offset
        int batchIndex = 0;
        int globalOffset = 0;
        int totalCassettes = 0;

        var parent = AttachedNode.ParentNode as FlowNode;
        if (parent != null)
        {
            bool foundSelf = false;
            foreach (var sibling in parent.GetChildren())
            {
                int siblingChildCount = sibling is FlowNode siblingFlowNode
                    ? siblingFlowNode.GetChildren().Count
                    : 0;
                totalCassettes += siblingChildCount;

                if (sibling == AttachedNode)
                {
                    foundSelf = true;
                    continue;
                }

                if (!foundSelf)
                {
                    globalOffset += siblingChildCount;
                    batchIndex++;
                }
            }
        }
        else
        {
            totalCassettes = myCassetteCount;
        }

        // Assign owned cassette indices
        _ownedCassetteIndices = new int[myCassetteCount];
        for (int i = 0; i < myCassetteCount; i++)
            _ownedCassetteIndices[i] = globalOffset + i;

        // Initialize blackboard cassette array if needed
        if (linkedBlackboard.CassetteSubtreeCompleted == null ||
            linkedBlackboard.CassetteSubtreeCompleted.Length < totalCassettes)
        {
            linkedBlackboard.CassetteSubtreeCompleted = new bool[totalCassettes];
        }

        // Objects file convention: ParameterInstances_PDDL.txt, _PDDL2.txt, _PDDL3.txt, ...
        string suffix = batchIndex == 0 ? "" : (batchIndex + 1).ToString();
        _objectsFilePath = $"python_service/Plannerinputs/static/ParameterInstances_PDDL{suffix}.txt";

        LoggingService.LogInfo($"🔧 BatchManager: Initialized batch '{batchNode.DebugDisplayName}' — " +
            $"batchIndex={batchIndex}, cassettes=[{string.Join(",", _ownedCassetteIndices)}], " +
            $"objectsFile={_objectsFilePath}, totalCassettes={totalCassettes}");
    }
}