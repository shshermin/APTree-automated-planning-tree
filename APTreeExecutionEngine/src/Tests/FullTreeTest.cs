using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using BehaviorTreeMainProject.Services;
using BehaviorTreeMainProject.Services.AIPlanning;
using PlanningDataStructures;
using AIPlanning;
using ModelLoader.ParameterTypes;
using BehaviorTreeMainProject.Log.Services;


namespace BehaviorTreeMainProject
{
    public class FullTreeTest
    {
        // Track all planner executions
        private List<BTServicePlanner> allPlanners = new List<BTServicePlanner>();
        private DateTime testStartTime;
        private DateTime testEndTime;
        private IBTNode rootNode; // Store root node for monitoring
        
        public async Task RunFullTreeTest()
        {
            // Initialize logging service
            LoggingService.Initialize("FullTreeTest", enableConsole: true, enableFile: true);
            
            // Initialize execution flow logger
            ExecutionFlowLogger.Initialize("FullTreeTest", enableConsole: true, enableFile: true);
            
            // BlackboardTrackingLogger is automatically initialized when first accessed
            // No need to call Initialize() explicitly
            
            testStartTime = DateTime.Now;
            
            LoggingService.LogSection("ÃƒÂ°Ã…Â¸Ã…â€™Ã‚Â³ FULL BEHAVIOR TREE TEST");
            LoggingService.LogSuccess($"ÃƒÂ°Ã…Â¸Ã…Â¡Ã¢â€šÂ¬ Started at: {testStartTime:yyyy-MM-dd HH:mm:ss.fff}");

            try
            {
                // Create blackboard instance and test Neo4j connection
                using var blackboard = new Blackboard<FastName>("bolt://localhost:7687", "neo4j", "12345678");
                
                // Test Neo4j connection
                LoggingService.LogInfo("ÃƒÂ°Ã…Â¸Ã¢â‚¬ÂÃ‚Â Testing Neo4j connection...");
                bool connectionSuccess = await TestNeo4jConnection(blackboard);

                if (connectionSuccess)
                {
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

                     // making the tree

                                         // Inspect blackboard contents
                    LoggingService.LogSection("INSPECTING BLACKBOARD CONTENTS");
                     await InspectBlackboard(blackboard);

                     // Create behavior tree with cassette flow nodes
                    LoggingService.LogSection("CREATING BEHAVIOR TREE WITH CASSETTE FLOW NODES");
                     await CreateCassetteBehaviorTree(blackboard);
                     

                }

                testEndTime = DateTime.Now;
                
                LoggingService.LogSection("ÃƒÂ°Ã…Â¸Ã…Â½Ã¢â‚¬Â° FULL BEHAVIOR TREE TEST COMPLETED!");
                LoggingService.LogSuccess($"ÃƒÂ¢Ã‚ÂÃ‚Â° Finished at: {testEndTime:yyyy-MM-dd HH:mm:ss.fff}");
                LoggingService.LogSuccess($"ÃƒÂ¢Ã‚ÂÃ‚Â±ÃƒÂ¯Ã‚Â¸Ã‚Â Total test duration: {testEndTime - testStartTime:hh\\:mm\\:ss\\.fff}");
                
                // Display execution summary
                await DisplayExecutionSummary();

                // Generate summary table at the end
                LoggingService.GenerateSummaryTable();
                
                // Generate execution summary
                ExecutionSummaryLogger.GenerateSummary();
                ExecutionSummaryLogger.Close();
                
                // Log final blackboard tracking statistics
                BlackboardTrackingLogger.LogStatistics();
                
                // Generate comprehensive CSV summary
                BlackboardSummaryLogger.GenerateCSVSummary(blackboard);
                BlackboardSummaryLogger.Close();
                
                // Generate behavior tree component CSV summary
                BehaviorTreeComponentLogger.GenerateCSVSummary(blackboard);
                BehaviorTreeComponentLogger.Close();
                
                // Generate planner statistics CSV summary
                PlannerSummaryLogger.GenerateCSVSummary();
                PlannerSummaryLogger.Close();
                
                // Generate tick timing CSV summary
                TickTimingLogger.GenerateCSVSummary();
                TickTimingLogger.Close();
                

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
                LoggingService.LogError($"\nÃƒÂ¢Ã‚ÂÃ…â€™ ERROR during full tree test: {ex.Message}");
                LoggingService.LogError($"   Stack trace: {ex.StackTrace}");
                
                // Generate summary table even if test failed
                LoggingService.GenerateSummaryTable();
                
                // Generate execution summary even if test failed
                ExecutionSummaryLogger.GenerateSummary();
                ExecutionSummaryLogger.Close();
                
                // Close logging service
                LoggingService.Close();
                
                // Close execution flow logger
                ExecutionFlowLogger.Close();
                
                throw;
            }
        }

        // Test Neo4j connection
        private async Task<bool> TestNeo4jConnection(Blackboard<FastName> blackboard)
        {
            try
            {
                // Try to connect to Neo4j
                bool connected = await blackboard.TestNeo4jConnection();
                if (connected)
                {
                    LoggingService.LogSuccess("ÃƒÂ¢Ã…â€œÃ¢â‚¬Â¦ Successfully connected to Neo4j");
                    return true;
                }
                else
                {
                    LoggingService.LogError("ÃƒÂ¢Ã‚ÂÃ…â€™ Neo4j connection test failed");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"ÃƒÂ¢Ã‚ÂÃ…â€™ Failed to connect to Neo4j: {ex.Message}");
                LoggingService.LogInfo("   Make sure Neo4j is running and accessible at bolt://localhost:7687");
                LoggingService.LogInfo("   Check your Neo4j credentials (neo4j/12345678)");
                return false;
            }
        }

