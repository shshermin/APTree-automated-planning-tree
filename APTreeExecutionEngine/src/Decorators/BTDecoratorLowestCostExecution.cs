using BehaviorTreeMainProject.Services;
using System.Collections.Generic;
using System.Linq;
using BehaviorTreeMainProject.Log.Services;

/// <summary>
/// Decorator that evaluates costs and picks the cheapest branch to execute next.
/// 
/// It writes the chosen branch to the blackboard (ChosenExecutingBranch). 
/// The ExclusiveBranchGate decorator (evaluated first) enforces that only the chosen branch runs.
/// 
/// This decorator only clears the chosen branch when it reaches SUCCESS — not Failure.
/// This ensures a branch finishes all its ML children before another branch can be selected.
/// </summary>
public class BTDecoratorLowestCostExecution : BTDecoratorBase
{
    public override bool CanPostProcessTickResult => false;
    public override BTNodeResult PostProcessTickResult(BTNodeResult InResult) => InResult;
    public int lowestCost;

    public BTDecoratorLowestCostExecution(BTFlowNodeDynamic AttachedNode) : base(false)
    {
        this.AttachedNode = AttachedNode;
    }


    protected override bool OnEvaluate(float InDeltaTime)
    {
        var currentChosen = LinkedBlackboard.ChosenExecutingBranch;

        // ── Step 1: If a branch is chosen, check if it has SUCCEEDED ──
        if (currentChosen != null)
        {
            // Only release on Success — Failure means retry, not release
            if (currentChosen.status == BTNodeResult.Success)
            {
                LoggingService.LogInfo($"🔓 LowestCostDecorator: Chosen branch '{currentChosen.InstanceName}' reached SUCCESS. Clearing chosen branch for re-evaluation.");
                LinkedBlackboard.ChosenExecutingBranch = null;
                currentChosen = null;
            }
            else
            {
                // Branch is still executing (InProgress) or failed (will be retried) — keep it chosen
                LoggingService.LogInfo($"🔒 LowestCostDecorator: Chosen branch '{currentChosen.InstanceName}' still active (status: {currentChosen.status}). Keeping lock.");
            }
        }

        // ── Step 2: If no branch is currently chosen, evaluate costs and pick one ──
        if (currentChosen == null)
        {
            ChooseNextBranch();
        }

        // ── Step 3: After choosing, also gate here — only the chosen branch passes ──
        // This catches the case where ExclusiveBranchGate let us through because no branch 
        // was chosen yet, but LowestCost just picked a DIFFERENT branch than this one.
        var finalChosen = LinkedBlackboard.ChosenExecutingBranch;
        if (finalChosen != null && AttachedNode != finalChosen)
        {
            LoggingService.LogInfo($"⏳ LowestCostDecorator: '{AttachedNode.InstanceName}' is NOT the chosen branch (chosen: '{finalChosen.InstanceName}') — BLOCKING");
            return false;
        }

        LoggingService.LogSuccess($"✅ LowestCostDecorator: '{AttachedNode.InstanceName}' IS the chosen branch — ALLOW execution");
        return true;
    }

    /// <summary>
    /// Evaluates all non-finished subtrees, finds the lowest cost, and picks the FIRST one
    /// with that cost. Writes the chosen branch to the blackboard.
    /// </summary>
    private void ChooseNextBranch()
    {
        try
        {
            var injectedSubtrees = LinkedBlackboard.GetAllInjectedSubtrees();
            LoggingService.LogInfo($"🔍 ChooseNextBranch: Evaluating {injectedSubtrees.Count} injected subtrees");

            var candidates = new List<(BTFlowNodeDynamic subtree, string name, int cost)>();

            foreach (var subtree in injectedSubtrees)
            {
                var subtreeName = subtree.InstanceName.ToString();

                // Skip subtrees that have succeeded (completed their job)
                if (subtree.status == BTNodeResult.Success)
                {
                    LoggingService.LogInfo($"📋 ChooseNextBranch: Skipping succeeded subtree {subtreeName}");
                    continue;
                }

                var actionGraph = subtree.GetActionGraph();
                if (actionGraph != null)
                {
                    var actionCount = actionGraph.GetAllActionNodes().Count;
                    candidates.Add((subtree, subtreeName, actionCount));
                }
                else
                {
                    LoggingService.LogWarning($"⚠️ ChooseNextBranch: Subtree {subtreeName} has no action graph");
                    candidates.Add((subtree, subtreeName, 0));
                }
            }

            if (candidates.Count > 0)
            {
                // Find the lowest cost among candidates
                var lowestCost = candidates.Min(c => c.cost);

                // Log cost analysis
                LoggingService.LogInfo("📊 COST ANALYSIS SUMMARY:");
                LoggingService.LogInfo("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                foreach (var c in candidates.OrderBy(x => x.cost))
                {
                    var marker = c.cost == lowestCost ? "🏆 LOWEST" : "   ";
                    LoggingService.LogInfo($"   {marker} {c.name}: {c.cost} actions");
                }
                LoggingService.LogInfo("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

                // Pick the FIRST candidate with the lowest cost (deterministic ordering)
                var chosen = candidates.First(c => c.cost == lowestCost);

                // Write chosen branch to blackboard — ExclusiveBranchGate will enforce it
                LinkedBlackboard.ChosenExecutingBranch = chosen.subtree;

                // Update blackboard cost for reference
                LinkedBlackboard.LowestCost = lowestCost;
                this.lowestCost = lowestCost;

                LoggingService.LogSuccess($"🎯 CHOSEN BRANCH: '{chosen.name}' with cost {lowestCost} — set on blackboard, locked until SUCCESS");
                LoggingService.LogInfo("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            }
            else
            {
                LoggingService.LogWarning($"⚠️ ChooseNextBranch: No eligible candidates (all subtrees succeeded or none found)");
                LinkedBlackboard.LowestCost = 0;
                this.lowestCost = 0;
            }
        }
        catch (System.Exception ex)
        {
            LoggingService.LogError($"❌ ChooseNextBranch: Error: {ex.Message}");
            LinkedBlackboard.LowestCost = 0;
            this.lowestCost = 0;
        }
    }

    /// <summary>
    /// Static reset for when the tree is restarted or a new planning phase begins.
    /// Clears the blackboard chosen branch if a blackboard reference is available.
    /// </summary>
    public static void ResetChosenBranch()
    {
        // Static reset can't access blackboard, so this is a no-op now.
        // The blackboard's ChosenExecutingBranch should be cleared by whoever resets the tree.
        LoggingService.LogInfo($"🔄 LowestCostDecorator: ResetChosenBranch called (blackboard should be cleared separately)");
    }
}
