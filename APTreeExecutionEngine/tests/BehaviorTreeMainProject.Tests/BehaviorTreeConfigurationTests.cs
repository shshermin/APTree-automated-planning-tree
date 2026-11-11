using System;
using System.IO;
using System.Text.Json;
using BehaviorTreeMainProject.ModelLoader;
using BehaviorTreeMainProject.Services.AIPlanning;
using Xunit;

namespace BehaviorTreeMainProject.Tests;

/// <summary>How BehaviorTreeConfiguration loads and validates its input.</summary>
public class BehaviorTreeConfigurationTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "aptree-cfg-" + Guid.NewGuid().ToString("N"));

    public BehaviorTreeConfigurationTests() => Directory.CreateDirectory(_dir);
    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private string Write(string json)
    {
        string path = Path.Combine(_dir, Guid.NewGuid().ToString("N") + ".json");
        File.WriteAllText(path, json);
        return path;
    }

    [Fact]
    public void LoadFromFile_WithAnEmptyObject_FallsBackToDefaults_InsteadOfRejectingMissingFields()
    {
        var config = BehaviorTreeConfiguration.LoadFromFile(Write("{}"));

        Assert.Equal(30, config.TimeoutSeconds);
        // A config without the key must never drive a robot.
        Assert.False(config.ExecutionActive);
        Assert.Equal("Sqlite", config.PredicateStoreType);
        // Required fields without defaults stay null until BehaviorTreeRunner's Validate() complains.
        Assert.Null(config.SetupObjectsFile);
        Assert.Null(config.InitialStateFile);
        Assert.Null(config.ActionInstancesFile);
    }

    [Theory]
    [InlineData("executionMode")]
    [InlineData("subtreeExecutionMode")]
    public void LoadFromFile_RejectsTheRemovedExecutionModeKeys(string key)
    {
        var ex = Assert.ThrowsAny<JsonException>(() =>
            BehaviorTreeConfiguration.LoadFromFile(Write($"{{ \"{key}\": \"Sequential\" }}")));

        Assert.Contains(key, ex.Message);
    }

    [Fact]
    public void LoadFromFile_ExecutionActiveTrue_IsKept()
    {
        var config = BehaviorTreeConfiguration.LoadFromFile(Write("{ \"executionActive\": true }"));

        Assert.True(config.ExecutionActive);
    }

    [Fact]
    public void LoadFromFile_MissingFile_ThrowsFileNotFound()
    {
        Assert.Throws<FileNotFoundException>(() =>
            BehaviorTreeConfiguration.LoadFromFile(Path.Combine(_dir, "nope.json")));
    }

    [Fact]
    public void LoadFromFile_MalformedJson_ThrowsJsonException()
    {
        Assert.ThrowsAny<JsonException>(() => BehaviorTreeConfiguration.LoadFromFile(Write("{ not json")));
    }

    [Fact]
    public void LoadFromFile_JsonNullLiteral_Throws()
    {
        Assert.Throws<InvalidDataException>(() => BehaviorTreeConfiguration.LoadFromFile(Write("null")));
    }

    [Fact]
    public void LoadFromFile_RejectsUnknownKeys()
    {
        var ex = Assert.ThrowsAny<JsonException>(() =>
            BehaviorTreeConfiguration.LoadFromFile(Write("{ \"timeoutSecondz\": 999, \"maxTicks\": 5 }")));
        Assert.Contains("timeoutSecondz", ex.Message);
    }

    [Fact]
    public void LoadFromFile_KeysAreCaseInsensitive()
    {
        var config = BehaviorTreeConfiguration.LoadFromFile(Write("{ \"MaxTicks\": 5 }"));

        Assert.Equal(5, config.MaxTicks);
    }

    [Fact]
    public void ShippedDemonstratorConfig_LoadsWithoutUnknownKeys()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "ModelLoader", "DemonstratorConfig.json");

        var config = BehaviorTreeConfiguration.LoadFromFile(path);

        // The demonstrator drives the real robot, so it opts in explicitly.
        Assert.True(config.ExecutionActive);
    }

    [Fact]
    public void LoadFromFile_AcceptsCommentsAndTrailingCommas()
    {
        var config = BehaviorTreeConfiguration.LoadFromFile(Write("{ // note\n \"maxTicks\": 7, }"));

        Assert.Equal(7, config.MaxTicks);
    }

    [Fact]
    public void SaveThenLoad_RoundTripsValues()
    {
        string path = Path.Combine(_dir, "roundtrip.json");
        new BehaviorTreeConfiguration { MaxTicks = 42, TickDelayMs = 3, PredicateStoreType = "Dictionary" }.SaveToFile(path);

        var loaded = BehaviorTreeConfiguration.LoadFromFile(path);

        Assert.Equal(42, loaded.MaxTicks);
        Assert.Equal(3, loaded.TickDelayMs);
        Assert.Equal("Dictionary", loaded.PredicateStoreType);
    }
}