        // Inspect blackboard contents
        private async Task InspectBlackboard(Blackboard<FastName> blackboard)
        {
            LoggingService.LogSubsection("ÃƒÂ°Ã…Â¸Ã¢â‚¬Å“Ã¢â‚¬Â¹ BLACKBOARD INSPECTION REPORT");

            try
            {
                // 1. Entity Types
                var entityTypes = blackboard.GetAllEntityTypes();
                LoggingService.LogInfo($"\nÃƒÂ°Ã…Â¸Ã‚ÂÃ‚Â·ÃƒÂ¯Ã‚Â¸Ã‚Â  ENTITY TYPES ({entityTypes.Count}):");
                foreach (var entityType in entityTypes)
                {
                    LoggingService.LogInfo($"   - {entityType.ToString()}");
                }

                // 2. Predicate Types
                var predicateTypes = blackboard.GetAllPredicateTypes();
                LoggingService.LogInfo($"\nÃƒÂ°Ã…Â¸Ã¢â‚¬ÂÃ‚Â PREDICATE TYPES ({predicateTypes.Count}):");
                foreach (var predicateType in predicateTypes)
                {
                    LoggingService.LogInfo($"   - {predicateType.ToString()}");
                }

                // 3. Action Types
                var actionTypes = blackboard.GetAllActionTypes();
                LoggingService.LogInfo($"\nÃƒÂ¢Ã…Â¡Ã‚Â¡ ACTION TYPES ({actionTypes.Count}):");
                foreach (var actionType in actionTypes)
                {
                    LoggingService.LogInfo($"   - {actionType.ToString()}");
                }

                // 4. Action Instances
                var actionInstances = blackboard.GetAllActionInstances();
                LoggingService.LogInfo($"\nÃƒÂ°Ã…Â¸Ã…Â½Ã‚Â¯ ACTION INSTANCES ({actionInstances.Count}):");
                foreach (var actionInstance in actionInstances)
                {
                    LoggingService.LogInfo($"   - {actionInstance.InstanceName.ToString()} (Type: {actionInstance.actionType.ToString()})");
                }

                // 5. Built-in Values
                LoggingService.LogInfo($"\nÃƒÂ°Ã…Â¸Ã¢â‚¬Å“Ã‚Â BUILT-IN VALUES:");
                LoggingService.LogInfo($"   - Int Values: {GetDictionaryCount(blackboard, "IntValues")}");
                if (GetDictionaryCount(blackboard, "IntValues") > 0)
                {
                    foreach (var item in GetDictionaryItems(blackboard, "IntValues"))
                    {
                        LoggingService.LogInfo($"     ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¢ {item.Key}: {item.Value}");
                    }
                }
                
                LoggingService.LogInfo($"   - Double Values: {GetDictionaryCount(blackboard, "DoubleValues")}");
                if (GetDictionaryCount(blackboard, "DoubleValues") > 0)
                {
                    foreach (var item in GetDictionaryItems(blackboard, "DoubleValues"))
                    {
                        LoggingService.LogInfo($"     ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¢ {item.Key}: {item.Value}");
                    }
                }
                
                LoggingService.LogInfo($"   - Bool Values: {GetDictionaryCount(blackboard, "BoolValues")}");
                if (GetDictionaryCount(blackboard, "BoolValues") > 0)
                {
                    foreach (var item in GetDictionaryItems(blackboard, "BoolValues"))
                    {
                        LoggingService.LogInfo($"     ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¢ {item.Key}: {item.Value}");
                    }
                }
                
                LoggingService.LogInfo($"   - String Values: {GetDictionaryCount(blackboard, "StringValues")}");
                if (GetDictionaryCount(blackboard, "StringValues") > 0)
                {
                    foreach (var item in GetDictionaryItems(blackboard, "StringValues"))
                    {
                        LoggingService.LogInfo($"     ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¢ {item.Key}: {item.Value}");
                    }
                }

                // 6. Entity Values
                LoggingService.LogInfo($"\nÃƒÂ°Ã…Â¸Ã‚ÂÃ¢â‚¬â€ÃƒÂ¯Ã‚Â¸Ã‚Â  ENTITY VALUES:");
                LoggingService.LogInfo($"   - Element Values: {GetDictionaryCount(blackboard, "ElementValues")}");
                if (GetDictionaryCount(blackboard, "ElementValues") > 0)
                {
                    foreach (var item in GetDictionaryItems(blackboard, "ElementValues"))
                    {
                        LoggingService.LogInfo($"     ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¢ {item.Key}: {item.Value}");
                    }
                }
                
                LoggingService.LogInfo($"   - Location Values: {GetDictionaryCount(blackboard, "LocationValues")}");
                if (GetDictionaryCount(blackboard, "LocationValues") > 0)
                {
                    foreach (var item in GetDictionaryItems(blackboard, "LocationValues"))
                    {
                        LoggingService.LogInfo($"     ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¢ {item.Key}: {item.Value}");
                    }
                }
                
                LoggingService.LogInfo($"   - Agent Values: {GetDictionaryCount(blackboard, "AgentValues")}");
                if (GetDictionaryCount(blackboard, "AgentValues") > 0)
                {
                    foreach (var item in GetDictionaryItems(blackboard, "AgentValues"))
                    {
                        LoggingService.LogInfo($"     ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¢ {item.Key}: {item.Value}");
                    }
                }
                
                LoggingService.LogInfo($"   - Layer Values: {GetDictionaryCount(blackboard, "LayerValues")}");
                if (GetDictionaryCount(blackboard, "LayerValues") > 0)
                {
                    foreach (var item in GetDictionaryItems(blackboard, "LayerValues"))
                    {
                        LoggingService.LogInfo($"     ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¢ {item.Key}: {item.Value}");
                    }
                }
                
                LoggingService.LogInfo($"   - Module Values: {GetDictionaryCount(blackboard, "ModuleValues")}");
                if (GetDictionaryCount(blackboard, "ModuleValues") > 0)
                {
                    foreach (var item in GetDictionaryItems(blackboard, "ModuleValues"))
                    {
                        LoggingService.LogInfo($"     ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¢ {item.Key}: {item.Value}");
                    }
                }
                
                LoggingService.LogInfo($"   - Tool Values: {GetDictionaryCount(blackboard, "ToolValues")}");
                if (GetDictionaryCount(blackboard, "ToolValues") > 0)
                {
                    foreach (var item in GetDictionaryItems(blackboard, "ToolValues"))
                    {
                        LoggingService.LogInfo($"     ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¢ {item.Key}: {item.Value}");
                    }
                }

                // 7. Predicate Values
                LoggingService.LogInfo($"\nÃƒÂ°Ã…Â¸Ã¢â‚¬ÂÃ‚Â PREDICATE VALUES:");
                LoggingService.LogInfo($"   - Predicate Values: {GetDictionaryCount(blackboard, "PredicateValues")}");
                if (GetDictionaryCount(blackboard, "PredicateValues") > 0)
                {
                    foreach (var item in GetDictionaryItems(blackboard, "PredicateValues"))
                    {
                        LoggingService.LogInfo($"     ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¢ {item.Key}: {item.Value}");
                    }
                }

                // 8. Action Values
                LoggingService.LogInfo($"\nÃƒÂ¢Ã…Â¡Ã‚Â¡ ACTION VALUES:");
                LoggingService.LogInfo($"   - Action Values: {GetDictionaryCount(blackboard, "ActionValues")}");
                if (GetDictionaryCount(blackboard, "ActionValues") > 0)
                {
                    foreach (var item in GetDictionaryItems(blackboard, "ActionValues"))
                    {
                        LoggingService.LogInfo($"     ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¢ {item.Key}: {item.Value}");
                    }
                }

                // 9. State Values
                LoggingService.LogInfo($"\nÃƒÂ°Ã…Â¸Ã…â€™Ã‚Â STATE VALUES:");
                LoggingService.LogInfo($"   - State Values: {GetDictionaryCount(blackboard, "StateValues")}");
                if (GetDictionaryCount(blackboard, "StateValues") > 0)
                {
                    foreach (var item in GetDictionaryItems(blackboard, "StateValues"))
                    {
                        LoggingService.LogInfo($"     ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¢ {item.Key}: {item.Value}");
                    }
                }

                // 8. NodeGraphs
                var nodeGraphs = blackboard.GetAllNodeGraphs();
                LoggingService.LogInfo($"\nÃƒÂ°Ã…Â¸Ã…â€™Ã‚Â³ NODEGRAPHS ({nodeGraphs.Count}):");
                foreach (var nodeGraph in nodeGraphs)
                {
                    LoggingService.LogInfo($"   - NodeGraph with {nodeGraph.GetAllActionNodes().Count} action nodes");
                }

                // 10. Summary
                LoggingService.LogInfo($"\nÃƒÂ°Ã…Â¸Ã¢â‚¬Å“Ã…Â  SUMMARY:");
                LoggingService.LogInfo($"   - Entity Types: {entityTypes.Count}");
                LoggingService.LogInfo($"   - Predicate Types: {predicateTypes.Count}");
                LoggingService.LogInfo($"   - Action Types: {actionTypes.Count}");
                LoggingService.LogInfo($"   - Action Instances: {actionInstances.Count}");
                LoggingService.LogInfo($"   - NodeGraphs: {nodeGraphs.Count}");
                LoggingService.LogInfo($"   - TOTAL ITEMS: {entityTypes.Count + predicateTypes.Count + actionTypes.Count + actionInstances.Count + nodeGraphs.Count}");

            }
            catch (Exception ex)
            {
                LoggingService.LogError($"ÃƒÂ¢Ã‚ÂÃ…â€™ Error during blackboard inspection: {ex.Message}");
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
                            // For Entity objects, use NameKey.ToString() instead of the full type name
                            object displayValue = entry.Value;
                            if (entry.Value is Entity entity)
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
                LoggingService.LogInfo("ÃƒÂ°Ã…Â¸Ã…â€™Ã‚Â³ Creating behavior tree with cassette flow nodes...");

                // Create behavior tree instance first
                var behaviorTree = new BehaviorTreeInstance();
                behaviorTree.Initialise(blackboard, "CassetteBehaviorTree");
                LoggingService.LogSuccess("ÃƒÂ¢Ã…â€œÃ¢â‚¬Â¦ Created behavior tree instance");

                // Create root composite flow node
                rootNode = new BTFlowNode_Composite(new FastName("RootComposite"), behaviorTree);
                //var rootNode = new BTFlowNode_CostBasedComposite(new FastName("RootComposite"), behaviorTree);
                
                // Ensure we start in planning phase
                blackboard.PlanningPhase = true;
                // Initialize cassette subtree completion flags to false (four cassettes)
                blackboard.CassetteSubtreeCompleted = new bool[4] { false, false, false, false };
                LoggingService.LogInfo("ÃƒÂ°Ã…Â¸Ã¢â‚¬ÂÃ‚Â§ Starting in PLANNING PHASE - all HL actions will generate NodeGraphs first");
                LoggingService.LogInfo("ÃƒÂ°Ã…Â¸Ã¢â‚¬ÂÃ‚Â§ Cassette subtree completion flags initialized to false for all 4 cassettes");
                LoggingService.LogSuccess("ÃƒÂ¢Ã…â€œÃ¢â‚¬Â¦ Created root composite flow node");

                // Create four cassette flow nodes
                var cassette1Node = new BTFlowNode_Dynamic(new FastName("cassette1"), behaviorTree, SuccessCriteria.ALL, 1.0f, true);  // Add LowestCost decorator
                var cassette2Node = new BTFlowNode_Dynamic(new FastName("cassette2"), behaviorTree, SuccessCriteria.ALL, 1.0f, true);  // Add LowestCost decorator
                var cassette3Node = new BTFlowNode_Dynamic(new FastName("cassette3"), behaviorTree, SuccessCriteria.ALL, 1.0f, true);  // Add LowestCost decorator
                var cassette4Node = new BTFlowNode_Dynamic(new FastName("cassette4"), behaviorTree, SuccessCriteria.ALL, 1.0f, true);  // Add LowestCost decorator

                LoggingService.LogSuccess("ÃƒÂ¢Ã…â€œÃ¢â‚¬Â¦ Created four cassette flow nodes");

                // Add all cassette nodes to the root composite node
                ((BTFlowNode_Composite)rootNode).AddChild(cassette1Node);
                ((BTFlowNode_Composite)rootNode).AddChild(cassette2Node);
                ((BTFlowNode_Composite)rootNode).AddChild(cassette3Node);
                ((BTFlowNode_Composite)rootNode).AddChild(cassette4Node);

                LoggingService.LogSuccess("ÃƒÂ¢Ã…â€œÃ¢â‚¬Â¦ Added all four cassette nodes to root composite node");

                // Add planning phase management service to the root composite node
                ((BTFlowNode_Composite)rootNode).AddPlanningPhaseService();
                LoggingService.LogSuccess("ÃƒÂ¢Ã…â€œÃ¢â‚¬Â¦ Added planning phase management service to root composite node");
                
                

                // Set the root node
                behaviorTree.root = (BTFlowNode_Composite)rootNode;
                rootNode.SetOwiningTree(behaviorTree);
                rootNode.SetTreeForAllServices(behaviorTree);

                // Create PDDL planners for all four cassettes (after behavior tree is created)
                // Different planners and problem files for each cassette
                var pddlRequest1 = new PDDLPlanningRequest("./Plannerinputs/domain.pddl", "./Plannerinputs/problemC1.pddl", "/home/shermin/ENHSP-Public/enhsp.jar", "ENHSP");
                var pddlRequest2 = new PDDLPlanningRequest("./Plannerinputs/domain.pddl", "./Plannerinputs/problemC2.pddl", "/home/shermin/ENHSP-Public/enhsp.jar", "ENHSP");
                var pddlRequest3 = new PDDLPlanningRequest("./Plannerinputs/domain.pddl", "./Plannerinputs/problemC3.pddl", "/home/shermin/ENHSP-Public/enhsp.jar", "ENHSP");
                var pddlRequest4 = new PDDLPlanningRequest("./Plannerinputs/domain.pddl", "./Plannerinputs/problemC4.pddl", "/home/shermin/ENHSP-Public/enhsp.jar", "ENHSP");

                var pddlPlanner1 = new CallPDDLPlanner(behaviorTree, pddlRequest1);
                var pddlPlanner2 = new CallPDDLPlanner(behaviorTree, pddlRequest2);
                var pddlPlanner3 = new CallPDDLPlanner(behaviorTree, pddlRequest3);
                var pddlPlanner4 = new CallPDDLPlanner(behaviorTree, pddlRequest4);
                
                // Track all planners for execution summary
                allPlanners.Add(pddlPlanner1);
                allPlanners.Add(pddlPlanner2);
                allPlanners.Add(pddlPlanner3);
                allPlanners.Add(pddlPlanner4);
                
                // Configure execution modes for the cassettes
                pddlPlanner1.ExecutionMode = CallPDDLPlanner.ParallelExecutionMode.Parallel;    // Parallel execution
                pddlPlanner2.ExecutionMode = CallPDDLPlanner.ParallelExecutionMode.Parallel;    // Parallel execution
                pddlPlanner3.ExecutionMode = CallPDDLPlanner.ParallelExecutionMode.Parallel;    // Parallel execution
                pddlPlanner4.ExecutionMode = CallPDDLPlanner.ParallelExecutionMode.Parallel;    // Parallel execution

                LoggingService.LogInfo("ÃƒÂ¢Ã…â€œÃ¢â‚¬Â¦ Created PDDL planners for all four cassettes");
                LoggingService.LogInfo($"ÃƒÂ°Ã…Â¸Ã¢â‚¬ÂÃ‚Â§ Execution Modes:");
                LoggingService.LogInfo($"   - Cassette 1: {pddlPlanner1.ExecutionMode} (Planner: {pddlRequest1.PlannerName})");
                LoggingService.LogInfo($"   - Cassette 2: {pddlPlanner2.ExecutionMode} (Planner: {pddlRequest2.PlannerName})");
                LoggingService.LogInfo($"   - Cassette 3: {pddlPlanner3.ExecutionMode} (Planner: {pddlRequest3.PlannerName})");
                LoggingService.LogInfo($"   - Cassette 4: {pddlPlanner4.ExecutionMode} (Planner: {pddlRequest4.PlannerName})");

                // Set the planning services on all flow nodes
                cassette1Node.SetPlanningService(pddlPlanner1);
                cassette2Node.SetPlanningService(pddlPlanner2);
                cassette3Node.SetPlanningService(pddlPlanner3);
                cassette4Node.SetPlanningService(pddlPlanner4);

                LoggingService.LogSuccess("ÃƒÂ¢Ã…â€œÃ¢â‚¬Â¦ Set planning services on all four cassette flow nodes");
                
                // Debug: Check if planners are properly configured
                LoggingService.LogInfo($"ÃƒÂ°Ã…Â¸Ã¢â‚¬ÂÃ‚Â Planner Debug Info:");
                LoggingService.LogInfo($"   Cassette 1 - Domain: {pddlRequest1.DomainFile}, Problem: {pddlRequest1.ProblemFile}, Planner: {pddlRequest1.PlannerName}");
                LoggingService.LogInfo($"   Cassette 2 - Domain: {pddlRequest2.DomainFile}, Problem: {pddlRequest2.ProblemFile}, Planner: {pddlRequest2.PlannerName}");
                LoggingService.LogInfo($"   Cassette 3 - Domain: {pddlRequest3.DomainFile}, Problem: {pddlRequest3.ProblemFile}, Planner: {pddlRequest3.PlannerName}");
                LoggingService.LogInfo($"   Cassette 4 - Domain: {pddlRequest4.DomainFile}, Problem: {pddlRequest4.ProblemFile}, Planner: {pddlRequest4.PlannerName}");

                // Store the behavior tree in the blackboard for later use
                blackboard.SetNodeGraph(new FastName("MainBehaviorTree"), new NodeGraph());
                LoggingService.LogSuccess("ÃƒÂ¢Ã…â€œÃ¢â‚¬Â¦ Stored behavior tree reference in blackboard");

                // Display tree structure
                LoggingService.LogInfo("\nÃƒÂ°Ã…Â¸Ã¢â‚¬Å“Ã¢â‚¬Â¹ BEHAVIOR TREE STRUCTURE:");
                LoggingService.LogInfo($"Root: BTFlowNode_Composite ({((BTFlowNode_Composite)rootNode).GetNodeName()})");
                LoggingService.LogInfo($"ÃƒÂ¢Ã¢â‚¬ÂÃ…â€œÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬ÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬ BTFlowNode_Dynamic ({cassette1Node.GetNodeName()}) - {pddlRequest1.PlannerName} Planner");
                LoggingService.LogInfo($"ÃƒÂ¢Ã¢â‚¬ÂÃ…â€œÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬ÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬ BTFlowNode_Dynamic ({cassette2Node.GetNodeName()}) - {pddlRequest2.PlannerName} Planner");
                LoggingService.LogInfo($"ÃƒÂ¢Ã¢â‚¬ÂÃ…â€œÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬ÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬ BTFlowNode_Dynamic ({cassette3Node.GetNodeName()}) - {pddlRequest3.PlannerName} Planner");
                LoggingService.LogInfo($"ÃƒÂ¢Ã¢â‚¬ÂÃ¢â‚¬ÂÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬ÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬ BTFlowNode_Dynamic ({cassette4Node.GetNodeName()}) - {pddlRequest4.PlannerName} Planner");

                LoggingService.LogSuccess("\nÃƒÂ°Ã…Â¸Ã…Â½Ã¢â‚¬Â° Behavior tree with cassette flow nodes created successfully!");

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
                
                // Optional: Continuous monitoring of subtree status (uncomment to enable)
                // await MonitorSubtreeStatusContinuously(behaviorTree);
                
                
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"ÃƒÂ¢Ã‚ÂÃ…â€™ Error creating behavior tree: {ex.Message}");
                LoggingService.LogError($"   Stack trace: {ex.StackTrace}");
            }
        }

