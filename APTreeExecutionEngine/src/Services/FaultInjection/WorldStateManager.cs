using System;
using System.Collections.Generic;
using System.Linq;
using BehaviorTreeMainProject.Log.Services;
using BehaviorTreeMainProject.ModelLoader;
using BehaviorTreeMainProject.Services.AIPlanning;
using ModelLoader;
using ModelLoader.ParameterTypes;
using ModelLoader.PredicateTypes;

namespace BehaviorTreeMainProject.Services.FaultInjection
{
    /// <summary>
    /// Single authority for mutating world state on the blackboard in response to
    /// sensor or camera events.
    ///
    /// Rules:
    ///  • Every sensor/camera that detects a world-state change calls a method here.
    ///  • Nothing else writes predicates, locations, or abort flags to the blackboard.
    ///  • Tree-walking (finding the active ML action, the owning flow node, etc.) is
    ///    the responsibility of the calling sensor service — WorldStateManager only
    ///    receives already-resolved objects and the raw effect parameters.
    /// </summary>
    public class WorldStateManager
    {
        private readonly Blackboard<FastName> _bb;

        public WorldStateManager(Blackboard<FastName> blackboard)
        {
            _bb = blackboard ?? throw new ArgumentNullException(nameof(blackboard));
        }

        // ─── Public fault handlers ────────────────────────────────────────

