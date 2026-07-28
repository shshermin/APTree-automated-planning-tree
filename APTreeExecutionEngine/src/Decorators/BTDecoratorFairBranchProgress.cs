using BehaviorTreeMainProject.Services;
using BehaviorTreeMainProject.Log.Services;

/// <summary>
/// Decorator attached to the ROOT composite node that enforces fair round-robin
/// progress across all cassette branches.
/// 
/// Tracks how many HL actions each branch has completed. When one branch makes
/// progress (an HL action succeeds), this decorator prioritizes the OTHER branches
/// on the next tick iteration by temporarily blocking the branch that is ahead.
/// 
/// This ensures all four cassettes advance at roughly the same pace rather than
/// one branch running to completion while others starve.
/// </summary>
public class BTDecoratorFairBranchProgress : Decorator
{
    public override bool CanPostProcessTickResult => true;

    /// <summary>
    /// Progress counter per cassette branch (index 0..3 = cassette1..cassette4)
    /// Incremented each time an HL action in that branch returns Success.
    /// </summary>
    public int[] BranchProgress { get; private set; } = new int[4] { 0, 0, 0, 0 };

    /// <summary>
    /// The index of the branch that most recently made progress.
    /// Used to deprioritize it on the next tick so other branches catch up.
    /// -1 means no branch has been deprioritized yet.
    /// </summary>
    public int LastProgressBranchIndex { get; private set; } = -1;

    /// <summary>
    /// Reference to the root composite node so we can inspect child branches.
    /// </summary>
    private FlowNode _rootComposite;

    /// <summary>
    /// Snapshot of child statuses from the previous tick, used to detect new successes.
    /// </summary>
    private BTNodeResult[] _previousChildStatuses;

    /// <summary>
    /// The Progress value of the chosen branch at the moment it was locked.
    /// When the branch's Progress exceeds this, one HL action (+ its ML subtree)
    /// has completed and we should release the lock to let another branch run.
    /// </summary>
    private int _chosenBranchProgressAtLock = 0;

    public BTDecoratorFairBranchProgress(FlowNode rootComposite) : base(false)
    {
        _rootComposite = rootComposite;
        _previousChildStatuses = new BTNodeResult[4];
        for (int i = 0; i < 4; i++)
            _previousChildStatuses[i] = BTNodeResult.Uninitialized;
    }