        // Test the behavior tree structure
        private async Task TestBehaviorTreeStructure(BehaviorTreeInstance behaviorTree)
        {
            try
            {
                LoggingService.LogInfo("\nÃƒÂ°Ã…Â¸Ã‚Â§Ã‚Âª Testing behavior tree structure...");

                // Track memory usage before tree execution
                var memoryBefore = GC.GetTotalMemory(false);
                ExecutionSummaryLogger.TrackMemoryUsage("Before Tree Execution", memoryBefore);
                
                // Test initial tick
                ExecutionSummaryLogger.StartTreeExecution();
                BlackboardSummaryLogger.StartTreeTicking();
                var result = behaviorTree.Tick(0.0f);
                BlackboardSummaryLogger.EndTreeTicking();
                ExecutionSummaryLogger.EndTreeExecution();
                LoggingService.LogSuccess($"ÃƒÂ¢Ã…â€œÃ¢â‚¬Â¦ Initial tree tick result: {result}");
                
                // Track memory usage after tree execution
                var memoryAfter = GC.GetTotalMemory(false);
                ExecutionSummaryLogger.TrackMemoryUsage("After Tree Execution", memoryAfter);
                
                // Track memory usage after planner execution
                var memoryAfterPlanner = GC.GetTotalMemory(false);
                ExecutionSummaryLogger.TrackMemoryUsage("After Planner Execution", memoryAfterPlanner);

                // Test individual cassette nodes
                var rootNode = behaviorTree.root as BTFlowNode_Composite;
                if (rootNode != null)
                {
                    var children = rootNode.GetChildren();
                    LoggingService.LogSuccess($"ÃƒÂ¢Ã…â€œÃ¢â‚¬Â¦ Root node has {children.Count} children");

                    for (int i = 0; i < children.Count; i++)
                    {
                        var child = children[i];
                        if (child is BTFlowNodeBase flowNode)
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
                    LoggingService.LogError($"ÃƒÂ¢Ã‚ÂÃ…â€™ Root node is not a BTFlowNode_Composite. Actual type: {behaviorTree.root?.GetType().Name ?? "null"}");
                }

                LoggingService.LogSuccess("ÃƒÂ¢Ã…â€œÃ¢â‚¬Â¦ Behavior tree structure test completed!");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"ÃƒÂ¢Ã‚ÂÃ…â€™ Error testing behavior tree structure: {ex.Message}");
            }
        }

