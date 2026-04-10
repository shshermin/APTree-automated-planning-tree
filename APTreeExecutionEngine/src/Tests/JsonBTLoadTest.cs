using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BehaviorTreeMainProject.Services;
using BehaviorTreeMainProject.Services.AIPlanning;
using AIPlanning;
using ModelLoader.ParameterTypes;
using BehaviorTreeMainProject.Log.Services;

namespace BehaviorTreeMainProject
{
    public class JsonBTLoadTest
    {
        public async Task RunJsonBTLoadTest()
        {
            Console.WriteLine("========================================");
            Console.WriteLine("  JSON BEHAVIOR TREE LOAD TEST");
            Console.WriteLine("========================================\n");

            // 1. Load the JSON file
            string jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "src", "ModelLoader", "BehaviorTreeModel.json");
            Console.WriteLine($"[1] Loading JSON from: {Path.GetFullPath(jsonPath)}");

            if (!File.Exists(jsonPath))
            {
                Console.WriteLine($"  ERROR: File not found at {Path.GetFullPath(jsonPath)}");
                return;
            }

            string jsonText = File.ReadAllText(jsonPath);

            // The JSON has invalid plannerRef values (Java object references), so we need to
            // strip them before parsing. Replace unquoted Java refs with quoted strings.
            jsonText = System.Text.RegularExpressions.Regex.Replace(
                jsonText,
                @"""plannerRef"":\s*([a-zA-Z_][\w.$@]*)",
                @"""plannerRef"": ""$1""");

            JsonDocument doc;
            try
            {
                doc = JsonDocument.Parse(jsonText);
                Console.WriteLine("  OK - JSON parsed successfully.");
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"  ERROR parsing JSON: {ex.Message}");
                return;
            }

            var root = doc.RootElement;
            var treesArray = root.GetProperty("behaviorTrees");
            int treeCount = root.GetProperty("treeCount").GetInt32();
            string generatedAt = root.GetProperty("generatedAt").GetString();
            string sourceFile = root.GetProperty("sourceFile").GetString();

            Console.WriteLine($"  Tree count:   {treeCount}");
            Console.WriteLine($"  Generated at: {generatedAt}");
            Console.WriteLine($"  Source file:   {sourceFile}\n");

            // 2. Read the first (and only) tree
            var treeJson = treesArray[0];
            string treeName = treeJson.GetProperty("name").GetString();
            var rootNodeJson = treeJson.GetProperty("root");

            Console.WriteLine($"[2] Tree name: {treeName}");
            Console.WriteLine($"  Root node: {rootNodeJson.GetProperty("name").GetString()} " +
                              $"(type={rootNodeJson.GetProperty("type").GetString()}, " +
                              $"successCriteria={rootNodeJson.GetProperty("successCriteria").GetString()}, " +
                              $"childType={rootNodeJson.GetProperty("childType").GetString()})");

            // 3. Read child nodes from the root's nodeGraph
            var nodeGraph = rootNodeJson.GetProperty("nodeGraph");
            int nodeCount = nodeGraph.GetProperty("nodeCount").GetInt32();
            int relationCount = nodeGraph.GetProperty("relationCount").GetInt32();
            var nodesArray = nodeGraph.GetProperty("nodes");
            var relationsArray = nodeGraph.GetProperty("relations");

            Console.WriteLine($"\n[3] NodeGraph: {nodeCount} nodes, {relationCount} relations");
            Console.WriteLine("  ----------------------------------------");

            // Print each child node info
            var nodeNames = new List<string>();
            foreach (var node in nodesArray.EnumerateArray())
            {
                string name = node.GetProperty("name").GetString();
                string type = node.GetProperty("type").GetString();
                string criteria = node.GetProperty("successCriteria").GetString();
                string childType = node.GetProperty("childType").GetString();
                nodeNames.Add(name);

                string serviceName = "";
                string serviceType = "";
                if (node.TryGetProperty("services", out var services))
                {
                    foreach (var svc in services.EnumerateArray())
                    {
                        serviceName = svc.GetProperty("name").GetString();
                        serviceType = svc.GetProperty("type").GetString();
                    }
                }

                Console.WriteLine($"  Node: {name,-16} type={type,-20} criteria={criteria,-5} childType={childType,-12} service={serviceName} ({serviceType})");
            }

            // Print relations
            Console.WriteLine("\n  Relations:");
            foreach (var rel in relationsArray.EnumerateArray())
            {
                string from = rel.GetProperty("from").GetString();
                string to = rel.GetProperty("to").GetString();
                string temporalType = rel.GetProperty("temporalType").GetString();
                Console.WriteLine($"    {from} --[{temporalType}]--> {to}");
            }

            // 4. Build the Behavior Tree from JSON
            Console.WriteLine("\n========================================");
            Console.WriteLine("  BUILDING BEHAVIOR TREE FROM JSON");
            Console.WriteLine("========================================\n");

            // Create blackboard
            using var blackboard = new Blackboard<FastName>();
            var blackboardWriter = new BlackboardWriter(blackboard);
            blackboardWriter.RegisterAllTypes();

            Console.WriteLine("[4] Blackboard created and types registered.");

            // Register instances if file exists
            string actionInstancesFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "src", "InputInstances", "ActionInstances.txt");
            if (File.Exists(actionInstancesFile))
            {
                blackboardWriter.RegisterAllInstances(actionInstancesFile);
                Console.WriteLine("  Action instances registered from file.");
            }
            else
            {
                Console.WriteLine($"  WARNING: ActionInstances.txt not found, skipping instance registration.");
            }

            // Create BehaviorTree
            string rootName = rootNodeJson.GetProperty("name").GetString();
            var behaviorTree = new BehaviorTree();
            behaviorTree.Initialise(blackboard, rootName);
            Console.WriteLine($"\n[5] BehaviorTree created with root: {rootName}");

            // Get the root composite that Initialise() created
            var rootComposite = behaviorTree.root as BTFlowNodeComposite;

            // Ensure planning phase is on
            blackboard.PlanningPhase = true;
            blackboard.CassetteSubtreeCompleted = new bool[nodeCount];
            Console.WriteLine($"  Planning phase enabled. Subtree flags: {nodeCount}");

            // 5. Create DynamicFlowNodes from JSON
            Console.WriteLine($"\n[6] Creating {nodeCount} DynamicFlowNodes from JSON...");
            var flowNodeMap = new Dictionary<string, DynamicFlowNode>();
            int plannerIndex = 0;

            foreach (var nodeJson in nodesArray.EnumerateArray())
            {
                plannerIndex++;
                string nodeName = nodeJson.GetProperty("name").GetString();

                var flowNode = new DynamicFlowNode(
                    new FastName(nodeName),
                    behaviorTree,
                    SuccessCriteria.ALL,
                    1.0f,
                    false);

                flowNodeMap[nodeName] = flowNode;

                // Create PDDL planner for this node
                // Domain is always DomainHL.pddl, problem file derived from node name
                string problemFile = $"./Plannerinputs/static/Problem{nodeName}.pddl";
                var pddlRequest = new PDDLPlanningRequest(
                    "./Plannerinputs/static/DomainHL.pddl",
                    problemFile,
                    "/home/ubuntu/ENHSP-Public/enhsp.jar",
                    "Enhsp");
                pddlRequest.EnhspConfig = "sat-hmrph";

                var pddlPlanner = new ServicePDDLPlanning(behaviorTree, pddlRequest);
                pddlPlanner.ExecutionMode = ServicePDDLPlanning.ParallelExecutionMode.Sequential;

                flowNode.SetPlanningService(pddlPlanner);

                // Add to root
                rootComposite.AddChild(flowNode);

                Console.WriteLine($"  [{plannerIndex,2}] {nodeName,-16}  planner=ENHSP  config=sat-hmrph  problem={problemFile}");
            }

            // Add planning phase service
            rootComposite.AddPlanningPhaseService();

            // Set owning tree properly
            behaviorTree.root = rootComposite;
            rootComposite.SetOwiningTree(behaviorTree);
            rootComposite.SetTreeForAllServices(behaviorTree);

            // 6. Print relations
            Console.WriteLine($"\n[7] Temporal relations ({relationCount}):");
            foreach (var rel in relationsArray.EnumerateArray())
            {
                string from = rel.GetProperty("from").GetString();
                string to = rel.GetProperty("to").GetString();
                string temporalType = rel.GetProperty("temporalType").GetString();
                Console.WriteLine($"    {from} --[{temporalType}]--> {to}");
            }

            // 7. Print the final tree structure
            Console.WriteLine("\n========================================");
            Console.WriteLine("  FINAL TREE STRUCTURE");
            Console.WriteLine("========================================\n");

            Console.WriteLine($"BehaviorTree: {behaviorTree.DebugDisplayName}");
            Console.WriteLine($"  Root: {rootComposite.GetNodeName()} (BTFlowNodeComposite)");

            var children = rootComposite.GetChildren();
            Console.WriteLine($"  Children: {children.Count}");

            for (int i = 0; i < children.Count; i++)
            {
                var child = children[i];
                string prefix = i == children.Count - 1 ? "  └──" : "  ├──";
                if (child is DynamicFlowNode dn)
                {
                    string plannerInfo = dn.ServicePlanning != null
                        ? dn.ServicePlanning.GetType().Name
                        : "NONE";
                    Console.WriteLine($"{prefix} [{i + 1}] {dn.GetNodeName()} (DynamicFlowNode) - Planner: {plannerInfo}");
                }
                else
                {
                    Console.WriteLine($"{prefix} [{i + 1}] {child.DebugDisplayName} ({child.GetType().Name})");
                }
            }

            // 8. Print execution chain from relations
            Console.WriteLine("\n  Execution order (from MEETS chain):");
            // Build the chain
            var fromTo = new Dictionary<string, string>();
            foreach (var rel in relationsArray.EnumerateArray())
            {
                fromTo[rel.GetProperty("from").GetString()] = rel.GetProperty("to").GetString();
            }

            // Find the start: a node that is in 'from' but never in 'to'
            var allTo = new HashSet<string>(fromTo.Values);
            string start = fromTo.Keys.FirstOrDefault(k => !allTo.Contains(k));
            if (start != null)
            {
                int step = 1;
                string current = start;
                while (current != null)
                {
                    Console.WriteLine($"    Step {step,2}: {current}");
                    fromTo.TryGetValue(current, out current);
                    step++;
                }
            }

            // 9. Single tick test
            Console.WriteLine("\n========================================");
            Console.WriteLine("  SINGLE TICK TEST");
            Console.WriteLine("========================================\n");

            Console.WriteLine("[8] Ticking tree once...");
            try
            {
                var result = behaviorTree.Tick(0.016f);
                Console.WriteLine($"  Tick result: {result}");
                Console.WriteLine($"  Tree finished: {behaviorTree.HasFinished()}");

                // Print status of each child after tick
                Console.WriteLine("\n  Node statuses after tick:");
                for (int i = 0; i < children.Count; i++)
                {
                    var child = children[i];
                    if (child is DynamicFlowNode dn)
                    {
                        Console.WriteLine($"    [{i + 1}] {dn.GetNodeName(),-16} status={dn.status}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Tick threw an exception: {ex.Message}");
                Console.WriteLine($"  This is expected if the planner service is not running.");
                Console.WriteLine($"  The tree structure was built successfully from JSON.");
            }

            Console.WriteLine("\n========================================");
            Console.WriteLine("  JSON BT LOAD TEST COMPLETE");
            Console.WriteLine("========================================");

            doc.Dispose();
        }

        public static async Task RunTest()
        {
            var test = new JsonBTLoadTest();
            await test.RunJsonBTLoadTest();
        }
    }
}