    /// <summary>
    /// Pre-tick evaluation. Always allows the root to tick (we don't want to block the
    /// root itself). The actual branch-level gating happens via the blackboard property
    /// DeprioritizedBranchIndex, which individual branch decorators or the composite
    /// tick logic can read.
    /// 
    /// This decorator also sets ChosenExecutingBranch on the blackboard so that
    /// BTDecoratorExclusiveBranchGate can enforce that once an ML subtree starts
    /// executing, it runs to completion before another branch gets a turn.
    /// </summary>
    protected override bool OnEvaluate(float InDeltaTime)
    {
        // Don't interfere during planning phase
        if (LinkedBlackboard.PlanningPhase)
        {
            return true;
        }

        var children = _rootComposite.GetChildren();
        int branchCount = Math.Min(children.Count, 4);

        // --- Step 1: Read Progress from each cassette DynamicFlowNode ---
        for (int i = 0; i < branchCount; i++)
        {
            var child = children[i];
            BTNodeResult currentStatus = child.status;

            if (child is DynamicFlowNode dynamicNode)
            {
                int nodeProgress = dynamicNode.Progress;
                
                if (nodeProgress > BranchProgress[i])
                {
                    int delta = nodeProgress - BranchProgress[i];
                    BranchProgress[i] = nodeProgress;
                    LastProgressBranchIndex = i;

                    LoggingService.LogInfo($"📊 FairBranchProgress: Branch {i + 1} ({dynamicNode.DebugDisplayName}) advanced! " +
                        $"Progress: {BranchProgress[i]} finished children (delta: {delta}). " +
                        $"Prioritizing other branches on next tick.");
                    
                    ExecutionFlowLogger.LogDecoratorTick("FairBranchProgress", "BranchAdvanced",
                        dynamicNode.DebugDisplayName, $"Progress={BranchProgress[i]}, Delta={delta}");
                }
            }

            _previousChildStatuses[i] = currentStatus;
        }

        // --- Step 2: Manage ChosenExecutingBranch for ExclusiveBranchGate ---
        var currentChosen = LinkedBlackboard.ChosenExecutingBranch;

        if (currentChosen != null)
        {
            // A branch is currently locked — check if it finished entirely
            if (currentChosen.status == BTNodeResult.Success || currentChosen.status == BTNodeResult.Failure)
            {
                LoggingService.LogInfo($"⚖️ FairBranchProgress: Chosen branch '{currentChosen.DebugDisplayName}' finished " +
                    $"(status: {currentChosen.status}). Clearing lock for re-evaluation.");
                LinkedBlackboard.ChosenExecutingBranch = null;
                currentChosen = null;
            }
            // Check if the branch made progress (an HL action + ML subtree completed)
            else if (currentChosen is DynamicFlowNode chosenDynamic && chosenDynamic.Progress > _chosenBranchProgressAtLock)
            {
                // Before releasing, check if the next executable action uses the same
                // end-effector as the one that just finished. If so, batch them to avoid
                // unnecessary deequip/equip cycles.
                var chosenGraph = chosenDynamic.GetActionGraph();
                var nextActions = chosenGraph?.GetExecutableNodesInternal();
                string lastType = chosenDynamic.LastFinishedActionType;

                if (nextActions != null && nextActions.Count > 0 && lastType != null)
                {
                    string nextType = nextActions[0].actionType.ToString();
                    string lastToolGroup = GetEndEffectorGroup(lastType);
                    string nextToolGroup = GetEndEffectorGroup(nextType);

                    if (nextToolGroup == lastToolGroup)
                    {
                        // Same end-effector on this cassette — keep the lock, batch
                        _chosenBranchProgressAtLock = chosenDynamic.Progress;
                        LoggingService.LogInfo($"\u2696\ufe0f FairBranchProgress: Branch '{currentChosen.DebugDisplayName}' made progress " +
                            $"but next action '{nextType}' uses same tool group '{lastToolGroup}' as last finished '{lastType}' \u2014 batching, keeping lock.");
                    }
                    else
                    {
                        // Different end-effector on this cassette — before releasing,
                        // check if any OTHER cassette still needs the current tool.
                        // This avoids deequip→equip→deequip→equip ping-pong.
                        DynamicFlowNode crossCassetteMatch = null;
                        int crossCassetteIndex = -1;

                        // Build sorted candidates by progress (least first), excluding current and finished cassettes
                        var crossCandidates = new List<(DynamicFlowNode node, int index, int progress)>();
                        for (int i = 0; i < branchCount; i++)
                        {
                            if (children[i] == currentChosen) continue;
                            if (children[i] is DynamicFlowNode dn && dn.status != BTNodeResult.Success)
                            {
                                crossCandidates.Add((dn, i, BranchProgress[i]));
                            }
                        }
                        crossCandidates.Sort((a, b) => a.progress.CompareTo(b.progress));

                        foreach (var (candidateNode, candidateIndex, candidateProgress) in crossCandidates)
                        {
                            var candGraph = candidateNode.GetActionGraph();
                            if (candGraph == null) continue;
                            var candActions = candGraph.GetExecutableNodesInternal();
                            if (candActions.Count == 0) continue;

                            string candType = candActions[0].actionType.ToString();
                            string candToolGroup = GetEndEffectorGroup(candType);

                            if (candToolGroup == lastToolGroup && candActions[0].CheckPreconditions())
                            {
                                crossCassetteMatch = candidateNode;
                                crossCassetteIndex = candidateIndex;
                                break;
                            }
                        }

                        if (crossCassetteMatch != null)
                        {
                            // Another cassette needs the same tool — hand off the lock to it
                            LinkedBlackboard.ChosenExecutingBranch = crossCassetteMatch;
                            _chosenBranchProgressAtLock = crossCassetteMatch.Progress;
                            currentChosen = crossCassetteMatch;
                            LoggingService.LogInfo($"\u2696\ufe0f FairBranchProgress: Cross-cassette tool batching \u2014 " +
                                $"switching from '{chosenDynamic.DebugDisplayName}' to '{crossCassetteMatch.DebugDisplayName}' " +
                                $"(same tool group '{lastToolGroup}', progress: {BranchProgress[crossCassetteIndex]}).");
                        }
                        else
                        {
                            // No other cassette needs the current tool — release lock
                            LoggingService.LogInfo($"\u2696\ufe0f FairBranchProgress: Branch '{currentChosen.DebugDisplayName}' made progress " +
                                $"({_chosenBranchProgressAtLock} \u2192 {chosenDynamic.Progress}). Tool group '{lastToolGroup}' exhausted across all cassettes \u2014 releasing lock.");
                            LinkedBlackboard.ChosenExecutingBranch = null;
                            currentChosen = null;
                        }
                    }
                }
                else
                {
                    // No next action or no last type — release the lock
                    LoggingService.LogInfo($"\u2696\ufe0f FairBranchProgress: Branch '{currentChosen.DebugDisplayName}' made progress " +
                        $"({_chosenBranchProgressAtLock} \u2192 {chosenDynamic.Progress}). No next action or type info \u2014 releasing lock.");
                    LinkedBlackboard.ChosenExecutingBranch = null;
                    currentChosen = null;
                }
            }
            else
            {
                // Branch still running, no progress yet — keep the lock
                LoggingService.LogInfo($"⚖️ FairBranchProgress: Branch '{currentChosen.DebugDisplayName}' still executing (progress: " +
                    $"{(currentChosen is DynamicFlowNode d ? d.Progress : -1)}, locked at: {_chosenBranchProgressAtLock}). Keeping lock.");
            }
        }

        if (currentChosen == null)
        {
            // No branch locked — pick the one with the LEAST progress whose next
            // executable HL action has its preconditions met on the blackboard.
            // This avoids switching to a branch that can't actually run yet
            // (e.g. PickUpHL for cassette2 needs the robot to be free, but
            // cassette1 hasn't done PlaceHL yet).

            // Build a sorted list of candidates by progress (ascending)
            var candidates = new List<(DynamicFlowNode node, int index, int progress)>();
            for (int i = 0; i < branchCount; i++)
            {
                var child = children[i];
                if (child is DynamicFlowNode dynamicNode)
                {
                    if (dynamicNode.status == BTNodeResult.Success)
                        continue;
                    candidates.Add((dynamicNode, i, BranchProgress[i]));
                }
            }
            candidates.Sort((a, b) => a.progress.CompareTo(b.progress));

            DynamicFlowNode bestCandidate = null;
            int bestIndex = -1;

            foreach (var (candidateNode, candidateIndex, candidateProgress) in candidates)
            {
                // Find the next executable HL action for this cassette
                var actionGraph = candidateNode.GetActionGraph();
                if (actionGraph == null) continue;

                var executableActions = actionGraph.GetExecutableNodesInternal();
                if (executableActions.Count == 0) continue;

                // Check preconditions of the first executable HL action
                var nextAction = executableActions[0];
                bool preconditionsMet = nextAction.CheckPreconditions();

                if (preconditionsMet)
                {
                    LoggingService.LogInfo($"\u2696\ufe0f FairBranchProgress: Branch {candidateIndex + 1} " +
                        $"('{candidateNode.DebugDisplayName}') next action '{nextAction.InstanceName}' " +
                        $"preconditions MET \u2014 selecting this branch.");
                    bestCandidate = candidateNode;
                    bestIndex = candidateIndex;
                    break;
                }
                else
                {
                    LoggingService.LogInfo($"\u2696\ufe0f FairBranchProgress: Branch {candidateIndex + 1} " +
                        $"('{candidateNode.DebugDisplayName}') next action '{nextAction.InstanceName}' " +
                        $"preconditions NOT met \u2014 skipping to next candidate.");
                }
            }

            if (bestCandidate != null)
            {
                LinkedBlackboard.ChosenExecutingBranch = bestCandidate;
                _chosenBranchProgressAtLock = bestCandidate.Progress;
                LoggingService.LogSuccess($"⚖️ FairBranchProgress: Selected Branch {bestIndex + 1} " +
                    $"('{bestCandidate.DebugDisplayName}', progress: {BranchProgress[bestIndex]}) " +
                    $"as next executing branch (least progress, locked at progress={_chosenBranchProgressAtLock}). ExclusiveBranchGate will enforce lock.");
                
                ExecutionFlowLogger.LogDecoratorTick("FairBranchProgress", "BranchSelected",
                    bestCandidate.DebugDisplayName, $"Progress={BranchProgress[bestIndex]}");
            }
            else if (candidates.Count > 0)
            {
                // No branch has met preconditions — fall back to the first candidate
                // (least progress) to keep the gate closed. Without this, ChosenExecutingBranch
                // stays null and ExclusiveBranchGate allows ALL branches through simultaneously.
                var fallback = candidates[0];
                LinkedBlackboard.ChosenExecutingBranch = fallback.node;
                _chosenBranchProgressAtLock = fallback.node.Progress;
                LoggingService.LogInfo($"⚖️ FairBranchProgress: No branch has met preconditions. " +
                    $"Falling back to Branch {fallback.index + 1} ('{fallback.node.DebugDisplayName}') " +
                    $"to keep gate closed.");
            }
        }

        // Write progress to blackboard
        LinkedBlackboard.BranchProgress = BranchProgress;
        LinkedBlackboard.DeprioritizedBranchIndex = LastProgressBranchIndex;

        // Log overall progress summary
        string chosenName = LinkedBlackboard.ChosenExecutingBranch?.DebugDisplayName ?? "None";
        LoggingService.LogInfo($"📊 FairBranchProgress: Progress = " +
            $"[C1:{BranchProgress[0]}, C2:{BranchProgress[1]}, C3:{BranchProgress[2]}, C4:{BranchProgress[3]}] " +
            $"| Executing: {chosenName}");

        return true; // Always allow root to tick
    }