        /// <summary>
        /// Applies "object dropped" world-state changes and writes an abort flag
        /// to the blackboard for <see cref="DecoratorFaultAbort"/> on
        /// <paramref name="mlFlow"/> to trigger a replan at the end of the current tick.
        ///
        /// The caller (sensor service) is responsible for:
        ///  • identifying which ML action is active
        ///  • resolving <paramref name="mlFlow"/> via FindOwningMLFlowNode
        /// </summary>
        public void ApplyDrop(FaultEffects effects, DynamicFlowNode mlFlow)
        {
            string dropped = effects?.DroppedObject ?? "";
            string robot   = effects?.Robot          ?? "";
            string gripper = effects?.Gripper        ?? "";
            string tempLocName = string.IsNullOrWhiteSpace(effects?.TempLocationName)
                ? "temploc1" : effects.TempLocationName;
            string tempLocType = string.IsNullOrWhiteSpace(effects?.TempLocationPddlType)
                ? "firstposition" : effects.TempLocationPddlType;

            if (string.IsNullOrEmpty(dropped) || string.IsNullOrEmpty(robot) || string.IsNullOrEmpty(gripper))
            {
                LoggingService.LogError("🌍 WorldStateManager: DropAfterClose requires DroppedObject, Robot, Gripper");
                return;
            }

            var bb = _bb;

            // 1) Register TempLocation on blackboard
            var pos = effects?.TempLocationPosition ?? new double[] { 0.0, 0.0, 0.0 };
            double px = pos.Length > 0 ? pos[0] : 0.0;
            double py = pos.Length > 1 ? pos[1] : 0.0;
            double pz = pos.Length > 2 ? pos[2] : 0.0;

            var tempLoc = new InitialLocation(
                tempLocName,
                new Coordinate(px, py, pz),
                new Coordinate(0, 0, 0));
            bb.SetLocation(new FastName(tempLocName), tempLoc);
            LoggingService.LogSuccess($"🌍 WorldStateManager: Registered TempLocation '{tempLocName}' at ({px},{py},{pz})");

            // 2) Resolve Element references
            Element droppedElement = null;
            try { droppedElement = bb.GetElement(new FastName(dropped)); }
            catch { /* element may only be reachable via predicate search */ }

            // Capture the original source Location(s) of the dropped object BEFORE
            // negating its atplace predicates, so we can mark those locations as
            // positionfree.  Without this step the PDDL replan goal (which still
            // carries the HL action's effect "positionfree initlocstick4") is
            // unreachable because nothing sits at initlocstick4 anymore.
            var sourceLocations = new List<Location>();
            foreach (var p in bb.GetAllPredicates())
            {
                if (p is AtPlace ap
                    && !ap.not
                    && ap.obj?.NameKey?.ToString()?.Equals(dropped, StringComparison.OrdinalIgnoreCase) == true
                    && ap.objLoc != null)
                {
                    sourceLocations.Add(ap.objLoc);
                }
            }

            NegateAllPredicatesOfType(bb, "atplace", requiredParam0: dropped);

            // 3b) For every source location mark positionfree = true
            foreach (var srcLoc in sourceLocations)
            {
                string srcLocName = srcLoc.NameKey?.ToString() ?? "";
                if (string.IsNullOrEmpty(srcLocName)) continue;

                if (!FlipPredicate(bb, "positionfree", new[] { srcLocName }, newNot: false))
                {
                    var pf = new PositionFree(srcLoc, isNegated: false);
                    bb.SetPredicateSync(pf.GetUniqueKey(), pf);
                    LoggingService.LogSuccess(
                        $"🌍 WorldStateManager: Added predicate positionfree({srcLocName}) = true");
                }
            }

            // 4) Add new atplace(dropped, temploc) = true
            if (droppedElement != null)
            {
                var newAtPlace = new AtPlace(droppedElement, tempLoc, isNegated: false);
                bb.SetPredicateSync(newAtPlace.GetUniqueKey(), newAtPlace);
                LoggingService.LogSuccess($"🌍 WorldStateManager: Added predicate atplace({dropped}, {tempLocName}) = true");
            }
            else
            {
                LoggingService.LogWarning($"🌍 WorldStateManager: Could not resolve Element '{dropped}' — atplace predicate NOT added");
            }

            // 4b) Move the robot: flip atagent(<robot>, <from>) → false,
            //     assert atagent(<robot>, <to>) → true.
            string fromLoc = effects?.RobotFromLocation ?? "";
            string toLoc   = effects?.RobotToLocation   ?? "";
            if (!string.IsNullOrEmpty(fromLoc))
                FlipPredicate(bb, "atagent", new[] { robot, fromLoc }, newNot: true);
            if (!string.IsNullOrEmpty(toLoc))
            {
                if (!FlipPredicate(bb, "atagent", new[] { robot, toLoc }, newNot: false))
                {
                    Agent    robotAgent = null;
                    Location toLocObj   = null;
                    try { robotAgent = bb.GetAgent(new FastName(robot)); }    catch { }
                    try { toLocObj   = bb.GetLocation(new FastName(toLoc)); } catch { }
                    if (robotAgent != null && toLocObj != null)
                    {
                        var aa = new AtAgent(robotAgent, toLocObj, isNegated: false);
                        bb.SetPredicateSync(aa.GetUniqueKey(), aa);
                        LoggingService.LogSuccess(
                            $"🌍 WorldStateManager: Added predicate atagent({robot}, {toLoc}) = true");
                    }
                    else
                    {
                        LoggingService.LogWarning(
                            $"🌍 WorldStateManager: Could not resolve Agent '{robot}' or Location '{toLoc}' — atagent predicate NOT added");
                    }
                }
            }

            // 5) Register temp location as a runtime PDDL :object on the owning
            //    ML flow node's DecoratorExtraPDDLObjects so the replan pipeline
            //    can include it in the (:objects) block.
            if (mlFlow != null)
            {
                var extras = ServicePDDLPlanning.GetOrCreateExtraObjectsDecorator(mlFlow);
                extras?.AddObject(tempLocName, tempLocType);
                LoggingService.LogSuccess(
                    $"🌍 WorldStateManager: Registered PDDL object '{tempLocName} - {tempLocType}' on {mlFlow.DebugDisplayName}");
            }
            else
            {
                LoggingService.LogWarning("🌍 WorldStateManager: No ML FlowNode — cannot register extra PDDL object");
            }

            // 6) Write abort flag + fault timestamp → DecoratorFaultAbort reads these
            //    at end-of-tick, resets the planner, and returns InProgress instead of Failure.
            //    The timestamp is used to compute Recovery Time in the decorator's resume log.
            if (mlFlow != null)
            {
                var tFault = DateTime.Now;
                var abortKey = BlackboardKeys.AbortKey(mlFlow.DebugDisplayName);
                var tsKey    = BlackboardKeys.FaultTimestampKey(mlFlow.DebugDisplayName);
                _bb.SetBool(abortKey, true);
                _bb.SetString(tsKey, tFault.Ticks.ToString());
                LoggingService.LogWarning(
                    $"🌍 WorldStateManager: Wrote abort flag '{abortKey}' for '{mlFlow.DebugDisplayName}'");
                LoggingService.LogWarning(
                    $"📋 FAULT_METRIC | t_fault={tFault:HH:mm:ss.fff} | node={mlFlow.DebugDisplayName} | type=Execution");
            }
            else
                LoggingService.LogWarning("🌍 WorldStateManager: No ML FlowNode — abort flag not written");
        }

