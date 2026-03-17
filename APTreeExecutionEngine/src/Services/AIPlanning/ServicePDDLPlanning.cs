using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using AIPlanning;
using BehaviorTreeMainProject.Services;
using BehaviorTreeMainProject.Log.Services;

namespace BehaviorTreeMainProject.Services.AIPlanning
{
    public class ServicePDDLPlanning : ServicePlanning
    {
        private DateTime planningStartTime;
        private bool planningStarted = false;
        private bool hlProblemPatched = false;

        private readonly Blackboard<FastName> blackboard;
        private readonly FactoryAction actionFactory;
        public FastName PlannerName = new FastName("PDDLPlanner");
        public List<PActionNode> TempActionList = new List<PActionNode>();
        public PDDLPlanningRequest PlanningRequest;
        
        /// <summary>
        /// Predicate types whose values represent transient robot state and must
        /// be carried forward from the blackboard into static HL problem files
        /// so that cross-cassette planning starts from the actual robot state
        /// rather than the assumptions baked into the file.
        /// </summary>
        private static readonly HashSet<string> RobotStatePredicateTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "gripperempty",
            "atagent",
            "hastool",
            "attool",
            "positionfree",
            "activetool"
        };

        // Parallel execution configuration
        public enum ParallelExecutionMode
        {
            Sequential,      // All actions run sequentially (MEETS)
            Parallel,        // Actions run in parallel (OVERLAPS)
            Hybrid           // Mix of sequential and parallel
        }
        
        public ParallelExecutionMode ExecutionMode { get; set; } = ParallelExecutionMode.Sequential;

        // Track generated problem files for debugging (static since generation happens before instance creation)
        private static readonly List<string> s_generatedProblemFiles = new List<string>();

        public ServicePDDLPlanning(BehaviorTree InOwningTree, PDDLPlanningRequest InPlanningRequest)
            : base(InOwningTree, new RestPlannerCommunicator("http://localhost:5000"), InPlanningRequest)
        {
            this.blackboard = InOwningTree.linkedBlackboard;
            this.actionFactory = FactoryAction.Instance;
            this.PlanningRequest = InPlanningRequest;
        }
      

        public override bool OnEvaluate(float InDeltaTime)
        {
            if (!planningStarted)
            {
                planningStartTime = DateTime.Now;
                planningStarted = true;
            }

            // Before each planning attempt, regenerate the PDDL problem file
            // from the current blackboard state so re-plans use fresh data.
            // Only regenerate if planning hasn't completed yet (the base guard
            // in ServicePlanning.OnEvaluate will skip if HasCompleted is true).
            if (!HasCompleted && !IsExecuting)
            {
                var parentAction = (OwningFlowNode as DynamicFlowNode)?.GetParentAction();
                if (parentAction != null && blackboard != null)
                {
                    // Goal-satisfaction check: skip planning if all goals are already met
                    var goalPredicates = parentAction.GetActionEffects();
                    var currentState = blackboard.GetTruePredicates();

                    bool allGoalsMet = goalPredicates.Count > 0 && goalPredicates.All(goal =>
                        currentState.Any(init => init.PredicateName == goal.PredicateName
                                              && init.not == goal.not));

                    if (allGoalsMet)
                    {
                        LoggingService.LogInfo($"⏭️ ServicePDDLPlanning: All {goalPredicates.Count} goal predicates already satisfied in blackboard — skipping planning for {parentAction.InstanceName}");
                        // Mark as completed successfully without calling the planner
                        HasCompleted = true;
                        WasSuccessful = true;
                        HasPlanGenerated = false; // No plan needed
                        return true;
                    }

                    // Regenerate the problem file with the current blackboard state
                    string originalProblemFile = PlanningRequest.ProblemFile;
                    string newProblemFile = GenerateDynamicPDDLProblem(parentAction, blackboard, originalProblemFile);
                    PlanningRequest.ProblemFile = newProblemFile;

                    // Send file content inline so the remote VM service doesn't need
                    // to read a path that only exists on this (Windows) machine.
                    string localPath = $"python_service/Plannerinputs/generated/{Path.GetFileName(newProblemFile)}";
                    if (File.Exists(localPath))
                        PlanningRequest.ProblemFileContent = File.ReadAllText(localPath, Encoding.UTF8);

                    LoggingService.LogInfo($"🔄 ServicePDDLPlanning: Regenerated problem file for re-plan: {newProblemFile}");
                }
            }

            // Always send domain and problem file contents inline so the VM
            // gets the latest versions from this machine, overriding whatever
            // is already on disk there.
            PopulateInlineFileContents();

            // For HL-level planning (no parentAction), patch the static problem
            // file with live robot-state predicates from the blackboard so that
            // cross-cassette transitions start from the actual robot state.
            if (!HasCompleted && !IsExecuting)
            {
                var parentAction2 = (OwningFlowNode as DynamicFlowNode)?.GetParentAction();
                if (parentAction2 == null && blackboard != null
                    && !string.IsNullOrEmpty(PlanningRequest.ProblemFileContent)
                    && !hlProblemPatched)
                {
                    PlanningRequest.ProblemFileContent = PatchRobotStatePredicates(
                        PlanningRequest.ProblemFileContent, blackboard);
                    hlProblemPatched = true;
                }
            }

            return base.OnEvaluate(InDeltaTime);
        }

        /// <summary>
        /// Reads domain and problem files from local disk and attaches their
        /// contents to the planning request so the Python service can save
        /// them on the VM before invoking the planner.
        /// </summary>
        private void PopulateInlineFileContents()
        {
            // Domain file
            if (!string.IsNullOrWhiteSpace(PlanningRequest.DomainFile))
            {
                string resolvedDomain = ResolveLocalFilePath(PlanningRequest.DomainFile);
                if (resolvedDomain != null)
                {
                    PlanningRequest.DomainFileContent = File.ReadAllText(resolvedDomain, Encoding.UTF8);
                    LoggingService.LogInfo($"📄 ServicePDDLPlanning: Loaded domain file inline: {resolvedDomain}");
                }
                else
                {
                    LoggingService.LogWarning($"⚠️ ServicePDDLPlanning: Could not find domain file locally: {PlanningRequest.DomainFile} — VM will use its own copy (may be outdated)");
                }
            }

            // Problem file — only if not already set (generated ML files are
            // populated earlier in OnEvaluate)
            if (string.IsNullOrWhiteSpace(PlanningRequest.ProblemFileContent)
                && !string.IsNullOrWhiteSpace(PlanningRequest.ProblemFile))
            {
                string resolvedProblem = ResolveLocalFilePath(PlanningRequest.ProblemFile);
                if (resolvedProblem != null)
                {
                    PlanningRequest.ProblemFileContent = File.ReadAllText(resolvedProblem, Encoding.UTF8);
                    LoggingService.LogInfo($"📄 ServicePDDLPlanning: Loaded problem file inline: {resolvedProblem}");
                }
                else
                {
                    LoggingService.LogWarning($"⚠️ ServicePDDLPlanning: Could not find problem file locally: {PlanningRequest.ProblemFile} — VM will use its own copy (may be outdated)");
                }
            }
        }