    /// <summary>
    /// Post-process: after all children have been ticked, clear the deprioritization
    /// if the other branches have caught up (i.e., all branches are within 1 action of each other).
    /// </summary>
    public override BTNodeResult PostProcessTickResult(BTNodeResult InResult)
    {
        if (LinkedBlackboard.PlanningPhase)
            return InResult;

        // Check if branches are balanced (all within 1 of each other)
        int minProgress = BranchProgress.Min();
        int maxProgress = BranchProgress.Max();

        if (maxProgress - minProgress <= 1)
        {
            // Branches are balanced — clear deprioritization
            if (LastProgressBranchIndex >= 0)
            {
                LoggingService.LogInfo($"⚖️ FairBranchProgress: Branches balanced (spread ≤ 1). Clearing deprioritization.");
                LastProgressBranchIndex = -1;
                LinkedBlackboard.DeprioritizedBranchIndex = -1;
            }
        }
        else
        {
            // Find the branch that is most ahead and deprioritize it
            int aheadBranch = Array.IndexOf(BranchProgress, maxProgress);
            if (aheadBranch != LastProgressBranchIndex)
            {
                LastProgressBranchIndex = aheadBranch;
                LinkedBlackboard.DeprioritizedBranchIndex = aheadBranch;
                LoggingService.LogInfo($"⚖️ FairBranchProgress: Branch {aheadBranch + 1} is ahead ({maxProgress} vs min {minProgress}). Deprioritizing it.");
            }
        }

        return InResult; // Don't change the root's result
    }

