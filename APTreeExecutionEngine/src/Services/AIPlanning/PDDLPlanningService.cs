using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using AIPlanning;
using BehaviorTreeMainProject.Services;
using BehaviorTreeMainProject.Log.Services;

namespace BehaviorTreeMainProject.Services.AIPlanning
{
    public class PDDLPlanningService : PlanningService
    {
        private DateTime planningStartTime;
        private bool planningStarted = false;

        private readonly Blackboard<FastName> blackboard;
        private readonly FactoryAction actionFactory;
        public FastName PlannerName = new FastName("PDDLPlanner");
        public List<PActionNode> TempActionList = new List<PActionNode>();
        public PDDLPlanningRequest PlanningRequest;
        
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

        public PDDLPlanningService(BehaviorTreeInstance InOwningTree, PDDLPlanningRequest InPlanningRequest)
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
            
            return base.OnEvaluate(InDeltaTime);
        }

        protected override NodeGraph GenerateNodeGraphFromResult(PlanningResult result)
        {
            var endTime = DateTime.Now;
            bool success = result.Success;
            int actionsGenerated = 0;
            NodeGraph nodeGraph = null;

            LoggingService.LogInfo($"🔧 PDDLPlanningService: Converting PDDL result to NodeGraph...");
            LoggingService.LogInfo($"📋 PDDLPlanningService: Execution Mode: {ExecutionMode}");
            LoggingService.LogInfo($"📋 PDDLPlanningService: Problem File: {PlanningRequest.ProblemFile}");
            
            try
            {
                if (string.IsNullOrEmpty(result.Plan))
                {
                    LoggingService.LogWarning("⚠️ PDDLPlanningService: No plan in planning result");
                    success = false;
                }
                else
                {
                    // Step 1: Transform raw planner output to DSL NodeGraph format
                    var plannerUsed = result.PlannerUsed ?? PlanningRequest.PlannerName ?? "ENHSP";
                    LoggingService.LogInfo($"🔧 PDDLPlanningService: Transforming raw {plannerUsed} output to APTree DSL format...");

                    var planner = Planner.FromName(plannerUsed);
                    var dslPlanString = planner.TransformToAPTreeModel(result.Plan);

                    LoggingService.LogInfo($"🔧 PDDLPlanningService: Transformed plan string:\n{dslPlanString}");

                    // Step 2: Parse the DSL plan string and create NodeGraph
                    nodeGraph = ParsePlanStringToNodeGraph(dslPlanString);
                    
                    if (nodeGraph != null)
                    {
                        actionsGenerated = nodeGraph.GetAllActionNodes().Count;
                        LoggingService.LogSuccess($"✅ PDDLPlanningService: Generated NodeGraph with {actionsGenerated} actions");
                        LoggingService.LogSuccess($"✅ PDDLPlanningService: Execution Mode applied: {ExecutionMode}");

                        // Write the generated DSL plan back into APTreeLivematFinal.bt
                        var cassetteName = OwningFlowNode?.GetNodeName();
                        if (!string.IsNullOrEmpty(cassetteName))
                        {
                            BtFileWriter.UpdateCassetteNodeGraph(cassetteName, dslPlanString);
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
                LoggingService.LogError($"❌ PDDLPlanningService: Error generating NodeGraph: {ex.Message}");
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
            LoggingService.LogInfo($"🔧 PDDLPlanningService: ParsePlanStringToNodeGraph called");
            LoggingService.LogInfo($"🔧 PDDLPlanningService: Plan string length: {planString?.Length ?? 0}");
            
            if (string.IsNullOrEmpty(planString))
            {
                LoggingService.LogError($"❌ PDDLPlanningService: Plan string is null or empty");
                return new NodeGraph();
            }
            
            try
            {
                // Step 1: Parse planner output to extract action instances and relations
                var (actionInstances, relations) = Parser.ParsePlannerOutput(planString);
                
                LoggingService.LogInfo($"🔧 PDDLPlanningService: Extracted {actionInstances.Count} action instances and {relations.Count} relations");
                
                // Step 2: Create NodeGraph from the extracted data
                var nodeGraph = Parser.ParseNodeGraph(actionInstances, relations, blackboard);
                
                LoggingService.LogSuccess($"✅ PDDLPlanningService: Successfully created NodeGraph with {nodeGraph.GetAllActionNodes().Count} nodes");
                return nodeGraph;
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"❌ PDDLPlanningService: Exception in ParsePlanStringToNodeGraph: {ex.Message}");
                LoggingService.LogError($"❌ PDDLPlanningService: Stack trace: {ex.StackTrace}");
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
            
            LoggingService.LogInfo($"🔧 PDDLPlanningService: Creating NodeGraph with {ExecutionMode} execution mode for {actions.Count} actions");
            
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
            LoggingService.LogInfo($"🔧 PDDLPlanningService: Creating sequential execution pattern");
            
            // Add sequential relations (MEETS constraints) between consecutive actions
            for (int i = 0; i < actions.Count - 1; i++)
            {
                nodeGraph.AddOrderRelation(actions[i], actions[i + 1]);
                nodeGraph.AddTemporalConstraint(actions[i], actions[i + 1], TemporalConstraint.MEETS);
                LoggingService.LogInfo($"🔧 PDDLPlanningService: Added sequential relation: {actions[i].InstanceName} → {actions[i + 1].InstanceName}");
            }
            
            return nodeGraph;
        }
        
        private NodeGraph CreateParallelNodeGraph(List<PActionNode> actions, NodeGraph nodeGraph)
        {
            LoggingService.LogInfo($"🔧 PDDLPlanningService: Creating parallel execution pattern");
            
            if (actions.Count == 1)
            {
                LoggingService.LogInfo($"🔧 PDDLPlanningService: Single action execution");
                return nodeGraph;
            }
            
            // First action starts, then all others run in parallel
            for (int i = 1; i < actions.Count; i++)
            {
                nodeGraph.AddOrderRelation(actions[0], actions[i]);
                nodeGraph.AddTemporalConstraint(actions[0], actions[i], TemporalConstraint.OVERLAPS);
                LoggingService.LogInfo($"🔧 PDDLPlanningService: Added parallel relation: {actions[0].InstanceName} || {actions[i].InstanceName}");
            }
            
            return nodeGraph;
        }
        
        private NodeGraph CreateHybridNodeGraph(List<PActionNode> actions, NodeGraph nodeGraph)
        {
            LoggingService.LogInfo($"🔧 PDDLPlanningService: Creating hybrid execution pattern");
            
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
            nodeGraph.AddTemporalConstraint(actions[0], actions[1], TemporalConstraint.MEETS);
            LoggingService.LogInfo($"🔧 PDDLPlanningService: Added sequential relation: {actions[0].InstanceName} → {actions[1].InstanceName}");
            
            // Second action to third action (parallel)
            if (actions.Count > 2)
            {
                nodeGraph.AddOrderRelation(actions[1], actions[2]);
                nodeGraph.AddTemporalConstraint(actions[1], actions[2], TemporalConstraint.OVERLAPS);
                LoggingService.LogInfo($"🔧 PDDLPlanningService: Added parallel relation: {actions[1].InstanceName} || {actions[2].InstanceName}");
            }
            
            // Remaining actions in parallel
            for (int i = 3; i < actions.Count; i++)
            {
                nodeGraph.AddOrderRelation(actions[1], actions[i]);
                nodeGraph.AddTemporalConstraint(actions[1], actions[i], TemporalConstraint.OVERLAPS);
                LoggingService.LogInfo($"🔧 PDDLPlanningService: Added parallel relation: {actions[1].InstanceName} || {actions[i].InstanceName}");
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
        public static string GenerateDynamicPDDLProblem(PActionNode action, Blackboard<FastName> blackboard)
        {
            try
            {
                string instanceName = action.InstanceName.ToString();
                LoggingService.LogInfo($"🔧 PDDLPlanningService: Starting GenerateDynamicPDDLProblem for instance: {instanceName}");

                if (action == null)
                {
                    LoggingService.LogError($"❌ PDDLPlanningService: action is null!");
                    throw new ArgumentNullException(nameof(action));
                }

                var actionType = action.actionType.ToString();
                var actionFullName = action.GetType().Name;
                string problemFileName = $"problem{instanceName}.pddl";
                string problemFilePath = $"python_service/Plannerinputs/generated/{problemFileName}";
                string relativeProblemPath = $"Plannerinputs/generated/{problemFileName}";

                LoggingService.LogInfo($"🔧 PDDLPlanningService: Generating PDDL problem file: {problemFileName}");
                LoggingService.LogInfo($"🔧 PDDLPlanningService: Action type: {actionType}, Action full name: {actionFullName}");

                if (blackboard == null)
                {
                    LoggingService.LogError($"❌ PDDLPlanningService: blackboard is null!");
                    throw new ArgumentNullException(nameof(blackboard));
                }

                // 1. Retrieve predicates from blackboard
                var initialstatepredicates = blackboard.GetTruePredicates();
                LoggingService.LogInfo($"🔧 PDDLPlanningService: Retrieved {initialstatepredicates?.Count ?? 0} initial state predicates");

                if (initialstatepredicates == null)
                    throw new InvalidOperationException("initialstatepredicates is null");

                string initialstatepredicatesPDDL = Parser.ConvertMultiplePredicatesToPDDL(initialstatepredicates);
                LoggingService.LogInfo($"📋 PDDLPlanningService: Initial state PDDL: {initialstatepredicatesPDDL}");

                // 2. Get action effects for goals
                var goalstatePredicates = action.GetActionEffects();
                LoggingService.LogInfo($"🔧 PDDLPlanningService: Retrieved {goalstatePredicates?.Count ?? 0} goal predicates from action effects");

                if (goalstatePredicates == null)
                    throw new InvalidOperationException("goalstatePredicates is null");

                foreach (var predicate in goalstatePredicates)
                    LoggingService.LogInfo($"   Goal predicate: {predicate?.PredicateName}");

                string goalstatepredicatesPDDL = Parser.ConvertMultiplePredicatesToPDDL(goalstatePredicates);
                LoggingService.LogInfo($"🎯 PDDLPlanningService: Goal state PDDL: {goalstatepredicatesPDDL}");

                // 3. Generate PDDL problem content
                string pddlContent = GeneratePDDLProblemContent(actionFullName, initialstatepredicatesPDDL, goalstatepredicatesPDDL);
                LoggingService.LogInfo($"🔧 PDDLPlanningService: Generated PDDL content length: {pddlContent?.Length ?? 0}");

                // 4. Write to file
                LoggingService.LogInfo($"🔧 PDDLPlanningService: About to write file to: {problemFilePath}");
                File.WriteAllText(problemFilePath, pddlContent, Encoding.UTF8);
                LoggingService.LogInfo($"🔧 PDDLPlanningService: File written successfully");

                // 5. Verify file was created and contains content
                if (File.Exists(problemFilePath))
                {
                    var fileContent = File.ReadAllText(problemFilePath);
                    LoggingService.LogInfo($"✅ PDDLPlanningService: Generated PDDL problem file: {problemFilePath}");
                    LoggingService.LogInfo($"📄 PDDLPlanningService: File size: {fileContent.Length} characters");
                    LoggingService.LogInfo($"📄 PDDLPlanningService: Problem file content preview:");
                    LoggingService.LogInfo(pddlContent);

                    if (fileContent.Contains("(:goal"))
                        LoggingService.LogInfo($"✅ PDDLPlanningService: Problem file contains goal section");
                    else
                        LoggingService.LogWarning($"⚠️ PDDLPlanningService: Problem file does NOT contain goal section!");
                }
                else
                {
                    LoggingService.LogError($"❌ PDDLPlanningService: Failed to create problem file: {problemFilePath}");
                }

                s_generatedProblemFiles.Add(problemFilePath);

                LoggingService.LogInfo($"✅ PDDLPlanningService: Successfully completed GenerateDynamicPDDLProblem");
                return relativeProblemPath;
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"❌ PDDLPlanningService: Error generating PDDL problem: {ex.Message}");
                LoggingService.LogError($"❌ PDDLPlanningService: Stack trace: {ex.StackTrace}");
                // Fallback to default problem file
                return "Plannerinputs/static/bigproblem.pddl";
            }
        }

        /// <summary>
        /// Generate PDDL problem content string from action type, initial predicates, and goal predicates.
        /// </summary>
        private static string GeneratePDDLProblemContent(string actionType, string initialPredicates, string goalPredicates)
        {
            actionType = actionType.ToLower();
            initialPredicates = initialPredicates.ToLower();
            goalPredicates = goalPredicates.ToLower();
            var objects = GetRelevantObjects(actionType);

            return $@"(define (problem {actionType.ToLower()})
  (:domain fit)
  (:objects 
    {objects}
  )
  (:init  
    {initialPredicates}
  )
  (:goal 
    (and
      {goalPredicates}
    ) 
  )
)";
        }

        /// <summary>
        /// Get relevant objects from ParameterInstances_PDDL.txt file.
        /// </summary>
        private static string GetRelevantObjects(string actionType)
        {
            try
            {
                string filePath = "src/InputInstances/ParameterInstances_PDDL.txt";

                if (!File.Exists(filePath))
                {
                    LoggingService.LogError($"❌ PDDLPlanningService: ParameterInstances_PDDL.txt file not found at {filePath}");
                    return string.Empty;
                }

                string content = File.ReadAllText(filePath);
                LoggingService.LogInfo($"✅ PDDLPlanningService: Successfully read {content.Length} characters from ParameterInstances_PDDL.txt");
                return content;
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"❌ PDDLPlanningService: Error reading ParameterInstances_PDDL.txt: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Get the list of generated problem file paths (for debugging/diagnostics).
        /// </summary>
        public static IReadOnlyList<string> GeneratedProblemFiles => s_generatedProblemFiles;
    }
}
