using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using Xunit;

namespace BehaviorTreeMainProject.Tests;

/// <summary>
/// docker/start.sh starts the backend with `--urls http://0.0.0.0:5254`.
/// Regression: Program.cs used to strip every "--xxx" argument before
/// WebApplication.CreateBuilder saw it, so Kestrel fell back to localhost:5000.
/// </summary>
[Collection("NetworkIntegrationTests")]
public class DockerEntrypointBindingTests : IDisposable
{
    private static string EngineRoot => Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string MainProjectDll => Path.Combine(EngineRoot, "bin", "Debug", "net8.0", "BehaviorTreeMainProject.dll");

    private Process? _process;

    public void Dispose()
    {
        if (_process != null && !_process.HasExited)
        {
            _process.Kill(entireProcessTree: true);
            _process.WaitForExit(5000);
        }
        _process?.Dispose();
    }

    private Process StartBackend(string? cliArgs, string? aspNetCoreUrlsEnv)
    {
        Assert.True(File.Exists(MainProjectDll), $"Built DLL not found at {MainProjectDll} - run `dotnet build` first.");

        var psi = new ProcessStartInfo("dotnet", $"{MainProjectDll} {cliArgs}".Trim())
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        if (aspNetCoreUrlsEnv != null)
            psi.Environment["ASPNETCORE_URLS"] = aspNetCoreUrlsEnv;

        _process = Process.Start(psi)!;
        return _process;
    }

    private static async Task<bool> CanConnectAsync(string host, int port, int timeoutMs = 500)
    {
        using var client = new TcpClient();
        try
        {
            var connectTask = client.ConnectAsync(host, port);
            var completed = await Task.WhenAny(connectTask, Task.Delay(timeoutMs));
            return completed == connectTask && client.Connected;
        }
        catch
        {
            return false;
        }
    }

    private static async Task WaitUntilAsync(Func<Task<bool>> condition, int timeoutMs)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < deadline)
        {
            if (await condition())
                return;
            await Task.Delay(200);
        }
    }

    [SlowFact("boots the real built backend with the exact docker/start.sh command line")]
    public async Task DockerStartShCommandLine_NowBindsTheDocumentedPort()
    {
        StartBackend("--urls http://0.0.0.0:5254", aspNetCoreUrlsEnv: null);

        await WaitUntilAsync(() => CanConnectAsync("127.0.0.1", 5254), timeoutMs: 8000);

        Assert.True(await CanConnectAsync("127.0.0.1", 5254),
            "port 5254 should be reachable - check that Program.cs passes --urls through to the web host");
        Assert.False(await CanConnectAsync("127.0.0.1", 5000),
            "Kestrel fell back to its default port 5000, so --urls was ignored");
    }

    [SlowFact("boots the real built backend with ASPNETCORE_URLS set")]
    public async Task AspNetCoreUrlsEnvironmentVariable_CorrectlyBindsTheIntendedPort()
    {
        StartBackend(cliArgs: null, aspNetCoreUrlsEnv: "http://0.0.0.0:5254");

        await WaitUntilAsync(() => CanConnectAsync("127.0.0.1", 5254), timeoutMs: 8000);

        Assert.True(await CanConnectAsync("127.0.0.1", 5254),
            "ASPNETCORE_URLS should bind port 5254");
    }
}
