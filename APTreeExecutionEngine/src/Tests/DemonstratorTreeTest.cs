using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using BehaviorTreeMainProject.Services;
using BehaviorTreeMainProject.Services.AIPlanning;
using AIPlanning;
using ModelLoader.ParameterTypes;
using BehaviorTreeMainProject.Log.Services;

namespace BehaviorTreeMainProject
{
    /// <summary>
    /// Explicitly builds the Demonstrator behavior tree matching the DSL definition:
    ///
    /// BehaviorTree Demonstrator {
    ///     Root FlowNode Main {
    ///         All / AllFlow
    ///         NodeGraph {
    ///             FlowNode Layers1_2 --[Meets]--> Layers3_4 --[Meets]--> ... --[Meets]--> Layer23
    ///         }
    ///     }
    /// }
    ///
    /// 12 sequential FlowNodes, each with a ServicePlanning (Enhsp) planner.
    /// </summary>
    public class DemonstratorTreeTest
    {
        private List<ServicePlanning> allPlanners = new List<ServicePlanning>();
        private DateTime testStartTime;
        private DateTime testEndTime;
        private IBTNode rootNode;

        public async Task RunDemonstratorTreeTest()
        {
            LoggingService.Initialize("DemonstratorTreeTest", enableConsole: false, enableFile: true);
            ExecutionFlowLogger.Initialize("DemonstratorTreeTest", enableConsole: false, enableFile: true);

            testStartTime = DateTime.Now;
            LoggingService.LogSection("DEMONSTRATOR BEHAVIOR TREE TEST");
            LoggingService.LogSuccess($"Started at: {testStartTime:yyyy-MM-dd HH:mm:ss.fff}");

            try
            {
                // ── 1. Create blackboard ──
                using var blackboard = new Blackboard<FastName>();
                var blackboardWriter = new BlackboardWriter(blackboard);

                LoggingService.LogSection("REGISTERING ALL TYPES");
                blackboardWriter.RegisterAllTypes();

                LoggingService.LogSection("REGISTERING ALL INSTANCES FROM FILES");
                string actionInstancesFile = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..",
                    "src", "InputInstances", "ActionInstances.txt");
                blackboardWriter.RegisterAllInstances(actionInstancesFile);

                BlackboardSummaryLogger.CaptureBlackboardState(blackboard);

                // ── 2. Create behavior tree ──
                LoggingService.LogSection("CREATING DEMONSTRATOR BEHAVIOR TREE");

                var behaviorTree = new BehaviorTree();
                behaviorTree.Initialise(blackboard, "Demonstrator");
                LoggingService.LogSuccess("Created behavior tree instance: Demonstrator");

                // ── 3. Create root composite: Main (All / AllFlow) ──
                rootNode = new BTFlowNodeComposite(
                    new FastName("Main"),
                    behaviorTree,
                    SuccessCriteria.ALL,
                    1.0f,
                    CompositeTerminationPolicy.NeverStop);
                LoggingService.LogSuccess("Created root composite flow node: Main (All / AllFlow)");

                // Planning phase
                blackboard.PlanningPhase = true;
                blackboard.CassetteSubtreeCompleted = new bool[12];
                for (int i = 0; i < 12; i++) blackboard.CassetteSubtreeCompleted[i] = false;

                // ── 4. Create 12 DynamicFlowNodes (Layers1_2 through Layer23) ──
                // Each has: All / AllAction / ServicePlanning Enhsp

                string[] nodeNames = new string[]
                {
                    "Layers1_2",   // planner1
                    "Layers3_4",   // planner2
                    "Layers5_6",   // planner3
                    "Layers7_8",   // planner4
                    "Layers9_10",  // planner5
                    "Layers11_12", // planner6
                    "Layers13_14", // planner7
                    "Layers15_16", // planner8
                    "Layers17_18", // planner9
                    "Layers19_20", // planner10
                    "Layers21_22", // planner11
                    "Layer23"      // planner12
                };

                string[] problemFiles = new string[]
                {
                    "ProblemL1L2",
                    "ProblemL3L4",
                    "ProblemL5L6",
                    "ProblemL7L8",
                    "ProblemL9L10",
                    "ProblemL11L12",
                    "ProblemL13L14",
                    "ProblemL15L16",
                    "ProblemL17L18",
                    "ProblemL19L20",
                    "ProblemL21L22",
                    "ProblemL23"
                };

                // Create each flow node explicitly
                var flowNodes = new DynamicFlowNode[12];

                // ── Layers1_2 ──
                flowNodes[0] = new DynamicFlowNode(
                    new FastName("Layers1_2"), behaviorTree,
                    SuccessCriteria.ALL, 1.0f, false);
                LoggingService.LogInfo("  Created FlowNode: Layers1_2 (All / AllAction)");

                // ── Layers3_4 ──
                flowNodes[1] = new DynamicFlowNode(
                    new FastName("Layers3_4"), behaviorTree,
                    SuccessCriteria.ALL, 1.0f, false);
                LoggingService.LogInfo("  Created FlowNode: Layers3_4 (All / AllAction)");

                // ── Layers5_6 ──
                flowNodes[2] = new DynamicFlowNode(
                    new FastName("Layers5_6"), behaviorTree,
                    SuccessCriteria.ALL, 1.0f, false);
                LoggingService.LogInfo("  Created FlowNode: Layers5_6 (All / AllAction)");

                // ── Layers7_8 ──
                flowNodes[3] = new DynamicFlowNode(
                    new FastName("Layers7_8"), behaviorTree,
                    SuccessCriteria.ALL, 1.0f, false);
                LoggingService.LogInfo("  Created FlowNode: Layers7_8 (All / AllAction)");

                // ── Layers9_10 ──
                flowNodes[4] = new DynamicFlowNode(
                    new FastName("Layers9_10"), behaviorTree,
                    SuccessCriteria.ALL, 1.0f, false);
                LoggingService.LogInfo("  Created FlowNode: Layers9_10 (All / AllAction)");

                // ── Layers11_12 ──
                flowNodes[5] = new DynamicFlowNode(
                    new FastName("Layers11_12"), behaviorTree,
                    SuccessCriteria.ALL, 1.0f, false);
                LoggingService.LogInfo("  Created FlowNode: Layers11_12 (All / AllAction)");

                // ── Layers13_14 ──
                flowNodes[6] = new DynamicFlowNode(
                    new FastName("Layers13_14"), behaviorTree,
                    SuccessCriteria.ALL, 1.0f, false);
                LoggingService.LogInfo("  Created FlowNode: Layers13_14 (All / AllAction)");

                // ── Layers15_16 ──
                flowNodes[7] = new DynamicFlowNode(
                    new FastName("Layers15_16"), behaviorTree,
                    SuccessCriteria.ALL, 1.0f, false);
                LoggingService.LogInfo("  Created FlowNode: Layers15_16 (All / AllAction)");

                // ── Layers17_18 ──
                flowNodes[8] = new DynamicFlowNode(
                    new FastName("Layers17_18"), behaviorTree,
                    SuccessCriteria.ALL, 1.0f, false);
                LoggingService.LogInfo("  Created FlowNode: Layers17_18 (All / AllAction)");

                // ── Layers19_20 ──
                flowNodes[9] = new DynamicFlowNode(
                    new FastName("Layers19_20"), behaviorTree,
                    SuccessCriteria.ALL, 1.0f, false);
                LoggingService.LogInfo("  Created FlowNode: Layers19_20 (All / AllAction)");

                // ── Layers21_22 ──
                flowNodes[10] = new DynamicFlowNode(
                    new FastName("Layers21_22"), behaviorTree,
                    SuccessCriteria.ALL, 1.0f, false);
                LoggingService.LogInfo("  Created FlowNode: Layers21_22 (All / AllAction)");

                // ── Layer23 ──
                flowNodes[11] = new DynamicFlowNode(
                    new FastName("Layer23"), behaviorTree,
                    SuccessCriteria.ALL, 1.0f, false);
                LoggingService.LogInfo("  Created FlowNode: Layer23 (All / AllAction)");

                LoggingService.LogSuccess("Created all 12 DynamicFlowNodes");

                // ── 5. Add all flow nodes to root (sequential = MEETS order) ──
                for (int i = 0; i < flowNodes.Length; i++)
                {
                    ((BTFlowNodeComposite)rootNode).AddChild(flowNodes[i]);
                }
                LoggingService.LogSuccess("Added all 12 flow nodes to root composite (sequential MEETS order)");

                // ── 6. Add planning phase management service ──
                ((BTFlowNodeComposite)rootNode).AddPlanningPhaseService();
                LoggingService.LogSuccess("Added planning phase management service to root");

                // ── 7. Set root on behavior tree ──
                behaviorTree.root = (BTFlowNodeComposite)rootNode;
                rootNode.SetOwiningTree(behaviorTree);
                rootNode.SetTreeForAllServices(behaviorTree);

                // ── 8. Create PDDL planners for each flow node ──
                LoggingService.LogSection("CREATING PDDL PLANNERS");

                string domainFile = "./Plannerinputs/static/DomainTrussHL.pddl";
                string enhspJar = "/home/ubuntu/ENHSP-Public/enhsp.jar";

                var pddlRequests = new PDDLPlanningRequest[12];
                var pddlPlanners = new ServicePDDLPlanning[12];

                // ── planner1: Layers1_2 → ProblemL1L2 ──
                pddlRequests[0] = new PDDLPlanningRequest(domainFile,
                    "./Plannerinputs/static/ProblemL1L2.pddl", enhspJar, "ENHSP");
                pddlPlanners[0] = new ServicePDDLPlanning(behaviorTree, pddlRequests[0]);
                LoggingService.LogInfo("  planner1: Layers1_2 -> DomainTrussHL / ProblemL1L2 (ENHSP)");

                // ── planner2: Layers3_4 → ProblemL3L4 ──
                pddlRequests[1] = new PDDLPlanningRequest(domainFile,
                    "./Plannerinputs/static/ProblemL3L4.pddl", enhspJar, "ENHSP");
                pddlPlanners[1] = new ServicePDDLPlanning(behaviorTree, pddlRequests[1]);
                LoggingService.LogInfo("  planner2: Layers3_4 -> DomainTrussHL / ProblemL3L4 (ENHSP)");

                // ── planner3: Layers5_6 → ProblemL5L6 ──
                pddlRequests[2] = new PDDLPlanningRequest(domainFile,
                    "./Plannerinputs/static/ProblemL5L6.pddl", enhspJar, "ENHSP");
                pddlPlanners[2] = new ServicePDDLPlanning(behaviorTree, pddlRequests[2]);
                LoggingService.LogInfo("  planner3: Layers5_6 -> DomainTrussHL / ProblemL5L6 (ENHSP)");

                // ── planner4: Layers7_8 → ProblemL7L8 ──
                pddlRequests[3] = new PDDLPlanningRequest(domainFile,
                    "./Plannerinputs/static/ProblemL7L8.pddl", enhspJar, "ENHSP");
                pddlPlanners[3] = new ServicePDDLPlanning(behaviorTree, pddlRequests[3]);
                LoggingService.LogInfo("  planner4: Layers7_8 -> DomainTrussHL / ProblemL7L8 (ENHSP)");

                // ── planner5: Layers9_10 → ProblemL9L10 ──
                pddlRequests[4] = new PDDLPlanningRequest(domainFile,
                    "./Plannerinputs/static/ProblemL9L10.pddl", enhspJar, "ENHSP");
                pddlPlanners[4] = new ServicePDDLPlanning(behaviorTree, pddlRequests[4]);
                LoggingService.LogInfo("  planner5: Layers9_10 -> DomainTrussHL / ProblemL9L10 (ENHSP)");

                // ── planner6: Layers11_12 → ProblemL11L12 ──
                pddlRequests[5] = new PDDLPlanningRequest(domainFile,
                    "./Plannerinputs/static/ProblemL11L12.pddl", enhspJar, "ENHSP");
                pddlPlanners[5] = new ServicePDDLPlanning(behaviorTree, pddlRequests[5]);
                LoggingService.LogInfo("  planner6: Layers11_12 -> DomainTrussHL / ProblemL11L12 (ENHSP)");

                // ── planner7: Layers13_14 → ProblemL13L14 ──
                pddlRequests[6] = new PDDLPlanningRequest(domainFile,
                    "./Plannerinputs/static/ProblemL13L14.pddl", enhspJar, "ENHSP");
                pddlPlanners[6] = new ServicePDDLPlanning(behaviorTree, pddlRequests[6]);
                LoggingService.LogInfo("  planner7: Layers13_14 -> DomainTrussHL / ProblemL13L14 (ENHSP)");

                // ── planner8: Layers15_16 → ProblemL15L16 ──
                pddlRequests[7] = new PDDLPlanningRequest(domainFile,
                    "./Plannerinputs/static/ProblemL15L16.pddl", enhspJar, "ENHSP");
                pddlPlanners[7] = new ServicePDDLPlanning(behaviorTree, pddlRequests[7]);
                LoggingService.LogInfo("  planner8: Layers15_16 -> DomainTrussHL / ProblemL15L16 (ENHSP)");

                // ── planner9: Layers17_18 → ProblemL17L18 ──
                pddlRequests[8] = new PDDLPlanningRequest(domainFile,
                    "./Plannerinputs/static/ProblemL17L18.pddl", enhspJar, "ENHSP");
                pddlPlanners[8] = new ServicePDDLPlanning(behaviorTree, pddlRequests[8]);
                LoggingService.LogInfo("  planner9: Layers17_18 -> DomainTrussHL / ProblemL17L18 (ENHSP)");

                // ── planner10: Layers19_20 → ProblemL19L20 ──
                pddlRequests[9] = new PDDLPlanningRequest(domainFile,
                    "./Plannerinputs/static/ProblemL19L20.pddl", enhspJar, "ENHSP");
                pddlPlanners[9] = new ServicePDDLPlanning(behaviorTree, pddlRequests[9]);
                LoggingService.LogInfo("  planner10: Layers19_20 -> DomainTrussHL / ProblemL19L20 (ENHSP)");

                // ── planner11: Layers21_22 → ProblemL21L22 ──
                pddlRequests[10] = new PDDLPlanningRequest(domainFile,
                    "./Plannerinputs/static/ProblemL21L22.pddl", enhspJar, "ENHSP");
                pddlPlanners[10] = new ServicePDDLPlanning(behaviorTree, pddlRequests[10]);
                LoggingService.LogInfo("  planner11: Layers21_22 -> DomainTrussHL / ProblemL21L22 (ENHSP)");

                // ── planner12: Layer23 → ProblemL23 ──
                pddlRequests[11] = new PDDLPlanningRequest(domainFile,
                    "./Plannerinputs/static/ProblemL23.pddl", enhspJar, "ENHSP");
                pddlPlanners[11] = new ServicePDDLPlanning(behaviorTree, pddlRequests[11]);
                LoggingService.LogInfo("  planner12: Layer23 -> DomainTrussHL / ProblemL23 (ENHSP)");

                LoggingService.LogSuccess("Created all 12 PDDL planners (ENHSP)");

                // Track all planners
                for (int i = 0; i < 12; i++)
                {
                    allPlanners.Add(pddlPlanners[i]);
                    pddlPlanners[i].ExecutionMode = ServicePDDLPlanning.ParallelExecutionMode.Sequential;
                }
                LoggingService.LogInfo("All planners set to Sequential execution mode (MEETS constraints)");

                // ── 9. Assign planners to flow nodes ──
                LoggingService.LogSection("ASSIGNING PLANNERS TO FLOW NODES");

                flowNodes[0].SetPlanningService(pddlPlanners[0]);   // Layers1_2
                flowNodes[1].SetPlanningService(pddlPlanners[1]);   // Layers3_4
                flowNodes[2].SetPlanningService(pddlPlanners[2]);   // Layers5_6
                flowNodes[3].SetPlanningService(pddlPlanners[3]);   // Layers7_8
                flowNodes[4].SetPlanningService(pddlPlanners[4]);   // Layers9_10
                flowNodes[5].SetPlanningService(pddlPlanners[5]);   // Layers11_12
                flowNodes[6].SetPlanningService(pddlPlanners[6]);   // Layers13_14
                flowNodes[7].SetPlanningService(pddlPlanners[7]);   // Layers15_16
                flowNodes[8].SetPlanningService(pddlPlanners[8]);   // Layers17_18
                flowNodes[9].SetPlanningService(pddlPlanners[9]);   // Layers19_20
                flowNodes[10].SetPlanningService(pddlPlanners[10]); // Layers21_22
                flowNodes[11].SetPlanningService(pddlPlanners[11]); // Layer23

                LoggingService.LogSuccess("Assigned all 12 planners to their flow nodes");

                // Store behavior tree reference in blackboard
                blackboard.SetNodeGraph(new FastName("DemonstratorBehaviorTree"), new NodeGraph());

                // ── 10. Display tree structure ──
                LoggingService.LogSection("DEMONSTRATOR TREE STRUCTURE");
                LoggingService.LogInfo("Root: BTFlowNodeComposite (Main) [All / AllFlow]");
                LoggingService.LogInfo("  |");
                for (int i = 0; i < flowNodes.Length; i++)
                {
                    string connector = (i < flowNodes.Length - 1) ? "├──" : "└──";
                    string meetsArrow = (i < flowNodes.Length - 1) ? $" --[Meets]--> {nodeNames[i + 1]}" : "";
                    LoggingService.LogInfo($"  {connector} DynamicFlowNode ({nodeNames[i]}) [All / AllAction]{meetsArrow}");
                    LoggingService.LogInfo($"  |     ServicePlanning: planner{i + 1} (ENHSP)");
                    LoggingService.LogInfo($"  |     Domain: DomainTrussHL  Problem: {problemFiles[i]}  Config: sat-hmrph");
                }

                LoggingService.LogSuccess("Demonstrator behavior tree created successfully!");

                // ── 11. Execute the tree ──
                LoggingService.LogSection("EXECUTING DEMONSTRATOR TREE");
                await ExecuteTree(behaviorTree);

                // ── 12. Wrap up ──
                testEndTime = DateTime.Now;
                LoggingService.LogSection("DEMONSTRATOR TREE TEST COMPLETED");
                LoggingService.LogSuccess($"Finished at: {testEndTime:yyyy-MM-dd HH:mm:ss.fff}");
                LoggingService.LogSuccess($"Total duration: {testEndTime - testStartTime:hh\\:mm\\:ss\\.fff}");

                await DisplayExecutionSummary();

                LoggingService.GenerateSummaryTable();
                BlackboardTrackingLogger.LogStatistics();
                BlackboardSummaryLogger.GenerateCSVSummary(blackboard);
                BlackboardSummaryLogger.Close();
                BehaviorTreeComponentLogger.GenerateCSVSummary(blackboard);
                BehaviorTreeComponentLogger.Close();
                TickTimingLogger.GenerateCSVSummary();
                TickTimingLogger.Close();
                PlannerCallLogger.GenerateCSVSummary();
                PlannerCallLogger.Close();
                ActionExecutionLogger.GenerateCSVSummary();
                ActionExecutionLogger.Instance.Close();
                LoggingService.Close();
                ExecutionFlowLogger.Close();
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"ERROR during demonstrator tree test: {ex.Message}");
                LoggingService.LogError($"   Stack trace: {ex.StackTrace}");
                LoggingService.GenerateSummaryTable();
                LoggingService.Close();
                ExecutionFlowLogger.Close();
                throw;
            }
        }

        private async Task ExecuteTree(BehaviorTree behaviorTree)
        {
            int maxTicks = 1300;
            int tickCount = 0;
            var actionStatusHistory = new Dictionary<string, BTNodeResult>();

            LoggingService.LogInfo($"Starting tree execution (max {maxTicks} ticks)...");
            LoggingService.LogInfo("Press any key to stop execution...");

            while (tickCount < maxTicks)
            {
                if (Console.KeyAvailable)
                {
                    Console.ReadKey(true);
                    LoggingService.LogWarning("Execution stopped by user");
                    break;
                }

                tickCount++;
                LoggingService.LogInfo($"\n--- TICK {tickCount} ---");

                BlackboardSummaryLogger.StartTreeTicking();
                var result = behaviorTree.Tick(0.1f);
                BlackboardSummaryLogger.EndTreeTicking();

                // Log flow node statuses
                var compositeRoot = behaviorTree.root as BTFlowNodeComposite;
                if (compositeRoot != null)
                {
                    var children = compositeRoot.GetChildren();
                    for (int i = 0; i < children.Count; i++)
                    {
                        if (children[i] is DynamicFlowNode dfn)
                        {
                            var graph = dfn.GetActionGraph();
                            var actionNodes = graph.GetAllActionNodes();
                            if (actionNodes.Count > 0)
                            {
                                LoggingService.LogInfo($"  {dfn.GetNodeName()}: {actionNodes.Count} actions in graph");
                            }
                        }
                    }
                }

                if (behaviorTree.HasFinished())
                {
                    LoggingService.LogSuccess($"Tree execution completed after {tickCount} ticks. Result: {result}");
                    break;
                }

                await Task.Delay(100);
            }

            if (tickCount >= maxTicks)
            {
                LoggingService.LogWarning($"Tree execution stopped after {maxTicks} ticks (max reached)");
            }
        }

        private async Task DisplayExecutionSummary()
        {
            LoggingService.LogSection("PLANNER EXECUTION SUMMARY");

            if (allPlanners.Count == 0)
            {
                LoggingService.LogWarning("No planners were executed.");
                return;
            }

            LoggingService.LogInfo($"Total planners: {allPlanners.Count}");

            var sortedPlanners = allPlanners.OrderBy(p => p.StartTime).ToList();
            for (int i = 0; i < sortedPlanners.Count; i++)
            {
                var planner = sortedPlanners[i];
                LoggingService.LogInfo($"  Planner {i + 1}: {planner.PlannerName}");
                LoggingService.LogInfo($"    Started: {planner.StartTime:HH:mm:ss.fff}");

                if (planner.HasCompleted)
                {
                    LoggingService.LogInfo($"    Finished: {planner.EndTime:HH:mm:ss.fff}");
                    LoggingService.LogInfo($"    Planner Duration: {planner.PlannerExecutionDuration:hh\\:mm\\:ss\\.fff}");
                    LoggingService.LogInfo($"    Total Duration: {planner.TotalExecutionDuration:hh\\:mm\\:ss\\.fff}");

                    if (planner.GeneratedNodeGraph != null)
                    {
                        LoggingService.LogInfo($"    Actions Generated: {planner.GeneratedNodeGraph.GetAllActionNodes().Count}");
                    }
                }
                else if (planner.IsExecuting)
                {
                    LoggingService.LogInfo($"    Still executing...");
                }
                else
                {
                    LoggingService.LogError($"    Failed or incomplete");
                }
            }

            var completed = allPlanners.Count(p => p.HasCompleted);
            var failed = allPlanners.Count(p => !p.HasCompleted && !p.IsExecuting);
            var executing = allPlanners.Count(p => p.IsExecuting);

            LoggingService.LogInfo($"  Completed: {completed}, Failed: {failed}, Executing: {executing}");
        }

        public static async Task RunTest()
        {
            var test = new DemonstratorTreeTest();
            await test.RunDemonstratorTreeTest();
        }
    }
}
