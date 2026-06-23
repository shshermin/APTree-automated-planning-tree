﻿﻿﻿using System;
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
        
        public async Task RunFullTreeTest()
        {
            // Silence all direct Console.WriteLine calls (factories, blackboard, etc.)
            // File logging is unaffected — it uses its own StreamWriter via LogFileManager
            Console.SetOut(TextWriter.Null);

            // Initialize logging service (console disabled, file logging only)
            LoggingService.Initialize("FullTreeTest", enableConsole: false, enableFile: true);
            
            // Initialize execution flow logger (console disabled, file logging only)
            ExecutionFlowLogger.Initialize("FullTreeTest", enableConsole: false, enableFile: true);
            
            // BlackboardTrackingLogger is automatically initialized when first accessed
            // No need to call Initialize() explicitly
            
            testStartTime = DateTime.Now;
            
            LoggingService.LogSection("ÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸Ãƒâ€¦Ã¢â‚¬â„¢Ãƒâ€šÃ‚Â³ FULL BEHAVIOR TREE TEST");
            LoggingService.LogSuccess($"ÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸Ãƒâ€¦Ã‚Â¡ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ Started at: {testStartTime:yyyy-MM-dd HH:mm:ss.fff}");

            try
            {
                // Create blackboard instance (without Neo4j)
                using var blackboard = new Blackboard<FastName>();

                // Create BlackboardWriter for type registration
                var blackboardWriter = new BlackboardWriter(blackboard);

                // Register all types
                LoggingService.LogSection("REGISTERING ALL TYPES");
                blackboardWriter.RegisterAllTypes();

                // Register all instances from files
                LoggingService.LogSection("REGISTERING ALL INSTANCES FROM FILES");
                string actionInstancesFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "src", "InputInstances", "ActionInstances.txt");
                blackboardWriter.RegisterAllInstances(actionInstancesFile);

                // Capture blackboard state before ticking starts
                LoggingService.LogSection("CAPTURING BLACKBOARD STATE BEFORE TICKING");
                BlackboardSummaryLogger.CaptureBlackboardState(blackboard);

                // Inspect blackboard contents
                LoggingService.LogSection("INSPECTING BLACKBOARD CONTENTS");
                await InspectBlackboard(blackboard);

                // Create behavior tree with cassette flow nodes
                LoggingService.LogSection("CREATING BEHAVIOR TREE WITH CASSETTE FLOW NODES");
                await CreateCassetteBehaviorTree(blackboard);

                testEndTime = DateTime.Now;
                
                LoggingService.LogSection("ÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸Ãƒâ€¦Ã‚Â½ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â° FULL BEHAVIOR TREE TEST COMPLETED!");
                LoggingService.LogSuccess($"ÃƒÆ’Ã‚Â¢Ãƒâ€šÃ‚ÂÃƒâ€šÃ‚Â° Finished at: {testEndTime:yyyy-MM-dd HH:mm:ss.fff}");
                LoggingService.LogSuccess($"ÃƒÆ’Ã‚Â¢Ãƒâ€šÃ‚ÂÃƒâ€šÃ‚Â±ÃƒÆ’Ã‚Â¯Ãƒâ€šÃ‚Â¸Ãƒâ€šÃ‚Â Total test duration: {testEndTime - testStartTime:hh\\:mm\\:ss\\.fff}");
                
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
                LoggingService.LogError($"\nÃƒÆ’Ã‚Â¢Ãƒâ€šÃ‚ÂÃƒâ€¦Ã¢â‚¬â„¢ ERROR during full tree test: {ex.Message}");
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

        // Inspect blackboard contents
        private async Task InspectBlackboard(Blackboard<FastName> blackboard)
        {
            LoggingService.LogSubsection("ÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¹ BLACKBOARD INSPECTION REPORT");

            try
            {
                // 1. CustomProperty Types
                var entityTypes = blackboard.GetAllEntityTypes();
                LoggingService.LogInfo($"\nÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸Ãƒâ€šÃ‚ÂÃƒâ€šÃ‚Â·ÃƒÆ’Ã‚Â¯Ãƒâ€šÃ‚Â¸Ãƒâ€šÃ‚Â  ENTITY TYPES ({entityTypes.Count}):");
                foreach (var entityType in entityTypes)
                {
                    LoggingService.LogInfo($"   - {entityType.ToString()}");
                }

                // 2. Predicate Types
                var predicateTypes = blackboard.GetAllPredicateTypes();
                LoggingService.LogInfo($"\nÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸ÃƒÂ¢Ã¢â€šÂ¬Ã‚ÂÃƒâ€šÃ‚Â PREDICATE TYPES ({predicateTypes.Count}):");
                foreach (var predicateType in predicateTypes)
                {
                    LoggingService.LogInfo($"   - {predicateType.ToString()}");
                }

                // 3. Action Types
                var actionTypes = blackboard.GetAllActionTypes();
                LoggingService.LogInfo($"\nÃƒÆ’Ã‚Â¢Ãƒâ€¦Ã‚Â¡Ãƒâ€šÃ‚Â¡ ACTION TYPES ({actionTypes.Count}):");
                foreach (var actionType in actionTypes)
                {
                    LoggingService.LogInfo($"   - {actionType.ToString()}");
                }

                // 4. Action Instances
                var actionInstances = blackboard.GetAllActionInstances();
                LoggingService.LogInfo($"\nÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸Ãƒâ€¦Ã‚Â½Ãƒâ€šÃ‚Â¯ ACTION INSTANCES ({actionInstances.Count}):");
                foreach (var actionInstance in actionInstances)
                {
                    LoggingService.LogInfo($"   - {actionInstance.InstanceName.ToString()} (Type: {actionInstance.actionType.ToString()})");
                }

                // 5. Built-in Values
                LoggingService.LogInfo($"\nÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œÃƒâ€šÃ‚Â BUILT-IN VALUES:");
                LoggingService.LogInfo($"   - Int Values: {GetDictionaryCount(blackboard, "IntValues")}");
                if (GetDictionaryCount(blackboard, "IntValues") > 0)
                {
                    foreach (var item in GetDictionaryItems(blackboard, "IntValues"))
                    {
                        LoggingService.LogInfo($"     ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€šÃ‚Â¢ {item.Key}: {item.Value}");
                    }
                }
                
                LoggingService.LogInfo($"   - Double Values: {GetDictionaryCount(blackboard, "DoubleValues")}");
                if (GetDictionaryCount(blackboard, "DoubleValues") > 0)
                {
                    foreach (var item in GetDictionaryItems(blackboard, "DoubleValues"))
                    {
                        LoggingService.LogInfo($"     ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€šÃ‚Â¢ {item.Key}: {item.Value}");
                    }
                }
                
                LoggingService.LogInfo($"   - Bool Values: {GetDictionaryCount(blackboard, "BoolValues")}");
                if (GetDictionaryCount(blackboard, "BoolValues") > 0)
                {
                    foreach (var item in GetDictionaryItems(blackboard, "BoolValues"))
                    {
                        LoggingService.LogInfo($"     ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€šÃ‚Â¢ {item.Key}: {item.Value}");
                    }
                }
                
                LoggingService.LogInfo($"   - String Values: {GetDictionaryCount(blackboard, "StringValues")}");
                if (GetDictionaryCount(blackboard, "StringValues") > 0)
                {
                    foreach (var item in GetDictionaryItems(blackboard, "StringValues"))
                    {
                        LoggingService.LogInfo($"     ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€šÃ‚Â¢ {item.Key}: {item.Value}");
                    }
                }

                // 6. CustomProperty Values
                LoggingService.LogInfo($"\nÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸Ãƒâ€šÃ‚ÂÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬ÂÃƒÆ’Ã‚Â¯Ãƒâ€šÃ‚Â¸Ãƒâ€šÃ‚Â  ENTITY VALUES:");
                LoggingService.LogInfo($"   - Element Values: {GetDictionaryCount(blackboard, "ElementValues")}");
                if (GetDictionaryCount(blackboard, "ElementValues") > 0)
                {
                    foreach (var item in GetDictionaryItems(blackboard, "ElementValues"))
                    {
                        LoggingService.LogInfo($"     ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€šÃ‚Â¢ {item.Key}: {item.Value}");
                    }
                }
                
                LoggingService.LogInfo($"   - Location Values: {GetDictionaryCount(blackboard, "LocationValues")}");
                if (GetDictionaryCount(blackboard, "LocationValues") > 0)
                {
                    foreach (var item in GetDictionaryItems(blackboard, "LocationValues"))
                    {
                        LoggingService.LogInfo($"     ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€šÃ‚Â¢ {item.Key}: {item.Value}");
                    }
                }
                
                LoggingService.LogInfo($"   - Agent Values: {GetDictionaryCount(blackboard, "AgentValues")}");
                if (GetDictionaryCount(blackboard, "AgentValues") > 0)
                {
                    foreach (var item in GetDictionaryItems(blackboard, "AgentValues"))
                    {
                        LoggingService.LogInfo($"     ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€šÃ‚Â¢ {item.Key}: {item.Value}");
                    }
                }
                
                LoggingService.LogInfo($"   - Layer Values: {GetDictionaryCount(blackboard, "LayerValues")}");
                if (GetDictionaryCount(blackboard, "LayerValues") > 0)
                {
                    foreach (var item in GetDictionaryItems(blackboard, "LayerValues"))
                    {
                        LoggingService.LogInfo($"     ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€šÃ‚Â¢ {item.Key}: {item.Value}");
                    }
                }
                
                LoggingService.LogInfo($"   - Module Values: {GetDictionaryCount(blackboard, "ModuleValues")}");
                if (GetDictionaryCount(blackboard, "ModuleValues") > 0)
                {
                    foreach (var item in GetDictionaryItems(blackboard, "ModuleValues"))
                    {
                        LoggingService.LogInfo($"     ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€šÃ‚Â¢ {item.Key}: {item.Value}");
                    }
                }
                
                LoggingService.LogInfo($"   - Tool Values: {GetDictionaryCount(blackboard, "ToolValues")}");
                if (GetDictionaryCount(blackboard, "ToolValues") > 0)
                {
                    foreach (var item in GetDictionaryItems(blackboard, "ToolValues"))
                    {
                        LoggingService.LogInfo($"     ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€šÃ‚Â¢ {item.Key}: {item.Value}");
                    }
                }

                // 7. Predicate Values
                LoggingService.LogInfo($"\nÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸ÃƒÂ¢Ã¢â€šÂ¬Ã‚ÂÃƒâ€šÃ‚Â PREDICATE VALUES:");
                LoggingService.LogInfo($"   - Predicate Values: {GetDictionaryCount(blackboard, "PredicateValues")}");
                if (GetDictionaryCount(blackboard, "PredicateValues") > 0)
                {
                    foreach (var item in GetDictionaryItems(blackboard, "PredicateValues"))
                    {
                        LoggingService.LogInfo($"     ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€šÃ‚Â¢ {item.Key}: {item.Value}");
                    }
                }

                // 8. Action Values
                LoggingService.LogInfo($"\nÃƒÆ’Ã‚Â¢Ãƒâ€¦Ã‚Â¡Ãƒâ€šÃ‚Â¡ ACTION VALUES:");
                LoggingService.LogInfo($"   - Action Values: {GetDictionaryCount(blackboard, "ActionValues")}");
                if (GetDictionaryCount(blackboard, "ActionValues") > 0)
                {
                    foreach (var item in GetDictionaryItems(blackboard, "ActionValues"))
                    {
                        LoggingService.LogInfo($"     ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€šÃ‚Â¢ {item.Key}: {item.Value}");
                    }
                }

                // 9. State Values
                LoggingService.LogInfo($"\nÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸Ãƒâ€¦Ã¢â‚¬â„¢Ãƒâ€šÃ‚Â STATE VALUES:");
                LoggingService.LogInfo($"   - State Values: {GetDictionaryCount(blackboard, "StateValues")}");
                if (GetDictionaryCount(blackboard, "StateValues") > 0)
                {
                    foreach (var item in GetDictionaryItems(blackboard, "StateValues"))
                    {
                        LoggingService.LogInfo($"     ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€šÃ‚Â¢ {item.Key}: {item.Value}");
                    }
                }

                // 8. NodeGraphs
                var nodeGraphs = blackboard.GetAllNodeGraphs();
                LoggingService.LogInfo($"\nÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸Ãƒâ€¦Ã¢â‚¬â„¢Ãƒâ€šÃ‚Â³ NODEGRAPHS ({nodeGraphs.Count}):");
                foreach (var nodeGraph in nodeGraphs)
                {
                    LoggingService.LogInfo($"   - NodeGraph with {nodeGraph.GetAllActionNodes().Count} action nodes");
                }

                // 10. Summary
                LoggingService.LogInfo($"\nÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œÃƒâ€¦Ã‚Â  SUMMARY:");
                LoggingService.LogInfo($"   - CustomProperty Types: {entityTypes.Count}");
                LoggingService.LogInfo($"   - Predicate Types: {predicateTypes.Count}");
                LoggingService.LogInfo($"   - Action Types: {actionTypes.Count}");
                LoggingService.LogInfo($"   - Action Instances: {actionInstances.Count}");
                LoggingService.LogInfo($"   - NodeGraphs: {nodeGraphs.Count}");
                LoggingService.LogInfo($"   - TOTAL ITEMS: {entityTypes.Count + predicateTypes.Count + actionTypes.Count + actionInstances.Count + nodeGraphs.Count}");

            }
            catch (Exception ex)
            {
                LoggingService.LogError($"ÃƒÆ’Ã‚Â¢Ãƒâ€šÃ‚ÂÃƒâ€¦Ã¢â‚¬â„¢ Error during blackboard inspection: {ex.Message}");
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
                LoggingService.LogInfo("ÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸Ãƒâ€¦Ã¢â‚¬â„¢Ãƒâ€šÃ‚Â³ Creating behavior tree with cassette flow nodes...");

                // Create behavior tree instance first
                var behaviorTree = new BehaviorTree();
                behaviorTree.Initialise(blackboard, "CassetteBehaviorTree");
                LoggingService.LogSuccess("ÃƒÆ’Ã‚Â¢Ãƒâ€¦Ã¢â‚¬Å“ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¦ Created behavior tree instance");

                // Create root composite flow node
                rootNode = new BTFlowNodeComposite(new FastName("RootComposite"), behaviorTree);
                //var rootNode = new BTFlowNode_CostBasedComposite(new FastName("RootComposite"), behaviorTree);
                
                // Ensure we start in planning phase
                blackboard.PlanningPhase = true;
                // Initialize cassette subtree completion flags to false (four cassettes)
                blackboard.CassetteSubtreeCompleted = new bool[4] { false, false, false, false };
                LoggingService.LogInfo("ÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸ÃƒÂ¢Ã¢â€šÂ¬Ã‚ÂÃƒâ€šÃ‚Â§ Starting in PLANNING PHASE - all HL actions will generate NodeGraphs first");
                LoggingService.LogInfo("ÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸ÃƒÂ¢Ã¢â€šÂ¬Ã‚ÂÃƒâ€šÃ‚Â§ Cassette subtree completion flags initialized to false for all 4 cassettes");
                LoggingService.LogSuccess("ÃƒÆ’Ã‚Â¢Ãƒâ€¦Ã¢â‚¬Å“ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¦ Created root composite flow node");

                // Create four cassette flow nodes
                var cassette1Node = new DynamicFlowNode(new FastName("cassette1"), behaviorTree, SuccessCriteria.ALL, 1.0f, true);  // Add LowestCost decorator
                var cassette2Node = new DynamicFlowNode(new FastName("cassette2"), behaviorTree, SuccessCriteria.ALL, 1.0f, true);  // Add LowestCost decorator
                var cassette3Node = new DynamicFlowNode(new FastName("cassette3"), behaviorTree, SuccessCriteria.ALL, 1.0f, true);  // Add LowestCost decorator
                var cassette4Node = new DynamicFlowNode(new FastName("cassette4"), behaviorTree, SuccessCriteria.ALL, 1.0f, true);  // Add LowestCost decorator

                LoggingService.LogSuccess("ÃƒÆ’Ã‚Â¢Ãƒâ€¦Ã¢â‚¬Å“ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¦ Created four cassette flow nodes");

                // Add all cassette nodes to the root composite node
                ((BTFlowNodeComposite)rootNode).AddChild(cassette1Node);
                ((BTFlowNodeComposite)rootNode).AddChild(cassette2Node);
                ((BTFlowNodeComposite)rootNode).AddChild(cassette3Node);
                ((BTFlowNodeComposite)rootNode).AddChild(cassette4Node);

                LoggingService.LogSuccess("ÃƒÆ’Ã‚Â¢Ãƒâ€¦Ã¢â‚¬Å“ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¦ Added all four cassette nodes to root composite node");

                // Add planning phase management service to the root composite node
                ((BTFlowNodeComposite)rootNode).AddPlanningPhaseService();
                LoggingService.LogSuccess("ÃƒÂ¢Ã…â€œÃ¢â‚¬Â¦ Added planning phase management service to root composite node");

                // Add fair branch progress decorator for round-robin execution with cross-cassette tool batching
                rootNode.AddDecorator(new BTDecoratorFairBranchProgress((BTFlowNodeComposite)rootNode));
                LoggingService.LogSuccess("Added FairBranchProgress decorator to root composite node");
                
                

                // Set the root node
                behaviorTree.root = (BTFlowNodeComposite)rootNode;
                rootNode.SetOwiningTree(behaviorTree);
                rootNode.SetTreeForAllServices(behaviorTree);

                // Create PDDL planners for all four cassettes (after behavior tree is created)
                // Different planners and problem files for each cassette
                var pddlRequest1 = new PDDLPlanningRequest("./Plannerinputs/static/DomainHL.pddl", "./Plannerinputs/static/problemC5.pddl", "/home/ubuntu/jpddlplus-master/jpddlplus.jar", "ENHSP", 120) { EnhspConfig = "sat-hadd" };
                var pddlRequest2 = new PDDLPlanningRequest("./Plannerinputs/static/DomainHL.pddl", "./Plannerinputs/static/problemC6.pddl", "/home/ubuntu/jpddlplus-master/jpddlplus.jar", "ENHSP", 120) { EnhspConfig = "sat-hadd" };
                var pddlRequest3 = new PDDLPlanningRequest("./Plannerinputs/static/DomainHL.pddl", "./Plannerinputs/static/problemC7.pddl", "/home/ubuntu/jpddlplus-master/jpddlplus.jar", "ENHSP", 120) { EnhspConfig = "sat-hadd" };
                var pddlRequest4 = new PDDLPlanningRequest("./Plannerinputs/static/DomainHL.pddl", "./Plannerinputs/static/problemC8.pddl", "/home/ubuntu/jpddlplus-master/jpddlplus.jar", "ENHSP", 120) { EnhspConfig = "sat-hadd" };

                var pddlPlanner1 = new ServicePDDLPlanning(behaviorTree, pddlRequest1);
                var pddlPlanner2 = new ServicePDDLPlanning(behaviorTree, pddlRequest2);
                var pddlPlanner3 = new ServicePDDLPlanning(behaviorTree, pddlRequest3);
                var pddlPlanner4 = new ServicePDDLPlanning(behaviorTree, pddlRequest4);
                
                // Track all planners for execution summary
                allPlanners.Add(pddlPlanner1);
                allPlanners.Add(pddlPlanner2);
                allPlanners.Add(pddlPlanner3);
                allPlanners.Add(pddlPlanner4);
                
                // Configure execution modes for the cassettes
                pddlPlanner1.ExecutionMode = ServicePDDLPlanning.ParallelExecutionMode.Parallel;    // Parallel execution
                pddlPlanner2.ExecutionMode = ServicePDDLPlanning.ParallelExecutionMode.Parallel;    // Parallel execution
                pddlPlanner3.ExecutionMode = ServicePDDLPlanning.ParallelExecutionMode.Parallel;    // Parallel execution
                pddlPlanner4.ExecutionMode = ServicePDDLPlanning.ParallelExecutionMode.Parallel;    // Parallel execution

                LoggingService.LogInfo("ÃƒÆ’Ã‚Â¢Ãƒâ€¦Ã¢â‚¬Å“ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¦ Created PDDL planners for all four cassettes");
                LoggingService.LogInfo($"ÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸ÃƒÂ¢Ã¢â€šÂ¬Ã‚ÂÃƒâ€šÃ‚Â§ Execution Modes:");
                LoggingService.LogInfo($"   - Cassette 1: {pddlPlanner1.ExecutionMode} (Planner: {pddlRequest1.PlannerName})");
                LoggingService.LogInfo($"   - Cassette 2: {pddlPlanner2.ExecutionMode} (Planner: {pddlRequest2.PlannerName})");
                LoggingService.LogInfo($"   - Cassette 3: {pddlPlanner3.ExecutionMode} (Planner: {pddlRequest3.PlannerName})");
                LoggingService.LogInfo($"   - Cassette 4: {pddlPlanner4.ExecutionMode} (Planner: {pddlRequest4.PlannerName})");

                // Set the planning services on all flow nodes
                cassette1Node.SetPlanningService(pddlPlanner1);
                cassette2Node.SetPlanningService(pddlPlanner2);
                cassette3Node.SetPlanningService(pddlPlanner3);
                cassette4Node.SetPlanningService(pddlPlanner4);

                LoggingService.LogSuccess("ÃƒÆ’Ã‚Â¢Ãƒâ€¦Ã¢â‚¬Å“ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¦ Set planning services on all four cassette flow nodes");
                
                // Debug: Check if planners are properly configured
                LoggingService.LogInfo($"ÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸ÃƒÂ¢Ã¢â€šÂ¬Ã‚ÂÃƒâ€šÃ‚Â Planner Debug Info:");
                LoggingService.LogInfo($"   Cassette 1 - Domain: {pddlRequest1.DomainFile}, Problem: {pddlRequest1.ProblemFile}, Planner: {pddlRequest1.PlannerName}");
                LoggingService.LogInfo($"   Cassette 2 - Domain: {pddlRequest2.DomainFile}, Problem: {pddlRequest2.ProblemFile}, Planner: {pddlRequest2.PlannerName}");
                LoggingService.LogInfo($"   Cassette 3 - Domain: {pddlRequest3.DomainFile}, Problem: {pddlRequest3.ProblemFile}, Planner: {pddlRequest3.PlannerName}");
                LoggingService.LogInfo($"   Cassette 4 - Domain: {pddlRequest4.DomainFile}, Problem: {pddlRequest4.ProblemFile}, Planner: {pddlRequest4.PlannerName}");

                // Store the behavior tree in the blackboard for later use
                blackboard.SetNodeGraph(new FastName("MainBehaviorTree"), new NodeGraph());
                LoggingService.LogSuccess("ÃƒÆ’Ã‚Â¢Ãƒâ€¦Ã¢â‚¬Å“ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¦ Stored behavior tree reference in blackboard");

                // Display tree structure
                LoggingService.LogInfo("\nÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¹ BEHAVIOR TREE STRUCTURE:");
                LoggingService.LogInfo($"Root: BTFlowNodeComposite ({((BTFlowNodeComposite)rootNode).GetNodeName()})");
                LoggingService.LogInfo($"ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã‚ÂÃƒâ€¦Ã¢â‚¬Å“ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã‚ÂÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã‚ÂÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ DynamicFlowNode ({cassette1Node.GetNodeName()}) - {pddlRequest1.PlannerName} Planner");
                LoggingService.LogInfo($"ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã‚ÂÃƒâ€¦Ã¢â‚¬Å“ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã‚ÂÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã‚ÂÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ DynamicFlowNode ({cassette2Node.GetNodeName()}) - {pddlRequest2.PlannerName} Planner");
                LoggingService.LogInfo($"ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã‚ÂÃƒâ€¦Ã¢â‚¬Å“ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã‚ÂÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã‚ÂÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ DynamicFlowNode ({cassette3Node.GetNodeName()}) - {pddlRequest3.PlannerName} Planner");
                LoggingService.LogInfo($"ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã‚ÂÃƒÂ¢Ã¢â€šÂ¬Ã‚ÂÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã‚ÂÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã‚ÂÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ DynamicFlowNode ({cassette4Node.GetNodeName()}) - {pddlRequest4.PlannerName} Planner");

                LoggingService.LogSuccess("\nÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸Ãƒâ€¦Ã‚Â½ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â° Behavior tree with cassette flow nodes created successfully!");

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
                LoggingService.LogError($"ÃƒÆ’Ã‚Â¢Ãƒâ€šÃ‚ÂÃƒâ€¦Ã¢â‚¬â„¢ Error creating behavior tree: {ex.Message}");
                LoggingService.LogError($"   Stack trace: {ex.StackTrace}");
            }
        }

        // Test the behavior tree structure
        private async Task TestBehaviorTreeStructure(BehaviorTree behaviorTree)
        {
            try
            {
                LoggingService.LogInfo("\nÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸Ãƒâ€šÃ‚Â§Ãƒâ€šÃ‚Âª Testing behavior tree structure...");

                // Track memory usage before tree execution
                var memoryBefore = GC.GetTotalMemory(false);
                
                // Test initial tick
                BlackboardSummaryLogger.StartTreeTicking();
                var result = behaviorTree.Tick(0.0f);
                BlackboardSummaryLogger.EndTreeTicking();
                LoggingService.LogSuccess($"ÃƒÆ’Ã‚Â¢Ãƒâ€¦Ã¢â‚¬Å“ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¦ Initial tree tick result: {result}");
                
                // Track memory usage after tree execution
                var memoryAfter = GC.GetTotalMemory(false);
                
                // Track memory usage after planner execution
                var memoryAfterPlanner = GC.GetTotalMemory(false);

                // Test individual cassette nodes
                var rootNode = behaviorTree.root as BTFlowNodeComposite;
                if (rootNode != null)
                {
                    var children = rootNode.GetChildren();
                    LoggingService.LogSuccess($"ÃƒÆ’Ã‚Â¢Ãƒâ€¦Ã¢â‚¬Å“ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¦ Root node has {children.Count} children");

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
                    LoggingService.LogError($"ÃƒÆ’Ã‚Â¢Ãƒâ€šÃ‚ÂÃƒâ€¦Ã¢â‚¬â„¢ Root node is not a BTFlowNodeComposite. Actual type: {behaviorTree.root?.GetType().Name ?? "null"}");
                }

                LoggingService.LogSuccess("ÃƒÆ’Ã‚Â¢Ãƒâ€¦Ã¢â‚¬Å“ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¦ Behavior tree structure test completed!");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"ÃƒÆ’Ã‚Â¢Ãƒâ€šÃ‚ÂÃƒâ€¦Ã¢â‚¬â„¢ Error testing behavior tree structure: {ex.Message}");
            }
        }

        // Display NodeGraph status for each flow node
        private async Task DisplayNodeGraphStatus(BehaviorTree behaviorTree)
        {
            try
            {
                LoggingService.LogSubsection("ÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œÃƒâ€¦Ã‚Â  NODEGRAPH STATUS REPORT");
                LoggingService.LogInfo("=".PadRight(50, '='));

                var rootNode = behaviorTree.root as BTFlowNodeComposite;
                if (rootNode != null)
                {
                    var children = rootNode.GetChildren();
                    LoggingService.LogInfo($"ÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸ÃƒÂ¢Ã¢â€šÂ¬Ã‚ÂÃƒâ€šÃ‚Â Checking {children.Count} flow nodes for NodeGraph status...\n");

                    for (int i = 0; i < children.Count; i++)
                    {
                        var child = children[i];
                        if (child is DynamicFlowNode dynamicNode)
                        {
                            LoggingService.LogInfo($"ÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸Ãƒâ€¦Ã‚Â½Ãƒâ€šÃ‚Â¯ FLOW NODE {i + 1}: {dynamicNode.GetNodeName()}");
                            LoggingService.LogInfo($"   ÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¹ Node Type: {child.GetType().Name}");
                            
                            // Check if planning service is set
                            if (dynamicNode.ServicePlanning != null)
                            {
                                LoggingService.LogInfo($"   ÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸ÃƒÂ¢Ã¢â€šÂ¬Ã‚ÂÃƒâ€šÃ‚Â§ Planning Service: {dynamicNode.ServicePlanning.GetType().Name}");
                                
                                // Check if it's a ServicePlanning
                                if (dynamicNode.ServicePlanning is ServicePlanning plannerService)
                                {
                                    LoggingService.LogInfo($"   ÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œÃƒâ€¦Ã‚Â  Has Generated NodeGraph: {plannerService.HasGeneratedNodeGraph()}");
                                    
                                    if (plannerService.HasGeneratedNodeGraph())
                                    {
                                        var generatedGraph = plannerService.GetGeneratedNodeGraph();
                                        var actions = generatedGraph.GetAllActionNodes();
                                        LoggingService.LogInfo($"   ÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œÃƒâ€¹Ã¢â‚¬Â  Generated NodeGraph Actions: {actions.Count}");
                                        
                                        // List the actions
                                        for (int j = 0; j < actions.Count; j++)
                                        {
                                            LoggingService.LogInfo($"      {j + 1}. {actions[j].InstanceName.ToString()}");
                                        }
                                    }
                                    else
                                    {
                                        LoggingService.LogInfo($"   ÃƒÆ’Ã‚Â¢Ãƒâ€¦Ã‚Â¡Ãƒâ€šÃ‚Â ÃƒÆ’Ã‚Â¯Ãƒâ€šÃ‚Â¸Ãƒâ€šÃ‚Â No NodeGraph generated yet");
                                    }
                                }
                            }
                            else
                            {
                                LoggingService.LogInfo($"   ÃƒÆ’Ã‚Â¢Ãƒâ€šÃ‚ÂÃƒâ€¦Ã¢â‚¬â„¢ No planning service set");
                            }
                            
                            // Check the actionGraph
                            var actionGraph = dynamicNode.GetActionGraph();
                            var actionGraphNodes = actionGraph.GetAllActionNodes();
                            LoggingService.LogInfo($"   ÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¹ ActionGraph Nodes: {actionGraphNodes.Count}");
                            
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

                LoggingService.LogSuccess("ÃƒÆ’Ã‚Â¢Ãƒâ€¦Ã¢â‚¬Å“ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¦ NodeGraph status report completed!");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"ÃƒÆ’Ã‚Â¢Ãƒâ€šÃ‚ÂÃƒâ€¦Ã¢â‚¬â„¢ Error displaying NodeGraph status: {ex.Message}");
            }
        }

        // Monitor planner execution in real-time
        private async Task MonitorPlannerExecution()
        {
            LoggingService.LogInfo("\nÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸ÃƒÂ¢Ã¢â€šÂ¬Ã‚ÂÃƒâ€šÃ‚Â MONITORING PLANNER EXECUTION");
            LoggingService.LogInfo("=".PadRight(50, '='));
            
            if (allPlanners.Count == 0)
            {
                LoggingService.LogWarning("ÃƒÆ’Ã‚Â¢Ãƒâ€¦Ã‚Â¡Ãƒâ€šÃ‚Â ÃƒÆ’Ã‚Â¯Ãƒâ€šÃ‚Â¸Ãƒâ€šÃ‚Â No planners to monitor.");
                return;
            }
            
            LoggingService.LogInfo($"ÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸ÃƒÂ¢Ã¢â€šÂ¬Ã‚ÂÃƒâ€šÃ‚Â Monitoring {allPlanners.Count} planners...");
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
                    LoggingService.LogInfo($"ÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸ÃƒÂ¢Ã¢â€šÂ¬Ã‚ÂÃƒâ€šÃ‚Â PLANNER EXECUTION STATUS - {currentTime:HH:mm:ss}");
                    LoggingService.LogInfo("=".PadRight(50, '='));
                    
                    var completedCount = allPlanners.Count(p => p.HasCompleted);
                    var executingCount = allPlanners.Count(p => p.IsExecuting);
                    var pendingCount = allPlanners.Count(p => !p.HasCompleted && !p.IsExecuting);
                    
                    LoggingService.LogInfo($"ÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œÃƒâ€¦Ã‚Â  Progress: {completedCount}/{allPlanners.Count} completed, {executingCount} executing, {pendingCount} pending");
                    
                                         // Planning phase monitoring
                     if (rootNode is BTFlowNodeComposite compositeNode)
                     {
                         var planningComplete = compositeNode.AreAllPlanningServicesComplete();
                         LoggingService.LogInfo($"\nÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¹ PLANNING PHASE STATUS:");
                         LoggingService.LogInfo($"   Planning Complete: {planningComplete}");
                         
                         var children = compositeNode.GetChildren();
                         LoggingService.LogInfo("ÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œÃƒâ€¦Ã‚Â  SUBTREE STATUSES:");
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
                        var status = planner.HasCompleted ? "ÃƒÆ’Ã‚Â¢Ãƒâ€¦Ã¢â‚¬Å“ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¦" : planner.IsExecuting ? "ÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸ÃƒÂ¢Ã¢â€šÂ¬Ã‚ÂÃƒÂ¢Ã¢â€šÂ¬Ã…Â¾" : "ÃƒÆ’Ã‚Â¢Ãƒâ€šÃ‚ÂÃƒâ€šÃ‚Â³";
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
                    LoggingService.LogInfo("\nÃƒÆ’Ã‚Â¢Ãƒâ€¦Ã¢â‚¬Å“ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¦ All planners have finished execution!");
                    break;
                }
                
                await Task.Delay(100); // Small delay to prevent high CPU usage
            }
            
            LoggingService.LogInfo($"ÃƒÆ’Ã‚Â¢Ãƒâ€šÃ‚ÂÃƒâ€šÃ‚Â±ÃƒÆ’Ã‚Â¯Ãƒâ€šÃ‚Â¸Ãƒâ€šÃ‚Â Monitoring duration: {DateTime.Now - monitoringStartTime:hh\\:mm\\:ss\\.fff}");
        }
        
        // Display execution summary for all planners
        private async Task DisplayExecutionSummary()
        {
            LoggingService.LogSubsection("ÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œÃƒâ€¦Ã‚Â  PLANNER EXECUTION SUMMARY");
            LoggingService.LogInfo("=".PadRight(80, '='));
            
            if (allPlanners.Count == 0)
            {
                LoggingService.LogWarning("ÃƒÆ’Ã‚Â¢Ãƒâ€¦Ã‚Â¡Ãƒâ€šÃ‚Â ÃƒÆ’Ã‚Â¯Ãƒâ€šÃ‚Â¸Ãƒâ€šÃ‚Â No planners were executed during this test.");
                return;
            }
            
            LoggingService.LogInfo($"ÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸ÃƒÂ¢Ã¢â€šÂ¬Ã‚ÂÃƒâ€šÃ‚Â Total planners executed: {allPlanners.Count}");
            LoggingService.LogInfo("");
            
            // Sort planners by start time
            var sortedPlanners = allPlanners.OrderBy(p => p.StartTime).ToList();
            
            for (int i = 0; i < sortedPlanners.Count; i++)
            {
                var planner = sortedPlanners[i];
                LoggingService.LogInfo($"ÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸Ãƒâ€¦Ã‚Â½Ãƒâ€šÃ‚Â¯ PLANNER {i + 1}: {planner.PlannerName}");
                LoggingService.LogInfo($"   ÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸Ãƒâ€¦Ã‚Â¡ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ Started: {planner.StartTime:HH:mm:ss.fff}");
                
                if (planner.HasCompleted)
                {
                    LoggingService.LogInfo($"   ÃƒÆ’Ã‚Â¢Ãƒâ€¦Ã¢â‚¬Å“ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¦ Finished: {planner.EndTime:HH:mm:ss.fff}");
                    LoggingService.LogInfo($"   ÃƒÆ’Ã‚Â¢Ãƒâ€šÃ‚ÂÃƒâ€šÃ‚Â±ÃƒÆ’Ã‚Â¯Ãƒâ€šÃ‚Â¸Ãƒâ€šÃ‚Â Planner Duration: {planner.PlannerExecutionDuration:hh\\:mm\\:ss\\.fff}");
                    LoggingService.LogInfo($"   ÃƒÆ’Ã‚Â¢Ãƒâ€šÃ‚ÂÃƒâ€šÃ‚Â±ÃƒÆ’Ã‚Â¯Ãƒâ€šÃ‚Â¸Ãƒâ€šÃ‚Â Total Duration: {planner.TotalExecutionDuration:hh\\:mm\\:ss\\.fff}");
                    
                    if (planner.GeneratedNodeGraph != null)
                    {
                        LoggingService.LogInfo($"   ÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œÃƒâ€¦Ã‚Â  Actions Generated: {planner.GeneratedNodeGraph.GetAllActionNodes().Count}");
                    }
                }
                else if (planner.IsExecuting)
                {
                    LoggingService.LogInfo($"   ÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸ÃƒÂ¢Ã¢â€šÂ¬Ã‚ÂÃƒÂ¢Ã¢â€šÂ¬Ã…Â¾ Still executing... (Started: {planner.StartTime:HH:mm:ss.fff})");
                }
                else
                {
                    LoggingService.LogError($"   ÃƒÆ’Ã‚Â¢Ãƒâ€šÃ‚ÂÃƒâ€¦Ã¢â‚¬â„¢ Failed or incomplete");
                }
                LoggingService.LogInfo("");
            }
            
            // Summary statistics
            var completedPlanners = allPlanners.Where(p => p.HasCompleted).ToList();
            var failedPlanners = allPlanners.Where(p => !p.HasCompleted && !p.IsExecuting).ToList();
            var executingPlanners = allPlanners.Where(p => p.IsExecuting).ToList();
            
            LoggingService.LogInfo("ÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œÃƒâ€¹Ã¢â‚¬Â  EXECUTION STATISTICS:");
            LoggingService.LogInfo($"   ÃƒÆ’Ã‚Â¢Ãƒâ€¦Ã¢â‚¬Å“ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¦ Successfully completed: {completedPlanners.Count}");
            LoggingService.LogError($"   ÃƒÆ’Ã‚Â¢Ãƒâ€šÃ‚ÂÃƒâ€¦Ã¢â‚¬â„¢ Failed: {failedPlanners.Count}");
            LoggingService.LogInfo($"   ÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸ÃƒÂ¢Ã¢â€šÂ¬Ã‚ÂÃƒÂ¢Ã¢â€šÂ¬Ã…Â¾ Still executing: {executingPlanners.Count}");
            
            if (completedPlanners.Any())
            {
                var avgPlannerDuration = TimeSpan.FromMilliseconds(completedPlanners.Average(p => p.PlannerExecutionDuration.TotalMilliseconds));
                var avgTotalDuration = TimeSpan.FromMilliseconds(completedPlanners.Average(p => p.TotalExecutionDuration.TotalMilliseconds));
                var minPlannerDuration = completedPlanners.Min(p => p.PlannerExecutionDuration);
                var maxPlannerDuration = completedPlanners.Max(p => p.PlannerExecutionDuration);
                var minTotalDuration = completedPlanners.Min(p => p.TotalExecutionDuration);
                var maxTotalDuration = completedPlanners.Max(p => p.TotalExecutionDuration);
                
                LoggingService.LogInfo($"   ÃƒÆ’Ã‚Â¢Ãƒâ€šÃ‚ÂÃƒâ€šÃ‚Â±ÃƒÆ’Ã‚Â¯Ãƒâ€šÃ‚Â¸Ãƒâ€šÃ‚Â Average Planner Duration: {avgPlannerDuration:hh\\:mm\\:ss\\.fff}");
                LoggingService.LogInfo($"   ÃƒÆ’Ã‚Â¢Ãƒâ€šÃ‚ÂÃƒâ€šÃ‚Â±ÃƒÆ’Ã‚Â¯Ãƒâ€šÃ‚Â¸Ãƒâ€šÃ‚Â Average Total Duration: {avgTotalDuration:hh\\:mm\\:ss\\.fff}");
                LoggingService.LogInfo($"   ÃƒÆ’Ã‚Â¢Ãƒâ€šÃ‚ÂÃƒâ€šÃ‚Â±ÃƒÆ’Ã‚Â¯Ãƒâ€šÃ‚Â¸Ãƒâ€šÃ‚Â Fastest Planner: {minPlannerDuration:hh\\:mm\\:ss\\.fff}");
                LoggingService.LogInfo($"   ÃƒÆ’Ã‚Â¢Ãƒâ€šÃ‚ÂÃƒâ€šÃ‚Â±ÃƒÆ’Ã‚Â¯Ãƒâ€šÃ‚Â¸Ãƒâ€šÃ‚Â Slowest Planner: {maxPlannerDuration:hh\\:mm\\:ss\\.fff}");
                LoggingService.LogInfo($"   ÃƒÆ’Ã‚Â¢Ãƒâ€šÃ‚ÂÃƒâ€šÃ‚Â±ÃƒÆ’Ã‚Â¯Ãƒâ€šÃ‚Â¸Ãƒâ€šÃ‚Â Fastest Total: {minTotalDuration:hh\\:mm\\:ss\\.fff}");
                LoggingService.LogInfo($"   ÃƒÆ’Ã‚Â¢Ãƒâ€šÃ‚ÂÃƒâ€šÃ‚Â±ÃƒÆ’Ã‚Â¯Ãƒâ€šÃ‚Â¸Ãƒâ€šÃ‚Â Slowest Total: {maxTotalDuration:hh\\:mm\\:ss\\.fff}");
            }
            
            LoggingService.LogInfo("=".PadRight(80, '='));
            
            // Display blackboard tracking statistics
            DisplayBlackboardTrackingSummary();
        }
        
        // Display blackboard tracking summary
        private void DisplayBlackboardTrackingSummary()
        {
            LoggingService.LogSubsection("ÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¹ BLACKBOARD TRACKING SUMMARY");
            LoggingService.LogInfo("=".PadRight(80, '='));
            
            try
            {
                // Get current blackboard tracking statistics
                var (types, instances, negations) = BlackboardTrackingLogger.GetCurrentCounts();
                
                LoggingService.LogInfo($"ÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¢ Total New Types Added: {types}");
                LoggingService.LogInfo($"ÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¢ Total New Instances Created: {instances}");
                LoggingService.LogInfo($"ÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸ÃƒÂ¢Ã¢â€šÂ¬Ã‚ÂÃƒÂ¢Ã¢â€šÂ¬Ã…Â¾ Total Predicate Negations: {negations}");
                
                LoggingService.LogInfo($"ÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œÃƒâ€šÃ‚Â Blackboard tracking log saved to: {BlackboardTrackingLogger.GetLogFilePath()}");
                LoggingService.LogInfo("=".PadRight(80, '='));
            }
            catch (Exception ex)
            {
                LoggingService.LogWarning($"ÃƒÆ’Ã‚Â¢Ãƒâ€¦Ã‚Â¡Ãƒâ€šÃ‚Â ÃƒÆ’Ã‚Â¯Ãƒâ€šÃ‚Â¸Ãƒâ€šÃ‚Â Could not retrieve blackboard tracking statistics: {ex.Message}");
            }
        }
        
        // Track subtree status for high-level actions generated by flow nodes
        private async Task TrackSubtreeStatusForHLActions(BehaviorTree behaviorTree)
        {
            LoggingService.LogSubsection("ÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸Ãƒâ€¦Ã¢â‚¬â„¢Ãƒâ€šÃ‚Â³ TRACKING SUBTREE STATUS FOR HL ACTIONS");
            LoggingService.LogInfo("=".PadRight(60, '='));
            
                         try
             {
                 var rootNode = behaviorTree.root as BTFlowNodeComposite;
                 if (rootNode == null)
                 {
                     LoggingService.LogError("ÃƒÆ’Ã‚Â¢Ãƒâ€šÃ‚ÂÃƒâ€¦Ã¢â‚¬â„¢ Root node is not a BTFlowNodeComposite");
                     return;
                 }

                var children = rootNode.GetChildren();
                LoggingService.LogInfo($"ÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸ÃƒÂ¢Ã¢â€šÂ¬Ã‚ÂÃƒâ€šÃ‚Â Tracking subtrees for {children.Count} flow nodes...\n");

                for (int i = 0; i < children.Count; i++)
                {
                    var child = children[i];
                    if (child is DynamicFlowNode dynamicNode)
                    {
                        LoggingService.LogInfo($"ÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸Ãƒâ€¦Ã‚Â½Ãƒâ€šÃ‚Â¯ FLOW NODE {i + 1}: {dynamicNode.GetNodeName()}");
                        
                        // Check if planning service has generated a NodeGraph
                        if (dynamicNode.ServicePlanning is ServicePlanning plannerService && plannerService.HasGeneratedNodeGraph())
                        {
                            var generatedGraph = plannerService.GetGeneratedNodeGraph();
                            var actions = generatedGraph.GetAllActionNodes();
                            
                            LoggingService.LogInfo($"   ÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œÃƒâ€¦Ã‚Â  Generated {actions.Count} actions from planner");
                            
                            // Track subtree status for each action
                            for (int j = 0; j < actions.Count; j++)
                            {
                                var action = actions[j];
                                if (action is PActionNode genericAction)
                                {
                                    LoggingService.LogInfo($"   ÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸ÃƒÂ¢Ã¢â€šÂ¬Ã‚ÂÃƒâ€šÃ‚Â Action {j + 1}: {action.InstanceName.ToString()}");
                                    
                                    // Check if this is a high-level action
                                    if (genericAction.IsHighLevelAction)
                                    {
                                        LoggingService.LogInfo($"      ÃƒÆ’Ã‚Â¢Ãƒâ€¦Ã¢â‚¬Å“ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¦ Is High-Level Action: Yes");
                                        
                                        // Check if it has a subtree
                                        if (genericAction.HighLevelSubtree != null)
                                        {
                                            LoggingService.LogInfo($"      ÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸Ãƒâ€¦Ã¢â‚¬â„¢Ãƒâ€šÃ‚Â³ Has Subtree: Yes");
                                            LoggingService.LogInfo($"      ÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¹ Subtree Type: {genericAction.HighLevelSubtree.GetType().Name}");
                                            LoggingService.LogInfo($"      ÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œÃƒâ€¦Ã‚Â  Subtree Status: {genericAction.HighLevelSubtree.status}");
                                            
                                            // Check if subtree has actions
                                            var subtreeActionGraph = genericAction.HighLevelSubtree.GetActionGraph();
                                            var subtreeActions = subtreeActionGraph.GetAllActionNodes();
                                            LoggingService.LogInfo($"      ÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œÃƒâ€¹Ã¢â‚¬Â  Subtree Actions: {subtreeActions.Count}");
                                            
                                            // List subtree actions and their status
                                            for (int k = 0; k < subtreeActions.Count; k++)
                                            {
                                                var subtreeAction = subtreeActions[k];
                                                LoggingService.LogInfo($"         {k + 1}. {subtreeAction.InstanceName.ToString()} - Status: {subtreeAction.status}");
                                            }
                                        }
                                        else
                                        {
                                            LoggingService.LogInfo($"      ÃƒÆ’Ã‚Â¢Ãƒâ€šÃ‚ÂÃƒâ€¦Ã¢â‚¬â„¢ Has Subtree: No");
                                        }
                                        
                                        // Check if it has a planning service
                                        if (genericAction.ServicePlanning != null)
                                        {
                                            LoggingService.LogInfo($"      ÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸ÃƒÂ¢Ã¢â€šÂ¬Ã‚ÂÃƒâ€šÃ‚Â§ Has Planning Service: Yes ({genericAction.ServicePlanning.GetType().Name})");
                                        }
                                        else
                                        {
                                            LoggingService.LogInfo($"      ÃƒÆ’Ã‚Â¢Ãƒâ€šÃ‚ÂÃƒâ€¦Ã¢â‚¬â„¢ Has Planning Service: No");
                                        }
                                    }
                                    else
                                    {
                                        LoggingService.LogInfo($"      ÃƒÆ’Ã‚Â¢Ãƒâ€šÃ‚ÂÃƒâ€¦Ã¢â‚¬â„¢ Is High-Level Action: No");
                                    }
                                    
                                    // Check if it has a ServiceSubtreeInject
                                    var subtreeService = genericAction.GetSubtreeInjectionService();
                                    if (subtreeService != null)
                                    {
                                        LoggingService.LogInfo($"      ÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸ÃƒÂ¢Ã¢â€šÂ¬Ã‚ÂÃƒâ€šÃ‚Â§ Has ServiceSubtreeInject: Yes");
                                        
                                        // Check if any problem files were generated
                                        var generatedFiles = ServicePDDLPlanning.GeneratedProblemFiles;
                                        if (generatedFiles.Count > 0)
                                        {
                                            LoggingService.LogInfo($"      ÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œÃƒÂ¢Ã¢â€šÂ¬Ã…Â¾ Generated Problem Files: {generatedFiles.Count}");
                                            foreach (var file in generatedFiles)
                                            {
                                                LoggingService.LogInfo($"         - {file}");
                                            }
                                        }
                                    }
                                    else
                                    {
                                        LoggingService.LogInfo($"      ÃƒÆ’Ã‚Â¢Ãƒâ€šÃ‚ÂÃƒâ€¦Ã¢â‚¬â„¢ Has ServiceSubtreeInject: No");
                                    }
                                }
                                else
                                {
                                    LoggingService.LogInfo($"   ÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸ÃƒÂ¢Ã¢â€šÂ¬Ã‚ÂÃƒâ€šÃ‚Â Action {j + 1}: {action.InstanceName.ToString()} (Not a GenericBTAction)");
                                }
                                LoggingService.LogInfo("");
                            }
                        }
                        else
                        {
                            LoggingService.LogInfo($"   ÃƒÆ’Ã‚Â¢Ãƒâ€¦Ã‚Â¡Ãƒâ€šÃ‚Â ÃƒÆ’Ã‚Â¯Ãƒâ€šÃ‚Â¸Ãƒâ€šÃ‚Â No NodeGraph generated yet by planner");
                        }
                        
                        LoggingService.LogInfo("");
                    }
                }
                
                LoggingService.LogSuccess("ÃƒÆ’Ã‚Â¢Ãƒâ€¦Ã¢â‚¬Å“ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¦ Subtree status tracking completed!");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"ÃƒÆ’Ã‚Â¢Ãƒâ€šÃ‚ÂÃƒâ€¦Ã¢â‚¬â„¢ Error tracking subtree status: {ex.Message}");
                LoggingService.LogError($"   Stack trace: {ex.StackTrace}");
            }
        }

        // Execute tree with comprehensive logging
        private async Task ExecuteTreeWithComprehensiveLogging(BehaviorTree behaviorTree)
        {
            LoggingService.LogSection("ÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸Ãƒâ€¦Ã‚Â¡ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ EXECUTING TREE WITH COMPREHENSIVE LOGGING");
            
            try
            {
                int maxTicks = 1300; // Maximum number of ticks to prevent infinite loops
                int tickCount = 0;
                
                // Dictionary to track action status changes
                var actionStatusHistory = new Dictionary<string, BTNodeResult>();
                
                LoggingService.LogInfo($"ÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸ÃƒÂ¢Ã¢â€šÂ¬Ã‚ÂÃƒÂ¢Ã¢â€šÂ¬Ã…Â¾ Starting tree execution (max {maxTicks} ticks)...");
                LoggingService.LogInfo("Press any key to stop execution...");
                
                while (tickCount < maxTicks)
                {
                    // Check if any key is pressed (non-blocking)
                    if (Console.KeyAvailable)
                    {
                        Console.ReadKey(true); // Clear the key
                        LoggingService.LogWarning("ÃƒÆ’Ã‚Â¢Ãƒâ€šÃ‚ÂÃƒâ€šÃ‚Â¹ÃƒÆ’Ã‚Â¯Ãƒâ€šÃ‚Â¸Ãƒâ€šÃ‚Â Execution stopped by user");
                        break;
                    }
                    
                    tickCount++;
                    
                    // Log tick start
                    LoggingService.LogInfo($"\nÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸ÃƒÂ¢Ã¢â€šÂ¬Ã‚ÂÃƒÂ¢Ã¢â€šÂ¬Ã…Â¾ TICK {tickCount} STARTING...");
                    
                    // Execute one tick
                    BlackboardSummaryLogger.StartTreeTicking();
                    var result = behaviorTree.Tick(0.1f); // 0.1 second delta time
                    BlackboardSummaryLogger.EndTreeTicking();
                    
                    // Log comprehensive tick information
                    LogComprehensiveTickInfo(behaviorTree, tickCount, actionStatusHistory);
                    
                    // Check if tree has finished
                    if (behaviorTree.HasFinished())
                    {
                        LoggingService.LogSuccess($"\nÃƒÆ’Ã‚Â¢Ãƒâ€¦Ã¢â‚¬Å“ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¦ Tree execution completed after {tickCount} ticks");
                        LoggingService.LogSuccess($"ÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œÃƒâ€¦Ã‚Â  Final result: {result}");
                        break;
                    }
                    
                    // Small delay between ticks
                    await Task.Delay(100);
                }
                
                if (tickCount >= maxTicks)
                {
                    LoggingService.LogWarning($"\nÃƒÆ’Ã‚Â¢Ãƒâ€¦Ã‚Â¡Ãƒâ€šÃ‚Â ÃƒÆ’Ã‚Â¯Ãƒâ€šÃ‚Â¸Ãƒâ€šÃ‚Â Tree execution stopped after {maxTicks} ticks (max reached)");
                }
                
                // Print final status summary
                LogFinalActionStatusSummary(actionStatusHistory);
                
                LoggingService.LogSuccess("ÃƒÆ’Ã‚Â¢Ãƒâ€¦Ã¢â‚¬Å“ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¦ Tree execution with comprehensive logging completed!");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"ÃƒÆ’Ã‚Â¢Ãƒâ€šÃ‚ÂÃƒâ€¦Ã¢â‚¬â„¢ Error during tree execution: {ex.Message}");
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
                LoggingService.LogError($"ÃƒÆ’Ã‚Â¢Ãƒâ€šÃ‚ÂÃƒâ€¦Ã¢â‚¬â„¢ Error logging comprehensive tick info on tick {tickNumber}: {ex.Message}");
            }
        }

        // Log NodeGraph details including order relations
        private void LogNodeGraphDetails(BehaviorTree behaviorTree, int tickNumber)
        {
            try
            {
                var rootNode = behaviorTree.root as BTFlowNodeComposite;
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
                            LoggingService.LogInfo($"\nÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸Ãƒâ€¦Ã¢â‚¬â„¢Ãƒâ€šÃ‚Â³ NODEGRAPH DETAILS ({dynamicNode.GetNodeName()}) - TICK {tickNumber}:");
                            LoggingService.LogInfo($"   ÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œÃƒâ€¦Ã‚Â  Total nodes: {nodes.Count}");
                            
                            // Log each node's details
                            foreach (var action in nodes)
                            {
                                var nodeInfo = actionGraph.GetNodeInfo(action);
                                if (nodeInfo != null)
                                {
                                    var statusEmoji = action.status switch
                                    {
                                        BTNodeResult.Success => "ÃƒÆ’Ã‚Â¢Ãƒâ€¦Ã¢â‚¬Å“ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¦",
                                        BTNodeResult.Failure => "ÃƒÆ’Ã‚Â¢Ãƒâ€šÃ‚ÂÃƒâ€¦Ã¢â‚¬â„¢",
                                        BTNodeResult.InProgress => "ÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸ÃƒÂ¢Ã¢â€šÂ¬Ã‚ÂÃƒÂ¢Ã¢â€šÂ¬Ã…Â¾",
                                        BTNodeResult.ReadyToTick => "ÃƒÆ’Ã‚Â¢Ãƒâ€šÃ‚ÂÃƒâ€šÃ‚Â³",
                                        _ => "ÃƒÆ’Ã‚Â¢Ãƒâ€šÃ‚ÂÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ"
                                    };
                                    
                                    LoggingService.LogInfo($"   {statusEmoji} {action.InstanceName}: Status={action.status}, Completed={nodeInfo.IsCompleted}, Predecessors={nodeInfo.Predecessors.Count}");
                                    
                                    // Log order relations for this node
                                    if (nodeInfo.Predecessors.Count > 0)
                                    {
                                        LoggingService.LogInfo($"      ÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¹ Predecessors:");
                                        foreach (var pred in nodeInfo.Predecessors)
                                        {
                                            LoggingService.LogInfo($"         - {pred.From.ActionNode.InstanceName} (MEETS)");
                                        }
                                    }
                                    
                                    if (nodeInfo.Successors.Count > 0)
                                    {
                                        LoggingService.LogInfo($"      ÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¹ Successors:");
                                        foreach (var succ in nodeInfo.Successors)
                                        {
                                            LoggingService.LogInfo($"         - {succ.To.ActionNode.InstanceName} (MEETS)");
                                        }
                                    }
                                }
                            }
                            
                            // Log all order relations in the graph
                            LoggingService.LogInfo($"   ÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸ÃƒÂ¢Ã¢â€šÂ¬Ã‚ÂÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â ALL ORDER RELATIONS:");
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
                LoggingService.LogError($"ÃƒÆ’Ã‚Â¢Ãƒâ€šÃ‚ÂÃƒâ€¦Ã¢â‚¬â„¢ Error logging NodeGraph details: {ex.Message}");
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
                            LoggingService.LogInfo($"\nÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸ÃƒÂ¢Ã¢â€šÂ¬Ã‚ÂÃƒÂ¢Ã¢â€šÂ¬Ã…Â¾ TICK {tickNumber} - ACTION STATUS CHANGES:");
                            hasStatusChanges = true;
                        }

                        var statusEmoji = currentStatus switch
                        {
                            BTNodeResult.Success => "ÃƒÆ’Ã‚Â¢Ãƒâ€¦Ã¢â‚¬Å“ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¦",
                            BTNodeResult.Failure => "ÃƒÆ’Ã‚Â¢Ãƒâ€šÃ‚ÂÃƒâ€¦Ã¢â‚¬â„¢",
                            BTNodeResult.InProgress => "ÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸ÃƒÂ¢Ã¢â€šÂ¬Ã‚ÂÃƒÂ¢Ã¢â€šÂ¬Ã…Â¾",
                            BTNodeResult.ReadyToTick => "ÃƒÆ’Ã‚Â¢Ãƒâ€šÃ‚ÂÃƒâ€šÃ‚Â³",
                            _ => "ÃƒÆ’Ã‚Â¢Ãƒâ€šÃ‚ÂÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ"
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
                    
                    LoggingService.LogInfo($"   ÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œÃƒâ€¦Ã‚Â  Tick {tickNumber}: {activeActions} active, {completedActions} completed, {failedActions} failed");
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"ÃƒÆ’Ã‚Â¢Ãƒâ€šÃ‚ÂÃƒâ€¦Ã¢â‚¬â„¢ Error logging action status changes: {ex.Message}");
            }
        }

        // Log subtree status for high-level actions
        private void LogSubtreeStatusForHLActions(BehaviorTree behaviorTree, int tickNumber)
        {
            try
            {
                var rootNode = behaviorTree.root as BTFlowNodeComposite;
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
                                            LoggingService.LogInfo($"\nÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸Ãƒâ€¦Ã¢â‚¬â„¢Ãƒâ€šÃ‚Â³ SUBTREE NODEGRAPH DETAILS ({genericAction.InstanceName}) - TICK {tickNumber}:");
                                            LoggingService.LogInfo($"   ÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œÃƒâ€¦Ã‚Â  Total subtree nodes: {subtreeActions.Count}");
                                            
                                            // Log each subtree node's details
                                            foreach (var subtreeAction in subtreeActions)
                                            {
                                                var subtreeNodeInfo = subtreeActionGraph.GetNodeInfo(subtreeAction);
                                                if (subtreeNodeInfo != null)
                                                {
                                                    var statusEmoji = subtreeAction.status switch
                                                    {
                                                        BTNodeResult.Success => "ÃƒÆ’Ã‚Â¢Ãƒâ€¦Ã¢â‚¬Å“ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¦",
                                                        BTNodeResult.Failure => "ÃƒÆ’Ã‚Â¢Ãƒâ€šÃ‚ÂÃƒâ€¦Ã¢â‚¬â„¢",
                                                        BTNodeResult.InProgress => "ÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸ÃƒÂ¢Ã¢â€šÂ¬Ã‚ÂÃƒÂ¢Ã¢â€šÂ¬Ã…Â¾",
                                                        BTNodeResult.ReadyToTick => "ÃƒÆ’Ã‚Â¢Ãƒâ€šÃ‚ÂÃƒâ€šÃ‚Â³",
                                                        _ => "ÃƒÆ’Ã‚Â¢Ãƒâ€šÃ‚ÂÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ"
                                                    };
                                                    
                                                    LoggingService.LogInfo($"   {statusEmoji} {subtreeAction.InstanceName}: Status={subtreeAction.status}, Completed={subtreeNodeInfo.IsCompleted}, Predecessors={subtreeNodeInfo.Predecessors.Count}");
                                                    
                                                    // Log order relations for this subtree node
                                                    if (subtreeNodeInfo.Predecessors.Count > 0)
                                                    {
                                                        LoggingService.LogInfo($"      ÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¹ Subtree Predecessors:");
                                                        foreach (var pred in subtreeNodeInfo.Predecessors)
                                                        {
                                                            LoggingService.LogInfo($"         - {pred.From.ActionNode.InstanceName} (MEETS)");
                                                        }
                                                    }
                                                    
                                                    if (subtreeNodeInfo.Successors.Count > 0)
                                                    {
                                                        LoggingService.LogInfo($"      ÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¹ Subtree Successors:");
                                                        foreach (var succ in subtreeNodeInfo.Successors)
                                                        {
                                                            LoggingService.LogInfo($"         - {succ.To.ActionNode.InstanceName} (MEETS)");
                                                        }
                                                    }
                                                }
                                            }
                                            
                                            // Log all order relations in the subtree graph
                                            LoggingService.LogInfo($"   ÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸ÃƒÂ¢Ã¢â€šÂ¬Ã‚ÂÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â SUBTREE ORDER RELATIONS:");
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
                                                    LoggingService.LogInfo($"\nÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸Ãƒâ€¦Ã¢â‚¬â„¢Ãƒâ€šÃ‚Â³ TICK {tickNumber} - SUBTREE STATUS UPDATE:");
                                                    hasSubtreeChanges = true;
                                                }
                                                
                                                var statusEmoji = subtreeAction.status switch
                                                {
                                                    BTNodeResult.Success => "ÃƒÆ’Ã‚Â¢Ãƒâ€¦Ã¢â‚¬Å“ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¦",
                                                    BTNodeResult.Failure => "ÃƒÆ’Ã‚Â¢Ãƒâ€šÃ‚ÂÃƒâ€¦Ã¢â‚¬â„¢",
                                                    BTNodeResult.InProgress => "ÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸ÃƒÂ¢Ã¢â€šÂ¬Ã‚ÂÃƒÂ¢Ã¢â€šÂ¬Ã…Â¾",
                                                    BTNodeResult.ReadyToTick => "ÃƒÆ’Ã‚Â¢Ãƒâ€šÃ‚ÂÃƒâ€šÃ‚Â³",
                                                    _ => "ÃƒÆ’Ã‚Â¢Ãƒâ€šÃ‚ÂÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ"
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
                LoggingService.LogError($"ÃƒÆ’Ã‚Â¢Ãƒâ€šÃ‚ÂÃƒâ€¦Ã¢â‚¬â„¢ Error logging subtree status: {ex.Message}");
            }
        }
        // Log detailed subtree NodeGraph information on every tick
        private void LogDetailedSubtreeNodeGraphs(BehaviorTree behaviorTree, int tickNumber)
        {
            try
            {
                var rootNode = behaviorTree.root as BTFlowNodeComposite;
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
                                            LoggingService.LogInfo($"\nÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸Ãƒâ€¦Ã¢â‚¬â„¢Ãƒâ€šÃ‚Â³ SUBTREE EXECUTION DETAILS ({genericAction.InstanceName}) - TICK {tickNumber}:");
                                            
                                            // Count statuses
                                            var succeededCount = subtreeActions.Count(a => a.status == BTNodeResult.Success);
                                            var failedCount = subtreeActions.Count(a => a.status == BTNodeResult.Failure);
                                            var inProgressCount = subtreeActions.Count(a => a.status == BTNodeResult.InProgress);
                                            var readyCount = subtreeActions.Count(a => a.status == BTNodeResult.ReadyToTick);
                                            
                                            LoggingService.LogInfo($"   ÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œÃƒâ€¦Ã‚Â  Subtree Progress: {succeededCount}ÃƒÆ’Ã‚Â¢Ãƒâ€¦Ã¢â‚¬Å“ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¦ {inProgressCount}ÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸ÃƒÂ¢Ã¢â€šÂ¬Ã‚ÂÃƒÂ¢Ã¢â€šÂ¬Ã…Â¾ {failedCount}ÃƒÆ’Ã‚Â¢Ãƒâ€šÃ‚ÂÃƒâ€¦Ã¢â‚¬â„¢ {readyCount}ÃƒÆ’Ã‚Â¢Ãƒâ€šÃ‚ÂÃƒâ€šÃ‚Â³");
                                            
                                            // Log each subtree action with its current status
                                            foreach (var subtreeAction in subtreeActions)
                                            {
                                                var statusEmoji = subtreeAction.status switch
                                                {
                                                    BTNodeResult.Success => "ÃƒÆ’Ã‚Â¢Ãƒâ€¦Ã¢â‚¬Å“ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¦",
                                                    BTNodeResult.Failure => "ÃƒÆ’Ã‚Â¢Ãƒâ€šÃ‚ÂÃƒâ€¦Ã¢â‚¬â„¢",
                                                    BTNodeResult.InProgress => "ÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸ÃƒÂ¢Ã¢â€šÂ¬Ã‚ÂÃƒÂ¢Ã¢â€šÂ¬Ã…Â¾",
                                                    BTNodeResult.ReadyToTick => "ÃƒÆ’Ã‚Â¢Ãƒâ€šÃ‚ÂÃƒâ€šÃ‚Â³",
                                                    _ => "ÃƒÆ’Ã‚Â¢Ãƒâ€šÃ‚ÂÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ"
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
                LoggingService.LogError($"ÃƒÆ’Ã‚Â¢Ãƒâ€šÃ‚ÂÃƒâ€¦Ã¢â‚¬â„¢ Error logging detailed subtree NodeGraphs: {ex.Message}");
            }
        }

        // Log final action status summary
        private void LogFinalActionStatusSummary(Dictionary<string, BTNodeResult> actionStatusHistory)
        {
            LoggingService.LogSubsection("ÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œÃƒâ€¦Ã‚Â  FINAL ACTION STATUS SUMMARY");
            
            var succeededActions = actionStatusHistory.Where(kvp => kvp.Value == BTNodeResult.Success).ToList();
            var failedActions = actionStatusHistory.Where(kvp => kvp.Value == BTNodeResult.Failure).ToList();
            var inProgressActions = actionStatusHistory.Where(kvp => kvp.Value == BTNodeResult.InProgress).ToList();
            var readyActions = actionStatusHistory.Where(kvp => kvp.Value == BTNodeResult.ReadyToTick).ToList();

            LoggingService.LogSuccess($"ÃƒÆ’Ã‚Â¢Ãƒâ€¦Ã¢â‚¬Å“ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¦ SUCCEEDED ({succeededActions.Count}):");
            foreach (var action in succeededActions)
            {
                LoggingService.LogSuccess($"   - {action.Key}");
            }

            LoggingService.LogError($"\nÃƒÆ’Ã‚Â¢Ãƒâ€šÃ‚ÂÃƒâ€¦Ã¢â‚¬â„¢ FAILED ({failedActions.Count}):");
            foreach (var action in failedActions)
            {
                LoggingService.LogError($"   - {action.Key}");
            }

            LoggingService.LogInfo($"\nÃƒÆ’Ã‚Â°Ãƒâ€¦Ã‚Â¸ÃƒÂ¢Ã¢â€šÂ¬Ã‚ÂÃƒÂ¢Ã¢â€šÂ¬Ã…Â¾ IN PROGRESS ({inProgressActions.Count}):");
            foreach (var action in inProgressActions)
            {
                LoggingService.LogInfo($"   - {action.Key}");
            }

            LoggingService.LogInfo($"\nÃƒÆ’Ã‚Â¢Ãƒâ€šÃ‚ÂÃƒâ€šÃ‚Â³ READY TO TICK ({readyActions.Count}):");
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
            
            if (node is BTFlowNodeComposite compositeNode)
            {
                foreach (var child in compositeNode.GetChildren())
                {
                    actionNodes.AddRange(GetAllActionNodes(child));
                }
            }
            
            return actionNodes;
        }

        // Public method to run the test from Program.cs
        public static async Task RunTest()
        {
            var test = new FullTreeTest();
            await test.RunFullTreeTest();
        }
    }
}
