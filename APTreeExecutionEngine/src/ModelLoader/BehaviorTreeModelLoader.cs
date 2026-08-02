using System.Text.Json;
using AIPlanning;
using BehaviorTreeMainProject.Services.AIPlanning;

namespace BehaviorTreeMainProject;

public sealed class LoadedBehaviorTree
{
    public required BehaviorTree Tree { get; init; }
    public required IReadOnlyList<ServicePlanning> Planners { get; init; }
    public required BehaviorTreeExecutionConfig Config { get; init; }
}

public static class BehaviorTreeModelLoader
{
    public static LoadedBehaviorTree Load(string configPath, Blackboard<FastName> blackboard)
    {
        var absoluteConfigPath = Path.GetFullPath(configPath);
        var config = Deserialize<BehaviorTreeExecutionConfig>(absoluteConfigPath);
        var configDirectory = Path.GetDirectoryName(absoluteConfigPath)!;
        var modelPath = Path.GetFullPath(config.ModelPath, configDirectory);
        using var modelDocument = JsonDocument.Parse(File.ReadAllText(modelPath));

        var treeElement = modelDocument.RootElement.GetProperty("behaviorTrees")
            .EnumerateArray()
            .FirstOrDefault();

        if (treeElement.ValueKind == JsonValueKind.Undefined)
            throw new InvalidOperationException($"No behavior trees found in '{modelPath}'.");

        var treeName = treeElement.TryGetProperty("root", out var rootEl) &&
                       rootEl.TryGetProperty("name", out var nameEl)
                       ? nameEl.GetString() ?? "BehaviorTree"
                       : "BehaviorTree";

        var behaviorTree = new BehaviorTree
        {
            linkedBlackboard = blackboard,
            DebugDisplayName = treeName
        };
        var planners = new List<ServicePlanning>();
        var root = BuildFlowNode(treeElement.GetProperty("root"), behaviorTree, config, planners);

        behaviorTree.root = root;
        root.SetOwiningTree(behaviorTree);
        root.SetTreeForAllServices(behaviorTree);

        blackboard.PlanningPhase = true;
        blackboard.SetNodeGraph(new FastName("MainBehaviorTree"), new NodeGraph());

        return new LoadedBehaviorTree { Tree = behaviorTree, Planners = planners, Config = config };
    }

    private static FlowNode BuildFlowNode(
        JsonElement node,
        BehaviorTree behaviorTree,
        BehaviorTreeExecutionConfig config,
        List<ServicePlanning> planners)
    {
        var name = node.GetProperty("name").GetString()
            ?? throw new InvalidOperationException("A flow node is missing its name.");
        var children = GetGraphNodes(node).ToArray();
        var isComposite = children.Any(IsFlowNode);
        var criteria = ParseSuccessCriteria(node);

        // All flow nodes (composite or leaf) are now DynamicFlowNode.
        // Composite (AllFlow) uses AddChild + AddFlowRelation for graph-based ordering.
        // Leaf (AllAction) uses the planning service path as before.
        var dynamicNode = new DynamicFlowNode(new FastName(name), behaviorTree, criteria, 1.0f, !isComposite);

        if (isComposite)
        {
            // Build child flow nodes and register them
            var builtChildren = new Dictionary<string, FlowNode>(StringComparer.OrdinalIgnoreCase);
            foreach (var child in children.Where(IsFlowNode))
            {
                var childNode = BuildFlowNode(child, behaviorTree, config, planners);
                dynamicNode.AddChild(childNode);
                builtChildren[childNode.InstanceName.ToString()] = childNode as FlowNode;
            }

            // Extract Meets relations from the graph to define execution order
            if (node.TryGetProperty("nodeGraph", out var graph) &&
                graph.TryGetProperty("relations", out var relations))
            {
                foreach (var rel in relations.EnumerateArray())
                {
                    var fromName = rel.TryGetProperty("from", out var f) ? f.GetString() : null;
                    var toName = rel.TryGetProperty("to", out var t) ? t.GetString() : null;
                    if (!string.IsNullOrEmpty(fromName) && !string.IsNullOrEmpty(toName))
                        dynamicNode.AddFlowRelation(fromName, toName);
                }
            }

            // Attach runtime services/decorators declared in the BT model by name
            if (HasNamedService(node, "planningPhase"))
                dynamicNode.AddPlanningPhaseService();
            if (HasNamedDecorator(node, "fairProgress"))
                dynamicNode.AddDecorator(new BTDecoratorFairBranchProgress(dynamicNode));
            if (HasNamedService(node, "batchManager"))
                dynamicNode.AddService(new ServiceBatchManager(behaviorTree, dynamicNode), false);

            return dynamicNode;
        }

        AttachPlanningService(node, dynamicNode, behaviorTree, config, planners);
        return dynamicNode;
    }