        /// <summary>
        /// Tries multiple path resolution strategies to find a PDDL file on the
        /// local machine. Returns the first path that exists, or null if none found.
        /// This accounts for different working directories (project root, bin output, etc.).
        /// </summary>
        private static string ResolveLocalFilePath(string requestPath)
        {
            string stripped = requestPath.TrimStart('.', '/', '\\');

            // Build a list of candidate paths from most to least likely
            var candidates = new List<string>
            {
                // 1. python_service/ + stripped path (CWD = project dir, path = "Plannerinputs/...")
                Path.Combine("python_service", stripped),
                // 2. Stripped path directly (CWD = project dir, path already includes full relative)
                stripped,
                // 3. Original path as-is
                requestPath,
            };

            // 4. Try from the executable's directory (handles bin/Debug/... or Docker /app/ scenarios)
            string exeDir = AppContext.BaseDirectory;
            candidates.Add(Path.Combine(exeDir, "python_service", stripped));
            candidates.Add(Path.Combine(exeDir, stripped));

            // 5. Walk up from executable dir looking for python_service folder
            string searchDir = exeDir;
            for (int i = 0; i < 6; i++)
            {
                string parent = Path.GetDirectoryName(searchDir);
                if (parent == null || parent == searchDir) break;
                searchDir = parent;
                string candidate = Path.Combine(searchDir, "python_service", stripped);
                candidates.Add(candidate);
            }

            foreach (var candidate in candidates)
            {
                if (File.Exists(candidate))
                    return Path.GetFullPath(candidate);
            }

            // Log all tried paths for debugging
            LoggingService.LogWarning($"⚠️ ServicePDDLPlanning: File not found at any candidate path for '{requestPath}':");
            foreach (var c in candidates)
                LoggingService.LogWarning($"   tried: {c}");
            LoggingService.LogWarning($"   CWD: {Environment.CurrentDirectory}");
            LoggingService.LogWarning($"   ExeDir: {exeDir}");

            return null;
        }

        /// <summary>
        /// Replaces the robot-state predicates inside a PDDL problem file's (:init ...)
        /// section with their current values from the blackboard.
        /// This ensures HL planning after a cassette transition uses the actual
        /// robot state rather than the static assumptions in the file.
        /// </summary>
        private string PatchRobotStatePredicates(string problemContent, Blackboard<FastName> bb)
        {
            // 1. Collect live robot-state predicates from the blackboard
            var livePredicates = bb.GetTruePredicates()
                .Where(p => RobotStatePredicateTypes.Contains(p.PredicateTypeName))
                .ToList();

            if (livePredicates.Count == 0)
            {
                LoggingService.LogWarning("⚠️ ServicePDDLPlanning: No robot-state predicates found on blackboard — skipping patch");
                return problemContent;
            }

            // 2. Remove existing robot-state lines from the (:init ...) block
            //    Each line looks like "    (gripperempty robot1)" etc.
            var pattern = new Regex(
                @"^[ \t]*\(" + "(" + string.Join("|", RobotStatePredicateTypes) + @")\b[^\)]*\)\s*$",
                RegexOptions.Multiline | RegexOptions.IgnoreCase);

            string patched = pattern.Replace(problemContent, "");

            // 3. Build replacement lines from live blackboard predicates
            var replacementLines = new StringBuilder();
            foreach (var pred in livePredicates)
            {
                string pddl = ConvertPredicateToPDDL(pred);
                if (!string.IsNullOrEmpty(pddl))
                    replacementLines.AppendLine($"    {pddl}");
            }

            // 4. Insert the live predicates right after the (:init line
            var initMarker = Regex.Match(patched, @"\(:init\b[^\n]*\n", RegexOptions.IgnoreCase);
            if (initMarker.Success)
            {
                int insertPos = initMarker.Index + initMarker.Length;
                patched = patched.Insert(insertPos,
                    "    ;; Robot state (carried forward from blackboard)\n" + replacementLines);
            }

            LoggingService.LogInfo($"🔄 ServicePDDLPlanning: Patched {livePredicates.Count} robot-state predicates into HL problem file");
            return patched;
        }

        /// <summary>
        /// Reset the planning service state, including PDDL-specific tracking.
        /// Called during cross-cassette reset to allow re-planning.
        /// </summary>
        public new void ResetPlanningService()
        {
            planningStarted = false;
            hlProblemPatched = false;
            base.ResetPlanningService();
        }

        protected override NodeGraph GenerateNodeGraphFromResult(PlanningResult result)
        {
            var endTime = DateTime.Now;
            bool success = result.Success;
            int actionsGenerated = 0;
            NodeGraph nodeGraph = null;

            LoggingService.LogInfo($"🔧 ServicePDDLPlanning: Converting PDDL result to NodeGraph...");
            LoggingService.LogInfo($"📋 ServicePDDLPlanning: Execution Mode: {ExecutionMode}");
            LoggingService.LogInfo($"📋 ServicePDDLPlanning: Problem File: {PlanningRequest.ProblemFile}");
            
            try
            {
                if (string.IsNullOrEmpty(result.Plan))
                {
                    LoggingService.LogWarning("⚠️ ServicePDDLPlanning: No plan in planning result");
                    success = false;
                }
                else
                {
                    // Step 1: Transform raw planner output to DSL NodeGraph format
                    var plannerUsed = result.PlannerUsed ?? PlanningRequest.PlannerName ?? "ENHSP";
                    LoggingService.LogInfo($"🔧 ServicePDDLPlanning: Transforming raw {plannerUsed} output to APTree DSL format...");

                    var planner = Planner.FromName(plannerUsed);
                    var dslPlanString = planner.TransformToAPTreeModel(result.Plan);

                    LoggingService.LogInfo($"🔧 ServicePDDLPlanning: Transformed plan string:\n{dslPlanString}");

                    // Step 2: Parse the DSL plan string and create NodeGraph
                    nodeGraph = ParsePlanStringToNodeGraph(dslPlanString);
                    
                    if (nodeGraph != null)
                    {
                        actionsGenerated = nodeGraph.GetAllActionNodes().Count;
                        LoggingService.LogSuccess($"✅ ServicePDDLPlanning: Generated NodeGraph with {actionsGenerated} actions");
                        LoggingService.LogSuccess($"✅ ServicePDDLPlanning: Execution Mode applied: {ExecutionMode}");

                        // Write the generated DSL plan back into APTreeLivematFinal.bt
                        var flowNodeName = OwningFlowNode?.GetNodeName();
                        if (!string.IsNullOrEmpty(flowNodeName))
                        {
                            APTreeModelWriter.UpdateCassetteNodeGraph(flowNodeName, dslPlanString);

                            // Patch the Problem: field now that the dynamic problem file is known.
                            // Only applies to subtree FlowNodes (named "<config>_DynamicFlow_<instance>").
                            const string dynMarker = "_DynamicFlow_";
                            if (flowNodeName.Contains(dynMarker) && !string.IsNullOrWhiteSpace(PlanningRequest?.ProblemFile))
                            {
                                var instanceName = flowNodeName.Substring(flowNodeName.IndexOf(dynMarker) + dynMarker.Length);
                                var plannerServiceName = $"subtreeSrv_{instanceName}";
                                var problemFileName = System.IO.Path.GetFileName(PlanningRequest.ProblemFile);
                                APTreeModelWriter.UpdateServicePlanningProblem(plannerServiceName, problemFileName);
                            }
                        }
                    }
                    else
                    {
                        success = false;
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"❌ ServicePDDLPlanning: Error generating NodeGraph: {ex.Message}");
                success = false;
            }

            return success ? nodeGraph : null;
        }
        
        /// <summary>
        /// Parses planner output string and converts it to a NodeGraph
        /// </summary>
        /// <param name="planString">Raw planner output string</param>
        /// <returns>A populated NodeGraph instance</returns>
        private NodeGraph ParsePlanStringToNodeGraph(string planString)
        {
            LoggingService.LogInfo($"🔧 ServicePDDLPlanning: ParsePlanStringToNodeGraph called");
            LoggingService.LogInfo($"🔧 ServicePDDLPlanning: Plan string length: {planString?.Length ?? 0}");
            
            if (string.IsNullOrEmpty(planString))
            {
                LoggingService.LogError($"❌ ServicePDDLPlanning: Plan string is null or empty");
                return new NodeGraph();
            }
            
            try
            {
                // Step 1: Parse planner output to extract action instances and relations
                var (actionInstances, relations) = ParsePlannerOutput(planString);
                
                LoggingService.LogInfo($"🔧 ServicePDDLPlanning: Extracted {actionInstances.Count} action instances and {relations.Count} relations");
                
                // Step 2: Create NodeGraph from the extracted data
                var nodeGraph = ParseNodeGraph(actionInstances, relations, blackboard);
                
                LoggingService.LogSuccess($"✅ ServicePDDLPlanning: Successfully created NodeGraph with {nodeGraph.GetAllActionNodes().Count} nodes");
                return nodeGraph;
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"❌ ServicePDDLPlanning: Exception in ParsePlanStringToNodeGraph: {ex.Message}");
                LoggingService.LogError($"❌ ServicePDDLPlanning: Stack trace: {ex.StackTrace}");
                return new NodeGraph();
            }
        }
        

        
        private NodeGraph CreateNodeGraphWithExecutionMode(List<PActionNode> actions)
        {
            var nodeGraph = new NodeGraph();
            
            // Add all actions to the NodeGraph
            foreach (var action in actions)
            {
                nodeGraph.AddNode(action);
            }
            
            if (actions.Count == 0) return nodeGraph;
            
            LoggingService.LogInfo($"🔧 ServicePDDLPlanning: Creating NodeGraph with {ExecutionMode} execution mode for {actions.Count} actions");
            
            switch (ExecutionMode)
            {
                case ParallelExecutionMode.Sequential:
                    return CreateSequentialNodeGraph(actions, nodeGraph);
                    
                case ParallelExecutionMode.Parallel:
                    return CreateParallelNodeGraph(actions, nodeGraph);
                    
                case ParallelExecutionMode.Hybrid:
                    return CreateHybridNodeGraph(actions, nodeGraph);
                    
                default:
                    return CreateParallelNodeGraph(actions, nodeGraph);
            }
        }
        