        // Display NodeGraph status for each flow node
        private async Task DisplayNodeGraphStatus(BehaviorTreeInstance behaviorTree)
        {
            try
            {
                LoggingService.LogSubsection("ÃƒÂ°Ã…Â¸Ã¢â‚¬Å“Ã…Â  NODEGRAPH STATUS REPORT");
                LoggingService.LogInfo("=".PadRight(50, '='));

                var rootNode = behaviorTree.root as BTFlowNode_Composite;
                if (rootNode != null)
                {
                    var children = rootNode.GetChildren();
                    LoggingService.LogInfo($"ÃƒÂ°Ã…Â¸Ã¢â‚¬ÂÃ‚Â Checking {children.Count} flow nodes for NodeGraph status...\n");

                    for (int i = 0; i < children.Count; i++)
                    {
                        var child = children[i];
                        if (child is BTFlowNode_Dynamic dynamicNode)
                        {
                            LoggingService.LogInfo($"ÃƒÂ°Ã…Â¸Ã…Â½Ã‚Â¯ FLOW NODE {i + 1}: {dynamicNode.GetNodeName()}");
                            LoggingService.LogInfo($"   ÃƒÂ°Ã…Â¸Ã¢â‚¬Å“Ã¢â‚¬Â¹ Node Type: {child.GetType().Name}");
                            
                            // Check if planning service is set
                            if (dynamicNode.PlanningService != null)
                            {
                                LoggingService.LogInfo($"   ÃƒÂ°Ã…Â¸Ã¢â‚¬ÂÃ‚Â§ Planning Service: {dynamicNode.PlanningService.GetType().Name}");
                                
                                // Check if it's a BTServicePlanner
                                if (dynamicNode.PlanningService is BTServicePlanner plannerService)
                                {
                                    LoggingService.LogInfo($"   ÃƒÂ°Ã…Â¸Ã¢â‚¬Å“Ã…Â  Has Generated NodeGraph: {plannerService.HasGeneratedNodeGraph()}");
                                    
                                    if (plannerService.HasGeneratedNodeGraph())
                                    {
                                        var generatedGraph = plannerService.GetGeneratedNodeGraph();
                                        var actions = generatedGraph.GetAllActionNodes();
                                        LoggingService.LogInfo($"   ÃƒÂ°Ã…Â¸Ã¢â‚¬Å“Ã‹â€  Generated NodeGraph Actions: {actions.Count}");
                                        
                                        // List the actions
                                        for (int j = 0; j < actions.Count; j++)
                                        {
                                            LoggingService.LogInfo($"      {j + 1}. {actions[j].InstanceName.ToString()}");
                                        }
                                    }
                                    else
                                    {
                                        LoggingService.LogInfo($"   ÃƒÂ¢Ã…Â¡Ã‚Â ÃƒÂ¯Ã‚Â¸Ã‚Â No NodeGraph generated yet");
                                    }
                                }
                            }
                            else
                            {
                                LoggingService.LogInfo($"   ÃƒÂ¢Ã‚ÂÃ…â€™ No planning service set");
                            }
                            
                            // Check the actionGraph
                            var actionGraph = dynamicNode.GetActionGraph();
                            var actionGraphNodes = actionGraph.GetAllActionNodes();
                            LoggingService.LogInfo($"   ÃƒÂ°Ã…Â¸Ã¢â‚¬Å“Ã¢â‚¬Â¹ ActionGraph Nodes: {actionGraphNodes.Count}");
                            
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

                LoggingService.LogSuccess("ÃƒÂ¢Ã…â€œÃ¢â‚¬Â¦ NodeGraph status report completed!");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"ÃƒÂ¢Ã‚ÂÃ…â€™ Error displaying NodeGraph status: {ex.Message}");
            }
        }

        // Monitor planner execution in real-time
        private async Task MonitorPlannerExecution()
        {
            LoggingService.LogInfo("\nÃƒÂ°Ã…Â¸Ã¢â‚¬ÂÃ‚Â MONITORING PLANNER EXECUTION");
            LoggingService.LogInfo("=".PadRight(50, '='));
            
            if (allPlanners.Count == 0)
            {
                LoggingService.LogWarning("ÃƒÂ¢Ã…Â¡Ã‚Â ÃƒÂ¯Ã‚Â¸Ã‚Â No planners to monitor.");
                return;
            }
            
            LoggingService.LogInfo($"ÃƒÂ°Ã…Â¸Ã¢â‚¬ÂÃ‚Â Monitoring {allPlanners.Count} planners...");
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
                    LoggingService.LogInfo($"ÃƒÂ°Ã…Â¸Ã¢â‚¬ÂÃ‚Â PLANNER EXECUTION STATUS - {currentTime:HH:mm:ss}");
                    LoggingService.LogInfo("=".PadRight(50, '='));
                    
                    var completedCount = allPlanners.Count(p => p.HasCompleted);
                    var executingCount = allPlanners.Count(p => p.IsExecuting);
                    var pendingCount = allPlanners.Count(p => !p.HasCompleted && !p.IsExecuting);
                    
                    LoggingService.LogInfo($"ÃƒÂ°Ã…Â¸Ã¢â‚¬Å“Ã…Â  Progress: {completedCount}/{allPlanners.Count} completed, {executingCount} executing, {pendingCount} pending");
                    
                                         // Planning phase monitoring
                     if (rootNode is BTFlowNode_Composite compositeNode)
                     {
                         var planningComplete = compositeNode.AreAllPlanningServicesComplete();
                         LoggingService.LogInfo($"\nÃƒÂ°Ã…Â¸Ã¢â‚¬Å“Ã¢â‚¬Â¹ PLANNING PHASE STATUS:");
                         LoggingService.LogInfo($"   Planning Complete: {planningComplete}");
                         
                         var children = compositeNode.GetChildren();
                         LoggingService.LogInfo("ÃƒÂ°Ã…Â¸Ã¢â‚¬Å“Ã…Â  SUBTREE STATUSES:");
                         for (int i = 0; i < children.Count; i++)
                         {
                             var child = children[i];
                             if (child is BTFlowNode_Dynamic dynamicNode)
                             {
                                 var hasPlanningService = dynamicNode.PlanningService != null;
                                 var planningServiceType = hasPlanningService ? dynamicNode.PlanningService.GetType().Name : "None";
                                 LoggingService.LogInfo($"   {dynamicNode.GetNodeName()}: PlanningService={planningServiceType}");
                             }
                         }
                     }
                    
                    LoggingService.LogInfo("");
                    
                    foreach (var planner in allPlanners)
                    {
                        var status = planner.HasCompleted ? "ÃƒÂ¢Ã…â€œÃ¢â‚¬Â¦" : planner.IsExecuting ? "ÃƒÂ°Ã…Â¸Ã¢â‚¬ÂÃ¢â‚¬Å¾" : "ÃƒÂ¢Ã‚ÂÃ‚Â³";
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
                    LoggingService.LogInfo("\nÃƒÂ¢Ã…â€œÃ¢â‚¬Â¦ All planners have finished execution!");
                    break;
                }
                
                await Task.Delay(100); // Small delay to prevent high CPU usage
            }
            
            LoggingService.LogInfo($"ÃƒÂ¢Ã‚ÂÃ‚Â±ÃƒÂ¯Ã‚Â¸Ã‚Â Monitoring duration: {DateTime.Now - monitoringStartTime:hh\\:mm\\:ss\\.fff}");
        }
        
        // Display execution summary for all planners
        private async Task DisplayExecutionSummary()
        {
            LoggingService.LogSubsection("ÃƒÂ°Ã…Â¸Ã¢â‚¬Å“Ã…Â  PLANNER EXECUTION SUMMARY");
            LoggingService.LogInfo("=".PadRight(80, '='));
            
            if (allPlanners.Count == 0)
            {
                LoggingService.LogWarning("ÃƒÂ¢Ã…Â¡Ã‚Â ÃƒÂ¯Ã‚Â¸Ã‚Â No planners were executed during this test.");
                return;
            }
            
            LoggingService.LogInfo($"ÃƒÂ°Ã…Â¸Ã¢â‚¬ÂÃ‚Â Total planners executed: {allPlanners.Count}");
            LoggingService.LogInfo("");
            
            // Sort planners by start time
            var sortedPlanners = allPlanners.OrderBy(p => p.StartTime).ToList();
            
            for (int i = 0; i < sortedPlanners.Count; i++)
            {
                var planner = sortedPlanners[i];
                LoggingService.LogInfo($"ÃƒÂ°Ã…Â¸Ã…Â½Ã‚Â¯ PLANNER {i + 1}: {planner.PlannerName}");
                LoggingService.LogInfo($"   ÃƒÂ°Ã…Â¸Ã…Â¡Ã¢â€šÂ¬ Started: {planner.StartTime:HH:mm:ss.fff}");
                
                if (planner.HasCompleted)
                {
                    LoggingService.LogInfo($"   ÃƒÂ¢Ã…â€œÃ¢â‚¬Â¦ Finished: {planner.EndTime:HH:mm:ss.fff}");
                    LoggingService.LogInfo($"   ÃƒÂ¢Ã‚ÂÃ‚Â±ÃƒÂ¯Ã‚Â¸Ã‚Â Planner Duration: {planner.PlannerExecutionDuration:hh\\:mm\\:ss\\.fff}");
                    LoggingService.LogInfo($"   ÃƒÂ¢Ã‚ÂÃ‚Â±ÃƒÂ¯Ã‚Â¸Ã‚Â Total Duration: {planner.TotalExecutionDuration:hh\\:mm\\:ss\\.fff}");
                    
                    if (planner.GeneratedNodeGraph != null)
                    {
                        LoggingService.LogInfo($"   ÃƒÂ°Ã…Â¸Ã¢â‚¬Å“Ã…Â  Actions Generated: {planner.GeneratedNodeGraph.GetAllActionNodes().Count}");
                    }
                }
                else if (planner.IsExecuting)
                {
                    LoggingService.LogInfo($"   ÃƒÂ°Ã…Â¸Ã¢â‚¬ÂÃ¢â‚¬Å¾ Still executing... (Started: {planner.StartTime:HH:mm:ss.fff})");
                }
                else
                {
                    LoggingService.LogError($"   ÃƒÂ¢Ã‚ÂÃ…â€™ Failed or incomplete");
                }
                LoggingService.LogInfo("");
            }
            
            // Summary statistics
            var completedPlanners = allPlanners.Where(p => p.HasCompleted).ToList();
            var failedPlanners = allPlanners.Where(p => !p.HasCompleted && !p.IsExecuting).ToList();
            var executingPlanners = allPlanners.Where(p => p.IsExecuting).ToList();
            
            LoggingService.LogInfo("ÃƒÂ°Ã…Â¸Ã¢â‚¬Å“Ã‹â€  EXECUTION STATISTICS:");
            LoggingService.LogInfo($"   ÃƒÂ¢Ã…â€œÃ¢â‚¬Â¦ Successfully completed: {completedPlanners.Count}");
            LoggingService.LogError($"   ÃƒÂ¢Ã‚ÂÃ…â€™ Failed: {failedPlanners.Count}");
            LoggingService.LogInfo($"   ÃƒÂ°Ã…Â¸Ã¢â‚¬ÂÃ¢â‚¬Å¾ Still executing: {executingPlanners.Count}");
            
            if (completedPlanners.Any())
            {
                var avgPlannerDuration = TimeSpan.FromMilliseconds(completedPlanners.Average(p => p.PlannerExecutionDuration.TotalMilliseconds));
                var avgTotalDuration = TimeSpan.FromMilliseconds(completedPlanners.Average(p => p.TotalExecutionDuration.TotalMilliseconds));
                var minPlannerDuration = completedPlanners.Min(p => p.PlannerExecutionDuration);
                var maxPlannerDuration = completedPlanners.Max(p => p.PlannerExecutionDuration);
                var minTotalDuration = completedPlanners.Min(p => p.TotalExecutionDuration);
                var maxTotalDuration = completedPlanners.Max(p => p.TotalExecutionDuration);
                
                LoggingService.LogInfo($"   ÃƒÂ¢Ã‚ÂÃ‚Â±ÃƒÂ¯Ã‚Â¸Ã‚Â Average Planner Duration: {avgPlannerDuration:hh\\:mm\\:ss\\.fff}");
                LoggingService.LogInfo($"   ÃƒÂ¢Ã‚ÂÃ‚Â±ÃƒÂ¯Ã‚Â¸Ã‚Â Average Total Duration: {avgTotalDuration:hh\\:mm\\:ss\\.fff}");
                LoggingService.LogInfo($"   ÃƒÂ¢Ã‚ÂÃ‚Â±ÃƒÂ¯Ã‚Â¸Ã‚Â Fastest Planner: {minPlannerDuration:hh\\:mm\\:ss\\.fff}");
                LoggingService.LogInfo($"   ÃƒÂ¢Ã‚ÂÃ‚Â±ÃƒÂ¯Ã‚Â¸Ã‚Â Slowest Planner: {maxPlannerDuration:hh\\:mm\\:ss\\.fff}");
                LoggingService.LogInfo($"   ÃƒÂ¢Ã‚ÂÃ‚Â±ÃƒÂ¯Ã‚Â¸Ã‚Â Fastest Total: {minTotalDuration:hh\\:mm\\:ss\\.fff}");
                LoggingService.LogInfo($"   ÃƒÂ¢Ã‚ÂÃ‚Â±ÃƒÂ¯Ã‚Â¸Ã‚Â Slowest Total: {maxTotalDuration:hh\\:mm\\:ss\\.fff}");
            }
            
            LoggingService.LogInfo("=".PadRight(80, '='));
            
            // Display blackboard tracking statistics
            DisplayBlackboardTrackingSummary();
        }
        
        // Display blackboard tracking summary
        private void DisplayBlackboardTrackingSummary()
        {
            LoggingService.LogSubsection("ÃƒÂ°Ã…Â¸Ã¢â‚¬Å“Ã¢â‚¬Â¹ BLACKBOARD TRACKING SUMMARY");
            LoggingService.LogInfo("=".PadRight(80, '='));
            
            try
            {
                // Get current blackboard tracking statistics
                var (types, instances, negations) = BlackboardTrackingLogger.GetCurrentCounts();
                
                LoggingService.LogInfo($"ÃƒÂ°Ã…Â¸Ã¢â‚¬Â Ã¢â‚¬Â¢ Total New Types Added: {types}");
                LoggingService.LogInfo($"ÃƒÂ°Ã…Â¸Ã¢â‚¬Â Ã¢â‚¬Â¢ Total New Instances Created: {instances}");
                LoggingService.LogInfo($"ÃƒÂ°Ã…Â¸Ã¢â‚¬ÂÃ¢â‚¬Å¾ Total Predicate Negations: {negations}");
                
                LoggingService.LogInfo($"ÃƒÂ°Ã…Â¸Ã¢â‚¬Å“Ã‚Â Blackboard tracking log saved to: {BlackboardTrackingLogger.GetLogFilePath()}");
                LoggingService.LogInfo("=".PadRight(80, '='));
            }
            catch (Exception ex)
            {
                LoggingService.LogWarning($"ÃƒÂ¢Ã…Â¡Ã‚Â ÃƒÂ¯Ã‚Â¸Ã‚Â Could not retrieve blackboard tracking statistics: {ex.Message}");
            }
        }
        
        // Track subtree status for high-level actions generated by flow nodes
        private async Task TrackSubtreeStatusForHLActions(BehaviorTreeInstance behaviorTree)
        {
            LoggingService.LogSubsection("ÃƒÂ°Ã…Â¸Ã…â€™Ã‚Â³ TRACKING SUBTREE STATUS FOR HL ACTIONS");
            LoggingService.LogInfo("=".PadRight(60, '='));
            
                         try
             {
                 var rootNode = behaviorTree.root as BTFlowNode_Composite;
                 if (rootNode == null)
                 {
                     LoggingService.LogError("ÃƒÂ¢Ã‚ÂÃ…â€™ Root node is not a BTFlowNode_Composite");
                     return;
                 }

                var children = rootNode.GetChildren();
                LoggingService.LogInfo($"ÃƒÂ°Ã…Â¸Ã¢â‚¬ÂÃ‚Â Tracking subtrees for {children.Count} flow nodes...\n");

                for (int i = 0; i < children.Count; i++)
                {
                    var child = children[i];
                    if (child is BTFlowNode_Dynamic dynamicNode)
                    {
                        LoggingService.LogInfo($"ÃƒÂ°Ã…Â¸Ã…Â½Ã‚Â¯ FLOW NODE {i + 1}: {dynamicNode.GetNodeName()}");
                        
                        // Check if planning service has generated a NodeGraph
                        if (dynamicNode.PlanningService is BTServicePlanner plannerService && plannerService.HasGeneratedNodeGraph())
                        {
                            var generatedGraph = plannerService.GetGeneratedNodeGraph();
                            var actions = generatedGraph.GetAllActionNodes();
                            
                            LoggingService.LogInfo($"   ÃƒÂ°Ã…Â¸Ã¢â‚¬Å“Ã…Â  Generated {actions.Count} actions from planner");
                            
                            // Track subtree status for each action
                            for (int j = 0; j < actions.Count; j++)
                            {
                                var action = actions[j];
                                if (action is PActionNode genericAction)
                                {
                                    LoggingService.LogInfo($"   ÃƒÂ°Ã…Â¸Ã¢â‚¬ÂÃ‚Â Action {j + 1}: {action.InstanceName.ToString()}");
                                    
                                    // Check if this is a high-level action
                                    if (genericAction.IsHighLevelAction)
                                    {
                                        LoggingService.LogInfo($"      ÃƒÂ¢Ã…â€œÃ¢â‚¬Â¦ Is High-Level Action: Yes");
                                        
                                        // Check if it has a subtree
                                        if (genericAction.HighLevelSubtree != null)
                                        {
                                            LoggingService.LogInfo($"      ÃƒÂ°Ã…Â¸Ã…â€™Ã‚Â³ Has Subtree: Yes");
                                            LoggingService.LogInfo($"      ÃƒÂ°Ã…Â¸Ã¢â‚¬Å“Ã¢â‚¬Â¹ Subtree Type: {genericAction.HighLevelSubtree.GetType().Name}");
                                            LoggingService.LogInfo($"      ÃƒÂ°Ã…Â¸Ã¢â‚¬Å“Ã…Â  Subtree Status: {genericAction.HighLevelSubtree.status}");
                                            
                                            // Check if subtree has actions
                                            var subtreeActionGraph = genericAction.HighLevelSubtree.GetActionGraph();
                                            var subtreeActions = subtreeActionGraph.GetAllActionNodes();
                                            LoggingService.LogInfo($"      ÃƒÂ°Ã…Â¸Ã¢â‚¬Å“Ã‹â€  Subtree Actions: {subtreeActions.Count}");
                                            
                                            // List subtree actions and their status
                                            for (int k = 0; k < subtreeActions.Count; k++)
                                            {
                                                var subtreeAction = subtreeActions[k];
                                                LoggingService.LogInfo($"         {k + 1}. {subtreeAction.InstanceName.ToString()} - Status: {subtreeAction.status}");
                                            }
                                        }
                                        else
                                        {
                                            LoggingService.LogInfo($"      ÃƒÂ¢Ã‚ÂÃ…â€™ Has Subtree: No");
                                        }
                                        
                                        // Check if it has a planning service
                                        if (genericAction.PlanningService != null)
                                        {
                                            LoggingService.LogInfo($"      ÃƒÂ°Ã…Â¸Ã¢â‚¬ÂÃ‚Â§ Has Planning Service: Yes ({genericAction.PlanningService.GetType().Name})");
                                        }
                                        else
                                        {
                                            LoggingService.LogInfo($"      ÃƒÂ¢Ã‚ÂÃ…â€™ Has Planning Service: No");
                                        }
                                    }
                                    else
                                    {
                                        LoggingService.LogInfo($"      ÃƒÂ¢Ã‚ÂÃ…â€™ Is High-Level Action: No");
                                    }
                                    
                                    // Check if it has a SubtreeInjectionService
                                    var subtreeService = genericAction.GetSubtreeInjectionService();
                                    if (subtreeService != null)
                                    {
                                        LoggingService.LogInfo($"      ÃƒÂ°Ã…Â¸Ã¢â‚¬ÂÃ‚Â§ Has SubtreeInjectionService: Yes");
                                        
                                        // Get statistics from the service
                                        var stats = subtreeService.GetStatistics();
                                        LoggingService.LogInfo($"      ÃƒÂ°Ã…Â¸Ã¢â‚¬Å“Ã…Â  Service Stats: {stats.cachedSubtrees} cached subtrees, {stats.configurations} configurations, {stats.plannerMappings} planner mappings");
                                        
                                        // Check if any problem files were generated
                                        var generatedFiles = subtreeService.GetGeneratedProblemFiles();
                                        if (generatedFiles.Count > 0)
                                        {
                                            LoggingService.LogInfo($"      ÃƒÂ°Ã…Â¸Ã¢â‚¬Å“Ã¢â‚¬Å¾ Generated Problem Files: {generatedFiles.Count}");
                                            foreach (var file in generatedFiles)
                                            {
                                                LoggingService.LogInfo($"         - {file}");
                                            }
                                        }
                                    }
                                    else
                                    {
                                        LoggingService.LogInfo($"      ÃƒÂ¢Ã‚ÂÃ…â€™ Has SubtreeInjectionService: No");
                                    }
                                }
                                else
                                {
                                    LoggingService.LogInfo($"   ÃƒÂ°Ã…Â¸Ã¢â‚¬ÂÃ‚Â Action {j + 1}: {action.InstanceName.ToString()} (Not a GenericBTAction)");
                                }
                                LoggingService.LogInfo("");
                            }
                        }
                        else
                        {
                            LoggingService.LogInfo($"   ÃƒÂ¢Ã…Â¡Ã‚Â ÃƒÂ¯Ã‚Â¸Ã‚Â No NodeGraph generated yet by planner");
                        }
                        
                        LoggingService.LogInfo("");
                    }
                }
                
                LoggingService.LogSuccess("ÃƒÂ¢Ã…â€œÃ¢â‚¬Â¦ Subtree status tracking completed!");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"ÃƒÂ¢Ã‚ÂÃ…â€™ Error tracking subtree status: {ex.Message}");
                LoggingService.LogError($"   Stack trace: {ex.StackTrace}");
            }
        }

