using System;
using System.Diagnostics;
using System.IO;
using Xunit;

namespace BehaviorTreeMainProject.Tests;

/// <summary>
///
/// Runs the exact command the Dockerfile's build stage runs
/// (`dotnet publish BehaviorTreeMainProject.csproj -c Release -o ...`) and
/// inspects the real output directory, rather than reading the .csproj and
/// guessing what the SDK's implicit item globs would include.
///
/// Finding, confirmed empirically: Microsoft.NET.Sdk.Web's implicit content
/// globbing picks up every *.json file under src/ (all 9 files in
/// src/ModelLoader/ land in the publish output) but NOT src/InputInstances/
/// ActionInstances.txt - a plain .txt file one directory over, referenced by
/// BehaviorTreeConfiguration.ActionInstancesFile and read by
/// BlackboardWriter.RegisterAllInstances. A published/deployed build is
/// missing it entirely. This currently has no visible effect because the
/// Docker image only ever runs in `--server` mode (FrontendServer, which
/// never reads ActionInstancesFile) - but the moment anyone runs the
/// published artifact with `--run` (BehaviorTreeRunner, the JSON-model
/// execution path), or if the Phase 9 Console.KeyAvailable headless blocker
/// is ever fixed and `--run` becomes usable from a packaged build, it would
/// fail to find a file that exists right next to it in source control.
///
/// Not fixed here (would mean editing the .csproj to add an explicit
/// &lt;Content Include="src/InputInstances/**" CopyToOutputDirectory=...&gt;),
/// per the project's policy of documenting findings rather than silently
/// patching them.
/// </summary>
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
        string stdout = process.StandardOutput.ReadToEnd();
        string stderr = process.StandardError.ReadToEnd();
        bool exited = process.WaitForExit(120_000);

        Assert.True(exited, "dotnet publish did not finish within 120s");
        Assert.True(process.ExitCode == 0, $"dotnet publish failed (exit {process.ExitCode}):\n{stdout}\n{stderr}");
    }

    [SlowFact("runs a real dotnet publish and inspects the output directory")]
    public void PublishOutput_IncludesEveryJsonFileUnderModelLoader_ButNotActionInstancesTxt()
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
        Assert.False(File.Exists(actionInstancesPath),
            "Expected ActionInstances.txt to be MISSING from the publish output (the confirmed gap). " +
            "If this now fails because the file IS present, someone fixed the .csproj's content globbing - " +
            "good, update this test to assert presence instead of absence, and remove the class-level finding.");

        // Confirms the source file genuinely exists (this isn't passing because of a typo'd path).
        string sourcePath = Path.Combine(EngineRoot, "src", "InputInstances", "ActionInstances.txt");
        Assert.True(File.Exists(sourcePath), $"sanity check failed - source file missing at {sourcePath}");
    }
}
