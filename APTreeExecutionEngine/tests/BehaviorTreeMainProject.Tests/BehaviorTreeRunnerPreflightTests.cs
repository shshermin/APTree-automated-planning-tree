using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using BehaviorTreeMainProject.ModelLoader;
using Xunit;

namespace BehaviorTreeMainProject.Tests;

/// <summary>
/// BehaviorTreeRunner.Run() starts with a private Validate() 
/// (pre-flight file checks) and then LoadJsonModel; both
/// run before any logging/blackboard setup, so failures can be observed
/// through the public Run() without side effects.
/// </summary>
public class BehaviorTreeRunnerPreflightTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "aptree-run-" + Guid.NewGuid().ToString("N"));

    public BehaviorTreeRunnerPreflightTests() => Directory.CreateDirectory(_dir);
    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private string Touch(string name, string content = "")
    {
        string path = Path.Combine(_dir, name);
        File.WriteAllText(path, content);
        return path;
    }

    private BehaviorTreeConfiguration ConfigWithExistingSupportFiles() => new()
    {
        SetupObjectsFile = Touch("setup.json", "{}"),
        InitialStateFile = Touch("init.json", "{}"),
        ActionInstancesFile = Touch("actions.txt"),
    };

    [Fact]
    public async Task Run_ReportsEveryMissingFileAtOnce_NotJustTheFirst()
    {
        var config = new BehaviorTreeConfiguration
        {
            SetupObjectsFile = Path.Combine(_dir, "missing-setup.json"),
            InitialStateFile = Path.Combine(_dir, "missing-init.json"),
            ActionInstancesFile = null,
            GoalStateFile = Path.Combine(_dir, "missing-goal.json"),
        };
        var runner = new BehaviorTreeRunner(Path.Combine(_dir, "missing-model.json"), config);

        var ex = await Assert.ThrowsAsync<FileNotFoundException>(() => runner.Run());

        Assert.Contains("Pre-flight validation failed", ex.Message);
        Assert.Contains("missing-model.json", ex.Message);
        Assert.Contains("missing-setup.json", ex.Message);
        Assert.Contains("missing-init.json", ex.Message);
        Assert.Contains("missing-goal.json", ex.Message);
        Assert.Contains("ActionInstancesFile: (not configured)", ex.Message);
    }

    [Fact]
    public async Task Run_WithAnEmptyDefaultConfig_FailsPreflight_NamingTheUnconfiguredFiles()
    {
        var runner = new BehaviorTreeRunner(Touch("model.json", "{}"), new BehaviorTreeConfiguration());

        var ex = await Assert.ThrowsAsync<FileNotFoundException>(() => runner.Run());

        Assert.Contains("SetupObjectsFile: (not configured)", ex.Message);
        Assert.Contains("InitialStateFile: (not configured)", ex.Message);
        Assert.Contains("ActionInstancesFile: (not configured)", ex.Message);
    }

    [Fact]
    public void Constructor_RejectsNullArguments()
    {
        Assert.Throws<ArgumentNullException>(() => new BehaviorTreeRunner(null!, new BehaviorTreeConfiguration()));
        Assert.Throws<ArgumentNullException>(() => new BehaviorTreeRunner("model.json", null!));
    }

    [Fact]
    public async Task Run_MalformedModelJson_FailsWithAJsonException()
    {
        var runner = new BehaviorTreeRunner(Touch("model.json", "{ this is not json"), ConfigWithExistingSupportFiles());

        await Assert.ThrowsAnyAsync<JsonException>(() => runner.Run());
    }

    /// <summary>
    /// Valid JSON that just isn't a behavior-tree model (no "behaviorTrees"
    /// array) is not caught by Validate() - it gets as far as
    /// `modelJson.GetProperty("behaviorTrees")`, which throws a bare
    /// KeyNotFoundException whose message ("The given property was not
    /// found...") never mentions the model file or what was expected.
    /// </summary>
    [Fact]
    public async Task Run_ModelJsonWithoutBehaviorTrees_FailsWithAnUnhelpfulKeyNotFound()
    {
        var runner = new BehaviorTreeRunner(Touch("model.json", "{ \"foo\": 1 }"), ConfigWithExistingSupportFiles());

        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() => runner.Run());

        Assert.DoesNotContain("model.json", ex.Message);
        Assert.DoesNotContain("behaviorTrees", ex.Message);
    }

    [Fact]
    public async Task Run_ModelJsonWithEmptyBehaviorTreesArray_FailsWithAnUnhelpfulIndexError()
    {
        var runner = new BehaviorTreeRunner(Touch("model.json", "{ \"behaviorTrees\": [] }"), ConfigWithExistingSupportFiles());

        var ex = await Assert.ThrowsAnyAsync<Exception>(() => runner.Run());

        Assert.IsNotType<FileNotFoundException>(ex);
        Assert.DoesNotContain("behaviorTrees", ex.Message);
    }

    /// <summary>#53: the checked-in model has the structure Run() dereferences.</summary>
    [Fact]
    public void ShippedBehaviorTreeModelJson_HasTheStructureTheRunnerReads()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "ModelLoader", "BehaviorTreeModel.json");
        Assert.True(File.Exists(path), $"shipped model not found at {Path.GetFullPath(path)}");

        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var tree = doc.RootElement.GetProperty("behaviorTrees")[0];

        Assert.False(string.IsNullOrWhiteSpace(tree.GetProperty("name").GetString()));
        Assert.True(tree.GetProperty("root").GetProperty("nodeGraph").GetProperty("nodes").GetArrayLength() > 0);
    }
}