        // Continuous monitoring of subtree status for high-level actions
        private async Task MonitorSubtreeStatusContinuously(BehaviorTreeInstance behaviorTree)
        {
            LoggingService.LogInfo("\nÃƒÂ°Ã…Â¸Ã¢â‚¬ÂÃ¢â‚¬Å¾ CONTINUOUS SUBTREE STATUS MONITORING");
            LoggingService.LogInfo("=".PadRight(60, '='));
            LoggingService.LogInfo("Press any key to stop monitoring...");
            
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
                
                // Update status every 3 seconds
                if ((currentTime - lastStatusTime).TotalSeconds >= 3)
                {
                    Console.Clear();
                    LoggingService.LogInfo($"ÃƒÂ°Ã…Â¸Ã¢â‚¬ÂÃ¢â‚¬Å¾ SUBTREE STATUS MONITORING - {currentTime:HH:mm:ss}");
                    LoggingService.LogInfo("=".PadRight(60, '='));
                    
                    var rootNode = behaviorTree.root as BTFlowNode_Composite;
                    if (rootNode != null)
                    {
                        var children = rootNode.GetChildren();
                        
                        for (int i = 0; i < children.Count; i++)
                        {
                            var child = children[i];
                            if (child is BTFlowNode_Dynamic dynamicNode)
                            {
                                LoggingService.LogInfo($"ÃƒÂ°Ã…Â¸Ã…Â½Ã‚Â¯ FLOW NODE {i + 1}: {dynamicNode.GetNodeName()}");
                                LoggingService.LogInfo($"   ÃƒÂ°Ã…Â¸Ã¢â‚¬Å“Ã…Â  Flow Node Status: {dynamicNode.status}");
                                
                                // Check if planning service has generated a NodeGraph
                                if (dynamicNode.PlanningService is BTServicePlanner plannerService && plannerService.HasGeneratedNodeGraph())
                                {
                                    var generatedGraph = plannerService.GetGeneratedNodeGraph();
                                    var actions = generatedGraph.GetAllActionNodes();
                                    
                                    LoggingService.LogInfo($"   ÃƒÂ°Ã…Â¸Ã¢â‚¬Å“Ã‹â€  Generated Actions: {actions.Count}");
                                    
                                    // Count high-level actions with subtrees
                                    int hlActionsWithSubtrees = 0;
                                    int totalHLActions = 0;
                                    
                                    foreach (var action in actions)
                                    {
                                        if (action is PActionNode genericAction)
                                        {
                                            if (genericAction.IsHighLevelAction)
                                            {
                                                totalHLActions++;
                                                if (genericAction.HighLevelSubtree != null)
                                                {
                                                    hlActionsWithSubtrees++;
                                                }
                                            }
                                        }
                                    }
                                    
                                    LoggingService.LogInfo($"   ÃƒÂ°Ã…Â¸Ã…â€™Ã‚Â³ HL Actions with Subtrees: {hlActionsWithSubtrees}/{totalHLActions}");
                                    
                                    // Show status of each action
                                    for (int j = 0; j < Math.Min(actions.Count, 5); j++) // Show first 5 actions
                                    {
                                        var action = actions[j];
                                        if (action is PActionNode genericAction)
                                        {
                                            var status = genericAction.IsHighLevelAction ? "HL" : "ML";
                                            var subtreeStatus = genericAction.HighLevelSubtree != null ? "ÃƒÂ°Ã…Â¸Ã…â€™Ã‚Â³" : "ÃƒÂ¢Ã‚ÂÃ…â€™";
                                            LoggingService.LogInfo($"      {j + 1}. {action.InstanceName.ToString()} [{status}] {subtreeStatus} - {action.status}");
                                        }
                                    }
                                    
                                    if (actions.Count > 5)
                                    {
                                        LoggingService.LogInfo($"      ... and {actions.Count - 5} more actions");
                                    }
                                }
                                else
                                {
                                    LoggingService.LogInfo($"   ÃƒÂ¢Ã…Â¡Ã‚Â ÃƒÂ¯Ã‚Â¸Ã‚Â No NodeGraph generated yet");
                                }
                                
                                LoggingService.LogInfo("");
                            }
                        }
                    }
                    
                    LoggingService.LogInfo($"ÃƒÂ¢Ã‚ÂÃ‚Â±ÃƒÂ¯Ã‚Â¸Ã‚Â Monitoring duration: {currentTime - monitoringStartTime:hh\\:mm\\:ss}");
                    LoggingService.LogInfo("Press any key to stop monitoring...");
                    lastStatusTime = currentTime;
                }
                
                await Task.Delay(100); // Small delay to prevent high CPU usage
            }
            
