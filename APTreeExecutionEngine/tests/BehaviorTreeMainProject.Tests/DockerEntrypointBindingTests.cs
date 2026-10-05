using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using Xunit;

namespace BehaviorTreeMainProject.Tests;

/// <summary>
///
/// docker/start.sh - the real, shipped container entrypoint
/// (docker-compose.yml maps host port 5254 to it) - starts the backend with:
///
///     exec dotnet /app/BehaviorTreeMainProject.dll --urls http://0.0.0.0:5254
///
/// This test runs that exact command line (against the locally built DLL,
/// not inside Docker, since the bug is in Program.cs's own argument parsing,
/// not anything Docker-specific) and found a release-blocking bug, confirmed
/// empirically, not assumed:
///
/// Program.cs's own CLI-mode dispatcher (`var mode = args.FirstOrDefault(a =>
/// a.StartsWith("--") && a != "--faults") ?? "--server";`) treats ANY
/// "--xxx" token as a candidate mode selector, including "--urls". Since
/// "--urls" matches no case in the switch, it falls through to `default:
/// FrontendServer.Run(remainingArgs)` - so far so harmless. But
/// `remainingArgs` is built by filtering OUT every token that starts with
/// "--" (`!a.StartsWith("--")`), which strips "--urls" itself and leaves
/// only the bare string "http://0.0.0.0:5254" - a positional argument with
/// no "--urls" prefix - to pass into `FrontendServer.Run`, which does
/// `WebApplication.CreateBuilder(args)`. ASP.NET Core's command-line
/// configuration provider only recognizes "--key value" / "--key=value"
/// pairs; a bare positional string is not one, so it is silently ignored,
/// and Kestrel falls back to its built-in default: http://localhost:5000.
///
/// Net effect: the exact command docker/start.sh runs does NOT bind
/// 0.0.0.0:5254 at all. It binds localhost:5000 instead - unreachable from
/// outside the container regardless of docker-compose's "5254:5254" and
/// "5000:5000" port mappings (loopback-only, and on the wrong port to boot).
/// Any Food4Rhino user following the Docker instructions as shipped would
/// get a container that looks like it started fine (no error, no crash) but
/// whose API is not reachable on the documented port.
///
/// Verified fix path (not applied here, per the project's policy of
/// documenting findings rather than silently patching them): set the
/// ASPNETCORE_URLS environment variable instead of passing --urls on the
/// command line - ASP.NET Core's environment-variable configuration source
/// is read directly by the host and is never touched by Program.cs's own
/// argument parsing. The second test below proves that path actually works.
/// </summary>
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
    public async Task DockerStartShCommandLine_NeverBindsTheDocumentedPort_BindsLocalhost5000Instead()
    {
        StartBackend("--urls http://0.0.0.0:5254", aspNetCoreUrlsEnv: null);

        // Give it a chance to either bind 5254 (if this regresses and starts
        // working) or fall back to 5000 (the current, broken behavior).
        await WaitUntilAsync(async () =>
            await CanConnectAsync("127.0.0.1", 5254) || await CanConnectAsync("127.0.0.1", 5000), timeoutMs: 8000);

        bool boundIntendedPort = await CanConnectAsync("127.0.0.1", 5254);
        bool boundFallbackPort = await CanConnectAsync("127.0.0.1", 5000);

        Assert.False(boundIntendedPort,
            "Expected port 5254 (what docker/start.sh and docker-compose.yml both document) to be unreachable. " +
            "If this now fails, the --urls argument-stripping bug in Program.cs may have been fixed - " +
            "update this test to assert the port IS reachable instead.");
        Assert.True(boundFallbackPort,
            "Expected the process to have silently fallen back to Kestrel's default http://localhost:5000, " +
            "which is the documented symptom of Program.cs stripping the --urls flag before ASP.NET Core sees it.");
    }

    [SlowFact("confirms the ASPNETCORE_URLS environment variable is an available fix")]
    public async Task AspNetCoreUrlsEnvironmentVariable_CorrectlyBindsTheIntendedPort()
    {
        StartBackend(cliArgs: null, aspNetCoreUrlsEnv: "http://0.0.0.0:5254");

        await WaitUntilAsync(() => CanConnectAsync("127.0.0.1", 5254), timeoutMs: 8000);

        Assert.True(await CanConnectAsync("127.0.0.1", 5254),
            "ASPNETCORE_URLS should bind the intended port even though the --urls CLI flag does not " +
            "(see the class-level finding) - if this now fails too, the fix recommendation below is wrong " +
            "and needs re-verifying before telling anyone to use it.");
    }
}
