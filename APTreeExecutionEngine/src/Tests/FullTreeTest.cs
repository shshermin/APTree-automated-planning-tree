﻿using System;
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
    public class FullTreeTest
    {
        // Track all planner executions
        private List<ServicePlanning> allPlanners = new List<ServicePlanning>();
        private DateTime testStartTime;
        private DateTime testEndTime;
        private IBTNode rootNode; // Store root node for monitoring
        private readonly bool useModelLoader;
        private int tickIntervalMilliseconds = 100;

        public FullTreeTest(bool useModelLoader = true)
        {
            this.useModelLoader = useModelLoader;
        }
        
        public async Task RunFullTreeTest()
        {
            // Initialize logging service
            var testName = useModelLoader ? "ModelLoaderFullTreeTest" : "FullTreeTest";
            LoggingService.Initialize(testName, enableConsole: false, enableFile: true);
            
            // Initialize execution flow logger
            ExecutionFlowLogger.Initialize(testName, enableConsole: false, enableFile: true);
            
            // BlackboardTrackingLogger is automatically initialized when first accessed
            // No need to call Initialize() explicitly
            
            testStartTime = DateTime.Now;
            
            LoggingService.LogSection(" FULL BEHAVIOR TREE TEST");
            LoggingService.LogSuccess($" Started at: {testStartTime:yyyy-MM-dd HH:mm:ss.fff}");

            try
            {
                // Create blackboard instance (without Neo4j)
                using var blackboard = new Blackboard<FastName>();

                // Create BlackboardWriter for type registration
                var blackboardWriter = new BlackboardWriter(blackboard);

                // Register all types
                LoggingService.LogSection("REGISTERING ALL TYPES");
                blackboardWriter.RegisterAllTypes();

                // Register the generated scene objects and initial state.
                LoggingService.LogSection("REGISTERING GENERATED MODEL STATE");
                string modelLoaderDirectory = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "src", "ModelLoader");
                blackboardWriter.RegisterParameterInstances(
                    Path.Combine(modelLoaderDirectory, "LiveMatSetupObjects.json"));
                blackboardWriter.RegisterPredicateInstances(
                    Path.Combine(modelLoaderDirectory, "InitialStatePredicates.json"));

                // Capture blackboard state before ticking starts
                LoggingService.LogSection("CAPTURING BLACKBOARD STATE BEFORE TICKING");
                BlackboardSummaryLogger.CaptureBlackboardState(blackboard);

                // Inspect blackboard contents
                LoggingService.LogSection("INSPECTING BLACKBOARD CONTENTS");
                await InspectBlackboard(blackboard);

                // Create behavior tree with cassette flow nodes
                LoggingService.LogSection("CREATING BEHAVIOR TREE WITH CASSETTE FLOW NODES");
                if (useModelLoader)
                    await CreateModelLoadedBehaviorTree(blackboard);
                else
                    await CreateCassetteBehaviorTree(blackboard);

                testEndTime = DateTime.Now;
                
                LoggingService.LogSection(" FULL BEHAVIOR TREE TEST COMPLETED!");
                LoggingService.LogSuccess($" Finished at: {testEndTime:yyyy-MM-dd HH:mm:ss.fff}");
                LoggingService.LogSuccess($" Total test duration: {testEndTime - testStartTime:hh\\:mm\\:ss\\.fff}");
                
                // Display execution summary
                await DisplayExecutionSummary();

                // Generate summary table at the end
                LoggingService.GenerateSummaryTable();
                
                // Log final blackboard tracking statistics
                BlackboardTrackingLogger.LogStatistics();
                
                // Generate comprehensive CSV summary
                BlackboardSummaryLogger.GenerateCSVSummary(blackboard);
                BlackboardSummaryLogger.Close();
                
                // Generate behavior tree component CSV summary
                BehaviorTreeComponentLogger.GenerateCSVSummary(blackboard);
                BehaviorTreeComponentLogger.Close();
                
                // Generate tick timing CSV summary
                TickTimingLogger.GenerateCSVSummary();
                TickTimingLogger.Close();

                // Generate planner call CSV summary
                PlannerCallLogger.GenerateCSVSummary();
                PlannerCallLogger.Close();

                // Generate action execution CSV summary
                ActionExecutionLogger.GenerateCSVSummary();
                ActionExecutionLogger.Instance.Close();
                
                // Close logging service
                LoggingService.Close();
                
                // Close execution flow logger
                ExecutionFlowLogger.Close();
                
                // Note: BlackboardTrackingLogger is automatically closed when blackboard is disposed
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"\n ERROR during full tree test: {ex.Message}");
                LoggingService.LogError($"   Stack trace: {ex.StackTrace}");
                
                // Generate summary table even if test failed
                LoggingService.GenerateSummaryTable();
                
                // Close logging service
                LoggingService.Close();
                
                // Close execution flow logger
                ExecutionFlowLogger.Close();
                
                throw;
            }
        }

        private async Task CreateModelLoadedBehaviorTree(Blackboard<FastName> blackboard)
        {
            var configPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "..", "..", "..", "src", "ModelLoader", "LiveMatExecutionConfig.json");
            var loaded = BehaviorTreeModelLoader.Load(configPath, blackboard);
            if (loaded.Config.TickIntervalMilliseconds <= 0)
                throw new InvalidOperationException("tickIntervalMilliseconds must be greater than zero.");

            tickIntervalMilliseconds = loaded.Config.TickIntervalMilliseconds;

            rootNode = loaded.Tree.root;
            allPlanners.AddRange(loaded.Planners);

            LoggingService.LogSuccess($"Loaded behavior tree model with {allPlanners.Count} planners");
            await TestBehaviorTreeStructure(loaded.Tree);
            await MonitorPlannerExecution();
            await DisplayNodeGraphStatus(loaded.Tree);
            await TrackSubtreeStatusForHLActions(loaded.Tree);
            await ExecuteTreeWithComprehensiveLogging(loaded.Tree);
        }

        // Inspect blackboard contents
        private async Task InspectBlackboard(Blackboard<FastName> blackboard)
        {
            LoggingService.LogSubsection(" BLACKBOARD INSPECTION REPORT");

            try
            {
                // 1. CustomProperty Types
                var entityTypes = blackboard.GetAllEntityTypes();
                LoggingService.LogInfo($"\n  ENTITY TYPES ({entityTypes.Count}):");
                foreach (var entityType in entityTypes)
                {
                    LoggingService.LogInfo($"   - {entityType.ToString()}");
                }

                // 2. Predicate Types
                var predicateTypes = blackboard.GetAllPredicateTypes();
                LoggingService.LogInfo($"\n PREDICATE TYPES ({predicateTypes.Count}):");
                foreach (var predicateType in predicateTypes)
                {
                    LoggingService.LogInfo($"   - {predicateType.ToString()}");
                }

                // 3. Action Types
                var actionTypes = blackboard.GetAllActionTypes();
                LoggingService.LogInfo($"\n ACTION TYPES ({actionTypes.Count}):");
                foreach (var actionType in actionTypes)
                {
                    LoggingService.LogInfo($"   - {actionType.ToString()}");
                }

                // 4. Action Instances
                var actionInstances = blackboard.GetAllActionInstances();
                LoggingService.LogInfo($"\n ACTION INSTANCES ({actionInstances.Count}):");
                foreach (var actionInstance in actionInstances)
                {
                    LoggingService.LogInfo($"   - {actionInstance.InstanceName.ToString()} (Type: {actionInstance.actionType.ToString()})");
                }

                // 5. Built-in Values
                LoggingService.LogInfo($"\n BUILT-IN VALUES:");
                LoggingService.LogInfo($"   - Int Values: {GetDictionaryCount(blackboard, "IntValues")}");
                if (GetDictionaryCount(blackboard, "IntValues") > 0)
                {
                    foreach (var item in GetDictionaryItems(blackboard, "IntValues"))
                    {
                        LoggingService.LogInfo($"      {item.Key}: {item.Value}");
                    }
                }
                
                LoggingService.LogInfo($"   - Double Values: {GetDictionaryCount(blackboard, "DoubleValues")}");
                if (GetDictionaryCount(blackboard, "DoubleValues") > 0)
                {
                    foreach (var item in GetDictionaryItems(blackboard, "DoubleValues"))
                    {
                        LoggingService.LogInfo($"      {item.Key}: {item.Value}");
                    }
                }
                
                LoggingService.LogInfo($"   - Bool Values: {GetDictionaryCount(blackboard, "BoolValues")}");
                if (GetDictionaryCount(blackboard, "BoolValues") > 0)
                {
                    foreach (var item in GetDictionaryItems(blackboard, "BoolValues"))
                    {
                        LoggingService.LogInfo($"      {item.Key}: {item.Value}");
                    }
                }
                
                LoggingService.LogInfo($"   - String Values: {GetDictionaryCount(blackboard, "StringValues")}");
                if (GetDictionaryCount(blackboard, "StringValues") > 0)
                {
                    foreach (var item in GetDictionaryItems(blackboard, "StringValues"))
                    {
                        LoggingService.LogInfo($"      {item.Key}: {item.Value}");
                    }
                }

                // 6. CustomProperty Values
                LoggingService.LogInfo($"\n  ENTITY VALUES:");
                LoggingService.LogInfo($"   - Element Values: {GetDictionaryCount(blackboard, "ElementValues")}");
                if (GetDictionaryCount(blackboard, "ElementValues") > 0)
                {
                    foreach (var item in GetDictionaryItems(blackboard, "ElementValues"))
                    {
                        LoggingService.LogInfo($"      {item.Key}: {item.Value}");
                    }
                }
                
                LoggingService.LogInfo($"   - Location Values: {GetDictionaryCount(blackboard, "LocationValues")}");
                if (GetDictionaryCount(blackboard, "LocationValues") > 0)
                {
                    foreach (var item in GetDictionaryItems(blackboard, "LocationValues"))
                    {
                        LoggingService.LogInfo($"      {item.Key}: {item.Value}");
                    }
                }
                
                LoggingService.LogInfo($"   - Agent Values: {GetDictionaryCount(blackboard, "AgentValues")}");
                if (GetDictionaryCount(blackboard, "AgentValues") > 0)
                {
                    foreach (var item in GetDictionaryItems(blackboard, "AgentValues"))
                    {
                        LoggingService.LogInfo($"      {item.Key}: {item.Value}");
                    }
                }
                
                LoggingService.LogInfo($"   - Layer Values: {GetDictionaryCount(blackboard, "LayerValues")}");
                if (GetDictionaryCount(blackboard, "LayerValues") > 0)
                {
                    foreach (var item in GetDictionaryItems(blackboard, "LayerValues"))
                    {
                        LoggingService.LogInfo($"      {item.Key}: {item.Value}");
                    }
                }
                
                LoggingService.LogInfo($"   - Module Values: {GetDictionaryCount(blackboard, "ModuleValues")}");
                if (GetDictionaryCount(blackboard, "ModuleValues") > 0)
                {
                    foreach (var item in GetDictionaryItems(blackboard, "ModuleValues"))
                    {
                        LoggingService.LogInfo($"      {item.Key}: {item.Value}");
                    }
                }
                
                LoggingService.LogInfo($"   - Tool Values: {GetDictionaryCount(blackboard, "ToolValues")}");
                if (GetDictionaryCount(blackboard, "ToolValues") > 0)
                {
                    foreach (var item in GetDictionaryItems(blackboard, "ToolValues"))
                    {
                        LoggingService.LogInfo($"      {item.Key}: {item.Value}");
                    }
                }

                // 7. Predicate Values
                LoggingService.LogInfo($"\n PREDICATE VALUES:");
                LoggingService.LogInfo($"   - Predicate Values: {GetDictionaryCount(blackboard, "PredicateValues")}");
                if (GetDictionaryCount(blackboard, "PredicateValues") > 0)
                {
                    foreach (var item in GetDictionaryItems(blackboard, "PredicateValues"))
                    {
                        LoggingService.LogInfo($"      {item.Key}: {item.Value}");
                    }
                }

                // 8. Action Values
                LoggingService.LogInfo($"\n ACTION VALUES:");
                LoggingService.LogInfo($"   - Action Values: {GetDictionaryCount(blackboard, "ActionValues")}");
                if (GetDictionaryCount(blackboard, "ActionValues") > 0)
                {
                    foreach (var item in GetDictionaryItems(blackboard, "ActionValues"))
                    {
                        LoggingService.LogInfo($"      {item.Key}: {item.Value}");
                    }
                }

                // 9. State Values
                LoggingService.LogInfo($"\n STATE VALUES:");
                LoggingService.LogInfo($"   - State Values: {GetDictionaryCount(blackboard, "StateValues")}");
                if (GetDictionaryCount(blackboard, "StateValues") > 0)
                {
                    foreach (var item in GetDictionaryItems(blackboard, "StateValues"))
                    {
                        LoggingService.LogInfo($"      {item.Key}: {item.Value}");
                    }
                }

                // 8. NodeGraphs
                var nodeGraphs = blackboard.GetAllNodeGraphs();
                LoggingService.LogInfo($"\n NODEGRAPHS ({nodeGraphs.Count}):");
                foreach (var nodeGraph in nodeGraphs)
                {
                    LoggingService.LogInfo($"   - NodeGraph with {nodeGraph.GetAllActionNodes().Count} action nodes");
                }

                // 10. Summary
                LoggingService.LogInfo($"\n SUMMARY:");
                LoggingService.LogInfo($"   - CustomProperty Types: {entityTypes.Count}");
                LoggingService.LogInfo($"   - Predicate Types: {predicateTypes.Count}");
                LoggingService.LogInfo($"   - Action Types: {actionTypes.Count}");
                LoggingService.LogInfo($"   - Action Instances: {actionInstances.Count}");
                LoggingService.LogInfo($"   - NodeGraphs: {nodeGraphs.Count}");
                LoggingService.LogInfo($"   - TOTAL ITEMS: {entityTypes.Count + predicateTypes.Count + actionTypes.Count + actionInstances.Count + nodeGraphs.Count}");

            }
            catch (Exception ex)
            {
                LoggingService.LogError($" Error during blackboard inspection: {ex.Message}");
            }
        }

        // Helper method to get dictionary count using reflection
        private int GetDictionaryCount(Blackboard<FastName> blackboard, string dictionaryName)
        {
            try
            {
                var field = typeof(Blackboard<FastName>).GetField(dictionaryName, 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    var dictionary = field.GetValue(blackboard);
                    if (dictionary is System.Collections.ICollection collection)
                    {
                        return collection.Count;
                    }
                }
                return 0;
            }
            catch
            {
                return 0;
            }
        }

        // Helper method to get dictionary items using reflection
        private IEnumerable<KeyValuePair<FastName, object>> GetDictionaryItems(Blackboard<FastName> blackboard, string dictionaryName)
        {
            var result = new List<KeyValuePair<FastName, object>>();
            try
            {
                var field = typeof(Blackboard<FastName>).GetField(dictionaryName, 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    var dictionary = field.GetValue(blackboard);
                    if (dictionary is System.Collections.IDictionary dict)
                    {
                        foreach (System.Collections.DictionaryEntry entry in dict)
                        {
                            // For CustomProperty objects, use NameKey.ToString() instead of the full type name
                            object displayValue = entry.Value;
                            if (entry.Value is CustomProperty entity)
                            {
                                displayValue = entity.NameKey?.ToString() ?? entry.Value.ToString();
                            }
                            result.Add(new KeyValuePair<FastName, object>((FastName)entry.Key, displayValue));
                        }
                    }
                }
            }
            catch
            {
                // Return empty collection on error
            }
            return result;
        }

        // Create behavior tree with cassette flow nodes
        private async Task CreateCassetteBehaviorTree(Blackboard<FastName> blackboard)
        {
            try
            {
                LoggingService.LogInfo(" Creating behavior tree with cassette flow nodes (3 batches x 4 cassettes)...");

                // Create behavior tree instance (set blackboard directly to avoid orphan root composite)
                var behaviorTree = new BehaviorTree();
                behaviorTree.linkedBlackboard = blackboard;
                behaviorTree.DebugDisplayName = "CassetteBehaviorTree";
                LoggingService.LogSuccess(" Created behavior tree instance");

                // Root composite runs its batch children one at a time
                var rootComposite = new BTFlowNodeComposite(new FastName("RootComposite"), behaviorTree);
                rootComposite.RunChildrenSequentially = true;
                rootNode = rootComposite;

                // Ensure we start in planning phase
                blackboard.PlanningPhase = true;
                // 12 cassettes across 3 batches
                blackboard.CassetteSubtreeCompleted = new bool[12]
                {
                    false, false, false, false,
                    false, false, false, false,
                    false, false, false, false
                };
                LoggingService.LogInfo(" Starting in PLANNING PHASE - HL actions will generate NodeGraphs first");
                LoggingService.LogInfo(" CassetteSubtreeCompleted initialised to false for 12 cassettes");
                LoggingService.LogSuccess(" Created root composite (Sequential mode, 3 batches)");

                // Wire root before adding children
                behaviorTree.root = rootComposite;
                rootComposite.SetOwiningTree(behaviorTree);

                // Three batches. Each batch is its own composite holding 4 cassette DynamicFlowNodes.
                // Cassettes keep globally unique names cassette1..cassette12 so tree walks
                // (e.g. DecoratorDynamicPlanningComplete.TraverseTreeForAction) keep working.
                // ObjectsFile per batch is pushed into ServicePDDLPlanning.CurrentObjectsFile
                // by ServiceBatchEntry on first tick of the batch.
                var batchDefs = new (string Name, int FirstCassette, string[] ProblemTags, string ObjectsFile)[]
                {
                    ("Batch1_C1toC4",  1, new[] { "C1",  "C2",  "C3",  "C4"  }, "python_service/Plannerinputs/static/ParameterInstances_PDDL.txt"),
                    ("Batch2_C5toC8",  5, new[] { "C5",  "C6",  "C7",  "C8"  }, "python_service/Plannerinputs/static/ParameterInstances_PDDL2.txt"),
                    ("Batch3_C9toC12", 9, new[] { "C9",  "C10", "C11", "C12" }, "python_service/Plannerinputs/static/ParameterInstances_PDDL3.txt"),
                };

                foreach (var batch in batchDefs)
                {
                    var batchComposite = new BTFlowNodeComposite(new FastName(batch.Name), behaviorTree);
                    batchComposite.SetOwiningTree(behaviorTree);

                    var ownedIndices = new int[4];
                    for (int i = 0; i < 4; i++)
                    {
                        int cassetteNumber = batch.FirstCassette + i;
                        ownedIndices[i] = cassetteNumber - 1;

                        var cassetteNode = new DynamicFlowNode(new FastName($"cassette{cassetteNumber}"), behaviorTree, SuccessCriteria.ALL, 1.0f, true);

                        var problemFile = $"./Plannerinputs/static/problem{batch.ProblemTags[i]}.pddl";
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

                        LoggingService.LogInfo($"   cassette{cassetteNumber} -> {problemFile} ({pddlRequest.PlannerName}, {pddlPlanner.ExecutionMode})");
                    }

                    // Per-batch services:
                    //  - ServiceBatchEntry: on first tick of this batch, re-arm PlanningPhase=true,
                    //    reset CassetteSubtreeCompleted for this batch's cassettes, and point
                    //    ServicePDDLPlanning.CurrentObjectsFile at this batch's objects file.
                    //  - PlanningPhaseService: flips PlanningPhase=false when this batch's 4 planners
                    //    have all generated their NodeGraphs (composite-scoped check).
                    batchComposite.AddService(new ServiceBatchEntry(behaviorTree, batchComposite, ownedIndices, batch.ObjectsFile), false);
                    batchComposite.AddPlanningPhaseService();
                    batchComposite.AddDecorator(new DecoratorLowestCostExecution(batchComposite));

                    rootComposite.AddChild(batchComposite);
                    LoggingService.LogSuccess($" Added batch '{batch.Name}' (cassettes {batch.FirstCassette}..{batch.FirstCassette + 3}) to root");
                }

                rootComposite.SetTreeForAllServices(behaviorTree);

                // Store the behavior tree in the blackboard for later use
                blackboard.SetNodeGraph(new FastName("MainBehaviorTree"), new NodeGraph());
                LoggingService.LogSuccess(" Stored behavior tree reference in blackboard");

                // Display tree structure
                LoggingService.LogInfo("\n BEHAVIOR TREE STRUCTURE:");
                LoggingService.LogInfo($"Root: BTFlowNodeComposite ({rootComposite.GetNodeName()}) [Sequential, 3 batches]");
                foreach (var rc in rootComposite.GetChildren())
                {
                    if (rc is BTFlowNodeComposite bc)
                    {
                        LoggingService.LogInfo($"  Batch: {bc.GetNodeName()}");
                        foreach (var cc in bc.GetChildren())
                        {
                            if (cc is DynamicFlowNode dyn)
                                LoggingService.LogInfo($"    - {dyn.GetNodeName()}");
                        }
                    }
                }

                LoggingService.LogSuccess("\n Behavior tree with cassette flow nodes created successfully!");

                // Test the tree structure
                await TestBehaviorTreeStructure(behaviorTree);
                
                // Monitor planner execution in real-time
                await MonitorPlannerExecution();
                
                // Display NodeGraph status for each flow node
                await DisplayNodeGraphStatus(behaviorTree);
                
                // Track subtree status for high-level actions generated by flow nodes
                await TrackSubtreeStatusForHLActions(behaviorTree);
                
                // Execute tree with comprehensive logging
                await ExecuteTreeWithComprehensiveLogging(behaviorTree);
                
                
            }
            catch (Exception ex)
            {
                LoggingService.LogError($" Error creating behavior tree: {ex.Message}");
                LoggingService.LogError($"   Stack trace: {ex.StackTrace}");
            }
        }

        // Test the behavior tree structure
        private async Task TestBehaviorTreeStructure(BehaviorTree behaviorTree)
        {
            try
            {
                LoggingService.LogInfo("\n Testing behavior tree structure...");

                // Track memory usage before tree execution
                var memoryBefore = GC.GetTotalMemory(false);
                
                // Test initial tick
                BlackboardSummaryLogger.StartTreeTicking();
                var result = behaviorTree.Tick(0.0f);
                BlackboardSummaryLogger.EndTreeTicking();
                LoggingService.LogSuccess($" Initial tree tick result: {result}");
                
                // Track memory usage after tree execution
                var memoryAfter = GC.GetTotalMemory(false);
                
                // Track memory usage after planner execution
                var memoryAfterPlanner = GC.GetTotalMemory(false);

                // Test individual cassette nodes
                var rootNode = behaviorTree.root as FlowNode;
                if (rootNode != null)
                {
                    var children = rootNode.GetChildren();
                    LoggingService.LogSuccess($" Root node has {children.Count} children");

                    for (int i = 0; i < children.Count; i++)
                    {
                        var child = children[i];
                        if (child is FlowNode flowNode)
                        {
                            LoggingService.LogInfo($"   Child {i + 1}: {child.GetType().Name} - {flowNode.GetNodeName()}");
                        }
                        else
                        {
                            LoggingService.LogInfo($"   Child {i + 1}: {child.GetType().Name} - {child.DebugDisplayName}");
                        }
                    }
                }
                else
                {
                    LoggingService.LogError($" Root node is not a FlowNode. Actual type: {behaviorTree.root?.GetType().Name ?? "null"}");
                }

                LoggingService.LogSuccess(" Behavior tree structure test completed!");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($" Error testing behavior tree structure: {ex.Message}");
            }
        }

        // Display NodeGraph status for each flow node
        private async Task DisplayNodeGraphStatus(BehaviorTree behaviorTree)
        {
            try
            {
                LoggingService.LogSubsection(" NODEGRAPH STATUS REPORT");
                LoggingService.LogInfo("=".PadRight(50, '='));

                var rootNode = behaviorTree.root as FlowNode;
                if (rootNode != null)
                {
                    var children = rootNode.GetChildren();
                    LoggingService.LogInfo($" Checking {children.Count} flow nodes for NodeGraph status...\n");

                    for (int i = 0; i < children.Count; i++)
                    {
                        var child = children[i];
                        if (child is DynamicFlowNode dynamicNode)
                        {
                            LoggingService.LogInfo($" FLOW NODE {i + 1}: {dynamicNode.GetNodeName()}");
                            LoggingService.LogInfo($"    Node Type: {child.GetType().Name}");
                            
                            // Check if planning service is set
                            if (dynamicNode.ServicePlanning != null)
                            {
                                LoggingService.LogInfo($"    Planning Service: {dynamicNode.ServicePlanning.GetType().Name}");
                                
                                // Check if it's a ServicePlanning
                                if (dynamicNode.ServicePlanning is ServicePlanning plannerService)
                                {
                                    LoggingService.LogInfo($"    Has Generated NodeGraph: {plannerService.HasGeneratedNodeGraph()}");
                                    
                                    if (plannerService.HasGeneratedNodeGraph())
                                    {
                                        var generatedGraph = plannerService.GetGeneratedNodeGraph();
                                        var actions = generatedGraph.GetAllActionNodes();
                                        LoggingService.LogInfo($"    Generated NodeGraph Actions: {actions.Count}");
                                        
                                        // List the actions
                                        for (int j = 0; j < actions.Count; j++)
                                        {
                                            LoggingService.LogInfo($"      {j + 1}. {actions[j].InstanceName.ToString()}");
                                        }
                                    }
                                    else
                                    {
                                        LoggingService.LogInfo($"    No NodeGraph generated yet");
                                    }
                                }
                            }
                            else
                            {
                                LoggingService.LogInfo($"    No planning service set");
                            }
                            
                            // Check the actionGraph
                            var actionGraph = dynamicNode.GetActionGraph();
                            var actionGraphNodes = actionGraph.GetAllActionNodes();
                            LoggingService.LogInfo($"    ActionGraph Nodes: {actionGraphNodes.Count}");
                            
                            if (actionGraphNodes.Count > 0)
                            {
                                for (int j = 0; j < actionGraphNodes.Count; j++)
                                {
                                    LoggingService.LogInfo($"      {j + 1}. {actionGraphNodes[j].InstanceName.ToString()}");
                                }
                            }
                            
                            LoggingService.LogInfo("");
                        }
                    }
                }

                LoggingService.LogSuccess(" NodeGraph status report completed!");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($" Error displaying NodeGraph status: {ex.Message}");
            }
        }

        // Monitor planner execution in real-time
        private async Task MonitorPlannerExecution()
        {
            LoggingService.LogInfo("\n MONITORING PLANNER EXECUTION");
            LoggingService.LogInfo("=".PadRight(50, '='));
            
            if (allPlanners.Count == 0)
            {
                LoggingService.LogWarning(" No planners to monitor.");
                return;
            }
            
            LoggingService.LogInfo($" Monitoring {allPlanners.Count} planners...");
            LoggingService.LogInfo("Press any key to stop monitoring and continue...");
            
            var monitoringStartTime = DateTime.Now;
            var lastStatusTime = DateTime.Now;
            
            while (true)
            {
                // Check if any key is pressed (non-blocking)
                if (Console.KeyAvailable)
                {
                    Console.ReadKey(true); // Clear the key
                    break;
                }
                
                var currentTime = DateTime.Now;
                
                // Update status every 2 seconds
                if ((currentTime - lastStatusTime).TotalSeconds >= 2)
                {
                    Console.Clear();
                    LoggingService.LogInfo($" PLANNER EXECUTION STATUS - {currentTime:HH:mm:ss}");
                    LoggingService.LogInfo("=".PadRight(50, '='));
                    
                    var completedCount = allPlanners.Count(p => p.HasCompleted);
                    var executingCount = allPlanners.Count(p => p.IsExecuting);
                    var pendingCount = allPlanners.Count(p => !p.HasCompleted && !p.IsExecuting);
                    
                    LoggingService.LogInfo($" Progress: {completedCount}/{allPlanners.Count} completed, {executingCount} executing, {pendingCount} pending");
                    
                                         // Planning phase monitoring
                     if (rootNode is FlowNode flowNode)
                     {
                         var planningComplete = allPlanners.All(planner => planner.HasGeneratedNodeGraph());
                         LoggingService.LogInfo($"\n PLANNING PHASE STATUS:");
                         LoggingService.LogInfo($"   Planning Complete: {planningComplete}");
                         
                         var children = flowNode.GetChildren();
                         LoggingService.LogInfo(" SUBTREE STATUSES:");
                         for (int i = 0; i < children.Count; i++)
                         {
                             var child = children[i];
                             if (child is DynamicFlowNode dynamicNode)
                             {
                                 var hasPlanningService = dynamicNode.ServicePlanning != null;
                                 var planningServiceType = hasPlanningService ? dynamicNode.ServicePlanning.GetType().Name : "None";
                                 LoggingService.LogInfo($"   {dynamicNode.GetNodeName()}: ServicePlanning={planningServiceType}");
                             }
                         }
                     }
                    
                    LoggingService.LogInfo("");
                    
                    foreach (var planner in allPlanners)
                    {
                        var status = planner.HasCompleted ? "" : planner.IsExecuting ? "" : "";
                        var currentDuration = planner.IsExecuting ? currentTime - planner.StartTime : planner.TotalExecutionDuration;
                        var plannerDuration = planner.IsExecuting ? currentTime - planner.StartTime : planner.PlannerExecutionDuration;
                        
                        LoggingService.LogInfo($"{status} {planner.PlannerName}: Total={currentDuration:hh\\:mm\\:ss\\.fff}, Planner={plannerDuration:hh\\:mm\\:ss\\.fff}");
                    }
                    
                    LoggingService.LogInfo("\nPress any key to stop monitoring...");
                    lastStatusTime = currentTime;
                }
                
                // Check if all planners are done
                if (allPlanners.All(p => p.HasCompleted || (!p.IsExecuting && !p.HasCompleted)))
                {
                    LoggingService.LogInfo("\n All planners have finished execution!");
                    break;
                }
                
                await Task.Delay(100); // Small delay to prevent high CPU usage
            }
            
            LoggingService.LogInfo($" Monitoring duration: {DateTime.Now - monitoringStartTime:hh\\:mm\\:ss\\.fff}");
        }
        
        // Display execution summary for all planners
        private async Task DisplayExecutionSummary()
        {
            LoggingService.LogSubsection(" PLANNER EXECUTION SUMMARY");
            LoggingService.LogInfo("=".PadRight(80, '='));
            
            if (allPlanners.Count == 0)
            {
                LoggingService.LogWarning(" No planners were executed during this test.");
                return;
            }
            
            LoggingService.LogInfo($" Total planners executed: {allPlanners.Count}");
            LoggingService.LogInfo("");
            
            // Sort planners by start time
            var sortedPlanners = allPlanners.OrderBy(p => p.StartTime).ToList();
            
            for (int i = 0; i < sortedPlanners.Count; i++)
            {
                var planner = sortedPlanners[i];
                LoggingService.LogInfo($" PLANNER {i + 1}: {planner.PlannerName}");
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
                    LoggingService.LogInfo($"    Still executing... (Started: {planner.StartTime:HH:mm:ss.fff})");
                }
                else
                {
                    LoggingService.LogError($"    Failed or incomplete");
                }
                LoggingService.LogInfo("");
            }
            
            // Summary statistics
            var completedPlanners = allPlanners.Where(p => p.HasCompleted).ToList();
            var failedPlanners = allPlanners.Where(p => !p.HasCompleted && !p.IsExecuting).ToList();
            var executingPlanners = allPlanners.Where(p => p.IsExecuting).ToList();
            
            LoggingService.LogInfo(" EXECUTION STATISTICS:");
            LoggingService.LogInfo($"    Successfully completed: {completedPlanners.Count}");
            LoggingService.LogError($"    Failed: {failedPlanners.Count}");
            LoggingService.LogInfo($"    Still executing: {executingPlanners.Count}");
            
            if (completedPlanners.Any())
            {
                var avgPlannerDuration = TimeSpan.FromMilliseconds(completedPlanners.Average(p => p.PlannerExecutionDuration.TotalMilliseconds));
                var avgTotalDuration = TimeSpan.FromMilliseconds(completedPlanners.Average(p => p.TotalExecutionDuration.TotalMilliseconds));
                var minPlannerDuration = completedPlanners.Min(p => p.PlannerExecutionDuration);
                var maxPlannerDuration = completedPlanners.Max(p => p.PlannerExecutionDuration);
                var minTotalDuration = completedPlanners.Min(p => p.TotalExecutionDuration);
                var maxTotalDuration = completedPlanners.Max(p => p.TotalExecutionDuration);
                
                LoggingService.LogInfo($"    Average Planner Duration: {avgPlannerDuration:hh\\:mm\\:ss\\.fff}");
                LoggingService.LogInfo($"    Average Total Duration: {avgTotalDuration:hh\\:mm\\:ss\\.fff}");
                LoggingService.LogInfo($"    Fastest Planner: {minPlannerDuration:hh\\:mm\\:ss\\.fff}");
                LoggingService.LogInfo($"    Slowest Planner: {maxPlannerDuration:hh\\:mm\\:ss\\.fff}");
                LoggingService.LogInfo($"    Fastest Total: {minTotalDuration:hh\\:mm\\:ss\\.fff}");
                LoggingService.LogInfo($"    Slowest Total: {maxTotalDuration:hh\\:mm\\:ss\\.fff}");
            }
            
            LoggingService.LogInfo("=".PadRight(80, '='));
            
            // Display blackboard tracking statistics
            DisplayBlackboardTrackingSummary();
        }
        
        // Display blackboard tracking summary
        private void DisplayBlackboardTrackingSummary()
        {
            LoggingService.LogSubsection(" BLACKBOARD TRACKING SUMMARY");
            LoggingService.LogInfo("=".PadRight(80, '='));
            
            try
            {
                // Get current blackboard tracking statistics
                var (types, instances, negations) = BlackboardTrackingLogger.GetCurrentCounts();
                
                LoggingService.LogInfo($" Total New Types Added: {types}");
                LoggingService.LogInfo($" Total New Instances Created: {instances}");
                LoggingService.LogInfo($" Total Predicate Negations: {negations}");
                
                LoggingService.LogInfo($" Blackboard tracking log saved to: {BlackboardTrackingLogger.GetLogFilePath()}");
                LoggingService.LogInfo("=".PadRight(80, '='));
            }
            catch (Exception ex)
            {
                LoggingService.LogWarning($" Could not retrieve blackboard tracking statistics: {ex.Message}");
            }
        }
        
        // Track subtree status for high-level actions generated by flow nodes
        private async Task TrackSubtreeStatusForHLActions(BehaviorTree behaviorTree)
        {
            LoggingService.LogSubsection(" TRACKING SUBTREE STATUS FOR HL ACTIONS");
            LoggingService.LogInfo("=".PadRight(60, '='));
            
                         try
             {
                 var rootNode = behaviorTree.root as FlowNode;
                 if (rootNode == null)
                 {
                     LoggingService.LogError(" Root node is not a FlowNode");
                     return;
                 }

                var children = rootNode.GetChildren();
                LoggingService.LogInfo($" Tracking subtrees for {children.Count} flow nodes...\n");

                for (int i = 0; i < children.Count; i++)
                {
                    var child = children[i];
                    if (child is DynamicFlowNode dynamicNode)
                    {
                        LoggingService.LogInfo($" FLOW NODE {i + 1}: {dynamicNode.GetNodeName()}");
                        
                        // Check if planning service has generated a NodeGraph
                        if (dynamicNode.ServicePlanning is ServicePlanning plannerService && plannerService.HasGeneratedNodeGraph())
                        {
                            var generatedGraph = plannerService.GetGeneratedNodeGraph();
                            var actions = generatedGraph.GetAllActionNodes();
                            
                            LoggingService.LogInfo($"    Generated {actions.Count} actions from planner");
                            
                            // Track subtree status for each action
                            for (int j = 0; j < actions.Count; j++)
                            {
                                var action = actions[j];
                                if (action is PActionNode genericAction)
                                {
                                    LoggingService.LogInfo($"    Action {j + 1}: {action.InstanceName.ToString()}");
                                    
                                    // Check if this is a high-level action
                                    if (genericAction.IsHighLevelAction)
                                    {
                                        LoggingService.LogInfo($"       Is High-Level Action: Yes");
                                        
                                        // Check if it has a subtree
                                        if (genericAction.HighLevelSubtree != null)
                                        {
                                            LoggingService.LogInfo($"       Has Subtree: Yes");
                                            LoggingService.LogInfo($"       Subtree Type: {genericAction.HighLevelSubtree.GetType().Name}");
                                            LoggingService.LogInfo($"       Subtree Status: {genericAction.HighLevelSubtree.status}");
                                            
                                            // Check if subtree has actions
                                            var subtreeActionGraph = genericAction.HighLevelSubtree.GetActionGraph();
                                            var subtreeActions = subtreeActionGraph.GetAllActionNodes();
                                            LoggingService.LogInfo($"       Subtree Actions: {subtreeActions.Count}");
                                            
                                            // List subtree actions and their status
                                            for (int k = 0; k < subtreeActions.Count; k++)
                                            {
                                                var subtreeAction = subtreeActions[k];
                                                LoggingService.LogInfo($"         {k + 1}. {subtreeAction.InstanceName.ToString()} - Status: {subtreeAction.status}");
                                            }
                                        }
                                        else
                                        {
                                            LoggingService.LogInfo($"       Has Subtree: No");
                                        }
                                        
                                        // Check if it has a planning service
                                        if (genericAction.ServicePlanning != null)
                                        {
                                            LoggingService.LogInfo($"       Has Planning Service: Yes ({genericAction.ServicePlanning.GetType().Name})");
                                        }
                                        else
                                        {
                                            LoggingService.LogInfo($"       Has Planning Service: No");
                                        }
                                    }
                                    else
                                    {
                                        LoggingService.LogInfo($"       Is High-Level Action: No");
                                    }
                                    
                                    // Check if it has a ServiceSubtreeInject
                                    var subtreeService = genericAction.GetSubtreeInjectionService();
                                    if (subtreeService != null)
                                    {
                                        LoggingService.LogInfo($"       Has ServiceSubtreeInject: Yes");
                                        
                                        // Check if any problem files were generated
                                        var generatedFiles = ServicePDDLPlanning.GeneratedProblemFiles;
                                        if (generatedFiles.Count > 0)
                                        {
                                            LoggingService.LogInfo($"       Generated Problem Files: {generatedFiles.Count}");
                                            foreach (var file in generatedFiles)
                                            {
                                                LoggingService.LogInfo($"         - {file}");
                                            }
                                        }
                                    }
                                    else
                                    {
                                        LoggingService.LogInfo($"       Has ServiceSubtreeInject: No");
                                    }
                                }
                                else
                                {
                                    LoggingService.LogInfo($"    Action {j + 1}: {action.InstanceName.ToString()} (Not a GenericBTAction)");
                                }
                                LoggingService.LogInfo("");
                            }
                        }
                        else
                        {
                            LoggingService.LogInfo($"    No NodeGraph generated yet by planner");
                        }
                        
                        LoggingService.LogInfo("");
                    }
                }
                
                LoggingService.LogSuccess(" Subtree status tracking completed!");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($" Error tracking subtree status: {ex.Message}");
                LoggingService.LogError($"   Stack trace: {ex.StackTrace}");
            }
        }

        // Execute tree with comprehensive logging
        private async Task ExecuteTreeWithComprehensiveLogging(BehaviorTree behaviorTree)
        {
            LoggingService.LogSection(" EXECUTING TREE WITH COMPREHENSIVE LOGGING");
            
            try
            {
                int tickCount = 0;
                
                // Dictionary to track action status changes
                var actionStatusHistory = new Dictionary<string, BTNodeResult>();
                
                LoggingService.LogInfo($" Starting tree execution (unlimited ticks)...");
                LoggingService.LogInfo("Press any key to stop execution...");
                
                while (true)
                {
                    // Check if any key is pressed (non-blocking)
                    if (Console.KeyAvailable)
                    {
                        Console.ReadKey(true); // Clear the key
                        LoggingService.LogWarning(" Execution stopped by user");
                        break;
                    }
                    
                    tickCount++;
                    
                    // Log tick start
                    LoggingService.LogInfo($"\n TICK {tickCount} STARTING...");
                    
                    // Execute one tick
                    BlackboardSummaryLogger.StartTreeTicking();
                    var result = behaviorTree.Tick(tickIntervalMilliseconds / 1000f);
                    BlackboardSummaryLogger.EndTreeTicking();
                    
                    // Log comprehensive tick information
                    LogComprehensiveTickInfo(behaviorTree, tickCount, actionStatusHistory);
                    
                    // Check if tree has finished
                    if (behaviorTree.HasFinished())
                    {
                        LoggingService.LogSuccess($"\n Tree execution completed after {tickCount} ticks");
                        LoggingService.LogSuccess($" Final result: {result}");
                        break;
                    }
                    
                    // Small delay between ticks
                    await Task.Delay(tickIntervalMilliseconds);
                }
                
                // Print final status summary
                LogFinalActionStatusSummary(actionStatusHistory);
                
                LoggingService.LogSuccess(" Tree execution with comprehensive logging completed!");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($" Error during tree execution: {ex.Message}");
            }
        }

        // Log comprehensive tick information including NodeGraph details and order relations
        private void LogComprehensiveTickInfo(BehaviorTree behaviorTree, int tickNumber, Dictionary<string, BTNodeResult> actionStatusHistory)
        {
            try
            {
                var rootNode = behaviorTree.root;
                if (rootNode == null) return;

                // Log NodeGraph information for each flow node
                LogNodeGraphDetails(behaviorTree, tickNumber);
                
                // Log action status changes
                LogActionStatusChanges(behaviorTree, tickNumber, actionStatusHistory);
                
                // Log subtree status for high-level actions
                LogSubtreeStatusForHLActions(behaviorTree, tickNumber);
                
                // Log detailed subtree NodeGraph information on every tick
                LogDetailedSubtreeNodeGraphs(behaviorTree, tickNumber);
                
            }
            catch (Exception ex)
            {
                LoggingService.LogError($" Error logging comprehensive tick info on tick {tickNumber}: {ex.Message}");
            }
        }

        // Log NodeGraph details including order relations
        private void LogNodeGraphDetails(BehaviorTree behaviorTree, int tickNumber)
        {
            try
            {
                var rootNode = behaviorTree.root as FlowNode;
                if (rootNode == null) return;

                var children = rootNode.GetChildren();
                
                foreach (var child in children)
                {
                    if (child is DynamicFlowNode dynamicNode)
                    {
                        var actionGraph = dynamicNode.GetActionGraph();
                        var nodes = actionGraph.GetAllActionNodes();
                        
                        if (nodes.Count > 0)
                        {
                            LoggingService.LogInfo($"\n NODEGRAPH DETAILS ({dynamicNode.GetNodeName()}) - TICK {tickNumber}:");
                            LoggingService.LogInfo($"    Total nodes: {nodes.Count}");
                            
                            // Log each node's details
                            foreach (var action in nodes)
                            {
                                var nodeInfo = actionGraph.GetNodeInfo(action);
                                if (nodeInfo != null)
                                {
                                    var statusEmoji = action.status switch
                                    {
                                        BTNodeResult.Success => "",
                                        BTNodeResult.Failure => "",
                                        BTNodeResult.InProgress => "",
                                        BTNodeResult.ReadyToTick => "",
                                        _ => ""
                                    };
                                    
                                    LoggingService.LogInfo($"   {statusEmoji} {action.InstanceName}: Status={action.status}, Completed={nodeInfo.IsCompleted}, Predecessors={nodeInfo.Predecessors.Count}");
                                    
                                    // Log order relations for this node
                                    if (nodeInfo.Predecessors.Count > 0)
                                    {
                                        LoggingService.LogInfo($"       Predecessors:");
                                        foreach (var pred in nodeInfo.Predecessors)
                                        {
                                            LoggingService.LogInfo($"         - {pred.From.ActionNode.InstanceName} (MEETS)");
                                        }
                                    }
                                    
                                    if (nodeInfo.Successors.Count > 0)
                                    {
                                        LoggingService.LogInfo($"       Successors:");
                                        foreach (var succ in nodeInfo.Successors)
                                        {
                                            LoggingService.LogInfo($"         - {succ.To.ActionNode.InstanceName} (MEETS)");
                                        }
                                    }
                                }
                            }
                            
                            // Log all order relations in the graph
                            LoggingService.LogInfo($"    ALL ORDER RELATIONS:");
                            foreach (var node in nodes)
                            {
                                var nodeInfo = actionGraph.GetNodeInfo(node);
                                if (nodeInfo != null && nodeInfo.Successors.Count > 0)
                                {
                                    foreach (var successor in nodeInfo.Successors)
                                    {
                                        LoggingService.LogInfo($"      {node.InstanceName} MEETS {successor.To.ActionNode.InstanceName}");
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($" Error logging NodeGraph details: {ex.Message}");
            }
        }

        // Log action status changes
        private void LogActionStatusChanges(BehaviorTree behaviorTree, int tickNumber, Dictionary<string, BTNodeResult> actionStatusHistory)
        {
            try
            {
                var actionNodes = GetAllActionNodes(behaviorTree.root);
                bool hasStatusChanges = false;

                foreach (var actionNode in actionNodes)
                {
                    var actionId = actionNode is ActionNode actionBase ? actionBase.InstanceName.ToString() : actionNode.GetType().Name;
                    var currentStatus = actionNode.status;

                    // Check if status has changed
                    if (!actionStatusHistory.ContainsKey(actionId) || actionStatusHistory[actionId] != currentStatus)
                    {
                        if (!hasStatusChanges)
                        {
                            LoggingService.LogInfo($"\n TICK {tickNumber} - ACTION STATUS CHANGES:");
                            hasStatusChanges = true;
                        }

                        var statusEmoji = currentStatus switch
                        {
                            BTNodeResult.Success => "",
                            BTNodeResult.Failure => "",
                            BTNodeResult.InProgress => "",
                            BTNodeResult.ReadyToTick => "",
                            _ => ""
                        };

                        LoggingService.LogInfo($"   {statusEmoji} {actionId}: {currentStatus}");
                        
                        // Update history
                        actionStatusHistory[actionId] = currentStatus;
                    }
                }

                // If no status changes, show a brief progress indicator every 5 ticks
                if (!hasStatusChanges && tickNumber % 5 == 0)
                {
                    var activeActions = actionNodes.Count(a => a.status == BTNodeResult.InProgress);
                    var completedActions = actionNodes.Count(a => a.status == BTNodeResult.Success);
                    var failedActions = actionNodes.Count(a => a.status == BTNodeResult.Failure);
                    
                    LoggingService.LogInfo($"    Tick {tickNumber}: {activeActions} active, {completedActions} completed, {failedActions} failed");
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($" Error logging action status changes: {ex.Message}");
            }
        }

        // Log subtree status for high-level actions
        private void LogSubtreeStatusForHLActions(BehaviorTree behaviorTree, int tickNumber)
        {
            try
            {
                var rootNode = behaviorTree.root as FlowNode;
                if (rootNode == null) return;

                var children = rootNode.GetChildren();
                bool hasSubtreeChanges = false;
                
                foreach (var child in children)
                {
                    if (child is DynamicFlowNode dynamicNode)
                    {
                        if (dynamicNode.ServicePlanning is ServicePlanning plannerService && plannerService.HasGeneratedNodeGraph())
                        {
                            var generatedGraph = plannerService.GetGeneratedNodeGraph();
                            var actions = generatedGraph.GetAllActionNodes();
                            
                            foreach (var action in actions)
                            {
                                if (action is PActionNode genericAction && genericAction.IsHighLevelAction)
                                {
                                    if (genericAction.HighLevelSubtree != null)
                                    {
                                        var subtreeActionGraph = genericAction.HighLevelSubtree.GetActionGraph();
                                        var subtreeActions = subtreeActionGraph.GetAllActionNodes();
                                        
                                        // Log detailed subtree NodeGraph information
                                        if (subtreeActions.Count > 0)
                                        {
                                            LoggingService.LogInfo($"\n SUBTREE NODEGRAPH DETAILS ({genericAction.InstanceName}) - TICK {tickNumber}:");
                                            LoggingService.LogInfo($"    Total subtree nodes: {subtreeActions.Count}");
                                            
                                            // Log each subtree node's details
                                            foreach (var subtreeAction in subtreeActions)
                                            {
                                                var subtreeNodeInfo = subtreeActionGraph.GetNodeInfo(subtreeAction);
                                                if (subtreeNodeInfo != null)
                                                {
                                                    var statusEmoji = subtreeAction.status switch
                                                    {
                                                        BTNodeResult.Success => "",
                                                        BTNodeResult.Failure => "",
                                                        BTNodeResult.InProgress => "",
                                                        BTNodeResult.ReadyToTick => "",
                                                        _ => ""
                                                    };
                                                    
                                                    LoggingService.LogInfo($"   {statusEmoji} {subtreeAction.InstanceName}: Status={subtreeAction.status}, Completed={subtreeNodeInfo.IsCompleted}, Predecessors={subtreeNodeInfo.Predecessors.Count}");
                                                    
                                                    // Log order relations for this subtree node
                                                    if (subtreeNodeInfo.Predecessors.Count > 0)
                                                    {
                                                        LoggingService.LogInfo($"       Subtree Predecessors:");
                                                        foreach (var pred in subtreeNodeInfo.Predecessors)
                                                        {
                                                            LoggingService.LogInfo($"         - {pred.From.ActionNode.InstanceName} (MEETS)");
                                                        }
                                                    }
                                                    
                                                    if (subtreeNodeInfo.Successors.Count > 0)
                                                    {
                                                        LoggingService.LogInfo($"       Subtree Successors:");
                                                        foreach (var succ in subtreeNodeInfo.Successors)
                                                        {
                                                            LoggingService.LogInfo($"         - {succ.To.ActionNode.InstanceName} (MEETS)");
                                                        }
                                                    }
                                                }
                                            }
                                            
                                            // Log all order relations in the subtree graph
                                            LoggingService.LogInfo($"    SUBTREE ORDER RELATIONS:");
                                            foreach (var subtreeAction in subtreeActions)
                                            {
                                                var subtreeNodeInfo = subtreeActionGraph.GetNodeInfo(subtreeAction);
                                                if (subtreeNodeInfo != null && subtreeNodeInfo.Successors.Count > 0)
                                                {
                                                    foreach (var successor in subtreeNodeInfo.Successors)
                                                    {
                                                        LoggingService.LogInfo($"      {subtreeAction.InstanceName} MEETS {successor.To.ActionNode.InstanceName}");
                                                    }
                                                }
                                            }
                                        }
                                        
                                        // Check if any subtree actions have changed status
                                        foreach (var subtreeAction in subtreeActions)
                                        {
                                            if (subtreeAction.status == BTNodeResult.InProgress || 
                                                subtreeAction.status == BTNodeResult.Success || 
                                                subtreeAction.status == BTNodeResult.Failure)
                                            {
                                                if (!hasSubtreeChanges)
                                                {
                                                    LoggingService.LogInfo($"\n TICK {tickNumber} - SUBTREE STATUS UPDATE:");
                                                    hasSubtreeChanges = true;
                                                }
                                                
                                                var statusEmoji = subtreeAction.status switch
                                                {
                                                    BTNodeResult.Success => "",
                                                    BTNodeResult.Failure => "",
                                                    BTNodeResult.InProgress => "",
                                                    BTNodeResult.ReadyToTick => "",
                                                    _ => ""
                                                };
                                                
                                                LoggingService.LogInfo($"   {statusEmoji} {genericAction.InstanceName} -> {subtreeAction.InstanceName}: {subtreeAction.status}");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($" Error logging subtree status: {ex.Message}");
            }
        }
        // Log detailed subtree NodeGraph information on every tick
        private void LogDetailedSubtreeNodeGraphs(BehaviorTree behaviorTree, int tickNumber)
        {
            try
            {
                var rootNode = behaviorTree.root as FlowNode;
                if (rootNode == null) return;

                var children = rootNode.GetChildren();
                
                foreach (var child in children)
                {
                    if (child is DynamicFlowNode dynamicNode)
                    {
                        if (dynamicNode.ServicePlanning is ServicePlanning plannerService && plannerService.HasGeneratedNodeGraph())
                        {
                            var generatedGraph = plannerService.GetGeneratedNodeGraph();
                            var actions = generatedGraph.GetAllActionNodes();
                            
                            foreach (var action in actions)
                            {
                                if (action is PActionNode genericAction && genericAction.IsHighLevelAction)
                                {
                                    if (genericAction.HighLevelSubtree != null)
                                    {
                                        var subtreeActionGraph = genericAction.HighLevelSubtree.GetActionGraph();
                                        var subtreeActions = subtreeActionGraph.GetAllActionNodes();
                                        
                                        if (subtreeActions.Count > 0)
                                        {
                                            LoggingService.LogInfo($"\n SUBTREE EXECUTION DETAILS ({genericAction.InstanceName}) - TICK {tickNumber}:");
                                            
                                            // Count statuses
                                            var succeededCount = subtreeActions.Count(a => a.status == BTNodeResult.Success);
                                            var failedCount = subtreeActions.Count(a => a.status == BTNodeResult.Failure);
                                            var inProgressCount = subtreeActions.Count(a => a.status == BTNodeResult.InProgress);
                                            var readyCount = subtreeActions.Count(a => a.status == BTNodeResult.ReadyToTick);
                                            
                                            LoggingService.LogInfo($"    Subtree Progress: {succeededCount} {inProgressCount} {failedCount} {readyCount}");
                                            
                                            // Log each subtree action with its current status
                                            foreach (var subtreeAction in subtreeActions)
                                            {
                                                var statusEmoji = subtreeAction.status switch
                                                {
                                                    BTNodeResult.Success => "",
                                                    BTNodeResult.Failure => "",
                                                    BTNodeResult.InProgress => "",
                                                    BTNodeResult.ReadyToTick => "",
                                                    _ => ""
                                                };
                                                
                                                var subtreeNodeInfo = subtreeActionGraph.GetNodeInfo(subtreeAction);
                                                var predecessorCount = subtreeNodeInfo?.Predecessors.Count ?? 0;
                                                var completed = subtreeNodeInfo?.IsCompleted ?? false;
                                                
                                                LoggingService.LogInfo($"   {statusEmoji} {subtreeAction.InstanceName}: {subtreeAction.status} (Predecessors: {predecessorCount}, Completed: {completed})");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($" Error logging detailed subtree NodeGraphs: {ex.Message}");
            }
        }

        // Log final action status summary
        private void LogFinalActionStatusSummary(Dictionary<string, BTNodeResult> actionStatusHistory)
        {
            LoggingService.LogSubsection(" FINAL ACTION STATUS SUMMARY");
            
            var succeededActions = actionStatusHistory.Where(kvp => kvp.Value == BTNodeResult.Success).ToList();
            var failedActions = actionStatusHistory.Where(kvp => kvp.Value == BTNodeResult.Failure).ToList();
            var inProgressActions = actionStatusHistory.Where(kvp => kvp.Value == BTNodeResult.InProgress).ToList();
            var readyActions = actionStatusHistory.Where(kvp => kvp.Value == BTNodeResult.ReadyToTick).ToList();

            LoggingService.LogSuccess($" SUCCEEDED ({succeededActions.Count}):");
            foreach (var action in succeededActions)
            {
                LoggingService.LogSuccess($"   - {action.Key}");
            }

            LoggingService.LogError($"\n FAILED ({failedActions.Count}):");
            foreach (var action in failedActions)
            {
                LoggingService.LogError($"   - {action.Key}");
            }

            LoggingService.LogInfo($"\n IN PROGRESS ({inProgressActions.Count}):");
            foreach (var action in inProgressActions)
            {
                LoggingService.LogInfo($"   - {action.Key}");
            }

            LoggingService.LogInfo($"\n READY TO TICK ({readyActions.Count}):");
            foreach (var action in readyActions)
            {
                LoggingService.LogInfo($"   - {action.Key}");
            }
        }

        // Helper method to get all action nodes from the tree
        private List<IBTNode> GetAllActionNodes(IBTNode node)
        {
            var actionNodes = new List<IBTNode>();
            
            if (node is IBTNode actionNode)
            {
                actionNodes.Add(actionNode);
            }
            
            if (node is FlowNode flowNode)
            {
                foreach (var child in flowNode.GetChildren())
                {
                    actionNodes.AddRange(GetAllActionNodes(child));
                }
            }
            
            return actionNodes;
        }

        // Public method to run the test from Program.cs
        public static async Task RunTest(bool useModelLoader = true)
        {
            var test = new FullTreeTest(useModelLoader);
            await test.RunFullTreeTest();
        }
    }
}