            LoggingService.LogInfo($"ÃƒÂ¢Ã‚ÂÃ‚Â±ÃƒÂ¯Ã‚Â¸Ã‚Â Total monitoring duration: {DateTime.Now - monitoringStartTime:hh\\:mm\\:ss}");
        }

        // Track subtree status during tree execution (call this on each tick)
        private void TrackSubtreeStatusOnTick(BehaviorTreeInstance behaviorTree, int tickNumber)
        {
            try
            {
                var rootNode = behaviorTree.root as BTFlowNode_Composite;
                if (rootNode == null) return;

                var children = rootNode.GetChildren();
                bool hasChanges = false;
                
                for (int i = 0; i < children.Count; i++)
                {
                    var child = children[i];
                    if (child is BTFlowNode_Dynamic dynamicNode)
                    {
                        // Check if planning service has generated a NodeGraph
                        if (dynamicNode.PlanningService is BTServicePlanner plannerService && plannerService.HasGeneratedNodeGraph())
                        {
                            var generatedGraph = plannerService.GetGeneratedNodeGraph();
                            var actions = generatedGraph.GetAllActionNodes();
                            
                            // Check for high-level actions with subtrees
                            foreach (var action in actions)
                            {
                                if (action is PActionNode genericAction && genericAction.IsHighLevelAction)
                                {
                                    if (genericAction.HighLevelSubtree != null)
                                    {
                                        var subtreeActionGraph = genericAction.HighLevelSubtree.GetActionGraph();
                                        var subtreeActions = subtreeActionGraph.GetAllActionNodes();
                                        
                                        // Check if any subtree actions have changed status
                                        foreach (var subtreeAction in subtreeActions)
                                        {
                                            if (subtreeAction.status == BTNodeResult.InProgress || 
                                                subtreeAction.status == BTNodeResult.Success || 
                                                subtreeAction.status == BTNodeResult.Failure)
                                            {
                                                if (!hasChanges)
                                                {
                                                    LoggingService.LogInfo($"\nÃƒÂ°Ã…Â¸Ã¢â‚¬ÂÃ¢â‚¬Å¾ TICK {tickNumber} - SUBTREE STATUS UPDATE:");
                                                    hasChanges = true;
                                                }
                                                
                                                LoggingService.LogInfo($"   ÃƒÂ°Ã…Â¸Ã…â€™Ã‚Â³ {genericAction.InstanceName.ToString()} -> {subtreeAction.InstanceName.ToString()}: {subtreeAction.status}");
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
                LoggingService.LogError($"ÃƒÂ¢Ã‚ÂÃ…â€™ Error tracking subtree status on tick {tickNumber}: {ex.Message}");
            }
        }

        // Execute tree with comprehensive logging
        private async Task ExecuteTreeWithComprehensiveLogging(BehaviorTreeInstance behaviorTree)
        {
            LoggingService.LogSection("ÃƒÂ°Ã…Â¸Ã…Â¡Ã¢â€šÂ¬ EXECUTING TREE WITH COMPREHENSIVE LOGGING");
            
            try
            {
                int maxTicks = 1300; // Maximum number of ticks to prevent infinite loops
                int tickCount = 0;
                
                // Dictionary to track action status changes
                var actionStatusHistory = new Dictionary<string, BTNodeResult>();
                
                LoggingService.LogInfo($"ÃƒÂ°Ã…Â¸Ã¢â‚¬ÂÃ¢â‚¬Å¾ Starting tree execution (max {maxTicks} ticks)...");
                LoggingService.LogInfo("Press any key to stop execution...");
                
                while (tickCount < maxTicks)
                {
                    // Check if any key is pressed (non-blocking)
                    if (Console.KeyAvailable)
                    {
                        Console.ReadKey(true); // Clear the key
                        LoggingService.LogWarning("ÃƒÂ¢Ã‚ÂÃ‚Â¹ÃƒÂ¯Ã‚Â¸Ã‚Â Execution stopped by user");
                        break;
                    }
                    
                    tickCount++;
                    
                    // Log tick start
                    LoggingService.LogInfo($"\nÃƒÂ°Ã…Â¸Ã¢â‚¬ÂÃ¢â‚¬Å¾ TICK {tickCount} STARTING...");
                    
                    // Execute one tick
                    ExecutionSummaryLogger.StartTreeExecution();
                    BlackboardSummaryLogger.StartTreeTicking();
                    var result = behaviorTree.Tick(0.1f); // 0.1 second delta time
                    BlackboardSummaryLogger.EndTreeTicking();
                    ExecutionSummaryLogger.EndTreeExecution();
                    
                    // Log comprehensive tick information
                    LogComprehensiveTickInfo(behaviorTree, tickCount, actionStatusHistory);
                    
                    // Check if tree has finished
                    if (behaviorTree.HasFinished())
                    {
                        LoggingService.LogSuccess($"\nÃƒÂ¢Ã…â€œÃ¢â‚¬Â¦ Tree execution completed after {tickCount} ticks");
                        LoggingService.LogSuccess($"ÃƒÂ°Ã…Â¸Ã¢â‚¬Å“Ã…Â  Final result: {result}");
                        break;
                    }
                    
                    // Small delay between ticks
                    await Task.Delay(100);
                }
                
                if (tickCount >= maxTicks)
                {
                    LoggingService.LogWarning($"\nÃƒÂ¢Ã…Â¡Ã‚Â ÃƒÂ¯Ã‚Â¸Ã‚Â Tree execution stopped after {maxTicks} ticks (max reached)");
                }
                
                // Print final status summary
                LogFinalActionStatusSummary(actionStatusHistory);
                
                LoggingService.LogSuccess("ÃƒÂ¢Ã…â€œÃ¢â‚¬Â¦ Tree execution with comprehensive logging completed!");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"ÃƒÂ¢Ã‚ÂÃ…â€™ Error during tree execution: {ex.Message}");
            }
        }

        // Log comprehensive tick information including NodeGraph details and order relations
        private void LogComprehensiveTickInfo(BehaviorTreeInstance behaviorTree, int tickNumber, Dictionary<string, BTNodeResult> actionStatusHistory)
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
                LoggingService.LogError($"ÃƒÂ¢Ã‚ÂÃ…â€™ Error logging comprehensive tick info on tick {tickNumber}: {ex.Message}");
            }
        }

        // Log NodeGraph details including order relations
        private void LogNodeGraphDetails(BehaviorTreeInstance behaviorTree, int tickNumber)
        {
            try
            {
                var rootNode = behaviorTree.root as BTFlowNode_Composite;
                if (rootNode == null) return;

                var children = rootNode.GetChildren();
                
                foreach (var child in children)
                {
                    if (child is BTFlowNode_Dynamic dynamicNode)
                    {
                        var actionGraph = dynamicNode.GetActionGraph();
                        var nodes = actionGraph.GetAllActionNodes();
                        
                        if (nodes.Count > 0)
                        {
                            LoggingService.LogInfo($"\nÃƒÂ°Ã…Â¸Ã…â€™Ã‚Â³ NODEGRAPH DETAILS ({dynamicNode.GetNodeName()}) - TICK {tickNumber}:");
                            LoggingService.LogInfo($"   ÃƒÂ°Ã…Â¸Ã¢â‚¬Å“Ã…Â  Total nodes: {nodes.Count}");
                            
                            // Log each node's details
                            foreach (var action in nodes)
                            {
                                var nodeInfo = actionGraph.GetNodeInfo(action);
                                if (nodeInfo != null)
                                {
                                    var statusEmoji = action.status switch
                                    {
                                        BTNodeResult.Success => "ÃƒÂ¢Ã…â€œÃ¢â‚¬Â¦",
                                        BTNodeResult.Failure => "ÃƒÂ¢Ã‚ÂÃ…â€™",
                                        BTNodeResult.InProgress => "ÃƒÂ°Ã…Â¸Ã¢â‚¬ÂÃ¢â‚¬Å¾",
                                        BTNodeResult.ReadyToTick => "ÃƒÂ¢Ã‚ÂÃ‚Â³",
                                        _ => "ÃƒÂ¢Ã‚ÂÃ¢â‚¬Å“"
                                    };
                                    
                                    LoggingService.LogInfo($"   {statusEmoji} {action.InstanceName}: Status={action.status}, Completed={nodeInfo.IsCompleted}, Predecessors={nodeInfo.Predecessors.Count}");
                                    
                                    // Log order relations for this node
                                    if (nodeInfo.Predecessors.Count > 0)
                                    {
                                        LoggingService.LogInfo($"      ÃƒÂ°Ã…Â¸Ã¢â‚¬Å“Ã¢â‚¬Â¹ Predecessors:");
                                        foreach (var pred in nodeInfo.Predecessors)
                                        {
                                            LoggingService.LogInfo($"         - {pred.From.ActionNode.InstanceName} (MEETS)");
                                        }
                                    }
                                    
                                    if (nodeInfo.Successors.Count > 0)
                                    {
                                        LoggingService.LogInfo($"      ÃƒÂ°Ã…Â¸Ã¢â‚¬Å“Ã¢â‚¬Â¹ Successors:");
                                        foreach (var succ in nodeInfo.Successors)
                                        {
                                            LoggingService.LogInfo($"         - {succ.To.ActionNode.InstanceName} (MEETS)");
                                        }
                                    }
                                }
                            }
                            
                            // Log all order relations in the graph
                            LoggingService.LogInfo($"   ÃƒÂ°Ã…Â¸Ã¢â‚¬ÂÃ¢â‚¬â€ ALL ORDER RELATIONS:");
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
                LoggingService.LogError($"ÃƒÂ¢Ã‚ÂÃ…â€™ Error logging NodeGraph details: {ex.Message}");
            }
        }

        // Log action status changes
        private void LogActionStatusChanges(BehaviorTreeInstance behaviorTree, int tickNumber, Dictionary<string, BTNodeResult> actionStatusHistory)
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
                            LoggingService.LogInfo($"\nÃƒÂ°Ã…Â¸Ã¢â‚¬ÂÃ¢â‚¬Å¾ TICK {tickNumber} - ACTION STATUS CHANGES:");
                            hasStatusChanges = true;
                        }

                        var statusEmoji = currentStatus switch
                        {
                            BTNodeResult.Success => "ÃƒÂ¢Ã…â€œÃ¢â‚¬Â¦",
                            BTNodeResult.Failure => "ÃƒÂ¢Ã‚ÂÃ…â€™",
                            BTNodeResult.InProgress => "ÃƒÂ°Ã…Â¸Ã¢â‚¬ÂÃ¢â‚¬Å¾",
                            BTNodeResult.ReadyToTick => "ÃƒÂ¢Ã‚ÂÃ‚Â³",
                            _ => "ÃƒÂ¢Ã‚ÂÃ¢â‚¬Å“"
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
                    
                    LoggingService.LogInfo($"   ÃƒÂ°Ã…Â¸Ã¢â‚¬Å“Ã…Â  Tick {tickNumber}: {activeActions} active, {completedActions} completed, {failedActions} failed");
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"ÃƒÂ¢Ã‚ÂÃ…â€™ Error logging action status changes: {ex.Message}");
            }
        }

        // Log subtree status for high-level actions
        private void LogSubtreeStatusForHLActions(BehaviorTreeInstance behaviorTree, int tickNumber)
        {
            try
            {
                var rootNode = behaviorTree.root as BTFlowNode_Composite;
                if (rootNode == null) return;

                var children = rootNode.GetChildren();
                bool hasSubtreeChanges = false;
                
                foreach (var child in children)
                {
                    if (child is BTFlowNode_Dynamic dynamicNode)
                    {
                        if (dynamicNode.PlanningService is BTServicePlanner plannerService && plannerService.HasGeneratedNodeGraph())
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
                                            LoggingService.LogInfo($"\nÃƒÂ°Ã…Â¸Ã…â€™Ã‚Â³ SUBTREE NODEGRAPH DETAILS ({genericAction.InstanceName}) - TICK {tickNumber}:");
                                            LoggingService.LogInfo($"   ÃƒÂ°Ã…Â¸Ã¢â‚¬Å“Ã…Â  Total subtree nodes: {subtreeActions.Count}");
                                            
                                            // Log each subtree node's details
                                            foreach (var subtreeAction in subtreeActions)
                                            {
                                                var subtreeNodeInfo = subtreeActionGraph.GetNodeInfo(subtreeAction);
                                                if (subtreeNodeInfo != null)
                                                {
                                                    var statusEmoji = subtreeAction.status switch
                                                    {
                                                        BTNodeResult.Success => "ÃƒÂ¢Ã…â€œÃ¢â‚¬Â¦",
                                                        BTNodeResult.Failure => "ÃƒÂ¢Ã‚ÂÃ…â€™",
                                                        BTNodeResult.InProgress => "ÃƒÂ°Ã…Â¸Ã¢â‚¬ÂÃ¢â‚¬Å¾",
                                                        BTNodeResult.ReadyToTick => "ÃƒÂ¢Ã‚ÂÃ‚Â³",
                                                        _ => "ÃƒÂ¢Ã‚ÂÃ¢â‚¬Å“"
                                                    };
                                                    
                                                    LoggingService.LogInfo($"   {statusEmoji} {subtreeAction.InstanceName}: Status={subtreeAction.status}, Completed={subtreeNodeInfo.IsCompleted}, Predecessors={subtreeNodeInfo.Predecessors.Count}");
                                                    
                                                    // Log order relations for this subtree node
                                                    if (subtreeNodeInfo.Predecessors.Count > 0)
                                                    {
                                                        LoggingService.LogInfo($"      ÃƒÂ°Ã…Â¸Ã¢â‚¬Å“Ã¢â‚¬Â¹ Subtree Predecessors:");
                                                        foreach (var pred in subtreeNodeInfo.Predecessors)
                                                        {
                                                            LoggingService.LogInfo($"         - {pred.From.ActionNode.InstanceName} (MEETS)");
                                                        }
                                                    }
                                                    
                                                    if (subtreeNodeInfo.Successors.Count > 0)
                                                    {
                                                        LoggingService.LogInfo($"      ÃƒÂ°Ã…Â¸Ã¢â‚¬Å“Ã¢â‚¬Â¹ Subtree Successors:");
                                                        foreach (var succ in subtreeNodeInfo.Successors)
                                                        {
                                                            LoggingService.LogInfo($"         - {succ.To.ActionNode.InstanceName} (MEETS)");
                                                        }
                                                    }
                                                }
                                            }
                                            
                                            // Log all order relations in the subtree graph
                                            LoggingService.LogInfo($"   ÃƒÂ°Ã…Â¸Ã¢â‚¬ÂÃ¢â‚¬â€ SUBTREE ORDER RELATIONS:");
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
                                                    LoggingService.LogInfo($"\nÃƒÂ°Ã…Â¸Ã…â€™Ã‚Â³ TICK {tickNumber} - SUBTREE STATUS UPDATE:");
                                                    hasSubtreeChanges = true;
                                                }
                                                
                                                var statusEmoji = subtreeAction.status switch
                                                {
                                                    BTNodeResult.Success => "ÃƒÂ¢Ã…â€œÃ¢â‚¬Â¦",
                                                    BTNodeResult.Failure => "ÃƒÂ¢Ã‚ÂÃ…â€™",
                                                    BTNodeResult.InProgress => "ÃƒÂ°Ã…Â¸Ã¢â‚¬ÂÃ¢â‚¬Å¾",
                                                    BTNodeResult.ReadyToTick => "ÃƒÂ¢Ã‚ÂÃ‚Â³",
                                                    _ => "ÃƒÂ¢Ã‚ÂÃ¢â‚¬Å“"
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
                LoggingService.LogError($"ÃƒÂ¢Ã‚ÂÃ…â€™ Error logging subtree status: {ex.Message}");
            }
        }

        // Check NodeGraph execution status to debug why actions aren't progressing
        private void CheckNodeGraphExecutionStatus(BehaviorTreeInstance behaviorTree, int tickNumber)
        {
            try
            {
                var rootNode = behaviorTree.root as BTFlowNode_Composite;
                if (rootNode == null) return;

                var children = rootNode.GetChildren();
                foreach (var child in children)
                {
                    if (child is BTFlowNode_Dynamic dynamicNode)
                    {
                        var actionGraph = dynamicNode.GetActionGraph();
                        var nodes = actionGraph.GetAllActionNodes();
                        
                        if (nodes.Count > 0)
                        {
                            LoggingService.LogInfo($"\nÃƒÂ°Ã…Â¸Ã¢â‚¬ÂÃ‚Â TICK {tickNumber} - NODEGRAPH DEBUG ({dynamicNode.GetNodeName()}):");
                            LoggingService.LogInfo($"   ÃƒÂ°Ã…Â¸Ã¢â‚¬Å“Ã…Â  Total nodes: {nodes.Count}");
                            
                            // Check each node's completion status and predecessors
                            foreach (var action in nodes)
                            {
                                var nodeInfo = actionGraph.GetNodeInfo(action);
                                if (nodeInfo != null)
                                {
                                    var statusEmoji = action.status switch
                                    {
                                        BTNodeResult.Success => "ÃƒÂ¢Ã…â€œÃ¢â‚¬Â¦",
                                        BTNodeResult.Failure => "ÃƒÂ¢Ã‚ÂÃ…â€™",
                                        BTNodeResult.InProgress => "ÃƒÂ°Ã…Â¸Ã¢â‚¬ÂÃ¢â‚¬Å¾",
                                        BTNodeResult.ReadyToTick => "ÃƒÂ¢Ã‚ÂÃ‚Â³",
                                        _ => "ÃƒÂ¢Ã‚ÂÃ¢â‚¬Å“"
                                    };
                                    
                                    LoggingService.LogInfo($"   {statusEmoji} {action.InstanceName}: Status={action.status}, Completed={nodeInfo.IsCompleted}, Predecessors={nodeInfo.Predecessors.Count}");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"ÃƒÂ¢Ã‚ÂÃ…â€™ Error checking NodeGraph status: {ex.Message}");
            }
        }

        // Log detailed subtree NodeGraph information on every tick
        private void LogDetailedSubtreeNodeGraphs(BehaviorTreeInstance behaviorTree, int tickNumber)
        {
            try
            {
                var rootNode = behaviorTree.root as BTFlowNode_Composite;
                if (rootNode == null) return;

                var children = rootNode.GetChildren();
                
                foreach (var child in children)
                {
                    if (child is BTFlowNode_Dynamic dynamicNode)
                    {
                        if (dynamicNode.PlanningService is BTServicePlanner plannerService && plannerService.HasGeneratedNodeGraph())
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
                                            LoggingService.LogInfo($"\nÃƒÂ°Ã…Â¸Ã…â€™Ã‚Â³ SUBTREE EXECUTION DETAILS ({genericAction.InstanceName}) - TICK {tickNumber}:");
                                            
                                            // Count statuses
                                            var succeededCount = subtreeActions.Count(a => a.status == BTNodeResult.Success);
                                            var failedCount = subtreeActions.Count(a => a.status == BTNodeResult.Failure);
                                            var inProgressCount = subtreeActions.Count(a => a.status == BTNodeResult.InProgress);
                                            var readyCount = subtreeActions.Count(a => a.status == BTNodeResult.ReadyToTick);
                                            
                                            LoggingService.LogInfo($"   ÃƒÂ°Ã…Â¸Ã¢â‚¬Å“Ã…Â  Subtree Progress: {succeededCount}ÃƒÂ¢Ã…â€œÃ¢â‚¬Â¦ {inProgressCount}ÃƒÂ°Ã…Â¸Ã¢â‚¬ÂÃ¢â‚¬Å¾ {failedCount}ÃƒÂ¢Ã‚ÂÃ…â€™ {readyCount}ÃƒÂ¢Ã‚ÂÃ‚Â³");
                                            
                                            // Log each subtree action with its current status
                                            foreach (var subtreeAction in subtreeActions)
                                            {
                                                var statusEmoji = subtreeAction.status switch
                                                {
                                                    BTNodeResult.Success => "ÃƒÂ¢Ã…â€œÃ¢â‚¬Â¦",
                                                    BTNodeResult.Failure => "ÃƒÂ¢Ã‚ÂÃ…â€™",
                                                    BTNodeResult.InProgress => "ÃƒÂ°Ã…Â¸Ã¢â‚¬ÂÃ¢â‚¬Å¾",
                                                    BTNodeResult.ReadyToTick => "ÃƒÂ¢Ã‚ÂÃ‚Â³",
                                                    _ => "ÃƒÂ¢Ã‚ÂÃ¢â‚¬Å“"
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
                LoggingService.LogError($"ÃƒÂ¢Ã‚ÂÃ…â€™ Error logging detailed subtree NodeGraphs: {ex.Message}");
            }
        }

        // Log final action status summary
        private void LogFinalActionStatusSummary(Dictionary<string, BTNodeResult> actionStatusHistory)
        {
            LoggingService.LogSubsection("ÃƒÂ°Ã…Â¸Ã¢â‚¬Å“Ã…Â  FINAL ACTION STATUS SUMMARY");
            
            var succeededActions = actionStatusHistory.Where(kvp => kvp.Value == BTNodeResult.Success).ToList();
            var failedActions = actionStatusHistory.Where(kvp => kvp.Value == BTNodeResult.Failure).ToList();
            var inProgressActions = actionStatusHistory.Where(kvp => kvp.Value == BTNodeResult.InProgress).ToList();
            var readyActions = actionStatusHistory.Where(kvp => kvp.Value == BTNodeResult.ReadyToTick).ToList();

            LoggingService.LogSuccess($"ÃƒÂ¢Ã…â€œÃ¢â‚¬Â¦ SUCCEEDED ({succeededActions.Count}):");
            foreach (var action in succeededActions)
            {
                LoggingService.LogSuccess($"   - {action.Key}");
            }

            LoggingService.LogError($"\nÃƒÂ¢Ã‚ÂÃ…â€™ FAILED ({failedActions.Count}):");
            foreach (var action in failedActions)
            {
                LoggingService.LogError($"   - {action.Key}");
            }

            LoggingService.LogInfo($"\nÃƒÂ°Ã…Â¸Ã¢â‚¬ÂÃ¢â‚¬Å¾ IN PROGRESS ({inProgressActions.Count}):");
            foreach (var action in inProgressActions)
            {
                LoggingService.LogInfo($"   - {action.Key}");
            }

            LoggingService.LogInfo($"\nÃƒÂ¢Ã‚ÂÃ‚Â³ READY TO TICK ({readyActions.Count}):");
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
            
            if (node is BTFlowNode_Composite compositeNode)
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
