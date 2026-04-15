using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BehaviorTreeMainProject.Services;
using BehaviorTreeMainProject.Services.AIPlanning;
using ModelLoader;
using ModelLoader.ParameterTypes;
using BehaviorTreeMainProject.Log.Services;

namespace BehaviorTreeMainProject.Services
{
    /// <summary>
    /// Handles runtime commands during paused execution.
    /// Supports listing/modifying predicates, locations, nail coordinates,
    /// and triggering retry/replan on active actions.
    /// </summary>
    public class RuntimeCommandHandler
    {
        private readonly BehaviorTree _behaviorTree;
        private Blackboard<FastName> Blackboard => _behaviorTree.linkedBlackboard;

        public RuntimeCommandHandler(BehaviorTree behaviorTree)
        {
            _behaviorTree = behaviorTree;
        }

        /// <summary>
        /// Enters the interactive command loop. Returns when the user types "resume" or "quit".
        /// </summary>
        /// <returns>
        /// CommandResult.Resume  — caller should continue ticking.
        /// CommandResult.Quit    — caller should stop execution entirely.
        /// </returns>
        public CommandResult EnterCommandLoop()
        {
            LoggingService.LogWarning("⏸️ PAUSED — entering command mode");
            PrintHelp();

            while (true)
            {
                Console.Write("bb> ");
                string input = Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(input)) continue;

                var result = ProcessCommand(input);
                if (result != null)
                    return result.Value;
            }
        }

        /// <summary>
        /// Processes a single command string.
        /// Returns null if the loop should continue, or a CommandResult to exit.
        /// </summary>
        private CommandResult? ProcessCommand(string input)
        {
            if (input.Equals("resume", StringComparison.OrdinalIgnoreCase))
            {
                LoggingService.LogSuccess("▶️ RESUMED");
                return CommandResult.Resume;
            }

            if (input.Equals("quit", StringComparison.OrdinalIgnoreCase))
            {
                LoggingService.LogWarning("Execution stopped by user (quit)");
                return CommandResult.Quit;
            }

            if (input.Equals("help", StringComparison.OrdinalIgnoreCase))
            {
                PrintHelp();
            }
            else if (input.Equals("list", StringComparison.OrdinalIgnoreCase))
            {
                HandleListPredicates(trueOnly: true);
            }
            else if (input.Equals("list all", StringComparison.OrdinalIgnoreCase))
            {
                HandleListPredicates(trueOnly: false);
            }
            else if (input.StartsWith("setpos ", StringComparison.OrdinalIgnoreCase))
            {
                HandleSetPos(input);
            }
            else if (input.StartsWith("setnail ", StringComparison.OrdinalIgnoreCase))
            {
                HandleSetNail(input);
            }
            else if (input.StartsWith("listloc", StringComparison.OrdinalIgnoreCase))
            {
                HandleListLoc(input);
            }
            else if (input.StartsWith("set ", StringComparison.OrdinalIgnoreCase))
            {
                HandleSetPredicate(input);
            }
            else if (input.Equals("retry", StringComparison.OrdinalIgnoreCase))
            {
                if (HandleRetry())
                    return CommandResult.Resume;
            }
            else if (input.Equals("replan", StringComparison.OrdinalIgnoreCase))
            {
                if (HandleReplan())
                    return CommandResult.Resume;
            }
            else
            {
                LoggingService.LogWarning($"Unknown command: {input}");
                PrintHelp();
            }

            return null;
        }

        private void PrintHelp()
        {
            LoggingService.LogInfo("Commands: list | list all | set <type> <p1> ... <true|false>");
            LoggingService.LogInfo("         listloc [filter] | setpos <loc> <x> <y> <z> | setnail <obj1> <obj2> <x> <y> <z>");
            LoggingService.LogInfo("         retry | replan | help | resume | quit");
        }

        // ─── Predicate commands ──────────────────────────────────────────

        private void HandleListPredicates(bool trueOnly)
        {
            var predicates = trueOnly
                ? Blackboard.GetTruePredicates()
                : Blackboard.GetAllPredicates();

            string label = trueOnly ? "True" : "All";
            LoggingService.LogInfo($"--- {label} predicates ({predicates.Count}) ---");
            foreach (var p in predicates.OrderBy(p => p.GetPredicateType()))
            {
                string suffix = (!trueOnly && p.not) ? " [NEGATED]" : "";
                LoggingService.LogInfo($"  {p.GetPredicateType()} {string.Join(" ", p.GetPDDLParameterValues())}{suffix}");
            }
        }

        private void HandleSetPredicate(string input)
        {
            var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3)
            {
                LoggingService.LogWarning("Usage: set <predicateType> <param1> [param2] ... <true|false>");
                return;
            }

            string boolStr = parts[^1];
            if (!boolStr.Equals("true", StringComparison.OrdinalIgnoreCase) &&
                !boolStr.Equals("false", StringComparison.OrdinalIgnoreCase))
            {
                LoggingService.LogWarning("Last argument must be 'true' or 'false'");
                return;
            }