        /// <summary>
        /// Applies "blocker placed on top of target" world-state changes.
        ///
        /// No abort flag is written — this fault fires during an earlier HL action
        /// so the target HL action picks up the updated blackboard state naturally
        /// when it plans for the first time.
        ///
        /// The caller (sensor service) is responsible for resolving
        /// <paramref name="targetHlFlow"/> via FindHLSubtreeForPickUpOfTarget.
        /// </summary>
        public void ApplyBlocker(FaultEffects effects, DynamicFlowNode targetHlFlow)
        {
            string target         = effects?.TargetObject    ?? "";
            string blocker        = effects?.BlockerObject   ?? "";
            string blockerLocName = effects?.BlockerAtLocation ?? "";
            string blockerPddlType = string.IsNullOrWhiteSpace(effects?.BlockerPddlType)
                ? "stick" : effects.BlockerPddlType;
            string stagingLocName = string.IsNullOrWhiteSpace(effects?.TempLocationName)
                ? "temploc2" : effects.TempLocationName;
            string stagingLocType = string.IsNullOrWhiteSpace(effects?.TempLocationPddlType)
                ? "firstposition" : effects.TempLocationPddlType;

            if (string.IsNullOrEmpty(target) || string.IsNullOrEmpty(blocker) || string.IsNullOrEmpty(blockerLocName))
            {
                LoggingService.LogError(
                    "🌍 WorldStateManager: BlockerOnTop requires TargetObject, BlockerObject, BlockerAtLocation");
                return;
            }

            // Record t_fault for Safety faults. Written to the target HL flow node's
            // timestamp key so ServicePDDLPlanning can compute Recovery Time when it
            // generates the plan that handles this blocker.
            var tFaultBlocker = DateTime.Now;
            string targetFlowName = targetHlFlow?.DebugDisplayName ?? target;
            _bb.SetString(BlackboardKeys.FaultTimestampKey(targetFlowName), tFaultBlocker.Ticks.ToString());
            LoggingService.LogWarning(
                $"📋 FAULT_METRIC | t_fault={tFaultBlocker:HH:mm:ss.fff} | node={targetFlowName} | type=Safety");

            var bb = _bb;

            // Resolve Element / Location references
            Element  targetEl   = null;
            Element  blockerEl  = null;
            Location blockerLoc = null;
            try { targetEl   = bb.GetElement(new FastName(target));          } catch { }
            try { blockerEl  = bb.GetElement(new FastName(blocker));         } catch { }
            try { blockerLoc = bb.GetLocation(new FastName(blockerLocName)); } catch { }

            if (targetEl == null || blockerLoc == null)
            {
                LoggingService.LogError(
                    $"🌍 WorldStateManager: Failed to resolve references — " +
                    $"target={target} ({(targetEl  != null ? "ok" : "MISSING")}), " +
                    $"blockerLoc={blockerLocName} ({(blockerLoc != null ? "ok" : "MISSING")})");
                return;
            }

            // If the blocker element doesn't exist create it dynamically.
            if (blockerEl == null)
            {
                blockerEl = new Stick(blocker, null, null);
                bb.SetElement(new FastName(blocker), blockerEl);
                LoggingService.LogSuccess(
                    $"🌍 WorldStateManager: Dynamically created blocker element '{blocker}' on blackboard");
            }

            // 1) Negate any pre-existing atplace(blocker, *) then assert atplace(blocker, blockerLoc)
            NegateAllPredicatesOfType(bb, "atplace", requiredParam0: blocker);
            var atPlaceBlocker = new AtPlace(blockerEl, blockerLoc, isNegated: false);
            bb.SetPredicateSync(atPlaceBlocker.GetUniqueKey(), atPlaceBlocker);
            LoggingService.LogSuccess(
                $"🌍 WorldStateManager: Set atplace({blocker}, {blockerLocName}) = true (prior atplaces negated)");

            // 2) stacked(blocker, target) = true
            if (!FlipPredicate(bb, "stacked", new[] { blocker, target }, newNot: false))
            {
                var s = new Stacked(blockerEl, targetEl, isNegated: false);
                bb.SetPredicateSync(s.GetUniqueKey(), s);
                LoggingService.LogSuccess(
                    $"🌍 WorldStateManager: Added stacked({blocker}, {target}) = true");
            }

            // 3) Target is blocked: clear → false, accessible → false
            if (!FlipPredicate(bb, "clear",       new[] { target }, newNot: true))
            {
                var c = new Clear(targetEl, isNegated: true);
                bb.SetPredicateSync(c.GetUniqueKey(), c);
            }
            if (!FlipPredicate(bb, "accessible",  new[] { target }, newNot: true))
            {
                var a = new Accessible(targetEl, isNegated: true);
                bb.SetPredicateSync(a.GetUniqueKey(), a);
            }

            // 4) Blocker is pickable: clear → true, accessible → true
            if (!FlipPredicate(bb, "clear",       new[] { blocker }, newNot: false))
            {
                var c = new Clear(blockerEl, isNegated: false);
                bb.SetPredicateSync(c.GetUniqueKey(), c);
            }
            if (!FlipPredicate(bb, "accessible",  new[] { blocker }, newNot: false))
            {
                var a = new Accessible(blockerEl, isNegated: false);
                bb.SetPredicateSync(a.GetUniqueKey(), a);
            }

            // 5) Blocker occupies its location → positionfree = false
            if (!FlipPredicate(bb, "positionfree", new[] { blockerLocName }, newNot: true))
            {
                var pf = new PositionFree(blockerLoc, isNegated: true);
                bb.SetPredicateSync(pf.GetUniqueKey(), pf);
                LoggingService.LogSuccess(
                    $"🌍 WorldStateManager: Set positionfree({blockerLocName}) = false (blocker occupies it)");
            }

            // 5b) Block competing staging candidates
            if (effects?.BlockPositionfreeLocations != null)
            {
                foreach (var locName in effects.BlockPositionfreeLocations)
                {
                    if (string.IsNullOrWhiteSpace(locName)) continue;
                    if (!FlipPredicate(bb, "positionfree", new[] { locName }, newNot: true))
                    {
                        Location loc = null;
                        try { loc = bb.GetLocation(new FastName(locName)); } catch { }
                        if (loc != null)
                        {
                            var pf = new PositionFree(loc, isNegated: true);
                            bb.SetPredicateSync(pf.GetUniqueKey(), pf);
                        }
                    }
                    LoggingService.LogSuccess(
                        $"🌍 WorldStateManager: Set positionfree({locName}) = false (blocked as staging candidate)");
                }
            }

            // 5c) Staging location must be positionfree = true
            Location stagingLoc = null;
            try { stagingLoc = bb.GetLocation(new FastName(stagingLocName)); } catch { }
            if (!FlipPredicate(bb, "positionfree", new[] { stagingLocName }, newNot: false))
            {
                if (stagingLoc != null)
                {
                    var pf = new PositionFree(stagingLoc, isNegated: false);
                    bb.SetPredicateSync(pf.GetUniqueKey(), pf);
                    LoggingService.LogSuccess(
                        $"🌍 WorldStateManager: Set positionfree({stagingLocName}) = true (staging destination)");
                }
                else
                {
                    LoggingService.LogWarning(
                        $"🌍 WorldStateManager: Staging location '{stagingLocName}' not found on blackboard");
                }
            }

            // 6) Register extra PDDL :objects on the target HL flow node
            if (targetHlFlow != null)
            {
                var extras = ServicePDDLPlanning.GetOrCreateExtraObjectsDecorator(targetHlFlow);
                extras?.AddObject(blockerLocName, "firstposition");
                LoggingService.LogSuccess(
                    $"🌍 WorldStateManager: Registered PDDL object '{blockerLocName} - firstposition' on {targetHlFlow.DebugDisplayName}");
                extras?.AddObject(stagingLocName, stagingLocType);
                LoggingService.LogSuccess(
                    $"🌍 WorldStateManager: Registered PDDL object '{stagingLocName} - {stagingLocType}' on {targetHlFlow.DebugDisplayName}");
                extras?.AddObject(blocker, blockerPddlType);
                LoggingService.LogSuccess(
                    $"🌍 WorldStateManager: Registered PDDL object '{blocker} - {blockerPddlType}' on {targetHlFlow.DebugDisplayName}");
            }
            else
            {
                // FlowNode hasn't ticked yet — queue via the pending registry
                LoggingService.LogWarning(
                    $"🌍 WorldStateManager: PickUpHL FlowNode for '{target}' not found yet — queuing extra PDDL objects as pending");
                ServicePDDLPlanning.RegisterPendingExtraObject(target, blockerLocName, "firstposition");
                ServicePDDLPlanning.RegisterPendingExtraObject(target, stagingLocName, stagingLocType);
                ServicePDDLPlanning.RegisterPendingExtraObject(target, blocker, blockerPddlType);
            }

            // 6b) Move the robot: flip atagent(<robot>, <from>) → false,
            //     assert atagent(<robot>, <to>) → true (avoids redundant TravelML in replan).
            string robotName  = effects?.Robot         ?? "robot1";
            string agentFrom  = effects?.RobotFromLocation ?? "";
            string agentTo    = effects?.RobotToLocation   ?? "";
            if (!string.IsNullOrEmpty(agentFrom) && !string.IsNullOrEmpty(agentTo))
            {
                FlipPredicate(bb, "atagent", new[] { robotName, agentFrom }, newNot: true);
                if (!FlipPredicate(bb, "atagent", new[] { robotName, agentTo }, newNot: false))
                {
                    Agent    robotAgent = null;
                    Location toLocObj   = null;
                    try { robotAgent = bb.GetAgent(new FastName(robotName)); } catch { }
                    try { toLocObj   = bb.GetLocation(new FastName(agentTo));  } catch { }
                    if (robotAgent != null && toLocObj != null)
                    {
                        var aa = new AtAgent(robotAgent, toLocObj, isNegated: false);
                        bb.SetPredicateSync(aa.GetUniqueKey(), aa);
                        LoggingService.LogSuccess(
                            $"🌍 WorldStateManager: Added predicate atagent({robotName}, {agentTo}) = true");
                    }
                    else
                    {
                        LoggingService.LogWarning(
                            $"🌍 WorldStateManager: Could not resolve Agent '{robotName}' or Location '{agentTo}' — atagent predicate NOT added");
                    }
                }
            }

            // 7) Write abort flag + fault timestamp so DecoratorFaultAbort on the ML DFN
            //    triggers a replan mid-execution (same two-phase mechanism as DropAfterClose).
            if (targetHlFlow != null)
            {
                var abortKey = BlackboardKeys.AbortKey(targetHlFlow.DebugDisplayName);
                _bb.SetBool(abortKey, true);
                LoggingService.LogWarning(
                    $"🌍 WorldStateManager: Wrote abort flag '{abortKey}' for '{targetHlFlow.DebugDisplayName}'");
            }
            else
            {
                LoggingService.LogWarning("🌍 WorldStateManager: targetHlFlow is null — abort flag NOT written");
            }
        }

