using System.Text.Json;
using AIPlanning;
using BehaviorTreeMainProject.Services.AIPlanning;

namespace BehaviorTreeMainProject;

public sealed class LoadedBehaviorTree
{
    public required BehaviorTree Tree { get; init; }
    public required IReadOnlyList<ServicePlanning> Planners { get; init; }
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
            .FirstOrDefault(tree => string.Equals(
                tree.GetProperty("name").GetString(), config.TreeName, StringComparison.OrdinalIgnoreCase));

        if (treeElement.ValueKind == JsonValueKind.Undefined)
            throw new InvalidOperationException($"Behavior tree '{config.TreeName}' was not found in '{modelPath}'.");

        var behaviorTree = new BehaviorTree
        {
            linkedBlackboard = blackboard,
            DebugDisplayName = config.TreeName
        };
        var planners = new List<ServicePlanning>();
        var root = BuildFlowNode(treeElement.GetProperty("root"), behaviorTree, config, planners);

        behaviorTree.root = root;
        root.SetOwiningTree(behaviorTree);
        root.SetTreeForAllServices(behaviorTree);

        blackboard.PlanningPhase = true;
        blackboard.CassetteSubtreeCompleted = new bool[config.CassetteCount];
        blackboard.SetNodeGraph(new FastName("MainBehaviorTree"), new NodeGraph());

        return new LoadedBehaviorTree { Tree = behaviorTree, Planners = planners };
    }

    private static FlowNode BuildFlowNode(
        JsonElement node,
        BehaviorTree behaviorTree,
        BehaviorTreeExecutionConfig config,
        List<ServicePlanning> planners)
    {
        var modelName = node.GetProperty("name").GetString()
            ?? throw new InvalidOperationException("A flow node is missing its name.");
        var name = NormalizeRuntimeNodeName(modelName);
        var children = GetGraphNodes(node).ToArray();
        var isComposite = children.Any(IsFlowNode);
        var criteria = ParseSuccessCriteria(node);

        // All flow nodes (composite or leaf) are now DynamicFlowNode.
        // Composite (AllFlow) uses AddChild + AddFlowRelation for graph-based ordering.
        // Leaf (AllAction) uses the planning service path as before.
        var dynamicNode = new DynamicFlowNode(new FastName(name), behaviorTree, criteria, 1.0f, !isComposite);

        if (isComposite)
        {
            var composite = new BTFlowNodeComposite(new FastName(name), behaviorTree, criteria);
            composite.RunChildrenSequentially = node.TryGetProperty("nodeGraph", out var graph) &&
                graph.TryGetProperty("relations", out var rels) &&
                rels.EnumerateArray().Any();

            foreach (var child in children.Where(IsFlowNode))
                composite.AddChild(BuildFlowNode(child, behaviorTree, config, planners));

            // Attach runtime services/decorators declared in the BT model by name
            if (HasNamedService(node, "planningPhase"))
                composite.AddPlanningPhaseService();
            if (HasNamedService(node, "batchManager"))
                composite.AddService(new ServiceBatchEntry(behaviorTree, composite,
                    GetBatchIndices(config, name, composite), GetBatchObjectsFile(config, name)), false);
            if (HasNamedDecorator(node, "lowestCostExecution"))
                composite.AddDecorator(new DecoratorLowestCostExecution(composite));

            return composite;
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

        var planner = new ServicePDDLPlanning(behaviorTree, request)
        {
            ExecutionMode = Enum.Parse<ServicePDDLPlanning.ParallelExecutionMode>(
                config.PlannerExecutionMode, ignoreCase: true)
        };
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

    private static string NormalizeRuntimeNodeName(string modelName)
    {
        const string cassettePrefix = "Cassette";
        if (modelName.StartsWith(cassettePrefix, StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(modelName[cassettePrefix.Length..], out var cassetteNumber))
            return $"cassette{cassetteNumber}";

        return modelName;
    }

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

    private static int[] GetBatchIndices(
        BehaviorTreeExecutionConfig config,
        string nodeName,
        BTFlowNodeComposite composite)
    {
        var batch = config.Batches.FirstOrDefault(candidate =>
            string.Equals(candidate.NodeName, nodeName, StringComparison.OrdinalIgnoreCase));
        if (batch?.CassetteIndices.Length > 0)
            return batch.CassetteIndices;

        int childCount = composite.GetChildren().Count;
        var indices = new int[childCount];
        for (int i = 0; i < childCount; i++)
            indices[i] = i;
        return indices;
    }

    private static string GetBatchObjectsFile(BehaviorTreeExecutionConfig config, string nodeName)
    {
        var batch = config.Batches.FirstOrDefault(b =>
            string.Equals(b.NodeName, nodeName, StringComparison.OrdinalIgnoreCase));
        return batch?.ObjectsFile;
    }
}

public sealed class BehaviorTreeExecutionConfig
{
    public string ModelPath { get; set; } = "BehaviorTreeModel.json";
    public string TreeName { get; set; } = "LiveMat";
    public string PddlBasePath { get; set; } = "Plannerinputs/static";
    public string PlannerPath { get; set; } = "/home/ubuntu/jpddlplus-master/jpddlplus.jar";
    public string PlannerName { get; set; } = "ENHSP";
    public int TimeoutSeconds { get; set; } = 120;
    public string PlannerExecutionMode { get; set; } = "Parallel";
    public int CassetteCount { get; set; } = 12;
    public List<BehaviorTreeBatchConfig> Batches { get; set; } = new();
}

public sealed class BehaviorTreeBatchConfig
{
    public string NodeName { get; set; } = "";
    public int[] CassetteIndices { get; set; } = Array.Empty<int>();
    public string? ObjectsFile { get; set; }
}