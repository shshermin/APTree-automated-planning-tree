using System;
using System.Diagnostics;
using System.IO;
using Xunit;

namespace BehaviorTreeMainProject.Tests;

/// <summary>
/// /api/aptree/validate needs APTreeDSL/target/libs/automaton-*-tool.jar, which
/// is gitignored, so the image has to build it in its javabuild stage.
/// </summary>
[Collection("NetworkIntegrationTests")]
public class DockerImageBuildTests
{
    private static string RepoRoot => Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".."));

    [Fact]
    public void Dockerfile_HasADedicatedJavaBuildStage_ThatRunsGradleShadowJar()
    {
        string dockerfilePath = Path.Combine(RepoRoot, "Dockerfile");
        Assert.True(File.Exists(dockerfilePath), $"Dockerfile not found at {dockerfilePath}");
        string dockerfile = File.ReadAllText(dockerfilePath);

        Assert.Contains("AS javabuild", dockerfile);
        Assert.Contains("gradle shadowJar", dockerfile);
        // Must copy the built APTreeDSL (with target/libs), not the raw source.
        Assert.Contains("COPY --from=javabuild /src/APTreeDSL /app/APTreeDSL", dockerfile);
    }

    [Fact]
    public void Dockerfile_JavaBuildStage_UsesAJdkMatchingTheProjectsPinnedToolchainVersion()
    {
        string dockerfilePath = Path.Combine(RepoRoot, "Dockerfile");
        string dockerfile = File.ReadAllText(dockerfilePath);

        Assert.Contains("eclipse-temurin:11-jdk", dockerfile);

        string buildGradlePath = Path.Combine(RepoRoot, "APTreeDSL", "build.gradle");
        Assert.True(File.Exists(buildGradlePath), $"build.gradle not found at {buildGradlePath}");
        string buildGradle = File.ReadAllText(buildGradlePath);

        // No toolchain download repository is configured, so the javabuild
        // base image's JDK has to match the pinned toolchain version.
        Assert.Contains("JavaLanguageVersion.of(11)", buildGradle);
    }

    [SlowFact("runs a real `docker build` of the full image - pulls several base images and runs a real Gradle build, can take a few minutes")]
    public void DockerBuild_ProducesAWorkingMontiCoreJar_UsableByApiAptreeValidate()
    {
        Assert.True(IsDockerAvailable(), "docker is not available in this environment - cannot verify the real image build.");

        const string imageTag = "aptree-test-publishcompleteness";
        string containerName = "aptree-run-" + Guid.NewGuid().ToString("N");

        try
        {
            RunOrThrow("docker", $"build -t {imageTag} -f Dockerfile .", RepoRoot, timeoutMs: 10 * 60_000);

            var find = RunOrThrow("docker",
                $"run --rm --entrypoint sh {imageTag} -c \"find /app/APTreeDSL/target/libs -name '*.jar'\"",
                RepoRoot, timeoutMs: 60_000);
            Assert.Contains("automaton", find.StdOut);
            Assert.Contains(".jar", find.StdOut);

            RunOrThrow("docker", $"run -d --name {containerName} -p 0:5254 {imageTag}", RepoRoot, timeoutMs: 30_000);
            System.Threading.Thread.Sleep(6000);

            string portOutput = RunOrThrow("docker", $"port {containerName} 5254/tcp", RepoRoot, timeoutMs: 10_000).StdOut.Trim();
            string hostPort = portOutput.Split(':')[^1];

            using var http = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            var response = http.PostAsync(
                $"http://localhost:{hostPort}/api/aptree/validate",
                new System.Net.Http.StringContent(
                    "{\"modelText\": \"garbage not valid bt syntax\"}",
                    System.Text.Encoding.UTF8, "application/json")).Result;
            string body = response.Content.ReadAsStringAsync().Result;

            Assert.True(response.IsSuccessStatusCode, $"unexpected status {response.StatusCode}: {body}");
            Assert.DoesNotContain("MontiCore tool jar not found", body);
            Assert.DoesNotContain("APTreeDSL directory not found", body);
            // A parse failure for the garbage model proves the jar actually ran.
            Assert.Contains("\"ok\":false", body);
        }
        finally
        {
            RunBestEffort("docker", $"rm -f {containerName}", RepoRoot);
            RunBestEffort("docker", $"rmi {imageTag}", RepoRoot);
        }
    }

    private static bool IsDockerAvailable()
    {
        try
        {
            var result = RunOrThrow("docker", "version --format \"{{.Server.Version}}\"", RepoRoot, timeoutMs: 10_000);
            return !string.IsNullOrWhiteSpace(result.StdOut);
        }
        catch
        {
            return false;
        }
    }

    private static (string StdOut, string StdErr) RunOrThrow(string fileName, string arguments, string workingDirectory, int timeoutMs)
    {
        var psi = new ProcessStartInfo(fileName, arguments)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        using var process = Process.Start(psi)!;
        // Drain both pipes concurrently; docker build writes heavily to stderr
        // and a full pipe buffer deadlocks it.
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        bool exited = process.WaitForExit(timeoutMs);

        if (!exited)
        {
            try { process.Kill(entireProcessTree: true); } catch { /* best effort */ }
            throw new TimeoutException($"`{fileName} {arguments}` did not finish within {timeoutMs}ms");
        }
        string stdout = stdoutTask.Result;
        string stderr = stderrTask.Result;
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"`{fileName} {arguments}` failed (exit {process.ExitCode}):\n{stdout}\n{stderr}");

        return (stdout, stderr);
    }

    private static void RunBestEffort(string fileName, string arguments, string workingDirectory)
    {
        try { RunOrThrow(fileName, arguments, workingDirectory, timeoutMs: 30_000); }
        catch { /* cleanup is best-effort */ }
    }
}