    /// <summary>
    /// Get a formatted progress report string for logging/debugging.
    /// </summary>
    public string GetProgressReport()
    {
        return $"Branch Progress: [C1:{BranchProgress[0]}, C2:{BranchProgress[1]}, " +
               $"C3:{BranchProgress[2]}, C4:{BranchProgress[3]}] | " +
               $"Deprioritized: {(LastProgressBranchIndex >= 0 ? $"Branch {LastProgressBranchIndex + 1}" : "None")}";
    }

    /// <summary>
    /// Maps an HL action type name to its end-effector group.
    /// Actions that share the same physical end-effector return the same group string.
    /// This enables cross-cassette tool batching — we keep the same tool equipped
    /// and service all cassettes that need it before switching tools.
    /// 
    /// Groups:
    ///   VacuumGripper (vg1): PickUpHL, PlaceHL, StackHL, StackOnMultipleHL
    ///   GlueGun (gg1):       GluingBeamHL, GluingPlateHL
    ///   Nailer (ng1):        NailingHL
    /// </summary>
    private static string GetEndEffectorGroup(string actionType)
    {
        return actionType switch
        {
            "PickUpHL" or "PlaceHL" or "StackHL" or "StackOnMultipleHL" => "VacuumGripper",
            "GluingBeamHL" or "GluingPlateHL" => "GlueGun",
            "NailingHL" => "Nailer",
            _ => actionType // Unknown type — treat as its own group
        };
    }
}
