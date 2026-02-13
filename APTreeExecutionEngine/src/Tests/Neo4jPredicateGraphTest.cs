using System;
using System.Threading.Tasks;
using System.IO;
using BehaviorTreeMainProject.Log.Services;

namespace BehaviorTreeMainProject.Tests
{
    /// <summary>
    /// Minimal integration test that mirrors sample predicates into Neo4j
    /// without mutating the blackboard state.
    /// </summary>
    public static class Neo4jPredicateGraphTest
    {
        public static async Task RunAsync()
        {
            LoggingService.Initialize("Neo4jPredicateGraphTest", enableConsole: true, enableFile: true);

            try
            {
                using var blackboard = new Blackboard<FastName>("bolt://localhost:7687", "neo4j", "12345678");

                // 1) Register all parameter, predicate, and action types
                var blackboardWriter = new BlackboardWriter(blackboard);
                LoggingService.LogSection("REGISTERING ALL TYPES");
                blackboardWriter.RegisterAllTypes();

                // 2) Read and register all instances (parameters, predicates, actions) from input files
                LoggingService.LogSection("REGISTERING ALL INSTANCES FROM FILES");
                string actionInstancesFile = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "..", "..", "..",
                    "src", "InputInstances", "ActionInstances.txt");
                string predicateInstancesFile = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "..", "..", "..",
                "src", "InputInstances", "initialstatespredicates.txt");
                string parameterInstancesFile = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "..", "..", "..",
                "src", "InputInstances", "ParameterInstances.txt");

                blackboardWriter.RegisterAllInstances(parameterInstancesFile, predicateInstancesFile, actionInstancesFile);

                // 3) Mirror all predicates currently on the blackboard into Neo4j
                LoggingService.LogSection("MIRRORING PREDICATES TO NEO4J");
                var allPredicates = blackboard.GetAllPredicates();
                LoggingService.LogInfo($"Found {allPredicates.Count} predicates on blackboard");

                foreach (var predicate in allPredicates)
                {
                    await blackboard.SetPredicateOnGraph(predicate.PredicateName, predicate);
                    LoggingService.LogSuccess($"Mirrored predicate to Neo4j: {predicate.PredicateName} ({predicate.GetPredicateType()})");
                }

                // // 4) Read and write goal predicates to "goalstates" database
                // LoggingService.LogSection("MIRRORING GOAL PREDICATES TO GOALSTATES DATABASE");
                // string goalPredicatesFile = Path.Combine(
                //     AppDomain.CurrentDomain.BaseDirectory,
                //     "..", "..", "..",
                //     "src", "InputInstances", "goalpredicates.txt");

                // if (File.Exists(goalPredicatesFile))
                // {
                //     var goalPredicates = blackboardWriter.ParseMontiCorePredicateFile(goalPredicatesFile, blackboard);
                //     LoggingService.LogInfo($"Found {goalPredicates.Count} goal predicates to write");

                //     foreach (var goalPredicate in goalPredicates)
                //     {
                //         await blackboard.SetPredicateOnGraphToDatabase(goalPredicate.PredicateName, goalPredicate, "goalstates");
                //         LoggingService.LogSuccess($"Mirrored goal predicate to goalstates database: {goalPredicate.PredicateName} ({goalPredicate.GetPredicateType()})");
                //     }
                // }
                // else
                // {
                //     LoggingService.LogWarning($"Goal predicates file not found at: {goalPredicatesFile}");
                // }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Neo4j predicate test failed: {ex.Message}");
                LoggingService.LogError(ex.StackTrace ?? string.Empty);
                throw;
            }
            finally
            {
                LoggingService.Close();
            }
        }
    }
}