        /// <summary>
        /// Applies "stacked element dislodged" world-state changes: undoes the effects
        /// of a previously completed <c>stackHL</c> or <c>stackOnTwoHL</c> on the blackboard,
        /// then writes the HL-replan flag and an abort flag on <paramref name="activeDfn"/>
        /// so <see cref="DecoratorFaultAbort"/> + <see cref="BehaviorTreeMainProject.Decorators.Replan.DecoratorHLFaultReplan"/>
        /// trigger a full HL replan on the next planning tick.
        ///
        /// :objects and :goal in the static problem file are preserved.
        /// Only (:init …) is rebuilt from the updated blackboard.
        /// </summary>
        public void ApplyDislodge(FaultEffects effects, DynamicFlowNode activeDfn)
        {
            string dislodged    = effects?.DislodgedObject    ?? "";
            string returnLoc    = effects?.ReturnToLocation   ?? "";
            string baseObj      = effects?.BaseObject         ?? "";

            if (string.IsNullOrEmpty(dislodged) || string.IsNullOrEmpty(returnLoc))
            {
                LoggingService.LogError("🌍 WorldStateManager: DislodgedAfterStack requires DislodgedObject and ReturnToLocation");
                return;
            }

            var bb = _bb;

            // 1) Resolve the dislodged element
            Element dislodgedEl = null;
            try { dislodgedEl = bb.GetElement(new FastName(dislodged)); } catch { }

            // 2) Resolve the return location (must already be in the static problem)
            Location returnLocObj = null;
            try { returnLocObj = bb.GetLocation(new FastName(returnLoc)); } catch { }

            if (dislodgedEl == null)
            {
                LoggingService.LogError($"🌍 WorldStateManager: Cannot resolve element '{dislodged}' for dislodge fault");
                return;
            }
            if (returnLocObj == null)
            {
                LoggingService.LogError($"🌍 WorldStateManager: Cannot resolve location '{returnLoc}' for dislodge fault");
                return;
            }

            // 3) Undo atfinalposition(dislodged)
            FlipPredicate(bb, "atfinalposition", new[] { dislodged }, newNot: true);

            // 4) Undo atplace(dislodged, finalpos) — negate all, then assert atplace(dislodged, returnLoc)
            //    Also free the final position.
            var finalPositions = new List<Location>();
            foreach (var p in bb.GetAllPredicates())
            {
                if (p is AtPlace ap
                    && !ap.not
                    && ap.obj?.NameKey?.ToString()?.Equals(dislodged, StringComparison.OrdinalIgnoreCase) == true
                    && ap.objLoc != null)
                {
                    finalPositions.Add(ap.objLoc);
                }
            }
            NegateAllPredicatesOfType(bb, "atplace", requiredParam0: dislodged);

            foreach (var fp in finalPositions)
            {
                string fpName = fp.NameKey?.ToString() ?? "";
                if (!string.IsNullOrEmpty(fpName))
                    FlipPredicate(bb, "positionfree", new[] { fpName }, newNot: false);
            }

            // 5) Assert atplace(dislodged, returnLoc) = true
            var atPlaceReturn = new AtPlace(dislodgedEl, returnLocObj, isNegated: false);
            bb.SetPredicateSync(atPlaceReturn.GetUniqueKey(), atPlaceReturn);
            // The return location is now occupied → positionfree = false
            FlipPredicate(bb, "positionfree", new[] { returnLoc }, newNot: true);
            LoggingService.LogSuccess($"🌍 WorldStateManager: Set atplace({dislodged}, {returnLoc}) = true, positionfree({returnLoc}) = false");

            // 6) Undo stacked(dislodged, *) and restore accessibility of base(s)
            NegateAllPredicatesOfType(bb, "stacked", requiredParam0: dislodged);
            if (!string.IsNullOrEmpty(baseObj))
            {
                FlipPredicate(bb, "accessible", new[] { baseObj }, newNot: false);
                FlipPredicate(bb, "clear",       new[] { baseObj }, newNot: false);
            }

            // 7) Dislodged element is no longer accessible at final position
            FlipPredicate(bb, "accessible", new[] { dislodged }, newNot: true);
            FlipPredicate(bb, "clear",       new[] { dislodged }, newNot: false); // clear on table = true

            // 8) Write HL-replan flag + fault timestamp on the active DFN.
            //    DecoratorHLFaultAbort reads hl_replan_<dfn> to intercept the next tick
            //    result and call ResetForNextRound, which puts the DFN back into planning.
            //    DecoratorHLFaultReplan then reads the same flag in the planning pipeline
            //    to fully rebuild (:init) from the blackboard.
            //    NOTE: abort_<dfn> is intentionally NOT written here — DecoratorFaultAbort
            //    (which handles ML-level faults) must not be triggered for HL faults.
            if (activeDfn != null)
            {
                var tFault = DateTime.Now;
                var hlKey  = BlackboardKeys.HLReplanKey(activeDfn.DebugDisplayName);
                var tsKey  = BlackboardKeys.FaultTimestampKey(activeDfn.DebugDisplayName);
                _bb.SetBool(hlKey, true);
                _bb.SetString(tsKey, tFault.Ticks.ToString());
                LoggingService.LogWarning(
                    $"🌍 WorldStateManager: Wrote HL-replan flag '{hlKey}' for '{activeDfn.DebugDisplayName}'");
                LoggingService.LogWarning(
                    $"📋 FAULT_METRIC | t_fault={tFault:HH:mm:ss.fff} | node={activeDfn.DebugDisplayName} | type=DislodgedAfterStack");
            }
            else
            {
                LoggingService.LogWarning("🌍 WorldStateManager: No active DFN — HL-replan flag NOT written");
            }
        }