    private static void AttachPlanningService(
        JsonElement node,
        DynamicFlowNode flowNode,
        BehaviorTree behaviorTree,
        BehaviorTreeExecutionConfig config,
        List<ServicePlanning> planners)
    {
        if (!node.TryGetProperty("services", out var services))
            return;

        var service = services.EnumerateArray().FirstOrDefault(candidate =>
            candidate.TryGetProperty("type", out var type) &&
            (string.Equals(type.GetString(), "ServicePDDLPlanning", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(type.GetString(), "ServicePDDLPlanner", StringComparison.OrdinalIgnoreCase)));
        if (service.ValueKind == JsonValueKind.Undefined)
            return;

        var domainName = GetRequiredString(service, "domain");
        var problemName = GetRequiredString(service, "problem");
        var request = new PDDLPlanningRequest(
            ResolvePddlFile(config.PddlBasePath, domainName),
            ResolvePddlFile(config.PddlBasePath, problemName),
            config.PlannerPath,
            config.PlannerName,
            config.TimeoutSeconds)
        {
            EnhspConfig = service.TryGetProperty("enhspConfig", out var plannerConfig)
                ? plannerConfig.GetString()
                : null
        };

        var planner = new ServicePDDLPlanning(behaviorTree, request);
        flowNode.SetPlanningService(planner);
        planners.Add(planner);
    }

    private static IEnumerable<JsonElement> GetGraphNodes(JsonElement node)
    {
        if (node.TryGetProperty("nodeGraph", out var graph) &&
            graph.TryGetProperty("nodes", out var nodes))
            return nodes.EnumerateArray();

        return Enumerable.Empty<JsonElement>();
    }

    private static bool IsFlowNode(JsonElement node) =>
        node.TryGetProperty("type", out var type) &&
        type.GetString()?.Contains("FlowNode", StringComparison.OrdinalIgnoreCase) == true;

    private static bool HasNamedService(JsonElement node, string serviceName) =>
        node.TryGetProperty("services", out var services) &&
        services.EnumerateArray().Any(s =>
            s.TryGetProperty("name", out var n) &&
            string.Equals(n.GetString(), serviceName, StringComparison.OrdinalIgnoreCase));

    private static bool HasNamedDecorator(JsonElement node, string decoratorName) =>
        node.TryGetProperty("decorators", out var decorators) &&
        decorators.EnumerateArray().Any(d =>
            d.TryGetProperty("name", out var n) &&
            string.Equals(n.GetString(), decoratorName, StringComparison.OrdinalIgnoreCase));

    private static SuccessCriteria ParseSuccessCriteria(JsonElement node)
    {
        if (!node.TryGetProperty("successCriteria", out var criteria))
            return SuccessCriteria.ALL;

        return Enum.Parse<SuccessCriteria>(criteria.GetString()!, ignoreCase: true);
    }

    private static string ResolvePddlFile(string basePath, string modelName)
    {
        var expectedFileName = modelName + ".pddl";

        // The Flask service expects paths relative to its own directory (e.g. Plannerinputs/static/...),
        // but locally the files live under python_service/. Try both locations for case-insensitive matching.
        foreach (var candidate in new[] { basePath, Path.Combine("python_service", basePath) })
        {
            if (!Directory.Exists(candidate))
                continue;

            var match = Directory.EnumerateFiles(candidate, "*.pddl")
                .FirstOrDefault(path => string.Equals(
                    Path.GetFileName(path), expectedFileName, StringComparison.OrdinalIgnoreCase));
            if (match != null)
            {
                // Return the Flask-relative path using the real on-disk filename casing
                return Path.Combine(basePath, Path.GetFileName(match)).Replace('\\', '/');
            }
        }

        return Path.Combine(basePath, expectedFileName).Replace('\\', '/');
    }

    private static string GetRequiredString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) && !string.IsNullOrWhiteSpace(property.GetString())
            ? property.GetString()!
            : throw new InvalidOperationException($"Planning service is missing '{propertyName}'.");

    private static T Deserialize<T>(string path)
    {
        var value = JsonSerializer.Deserialize<T>(File.ReadAllText(path), new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        return value ?? throw new InvalidOperationException($"Could not deserialize '{path}'.");
    }
}

public sealed class BehaviorTreeExecutionConfig
{
    public string ModelPath { get; set; } = "BehaviorTreeModel.json";
    public string PddlBasePath { get; set; } = "Plannerinputs/static";
    public string PlannerPath { get; set; } = "/home/ubuntu/jpddlplus-master/jpddlplus.jar";
    public string PlannerName { get; set; } = "ENHSP";
    public int TimeoutSeconds { get; set; } = 120;
    public int TickIntervalMilliseconds { get; set; } = 100;
}