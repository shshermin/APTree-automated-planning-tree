using System;
using System.IO;
using System.Text.Json;
using BehaviorTreeMainProject.ModelLoader;
using BehaviorTreeMainProject.Services.AIPlanning;
using Xunit;

namespace BehaviorTreeMainProject.Tests;

/// <summary>
/// BehaviorTreeConfiguration is a plain DTO with no validation; these tests
/// pin down how it handles bad input.
/// </summary>
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
        Assert.Equal("Sequential", config.ExecutionMode);
        Assert.Equal("Sqlite", config.PredicateStoreType);
        // Required fields without defaults stay null until BehaviorTreeRunner's Validate() complains.
        Assert.Null(config.SetupObjectsFile);
        Assert.Null(config.InitialStateFile);
        Assert.Null(config.ActionInstancesFile);
    }

    [Theory]
    [InlineData("Parallel", ServicePDDLPlanning.ParallelExecutionMode.Parallel)]
    [InlineData("HYBRID", ServicePDDLPlanning.ParallelExecutionMode.Hybrid)]
    [InlineData("Sequential", ServicePDDLPlanning.ParallelExecutionMode.Sequential)]
    public void GetExecutionMode_ParsesKnownValues_CaseInsensitively(string text, ServicePDDLPlanning.ParallelExecutionMode expected)
    {
        var config = new BehaviorTreeConfiguration { ExecutionMode = text };

        Assert.Equal(expected, config.GetExecutionMode());
    }

    [Theory]
    [InlineData("Paralel")]      // typo
    [InlineData("")]
    [InlineData("garbage")]
    [InlineData(null)]
    public void GetExecutionMode_SilentlyFallsBackToSequential_ForInvalidValues(string? text)
    {
        var config = new BehaviorTreeConfiguration { ExecutionMode = text! };

        // Unknown values silently fall back to Sequential.
        Assert.Equal(ServicePDDLPlanning.ParallelExecutionMode.Sequential, config.GetExecutionMode());
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
    public void LoadFromFile_JsonNullLiteral_ReturnsNull_ForCallersToTripOver()
    {
        // A literal `null` file returns null instead of throwing.
        Assert.Null(BehaviorTreeConfiguration.LoadFromFile(Write("null")));
    }

    [Fact]
    public void LoadFromFile_IgnoresUnknownKeys_SoTyposInFieldNamesAreSilent()
    {
        var config = BehaviorTreeConfiguration.LoadFromFile(Write("{ \"timeoutSecondz\": 999, \"maxTicks\": 5 }"));

        Assert.Equal(30, config.TimeoutSeconds); // typo'd key ignored, default kept
        Assert.Equal(5, config.MaxTicks);         // correct key (case-insensitive) honored
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