        // ─── Blackboard helpers ───────────────────────────────────────────

        private static bool FlipPredicate(Blackboard<FastName> bb, string predType,
            string[] paramValues, bool newNot)
        {
            var match = bb.GetAllPredicates().FirstOrDefault(p =>
                p.GetPredicateType().Equals(predType, StringComparison.OrdinalIgnoreCase) &&
                p.GetPDDLParameterValues()
                    .Select(v => (v ?? "").ToLowerInvariant())
                    .SequenceEqual(paramValues.Select(v => (v ?? "").ToLowerInvariant())));

            if (match != null)
            {
                bool oldNot = match.not;
                match.not = newNot;
                LoggingService.LogSuccess(
                    $"🌍 WorldStateManager: Flipped {predType}({string.Join(",", paramValues)}) not: {oldNot} → {newNot}");
                return true;
            }

            LoggingService.LogWarning(
                $"🌍 WorldStateManager: No existing predicate {predType}({string.Join(",", paramValues)}) to flip");
            return false;
        }

        private static void NegateAllPredicatesOfType(Blackboard<FastName> bb, string predType,
            string requiredParam0)
        {
            int count = 0;
            foreach (var p in bb.GetAllPredicates())
            {
                if (!p.GetPredicateType().Equals(predType, StringComparison.OrdinalIgnoreCase)) continue;
                var vals = p.GetPDDLParameterValues();
                if (vals == null || vals.Count == 0) continue;
                if (!string.Equals(vals[0], requiredParam0, StringComparison.OrdinalIgnoreCase)) continue;
                if (p.not) continue; // already negated
                p.not = true;
                count++;
            }
            if (count > 0)
                LoggingService.LogSuccess(
                    $"🌍 WorldStateManager: Negated {count} existing {predType}({requiredParam0},*) predicate(s)");
        }
    }
}