            bool setTrue = boolStr.Equals("true", StringComparison.OrdinalIgnoreCase);
            string predType = parts[1].ToLower();
            var paramValues = parts[2..^1];

            string keyStr = predType + "_" + string.Join("_", paramValues);

            var allPredicates = Blackboard.GetAllPredicates();
            var match = allPredicates.FirstOrDefault(p =>
                p.GetPredicateType().Equals(predType, StringComparison.OrdinalIgnoreCase) &&
                p.GetPDDLParameterValues().Select(v => v.ToLower()).SequenceEqual(
                    paramValues.Select(v => v.ToLower())));

            if (match != null)
            {
                bool oldNot = match.not;
                match.not = !setTrue;
                LoggingService.LogSuccess($"✅ Updated: {predType} {string.Join(" ", paramValues)} — not: {oldNot} → {match.not}");
            }
            else
            {
                LoggingService.LogWarning($"Predicate not found: {keyStr}");
                LoggingService.LogInfo("Tip: use 'list all' to see available predicates and their exact parameter names");
            }
        }

        // ─── Location commands ───────────────────────────────────────────

        private void HandleListLoc(string input)
        {
            var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string filter = parts.Length > 1 ? parts[1].ToLower() : null;

            var locations = Blackboard.GetAllLocations();
            int shown = 0;
            foreach (var loc in locations.OrderBy(l => l.NameKey?.ToString() ?? ""))
            {
                string name = loc.NameKey?.ToString() ?? "(unnamed)";
                if (filter != null && !name.ToLower().Contains(filter))
                    continue;

                if (loc is FinalLocation fl)
                {
                    string pos = fl.Position != null ? $"({fl.Position.X}, {fl.Position.Y}, {fl.Position.Z})" : "(none)";
                    string ori = fl.Orientation != null ? $"({fl.Orientation.X}, {fl.Orientation.Y}, {fl.Orientation.Z})" : "(none)";
                    LoggingService.LogInfo($"  📍 {name}: pos={pos}  ori={ori}");
                }
                else
                {
                    LoggingService.LogInfo($"  📍 {name}: {loc.GetType().Name}");
                }
                shown++;
            }
            LoggingService.LogInfo($"--- {shown} location(s) shown ---");
        }

        private void HandleSetPos(string input)
        {
            var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 5)
            {
                LoggingService.LogWarning("Usage: setpos <locationName> <x> <y> <z>");
                return;
            }

            string locName = parts[1].ToLower();
            if (!TryParseXYZ(parts[2], parts[3], parts[4], out double x, out double y, out double z))
            {
                LoggingService.LogWarning("Invalid coordinates. Use decimal numbers, e.g.: setpos finallocstick88 0.360 0.835 0.460");
                return;
            }

            var fl = Blackboard.GetFinalLocationByName(locName);
            if (fl == null)
            {
                LoggingService.LogWarning($"FinalLocation '{locName}' not found on blackboard");
                LoggingService.LogInfo("Tip: use 'listloc' to see available locations");
                return;
            }

            var oldPos = fl.Position != null ? $"({fl.Position.X}, {fl.Position.Y}, {fl.Position.Z})" : "(none)";
            fl.Position = new Coordinate(x, y, z);
            LoggingService.LogSuccess($"✅ Updated {locName} position: {oldPos} → ({x}, {y}, {z})");
        }

        // ─── Nail coordinate commands ────────────────────────────────────

        private void HandleSetNail(string input)
        {
            var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 6)
            {
                LoggingService.LogWarning("Usage: setnail <obj1> <obj2> <x> <y> <z>");
                return;
            }

            string obj1 = parts[1].ToLower();
            string obj2 = parts[2].ToLower();
            if (!TryParseXYZ(parts[3], parts[4], parts[5], out double x, out double y, out double z))
            {
                LoggingService.LogWarning("Invalid coordinates. Use decimal numbers, e.g.: setnail stick88 stick87 0.360 0.835 0.460");
                return;
            }

            var newCoord = new Coordinate(x, y, z);

            // Find the matching NailLocation from goal state Nailed predicates and update its position
            string oldStr = "(none)";
            var goalPredicates = Blackboard.GetGoalStatePredicates();
            var matchingNailed = goalPredicates
                .OfType<ModelLoader.PredicateTypes.Nailed>()
                .FirstOrDefault(p =>
                {
                    string p1 = p.obj1?.NameKey?.ToString()?.ToLower() ?? "";
                    string p2 = p.obj2?.NameKey?.ToString()?.ToLower() ?? "";
                    return (p1 == obj1 && p2 == obj2) || (p1 == obj2 && p2 == obj1);
                });

            if (matchingNailed?.nailloc is ModelLoader.ParameterTypes.NailLocation nailLoc)
            {
                if (nailLoc.Position != null)
                    oldStr = $"({nailLoc.Position.X}, {nailLoc.Position.Y}, {nailLoc.Position.Z})";
                nailLoc.Position = newCoord;
            }
            LoggingService.LogSuccess($"✅ Updated nail coordinate ({obj1}, {obj2}): {oldStr} → ({x}, {y}, {z})");

            // Also update any live NailingML action instances that match
            int liveUpdated = 0;
            var allActions = Blackboard.GetAllActionInstances();
            foreach (var action in allActions)
            {
                if (action is NailingML nailing)
                {
                    string a1 = nailing.obj1?.NameKey?.ToString()?.ToLower() ?? "";
                    string a2 = nailing.obj2?.NameKey?.ToString()?.ToLower() ?? "";
                    if ((a1 == obj1 && a2 == obj2) || (a1 == obj2 && a2 == obj1))
                    {
                        if (nailing.nailloc != null)
                            nailing.nailloc.Position = newCoord;
                        liveUpdated++;
                    }
                }
            }
            if (liveUpdated > 0)
                LoggingService.LogSuccess($"   Also updated {liveUpdated} live NailingML action instance(s)");
        }

        // ─── Retry / Replan commands ─────────────────────────────────────

        private bool HandleRetry()
        {
            var mlAction = FindActiveMLAction();
            if (mlAction == null)
            {
                LoggingService.LogWarning("No active ML action found to retry");
                return false;
            }

            var actionName = mlAction.InstanceName.ToString();
            LoggingService.LogInfo($"🔄 RETRY: Resetting LL subtree for '{actionName}'");

            var llFlowNode = mlAction.HighLevelSubtree as DynamicFlowNode;
            var graph = llFlowNode.GetActionGraph();
            graph.ResetAllNodeStatuses();
            llFlowNode.ResetForRetry();
            mlAction.Reset();

            EndToEndSummaryLogger.LogRecovery();
            LoggingService.LogSuccess($"✅ RETRY complete for '{actionName}'");
            LoggingService.LogSuccess("▶️ RESUMED — will retry on next tick");
            return true;
        }

        private bool HandleReplan()
        {
            var mlAction = FindActiveMLAction();
            if (mlAction == null)
            {
                LoggingService.LogWarning("No active ML action found to replan");
                return false;
            }

            var actionName = mlAction.InstanceName.ToString();
            LoggingService.LogInfo($"🔄 REPLAN: Resetting ML-level planning for '{actionName}'");

            var current = mlAction.ParentNode;
            DynamicFlowNode mlFlowNode = null;
            while (current != null)
            {
                if (current is DynamicFlowNode dfn) { mlFlowNode = dfn; break; }
                current = current.ParentNode;
            }

            if (mlFlowNode == null)
            {
                LoggingService.LogError($"Could not find parent ML FlowNode for '{actionName}'");
                return false;
            }

            if (mlFlowNode.ServicePlanning is ServicePlanning plannerService)
                plannerService.ResetPlanningService();

            mlFlowNode.ResetForNextRound();

            EndToEndSummaryLogger.LogReplan();
            LoggingService.LogSuccess($"✅ REPLAN complete for '{actionName}'");
            LoggingService.LogSuccess("▶️ RESUMED — will replan on next tick");
            return true;
        }

        private PActionNode FindActiveMLAction()
        {
            var compositeRoot = _behaviorTree.root as BTFlowNodeComposite;
            if (compositeRoot == null) return null;

            var children = compositeRoot.GetChildren();
            foreach (var child in children)
            {
                if (child is DynamicFlowNode dfn)
                {
                    var graph = dfn.GetActionGraph();
                    foreach (var action in graph.GetAllActionNodes())
                    {
                        if (action is PActionNode hlAction &&
                            hlAction.IsHighLevelAction &&
                            hlAction.HighLevelSubtree != null &&
                            hlAction.status == BTNodeResult.InProgress)
                        {
                            var mlGraph = hlAction.HighLevelSubtree.GetActionGraph();
                            foreach (var mlNode in mlGraph.GetAllActionNodes())
                            {
                                if (mlNode is PActionNode mlAction &&
                                    mlAction.actionType.ToString().EndsWith("ML") &&
                                    mlAction.status == BTNodeResult.InProgress)
                                {
                                    return mlAction;
                                }
                            }

                            if (hlAction.actionType.ToString().EndsWith("HL"))
                            {
                                LoggingService.LogInfo($"No active ML action, but found active HL action: {hlAction.InstanceName}");
                                return hlAction;
                            }
                        }
                    }
                }
            }
            return null;
        }

        // ─── Helpers ─────────────────────────────────────────────────────

        private static bool TryParseXYZ(string sx, string sy, string sz, out double x, out double y, out double z)
        {
            x = y = z = 0;
            return double.TryParse(sx, NumberStyles.Float, CultureInfo.InvariantCulture, out x)
                && double.TryParse(sy, NumberStyles.Float, CultureInfo.InvariantCulture, out y)
                && double.TryParse(sz, NumberStyles.Float, CultureInfo.InvariantCulture, out z);
        }
    }

    public enum CommandResult
    {
        Resume,
        Quit
    }
}
