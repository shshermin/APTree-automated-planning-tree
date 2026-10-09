using System;
using System.Diagnostics;
using System.IO;
using Xunit;

namespace BehaviorTreeMainProject.Tests;

/// <summary>
/// Runs the Dockerfile's `dotnet publish` and checks the output. The Web SDK
/// only publishes *.json data implicitly, so non-JSON inputs like
/// ActionInstances.txt need an explicit Content item in the .csproj.
/// </summary>
[Collection("NetworkIntegrationTests")]
public class PublishOutputCompletenessTests : IDisposable
{
    private readonly string _publishDir = Path.Combine(Path.GetTempPath(), "aptree-publish-" + Guid.NewGuid().ToString("N"));

    private static string EngineRoot => Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    public void Dispose()
    {
        if (Directory.Exists(_publishDir))
            Directory.Delete(_publishDir, recursive: true);
    }

    private void RunDotnetPublish()
    {
        var psi = new ProcessStartInfo("dotnet",
            $"publish BehaviorTreeMainProject.csproj -c Release -o \"{_publishDir}\"")
        {
            WorkingDirectory = EngineRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        using var process = Process.Start(psi)!;
        // Drain both pipes concurrently, or a full stderr buffer can deadlock the child.
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        bool exited = process.WaitForExit(120_000);

        Assert.True(exited, "dotnet publish did not finish within 120s");
        string stdout = stdoutTask.Result;
        string stderr = stderrTask.Result;
        Assert.True(process.ExitCode == 0, $"dotnet publish failed (exit {process.ExitCode}):\n{stdout}\n{stderr}");
    }

    [SlowFact("runs a real dotnet publish and inspects the output directory")]
    public void PublishOutput_IncludesEveryModelLoaderJsonFile_AndActionInstancesTxt()
    {
        RunDotnetPublish();

        string[] expectedJsonFiles =
        {
            "BehaviorTreeModel.json", "DemonstratorConfig.json", "DemonstratorGoalState.json",
            "DemonstratorInitState.json", "DemonstratorLLSubtrees.json", "DemonstratorSetupObjects.json",
            "InitialStatePredicates.json", "LiveMatSetupObjects.json", "PropertyInstances.json",
        };
        foreach (var file in expectedJsonFiles)
        {
            string path = Path.Combine(_publishDir, "src", "ModelLoader", file);
            Assert.True(File.Exists(path), $"expected {file} to be published to {path}");
        }

        string actionInstancesPath = Path.Combine(_publishDir, "src", "InputInstances", "ActionInstances.txt");
        Assert.True(File.Exists(actionInstancesPath),
            $"ActionInstances.txt missing from the publish output at {actionInstancesPath} - " +
            "check the src/InputInstances Content item in BehaviorTreeMainProject.csproj.");
    }
}
