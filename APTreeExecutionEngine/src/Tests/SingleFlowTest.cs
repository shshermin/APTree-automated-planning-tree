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
    /// Simplified test with a single DynamicFlowNode using problemC1.
    /// Used for comparison benchmarks (e.g. APTree vs PlanSys2).
    /// </summary>
    public class SingleFlowTest
    {
        private List<ServicePlanning> allPlanners = new List<ServicePlanning>();
        private DateTime testStartTime;
        private DateTime testEndTime;
        private IBTNode rootNode;

        public async Task RunSingleFlowTest()
        {
            LoggingService.Initialize("SingleFlowTest", enableConsole: false, enableFile: true);
            ExecutionFlowLogger.Initialize("SingleFlowTest", enableConsole: false, enableFile: true);

            testStartTime = DateTime.Now;

            LoggingService.LogSection(" SINGLE FLOW TEST (problemC1)");
            LoggingService.LogSuccess($" Started at: {testStartTime:yyyy-MM-dd HH:mm:ss.fff}");

            try
            {
                using var blackboard = new Blackboard<FastName>();
                var blackboardWriter = new BlackboardWriter(blackboard);

                LoggingService.LogSection("REGISTERING ALL TYPES");
                blackboardWriter.RegisterAllTypes();

                LoggingService.LogSection("REGISTERING ALL INSTANCES FROM FILES");
                string actionInstancesFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "src", "InputInstances", "ActionInstances.txt");
                blackboardWriter.RegisterAllInstances(actionInstancesFile);

                BlackboardSummaryLogger.CaptureBlackboardState(blackboard);

                LoggingService.LogSection("CREATING SINGLE-FLOW BEHAVIOR TREE");
                await CreateSingleFlowBehaviorTree(blackboard);

                testEndTime = DateTime.Now;

                LoggingService.LogSection(" SINGLE FLOW TEST COMPLETED!");
                LoggingService.LogSuccess($" Finished at: {testEndTime:yyyy-MM-dd HH:mm:ss.fff}");
                LoggingService.LogSuccess($" Total test duration: {testEndTime - testStartTime:hh\\:mm\\:ss\\.fff}");

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
                LoggingService.LogError($"\n ERROR during single flow test: {ex.Message}");
                LoggingService.LogError($"   Stack trace: {ex.StackTrace}");
                LoggingService.GenerateSummaryTable();
                LoggingService.Close();
                ExecutionFlowLogger.Close();
                throw;
            }
        }

        private async Task CreateSingleFlowBehaviorTree(Blackboard<FastName> blackboard)
        {
            try
            {
                LoggingService.LogInfo(" Creating single-flow behavior tree (1 cassette: problemC1)...");

                var behaviorTree = new BehaviorTree();
                behaviorTree.linkedBlackboard = blackboard;
                behaviorTree.DebugDisplayName = "SingleFlowBehaviorTree";
                LoggingService.LogSuccess(" Created behavior tree instance");

                // Root composite with a single batch
                var rootComposite = new BTFlowNodeComposite(new FastName("RootComposite"), behaviorTree);
                rootComposite.RunChildrenSequentially = true;
                rootNode = rootComposite;

                blackboard.PlanningPhase = true;
                blackboard.CassetteSubtreeCompleted = new bool[1] { false };
                LoggingService.LogInfo(" Starting in PLANNING PHASE");
                LoggingService.LogSuccess(" Created root composite (Sequential mode, 1 batch)");

                behaviorTree.root = rootComposite;
                rootComposite.SetOwiningTree(behaviorTree);

                // Single batch with one cassette (problemC1)
                var batchComposite = new BTFlowNodeComposite(new FastName("Batch1_C1"), behaviorTree);
                batchComposite.SetOwiningTree(behaviorTree);

                var ownedIndices = new int[] { 0 };

                var cassetteNode = new DynamicFlowNode(new FastName("cassette1"), behaviorTree, SuccessCriteria.ALL, 1.0f, true);

                var problemFile = "./Plannerinputs/static/ProblepartialC1.pddl";
                var pddlRequest = new PDDLPlanningRequest(
                    "./Plannerinputs/static/DomainHL.pddl",
                    problemFile,
                    "/home/ubuntu/jpddlplus-master/jpddlplus.jar",
                    "ENHSP");
                var pddlPlanner = new ServicePDDLPlanning(behaviorTree, pddlRequest);
                pddlPlanner.ExecutionMode = ServicePDDLPlanning.ParallelExecutionMode.Parallel;
                allPlanners.Add(pddlPlanner);

                cassetteNode.SetPlanningService(pddlPlanner);
                batchComposite.AddChild(cassetteNode);

                LoggingService.LogInfo($"   cassette1 -> {problemFile} ({pddlRequest.PlannerName}, {pddlPlanner.ExecutionMode})");

                batchComposite.AddService(new ServiceBatchEntry(behaviorTree, batchComposite, ownedIndices, "python_service/Plannerinputs/static/ParameterInstances_PDDL.txt"), false);
                batchComposite.AddPlanningPhaseService();

                rootComposite.AddChild(batchComposite);
                rootComposite.SetTreeForAllServices(behaviorTree);

                LoggingService.LogSuccess(" Added batch 'Batch1_C1' (cassette1) to root");

                blackboard.SetNodeGraph(new FastName("MainBehaviorTree"), new NodeGraph());
                LoggingService.LogSuccess(" Stored behavior tree reference in blackboard");

                LoggingService.LogInfo("\n BEHAVIOR TREE STRUCTURE:");
                LoggingService.LogInfo($"Root: BTFlowNodeComposite ({rootComposite.GetNodeName()}) [Sequential, 1 batch]");
                LoggingService.LogInfo($"  Batch: {batchComposite.GetNodeName()}");
                LoggingService.LogInfo($"    - cassette1 (problemC1.pddl)");

                LoggingService.LogSuccess("\n Single-flow behavior tree created successfully!");

                // Execute tree
                await ExecuteTree(behaviorTree);
            }
            catch (Exception ex)
            {
                LoggingService.LogError($" Error creating behavior tree: {ex.Message}");
                LoggingService.LogError($"   Stack trace: {ex.StackTrace}");
            }
        }

        private async Task ExecuteTree(BehaviorTree behaviorTree)
        {
            LoggingService.LogSection(" EXECUTING TREE");

            try
            {
                int maxTicks = 1300;
                int tickCount = 0;

                LoggingService.LogInfo($" Starting tree execution (max {maxTicks} ticks)...");

                while (tickCount < maxTicks)
                {
                    if (Console.KeyAvailable)
                    {
                        Console.ReadKey(true);
                        LoggingService.LogWarning(" Execution stopped by user");
                        break;
                    }

                    tickCount++;
                    LoggingService.LogInfo($"\n TICK {tickCount} STARTING...");

                    BlackboardSummaryLogger.StartTreeTicking();
                    var result = behaviorTree.Tick(0.1f);
                    BlackboardSummaryLogger.EndTreeTicking();

                    if (behaviorTree.HasFinished())
                    {
                        LoggingService.LogSuccess($"\n Tree execution completed after {tickCount} ticks");
                        LoggingService.LogSuccess($" Final result: {result}");
                        break;
                    }

                    await Task.Delay(100);
                }

                if (tickCount >= maxTicks)
                {
                    LoggingService.LogWarning($"\n Tree execution stopped after {maxTicks} ticks (max reached)");
                }

                LoggingService.LogSuccess(" Tree execution completed!");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($" Error during tree execution: {ex.Message}");
            }
        }

        // Public method to run the test from Program.cs
        public static async Task RunTest()
        {
            var test = new SingleFlowTest();
            await test.RunSingleFlowTest();
        }
    }
}