        private NodeGraph CreateSequentialNodeGraph(List<PActionNode> actions, NodeGraph nodeGraph)
        {
            LoggingService.LogInfo($"🔧 ServicePDDLPlanning: Creating sequential execution pattern");
            
            // Add sequential relations (MEETS constraints) between consecutive actions
            for (int i = 0; i < actions.Count - 1; i++)
            {
                nodeGraph.AddOrderRelation(actions[i], actions[i + 1]);
                nodeGraph.AddTemporalConstraint(actions[i], actions[i + 1], TemporalType.MEETS);
                LoggingService.LogInfo($"🔧 ServicePDDLPlanning: Added sequential relation: {actions[i].InstanceName} → {actions[i + 1].InstanceName}");
            }
            
            return nodeGraph;
        }
        
        private NodeGraph CreateParallelNodeGraph(List<PActionNode> actions, NodeGraph nodeGraph)
        {
            LoggingService.LogInfo($"🔧 ServicePDDLPlanning: Creating parallel execution pattern");
            
            if (actions.Count == 1)
            {
                LoggingService.LogInfo($"🔧 ServicePDDLPlanning: Single action execution");
                return nodeGraph;
            }
            
            // First action starts, then all others run in parallel
            for (int i = 1; i < actions.Count; i++)
            {
                nodeGraph.AddOrderRelation(actions[0], actions[i]);
                nodeGraph.AddTemporalConstraint(actions[0], actions[i], TemporalType.OVERLAPS);
                LoggingService.LogInfo($"🔧 ServicePDDLPlanning: Added parallel relation: {actions[0].InstanceName} || {actions[i].InstanceName}");
            }
            
            return nodeGraph;
        }
        
        private NodeGraph CreateHybridNodeGraph(List<PActionNode> actions, NodeGraph nodeGraph)
        {
            LoggingService.LogInfo($"🔧 ServicePDDLPlanning: Creating hybrid execution pattern");
            
            if (actions.Count <= 2)
            {
                return CreateParallelNodeGraph(actions, nodeGraph);
            }
            
            // Hybrid pattern: First action sequential, then parallel groups
            // Group 1: First action
            // Group 2: Actions 2-3 run in parallel
            // Group 3: Actions 4+ run in parallel after group 2
            
            // First action to second action (sequential)
            nodeGraph.AddOrderRelation(actions[0], actions[1]);
            nodeGraph.AddTemporalConstraint(actions[0], actions[1], TemporalType.MEETS);
            LoggingService.LogInfo($"🔧 ServicePDDLPlanning: Added sequential relation: {actions[0].InstanceName} → {actions[1].InstanceName}");
            
            // Second action to third action (parallel)
            if (actions.Count > 2)
            {
                nodeGraph.AddOrderRelation(actions[1], actions[2]);
                nodeGraph.AddTemporalConstraint(actions[1], actions[2], TemporalType.OVERLAPS);
                LoggingService.LogInfo($"🔧 ServicePDDLPlanning: Added parallel relation: {actions[1].InstanceName} || {actions[2].InstanceName}");
            }
            
            // Remaining actions in parallel
            for (int i = 3; i < actions.Count; i++)
            {
                nodeGraph.AddOrderRelation(actions[1], actions[i]);
                nodeGraph.AddTemporalConstraint(actions[1], actions[i], TemporalType.OVERLAPS);
                LoggingService.LogInfo($"🔧 ServicePDDLPlanning: Added parallel relation: {actions[1].InstanceName} || {actions[i].InstanceName}");
            }
            
            return nodeGraph;
        }

        // ── PDDL Problem File Generation ──

        /// <summary>
        /// Generate a dynamic PDDL problem file for the given action.
        /// Reads the current blackboard state as initial predicates and the action's
        /// effects as goal predicates, writes a .pddl problem file, and returns
        /// the relative path suitable for use in a PDDLPlanningRequest.
        /// </summary>
        /// <param name="action">The action whose effects become the PDDL goal</param>
        /// <param name="blackboard">The blackboard whose true predicates become the PDDL init</param>
        /// <returns>Relative path to the generated problem file (e.g. "Plannerinputs/generated/problemX.pddl")</returns>
        public static string GenerateDynamicPDDLProblem(PActionNode action, Blackboard<FastName> blackboard, string originalProblemFile)
        {
            try
            {
                string instanceName = action.InstanceName.ToString();
                LoggingService.LogInfo($"🔧 ServicePDDLPlanning: Starting GenerateDynamicPDDLProblem for instance: {instanceName}");

                if (action == null)
                {
                    LoggingService.LogError($"❌ ServicePDDLPlanning: action is null!");
                    throw new ArgumentNullException(nameof(action));
                }

                var actionType = action.actionType.ToString();
                var actionFullName = action.GetType().Name;
                string problemFileName = $"problem{instanceName}.pddl";
                string problemFilePath = $"python_service/Plannerinputs/generated/{problemFileName}";
                string relativeProblemPath = $"Plannerinputs/generated/{problemFileName}";

                LoggingService.LogInfo($"🔧 ServicePDDLPlanning: Generating PDDL problem file: {problemFileName}");
                LoggingService.LogInfo($"🔧 ServicePDDLPlanning: Action type: {actionType}, Action full name: {actionFullName}");

                if (blackboard == null)
                {
                    LoggingService.LogError($"❌ ServicePDDLPlanning: blackboard is null!");
                    throw new ArgumentNullException(nameof(blackboard));
                }

                // 1. Retrieve predicates from blackboard
                var initialstatepredicates = blackboard.GetTruePredicates();
                LoggingService.LogInfo($"🔧 ServicePDDLPlanning: Retrieved {initialstatepredicates?.Count ?? 0} initial state predicates");

                if (initialstatepredicates == null)
                    throw new InvalidOperationException("initialstatepredicates is null");

                string initialstatepredicatesPDDL = ConvertMultiplePredicatesToPDDL(initialstatepredicates);
                LoggingService.LogInfo($"📋 ServicePDDLPlanning: Initial state PDDL: {initialstatepredicatesPDDL}");

                // 2. Get action effects for goals
                var goalstatePredicates = action.GetActionEffects();
                LoggingService.LogInfo($"🔧 ServicePDDLPlanning: Retrieved {goalstatePredicates?.Count ?? 0} goal predicates from action effects");

                if (goalstatePredicates == null)
                    throw new InvalidOperationException("goalstatePredicates is null");

                foreach (var predicate in goalstatePredicates)
                    LoggingService.LogInfo($"   Goal predicate: {predicate?.PredicateName}");

                string goalstatepredicatesPDDL = ConvertMultiplePredicatesToPDDL(goalstatePredicates);
                LoggingService.LogInfo($"🎯 ServicePDDLPlanning: Goal state PDDL: {goalstatepredicatesPDDL}");

                // 3. Generate PDDL problem content
                string pddlContent = GeneratePDDLProblemContent(actionFullName, initialstatepredicatesPDDL, goalstatepredicatesPDDL, originalProblemFile);
                LoggingService.LogInfo($"🔧 ServicePDDLPlanning: Generated PDDL content length: {pddlContent?.Length ?? 0}");

                // 4. Write to file
                LoggingService.LogInfo($"🔧 ServicePDDLPlanning: About to write file to: {problemFilePath}");
                File.WriteAllText(problemFilePath, pddlContent, Encoding.UTF8);
                LoggingService.LogInfo($"🔧 ServicePDDLPlanning: File written successfully");

                // 5. Verify file was created and contains content
                if (File.Exists(problemFilePath))
                {
                    var fileContent = File.ReadAllText(problemFilePath);
                    LoggingService.LogInfo($"✅ ServicePDDLPlanning: Generated PDDL problem file: {problemFilePath}");
                    LoggingService.LogInfo($"📄 ServicePDDLPlanning: File size: {fileContent.Length} characters");
                    LoggingService.LogInfo($"📄 ServicePDDLPlanning: Problem file content preview:");
                    LoggingService.LogInfo(pddlContent ?? "(empty)");

                    if (fileContent.Contains("(:goal"))
                        LoggingService.LogInfo($"✅ ServicePDDLPlanning: Problem file contains goal section");
                    else
                        LoggingService.LogWarning($"⚠️ ServicePDDLPlanning: Problem file does NOT contain goal section!");
                }
                else
                {
                    LoggingService.LogError($"❌ ServicePDDLPlanning: Failed to create problem file: {problemFilePath}");
                }

                s_generatedProblemFiles.Add(problemFilePath);

                LoggingService.LogInfo($"✅ ServicePDDLPlanning: Successfully completed GenerateDynamicPDDLProblem");
                return relativeProblemPath;
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"❌ ServicePDDLPlanning: Error generating PDDL problem: {ex.Message}");
                LoggingService.LogError($"❌ ServicePDDLPlanning: Stack trace: {ex.StackTrace}");
                // Fallback to default problem file
                return "Plannerinputs/static/bigproblem.pddl";
            }
        }

        /// <summary>
        /// Generate PDDL problem content string from action type, initial predicates, and goal predicates.
        /// </summary>
        private static string GeneratePDDLProblemContent(string actionType, string initialPredicates, string goalPredicates, string parentProblemFile)
        {
            actionType = actionType.ToLower();
            goalPredicates = goalPredicates.ToLower();
            var objects = GetRelevantObjects(parentProblemFile);

            // Parse declared object names from the objects block
            var declaredObjects = ParseDeclaredObjectNames(objects);
            LoggingService.LogInfo($"🔧 ServicePDDLPlanning: Declared objects count: {declaredObjects.Count}");

            // Filter init predicates to only include those referencing declared objects
            string filteredPredicates = FilterPredicatesByDeclaredObjects(initialPredicates, declaredObjects);

            return $@"(define (problem {actionType.ToLower()})
  (:domain trussml)
  (:objects 
    {objects}
  )
  (:init  
    {filteredPredicates}
  )
  (:goal 
    (and
      {goalPredicates}
    ) 
  )
)";
        }

        /// <summary>
        /// Parse object names from the (:objects ...) block content.
        /// Each line looks like: "stick1 - stick" or "robot1 - robot"
        /// </summary>
        private static HashSet<string> ParseDeclaredObjectNames(string objectsBlock)
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(objectsBlock))
                return names;

            foreach (var line in objectsBlock.Split('\n'))
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith(";"))
                    continue;

                // Format: "name - type" or "name1 name2 ... - type"
                var dashIndex = trimmed.IndexOf(" - ");
                if (dashIndex < 0)
                    continue;

                var namesPart = trimmed.Substring(0, dashIndex).Trim();
                foreach (var name in namesPart.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    names.Add(name.Trim());
                }
            }

            return names;
        }

        /// <summary>
        /// Filter PDDL predicate lines to only keep those whose parameters all reference declared objects.
        /// Each line looks like: "(predicatename param1 param2)" or "(not (predicatename param1))"
        /// Boolean predicates with no object references (unlikely) are kept.
        /// </summary>
        private static string FilterPredicatesByDeclaredObjects(string predicatesBlock, HashSet<string> declaredObjects)
        {
            if (string.IsNullOrWhiteSpace(predicatesBlock))
                return predicatesBlock;

            var filtered = new List<string>();
            var lines = predicatesBlock.Split('\n');
            int removedCount = 0;

            foreach (var line in lines)
            {
                var trimmed = line.Trim().ToLower();
                if (string.IsNullOrEmpty(trimmed))
                    continue;

                // Extract the inner predicate content: strip outer (not (...)) if present
                var inner = trimmed;
                if (inner.StartsWith("(not "))
                {
                    // "(not (pred p1 p2))" → "(pred p1 p2)"
                    inner = inner.Substring(5, inner.Length - 6).Trim();
                }

                // "(predicatename p1 p2 ...)" → extract tokens
                if (inner.StartsWith("(") && inner.EndsWith(")"))
                {
                    inner = inner.Substring(1, inner.Length - 2).Trim();
                }

                var tokens = inner.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length <= 1)
                {
                    // Predicate with no parameters — keep it
                    filtered.Add(trimmed);
                    continue;
                }

                // tokens[0] is the predicate name, tokens[1..] are parameter values
                bool allDeclared = true;
                for (int i = 1; i < tokens.Length; i++)
                {
                    if (!declaredObjects.Contains(tokens[i]))
                    {
                        allDeclared = false;
                        break;
                    }
                }

                if (allDeclared)
                {
                    filtered.Add(trimmed);
                }
                else
                {
                    removedCount++;
                }
            }

            LoggingService.LogInfo($"🔧 ServicePDDLPlanning: Filtered init predicates — kept {filtered.Count}, removed {removedCount}");
            return string.Join("\n", filtered);
        }

        /// <summary>
        /// Get objects from the parent (static) problem file's (:objects ...) section.
        /// </summary>
        private static string GetRelevantObjects(string parentProblemFile)
        {
            try
            {
                // Normalise the path: strip leading "./" and prepend "python_service/"
                string normalised = parentProblemFile.TrimStart('.', '/', '\\');
                string localPath = Path.Combine("python_service", normalised);

                if (!File.Exists(localPath))
                {
                    LoggingService.LogError($"❌ ServicePDDLPlanning: Parent problem file not found at {localPath}");
                    return string.Empty;
                }

                string content = File.ReadAllText(localPath);

                // Extract the (:objects ... ) block
                int objectsStart = content.IndexOf("(:objects", StringComparison.OrdinalIgnoreCase);
                if (objectsStart < 0)
                {
                    LoggingService.LogError($"❌ ServicePDDLPlanning: No (:objects) section found in {localPath}");
                    return string.Empty;
                }

                // Find the matching closing parenthesis
                int depth = 0;
                int objectsBodyStart = -1;
                for (int i = objectsStart; i < content.Length; i++)
                {
                    if (content[i] == '(')
                    {
                        depth++;
                        if (depth == 1)
                            objectsBodyStart = i + "(:objects".Length;
                    }
                    else if (content[i] == ')')
                    {
                        depth--;
                        if (depth == 0)
                        {
                            string objects = content.Substring(objectsBodyStart, i - objectsBodyStart).Trim();
                            LoggingService.LogInfo($"✅ ServicePDDLPlanning: Extracted {objects.Split('\n').Length} object lines from {localPath}");
                            return objects;
                        }
                    }
                }

                LoggingService.LogError($"❌ ServicePDDLPlanning: Malformed (:objects) section in {localPath} — no closing parenthesis");
                return string.Empty;
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"❌ ServicePDDLPlanning: Error reading parent problem file: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Get the list of generated problem file paths (for debugging/diagnostics).
        /// </summary>
        public static IReadOnlyList<string> GeneratedProblemFiles => s_generatedProblemFiles;

        #region Parsing (migrated from Parser)

        /// <summary>
        /// Parses a NodeGraph from a list of action instance strings and relation strings
        /// </summary>
        /// <param name="actionInstanceStrings">List of action instance strings in MontiCore format</param>
        /// <param name="relationStrings">List of relation strings in the format "source --[TemporalType]--> target"</param>
        /// <param name="blackboard">The blackboard containing parameter instances</param>
        /// <returns>A populated NodeGraph instance</returns>
        public static NodeGraph ParseNodeGraph(List<string> actionInstanceStrings, List<string> relationStrings, Blackboard<FastName> blackboard)
        {
            LoggingService.LogInfo($"🔧 ParseNodeGraph called with {actionInstanceStrings?.Count ?? 0} actions and {relationStrings?.Count ?? 0} relations");
            
            var nodeGraph = new NodeGraph();
            var actionInstances = new Dictionary<string, PActionNode>();
            var blackboardWriter = new BlackboardWriter(blackboard);
            
            if (actionInstanceStrings == null || actionInstanceStrings.Count == 0)
            {
                LoggingService.LogError($"❌ ParseNodeGraph: No action instances provided");
                return nodeGraph;
            }
            
            // Step 1: Convert action instances to MontiCore format
            LoggingService.LogInfo($"🔧 ParseNodeGraph: Step 1 - Converting {actionInstanceStrings.Count} action instances to MontiCore format");
            var montiCoreActionStrings = ConvertToMontiCoreFormat(actionInstanceStrings);
            
            // Step 2: Create action instances
            // Use the original DSL name (which may contain _dup<N>) as the dictionary key
            // so that duplicate actions with the same parameters get separate entries.
            LoggingService.LogInfo($"🔧 ParseNodeGraph: Step 2 - Creating action instances from {montiCoreActionStrings.Count} MontiCore action strings");
            
            for (int idx = 0; idx < montiCoreActionStrings.Count; idx++)
            {
                var actionString = montiCoreActionStrings[idx];
                LoggingService.LogInfo($"🔧 ParseNodeGraph: Processing action string: {actionString}");
                
                try
                {
                    // Use BlackboardWriter to create and register the action
                    LoggingService.LogInfo($"🔧 ParseNodeGraph: Calling BlackboardWriter.CreateAndRegisterActionInstance...");
                    var actionInstance = blackboardWriter.CreateAndRegisterActionInstance(actionString);
                    LoggingService.LogInfo($"🔍 ParseNodeGraph: Action created: {actionInstance?.InstanceName.ToString() ?? "NULL"}");
                    
                    if (actionInstance != null)
                    {
                        // Use the original DSL instance name (with _dup suffix) as the key
                        // so duplicates don't overwrite each other in the dictionary.
                        string actionKey = ExtractDslInstanceName(actionInstanceStrings[idx]);
                        actionInstances[actionKey] = actionInstance;
                        LoggingService.LogSuccess($"✅ ParseNodeGraph: Created action instance: {actionKey} -> {actionInstance.InstanceName.ToString()}");
                    }
                    else
                    {
                        LoggingService.LogError($"❌ ParseNodeGraph: Failed to create action instance from: {actionString}");
                    }
                }
                catch (Exception ex)
                {
                    LoggingService.LogError($"❌ ParseNodeGraph: Exception creating action instance from '{actionString}': {ex.Message}");
                }
            }
            
            LoggingService.LogSuccess($"✅ ParseNodeGraph: Created {actionInstances.Count} action instances");
            
            // Step 3: Add all actions to the NodeGraph
            LoggingService.LogInfo($"🔧 ParseNodeGraph: Step 3 - Adding {actionInstances.Count} actions to NodeGraph");
            foreach (var kvp in actionInstances)
            {
                LoggingService.LogInfo($"🔧 ParseNodeGraph: Adding action to NodeGraph: {kvp.Key} -> {kvp.Value.InstanceName.ToString()}");
                nodeGraph.AddNode(kvp.Value);
            }
            
            LoggingService.LogSuccess($"✅ ParseNodeGraph: Added {actionInstances.Count} actions to NodeGraph");
            
            // Step 4: Create relations
            if (relationStrings != null && relationStrings.Count > 0)
            {
                LoggingService.LogInfo($"🔧 ParseNodeGraph: Step 4 - Creating {relationStrings.Count} relations");
                
                // Log all available action instances for relation parsing
                LoggingService.LogInfo($"🔍 ParseNodeGraph: Available action instances for relation parsing:");
                foreach (var kvp in actionInstances)
                {
                    LoggingService.LogInfo($"   - {kvp.Key} -> {kvp.Value.InstanceName.ToString()}");
                }
                
                foreach (var relationString in relationStrings)
                {
                    LoggingService.LogInfo($"🔧 ParseNodeGraph: Processing relation: {relationString}");
                    try
                    {
                        ParseRelation(relationString, actionInstances, nodeGraph);
                    }
                    catch (Exception ex)
                    {
                        LoggingService.LogError($"❌ ParseNodeGraph: Exception processing relation '{relationString}': {ex.Message}");
                    }
                }
                
                LoggingService.LogSuccess($"✅ ParseNodeGraph: Processed {relationStrings.Count} relations");
            }
            else
            {
                LoggingService.LogWarning($"⚠️ ParseNodeGraph: No relations provided, NodeGraph will have no dependencies");
            }
            
            LoggingService.LogSuccess($"✅ ParseNodeGraph: Successfully created NodeGraph with {nodeGraph.GetAllActionNodes().Count} nodes");
            return nodeGraph;
        }

        /// <summary>
        /// Parses planner output string and extracts action instances and relations into separate lists
        /// </summary>
        /// <param name="plannerOutput">Raw planner output string containing both actions and relations</param>
        /// <returns>Tuple containing list of action instances and list of relations</returns>
        public static (List<string> ActionInstances, List<string> Relations) ParsePlannerOutput(string plannerOutput)
        {
            LoggingService.LogInfo($"🔧 ParsePlannerOutput called");
            LoggingService.LogInfo($"🔧 ParsePlannerOutput: Planner output length: {plannerOutput?.Length ?? 0}");
            
            var actionInstances = new List<string>();
            var relations = new List<string>();
            
            if (string.IsNullOrEmpty(plannerOutput))
            {
                LoggingService.LogError($"❌ ParsePlannerOutput: Planner output is null or empty");
                return (actionInstances, relations);
            }
            
            // Split the planner output into lines
            string[] lines = plannerOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            LoggingService.LogInfo($"🔧 ParsePlannerOutput: Processing {lines.Length} lines from planner output");
            
            // Track the current action instance name for inline DSL relations
            string currentActionInstanceName = null;

            foreach (string line in lines)
            {
                string trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine))
                    continue;
                    
                LoggingService.LogInfo($"🔧 ParsePlannerOutput: Processing line: {trimmedLine}");

                // --- DSL NodeGraph format ---
                // Lines like: Action PickUpHL PickUpHL_lp1_fp1_r1 (lp1 fp1 r1) {
                if (trimmedLine.StartsWith("Action "))
                {
                    // Strip optional trailing '{'
                    var clean = trimmedLine.TrimEnd('{').Trim();
                    var match = Regex.Match(clean, @"^Action\s+(\w+)\s+(\w+)\s+\(([^)]*)\)");
                    if (match.Success)
                    {
                        var instanceName = match.Groups[2].Value;
                        // Produce the ActionInstance: format the downstream pipeline expects
                        actionInstances.Add($"ActionInstance: {instanceName}");
                        currentActionInstanceName = instanceName;
                        LoggingService.LogInfo($"🔧 ParsePlannerOutput: Added DSL action instance: {instanceName}");
                    }
                    continue;
                }

                // --- Relation lines (both DSL inline and standalone) ---
                if (trimmedLine.Contains("--["))
                {
                    // Remove trailing semicolon (DSL inline style)
                    var relLine = trimmedLine.TrimEnd(';').Trim();

                    // If the line starts with the arrow (DSL inline: "--[Meets]--> Target"),
                    // prepend the current action as source
                    if (relLine.StartsWith("--[") && currentActionInstanceName != null)
                    {
                        relLine = $"{currentActionInstanceName} {relLine}";
                    }

                    relations.Add(relLine);
                    LoggingService.LogInfo($"🔧 ParsePlannerOutput: Added relation: {relLine}");
                    continue;
                }

                // --- Legacy flat format ---
                if (trimmedLine.StartsWith("ActionInstance:"))
                {
                    actionInstances.Add(trimmedLine);
                    LoggingService.LogInfo($"🔧 ParsePlannerOutput: Added action instance: {trimmedLine}");
                    continue;
                }

                // Skip structural tokens (NodeGraph, braces)
                if (trimmedLine == "NodeGraph {" || trimmedLine == "NodeGraph{" || trimmedLine == "{" || trimmedLine == "}")
                    continue;

                LoggingService.LogWarning($"⚠️ ParsePlannerOutput: Ignoring unrecognized line: {trimmedLine}");
            }
            
            LoggingService.LogSuccess($"✅ ParsePlannerOutput: Extracted {actionInstances.Count} action instances and {relations.Count} relations");
            return (actionInstances, relations);
        }

        /// <summary>
        /// Extracts the instance name from an ActionInstance definition
        /// </summary>
        private static string GetActionInstanceName(string actionInstanceLine)
        {
            // Return the full MontiCore format string as the key
            LoggingService.LogInfo($"🔧 GetActionInstanceName called with: {actionInstanceLine}");
            string fullActionName = actionInstanceLine;
            LoggingService.LogInfo($"🔧 GetActionInstanceName returning: {fullActionName}");
            
            return fullActionName;
        }

        /// <summary>
        /// Extracts the DSL instance name from an "ActionInstance: TypeName_p1_p2[_dupN]" string.
        /// This preserves any _dup suffix so each duplicate gets a unique dictionary key.
        /// </summary>
        private static string ExtractDslInstanceName(string actionInstanceLine)
        {
            const string prefix = "ActionInstance:";
            if (actionInstanceLine.StartsWith(prefix))
                return actionInstanceLine.Substring(prefix.Length).Trim();
            return actionInstanceLine.Trim();
        }

        /// <summary>
        /// Parses a relation definition like "source --[MEETS]--> target"
        /// Updated to handle simplified action names (without ActionInstance: prefix)
        /// </summary>
        private static void ParseRelation(string relationLine, Dictionary<string, PActionNode> actionInstances, NodeGraph nodeGraph)
        {
            LoggingService.LogInfo($"🔧 ParseRelation called with line: {relationLine}");
            
            // Expected format: sourceAction --[CONSTRAINT]--> targetAction
            // Example: PickUpHL_lp1_fp1_r1 --[MEETS]--> PlaceHL_lp1_pr1_r1 (simplified format)
            // OR: ActionInstance: PickUpHL_lp1_fp1_r1 --[MEETS]--> ActionInstance: PlaceHL_lp1_pr1_r1 (full format)
            
            // Find the arrow pattern "--[CONSTRAINT]-->"
            int arrowStart = relationLine.IndexOf("--[");
            if (arrowStart == -1)
            {
                LoggingService.LogError($"❌ ParseRelation: No arrow pattern '--[' found in relation: {relationLine}");
                return;
            }
            
            int arrowEnd = relationLine.IndexOf("]-->", arrowStart);
            if (arrowEnd == -1)
            {
                LoggingService.LogError($"❌ ParseRelation: No closing arrow pattern ']-->' found in relation: {relationLine}");
                return;
            }
            
            // Extract source action name
            string sourceActionName = relationLine.Substring(0, arrowStart).Trim();
            
            // Extract temporal constraint
            string constraintStr = relationLine.Substring(arrowStart + 3, arrowEnd - arrowStart - 3).Trim();
            TemporalType temporalConstraint = ParseTemporalConstraint(constraintStr);
            
            // Extract target action name
            string targetActionName = relationLine.Substring(arrowEnd + 4).Trim();
            
            LoggingService.LogInfo($"🔧 ParseRelation: Parsed relation - Source: '{sourceActionName}' -> Target: '{targetActionName}' [Constraint: {temporalConstraint}]");
            
            // Find the action instances by matching simplified names to full action instance names
            var sourceAction = FindActionInstanceBySimplifiedName(sourceActionName, actionInstances);
            if (sourceAction == null)
            {
                LoggingService.LogError($"❌ ParseRelation: Source action not found: {sourceActionName}");
                LoggingService.LogInfo($"🔍 ParseRelation: Available action instances:");
                foreach (var kvp in actionInstances)
                {
                    LoggingService.LogInfo($"   - {kvp.Key} -> {kvp.Value.InstanceName.ToString()}");
                }
                return;
            }
            
            var targetAction = FindActionInstanceBySimplifiedName(targetActionName, actionInstances);
            if (targetAction == null)
            {
                LoggingService.LogError($"❌ ParseRelation: Target action not found: {targetActionName}");
                LoggingService.LogInfo($"🔍 ParseRelation: Available action instances:");
                foreach (var kvp in actionInstances)
                {
                    LoggingService.LogInfo($"   - {kvp.Key} -> {kvp.Value.InstanceName.ToString()}");
                }
                return;
            }
            
            LoggingService.LogInfo($"🔧 ParseRelation: Found action instances:");
            LoggingService.LogInfo($"   Source: {sourceAction.InstanceName.ToString()} (type: {sourceAction.GetType().Name})");
            LoggingService.LogInfo($"   Target: {targetAction.InstanceName.ToString()} (type: {targetAction.GetType().Name})");
            
            // Check for self-reference before adding
            if (sourceAction == targetAction)
            {
                LoggingService.LogError($"❌ ParseRelation: SELF-REFERENCE DETECTED! {sourceAction.InstanceName.ToString()} is trying to relate to itself");
                LoggingService.LogError($"❌ ParseRelation: This will create a circular dependency!");
                return;
            }
            
            // Add the relation to the NodeGraph
            LoggingService.LogInfo($"🔧 ParseRelation: Adding order relation: {sourceAction.InstanceName.ToString()} → {targetAction.InstanceName.ToString()}");
            nodeGraph.AddOrderRelation(sourceAction, targetAction);
            
            LoggingService.LogInfo($"🔧 ParseRelation: Adding temporal constraint: {sourceAction.InstanceName.ToString()} {temporalConstraint} {targetAction.InstanceName.ToString()}");
            nodeGraph.AddTemporalConstraint(sourceAction, targetAction, temporalConstraint);
            
            LoggingService.LogSuccess($"✅ ParseRelation: Successfully added relation: {sourceAction.InstanceName.ToString()} -> {targetAction.InstanceName.ToString()} [{temporalConstraint}]");
        }

        /// <summary>
        /// Finds an action instance by simplified name.
        /// First checks the dictionary key (DSL instance name, may include _dup suffix),
        /// then falls back to matching by PActionNode.InstanceName.
        /// </summary>
        private static PActionNode FindActionInstanceBySimplifiedName(string simplifiedName, Dictionary<string, PActionNode> actionInstances)
        {
            LoggingService.LogInfo($"🔧 FindActionInstanceBySimplifiedName called with: {simplifiedName}");
            
            // 1. Direct dictionary key lookup (handles _dup suffixed names)
            if (actionInstances.TryGetValue(simplifiedName, out var directMatch))
            {
                LoggingService.LogInfo($"🔧 FindActionInstanceBySimplifiedName: Found direct key match: {simplifiedName}");
                return directMatch;
            }
            
            // 2. Case-insensitive dictionary key lookup
            foreach (var kvp in actionInstances)
            {
                if (string.Equals(kvp.Key, simplifiedName, StringComparison.OrdinalIgnoreCase))
                {
                    LoggingService.LogInfo($"🔧 FindActionInstanceBySimplifiedName: Found case-insensitive key match: {simplifiedName} -> {kvp.Key}");
                    return kvp.Value;
                }
            }
            
            // 3. Match by PActionNode.InstanceName (legacy fallback)
            foreach (var kvp in actionInstances)
            {
                string instanceName = kvp.Value.InstanceName.ToString();
                if (instanceName == simplifiedName)
                {
                    LoggingService.LogInfo($"🔧 FindActionInstanceBySimplifiedName: Found InstanceName match: {simplifiedName}");
                    return kvp.Value;
                }
            }
            
            // 4. Case-insensitive InstanceName fallback
            foreach (var kvp in actionInstances)
            {
                string instanceName = kvp.Value.InstanceName.ToString();
                if (string.Equals(instanceName, simplifiedName, StringComparison.OrdinalIgnoreCase))
                {
                    LoggingService.LogInfo($"🔧 FindActionInstanceBySimplifiedName: Found case-insensitive InstanceName match: {simplifiedName} -> {instanceName}");
                    return kvp.Value;
                }
            }
            
            LoggingService.LogError($"❌ FindActionInstanceBySimplifiedName: No action instance found for simplified name: {simplifiedName}");
            return null;
        }

        public static string ConvertMultiplePredicatesToPDDL(List<Predicate> predicates)
        {
            var pddlPredicates = new List<string>();
            foreach (var predicate in predicates)
            {
                var pddlPredicate = ConvertPredicateToPDDL(predicate);
                if (!string.IsNullOrEmpty(pddlPredicate))
                {
                    pddlPredicates.Add(pddlPredicate);
                }
            }
            return string.Join("\n", pddlPredicates);
        }

        /// <summary>
        /// Converts a single predicate to PDDL format
        /// </summary>
        private static string ConvertPredicateToPDDL(Predicate predicate)
        {
            try
            {
                if (predicate == null)
                    return string.Empty;

                // Get the predicate type name (not the unique key)
                string predicateName = predicate.PredicateTypeName;
                
                // Use the GetParameterValues method to get clean parameter values in correct order
                var parameterValues = predicate.GetParameterValues();

                // Create PDDL format
                string pddlFormat = $"({predicateName} {string.Join(" ", parameterValues)})";
                
                // Handle negation
                if (predicate.not)
                {
                    pddlFormat = $"(not {pddlFormat})";
                }

                return pddlFormat;
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"❌ ConvertPredicateToPDDL: Error converting predicate to PDDL: {ex.Message}");
                return string.Empty;
            }
        }

        private static TemporalType ParseTemporalConstraint(string constraintStr)
        {
            // Convert temporal constraint string to enum
            if (Enum.TryParse<TemporalType>(constraintStr, true, out var temporalConstraint))
            {
                return temporalConstraint;
            }
            
            // Handle common variations
            switch (constraintStr.ToUpper())
            {
                case "PRECEDES":
                case "BEFORE":
                    return TemporalType.PRECEDES;
                case "MEETS":
                case "SEQUENTIAL":
                    return TemporalType.MEETS;
                case "OVERLAPS":
                case "PARALLEL":
                    return TemporalType.OVERLAPS;
                case "STARTS":
                    return TemporalType.STARTS;
                case "FINISHES":
                    return TemporalType.FINISHES;
                case "CONTAINS":
                    return TemporalType.CONTAINS;
                case "EQUALS":
                    return TemporalType.EQUALS;
                default:
                    LoggingService.LogWarning($"⚠️ ParseTemporalConstraint: Unknown temporal constraint '{constraintStr}', defaulting to MEETS");
                    return TemporalType.MEETS;
            }
        }

        /// <summary>
        /// Converts action instances from planner format to MontiCore format
        /// </summary>
        private static List<string> ConvertToMontiCoreFormat(List<string> actionInstanceStrings)
        {
            LoggingService.LogInfo($"🔧 ConvertToMontiCoreFormat called with {actionInstanceStrings?.Count ?? 0} action instances");
            
            var montiCoreActions = new List<string>();
            
            if (actionInstanceStrings == null || actionInstanceStrings.Count == 0)
            {
                LoggingService.LogWarning($"⚠️ ConvertToMontiCoreFormat: No action instances to convert");
                return montiCoreActions;
            }
            
            foreach (var actionString in actionInstanceStrings)
            {
                try
                {
                    string montiCoreAction = ConvertSingleActionToMontiCore(actionString);
                    montiCoreActions.Add(montiCoreAction);
                    LoggingService.LogInfo($"🔧 ConvertToMontiCoreFormat: Converted: {actionString} -> {montiCoreAction}");
                }
                catch (Exception ex)
                {
                    LoggingService.LogError($"❌ ConvertToMontiCoreFormat: Error converting action '{actionString}': {ex.Message}");
                    // Keep the original format if conversion fails
                    montiCoreActions.Add(actionString);
                }
            }
            
            LoggingService.LogSuccess($"✅ ConvertToMontiCoreFormat: Successfully converted {montiCoreActions.Count} action instances to MontiCore format");
            return montiCoreActions;
        }

        /// <summary>
        /// Converts a single action instance from planner format to MontiCore format
        /// </summary>
        private static string ConvertSingleActionToMontiCore(string actionString)
        {
            LoggingService.LogInfo($"🔧 ConvertSingleActionToMontiCore called with: {actionString}");
            
            // Remove "ActionInstance: " prefix
            if (!actionString.StartsWith("ActionInstance:"))
            {
                LoggingService.LogError($"❌ ConvertSingleActionToMontiCore: Action string doesn't start with 'ActionInstance:': {actionString}");
                return actionString;
            }
            
            string actionPart = actionString.Substring("ActionInstance:".Length).Trim();
            
            // Strip the planner's local _dup<N> suffix — it is only valid within
            // a single plan.  Global cross-cassette disambiguation is handled by
            // BlackboardWriter using ActionInstanceCounts on the blackboard.
            actionPart = System.Text.RegularExpressions.Regex.Replace(actionPart, @"_dup\d+$", "");

            // Split by underscore to get action type and parameters
            string[] parts = actionPart.Split('_');
            if (parts.Length < 1)
            {
                LoggingService.LogError($"❌ ConvertSingleActionToMontiCore: Invalid action format (no parts after split): {actionString}");
                return actionString;
            }
            
            string actionType = parts[0];
            string[] parameters = parts.Skip(1).ToArray();
            
            LoggingService.LogInfo($"🔧 ConvertSingleActionToMontiCore: Parsed action type: {actionType}, parameters: [{string.Join(", ", parameters)}]");
            
            // Get parameter names for this action type
            string[] paramNames = GetParameterNamesForAction(actionType);
            
            // Create MontiCore format
            var paramPairs = new List<string>();
            for (int i = 0; i < parameters.Length; i++)
            {
                string paramName = i < paramNames.Length ? paramNames[i] : $"param{i + 1}";
                paramPairs.Add($"{paramName} : {parameters[i]}");
            }
            
            string montiCoreFormat = $"{actionType}({string.Join(", ", paramPairs)})";
            string result = $"ActionInstance: {montiCoreFormat}";
            
            LoggingService.LogInfo($"🔧 ConvertSingleActionToMontiCore: Converted to MontiCore format: {result}");
            return result;
        }

        /// <summary>
        /// Gets parameter names for a given action type dynamically using reflection
        /// </summary>
        private static string[] GetParameterNamesForAction(string actionType)
        {
            LoggingService.LogInfo($"🔧 GetParameterNamesForAction called with: {actionType}");
            
            try
            {
                // Dynamically find the action type using reflection
                Type actionTypeClass = FindActionTypeDynamically(actionType);
                
                if (actionTypeClass == null)
                {
                    LoggingService.LogWarning($"⚠️ GetParameterNamesForAction: Could not find action type '{actionType}', using generic names");
                    return new string[0];
                }
                
                LoggingService.LogInfo($"🔧 GetParameterNamesForAction: Found action type class: {actionTypeClass.Name}");
                
                // Get the constructor that matches our expected signature
                var constructors = actionTypeClass.GetConstructors();
                var targetConstructor = constructors.FirstOrDefault(c => 
                {
                    var parameters = c.GetParameters();
                    // Check if this constructor has the expected signature: (string, string, Blackboard, ...)
                    return parameters.Length >= 3 && 
                           parameters[0].ParameterType == typeof(string) &&
                           parameters[1].ParameterType == typeof(string) &&
                           parameters[2].ParameterType == typeof(Blackboard<FastName>);
                });

                if (targetConstructor == null)
                {
                    LoggingService.LogWarning($"⚠️ GetParameterNamesForAction: No suitable constructor found for action type '{actionType}', using generic names");
                    return new string[0];
                }

                LoggingService.LogInfo($"🔧 GetParameterNamesForAction: Found constructor with {targetConstructor.GetParameters().Length} parameters");
                
                // Get constructor parameters (skip the first 3: actionType, instanceName, blackboard)
                var constructorParams = targetConstructor.GetParameters().Skip(3).ToArray();
                
                // Extract parameter names in constructor order
                var paramNames = new string[constructorParams.Length];
                for (int i = 0; i < constructorParams.Length; i++)
                {
                    paramNames[i] = constructorParams[i].Name;
                    LoggingService.LogInfo($"🔧 GetParameterNamesForAction: Parameter {i}: {paramNames[i]} (type: {constructorParams[i].ParameterType.Name})");
                }
                
                LoggingService.LogInfo($"🔧 GetParameterNamesForAction: Dynamic parameter mapping for {actionType}: [{string.Join(", ", paramNames)}]");
                return paramNames;
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"❌ GetParameterNamesForAction: Error getting parameter names for action type '{actionType}': {ex.Message}");
                return new string[0];
            }
        }

        /// <summary>
        /// Dynamically finds an action type by name using reflection
        /// </summary>
        private static Type FindActionTypeDynamically(string actionTypeName)
        {
            LoggingService.LogInfo($"🔧 FindActionTypeDynamically called with: {actionTypeName}");
            
            try
            {
                // Get the assembly containing GenericBTAction types
                var assembly = typeof(PActionNode).Assembly;
                
                // Search for types that inherit from GenericBTAction
                var actionTypes = assembly.GetTypes()
                    .Where(t => t.IsSubclassOf(typeof(PActionNode)) && !t.IsAbstract)
                    .ToList();
                
                LoggingService.LogInfo($"🔧 FindActionTypeDynamically: Found {actionTypes.Count} action types: {string.Join(", ", actionTypes.Select(t => t.Name))}");
                
                // Try exact match first (case-insensitive)
                var exactMatch = actionTypes.FirstOrDefault(t => 
                    string.Equals(t.Name, actionTypeName, StringComparison.OrdinalIgnoreCase));
                
                if (exactMatch != null)
                {
                    LoggingService.LogInfo($"🔧 FindActionTypeDynamically: Found exact match: {exactMatch.Name}");
                    return exactMatch;
                }
                
                // Try partial match (e.g., "pickup" matches "PickUp")
                var partialMatch = actionTypes.FirstOrDefault(t => 
                    string.Equals(t.Name.Replace(" ", ""), actionTypeName.Replace(" ", ""), StringComparison.OrdinalIgnoreCase));
                
                if (partialMatch != null)
                {
                    LoggingService.LogInfo($"🔧 FindActionTypeDynamically: Found partial match: {partialMatch.Name}");
                    return partialMatch;
                }
                
                LoggingService.LogWarning($"⚠️ FindActionTypeDynamically: No match found for action name: {actionTypeName}");
                return null;
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"❌ FindActionTypeDynamically: Error finding action type '{actionTypeName}': {ex.Message}");
                return null;
            }
        }

        #endregion
    }
}
