using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using BehaviorTreeMainProject.ModelLoader;
using Xunit;

namespace BehaviorTreeMainProject.Tests;

/// <summary>
/// Drives the real BehaviorTreeRunner with the shipped Demonstrator model, config
/// and PDDL files over real HTTP; only the planner process is replaced by an
/// HttpListener stub, so no Python/Java/ENHSP is needed. The stub returns
/// unparseable plan content, which ServicePDDLPlanning reports as an empty
/// successful plan, so ML subtree planning is never reached.
/// </summary>
[Collection("NetworkIntegrationTests")]
public class FullPipelineSmokeTests : IDisposable
{
    private const int PlannerPort = 5000;

    private static string EngineRoot => Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private readonly string _pythonServiceSymlink;
    private HttpListener? _listener;

    public FullPipelineSmokeTests()
    {
        // BehaviorTreeRunner.Validate() resolves PDDL files relative to
        // AppDomain.BaseDirectory + "../../../python_service", which only matches
        // the main project's bin folder; symlink it in for the test binary.
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        _pythonServiceSymlink = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "python_service"));

        if (Directory.Exists(_pythonServiceSymlink) || File.Exists(_pythonServiceSymlink))
        {
            // Only ever remove a stale symlink from a crashed run, never a real directory.
            var info = new DirectoryInfo(_pythonServiceSymlink);
            if (info.LinkTarget != null)
                Directory.Delete(_pythonServiceSymlink, recursive: false);
        }

        Directory.CreateSymbolicLink(_pythonServiceSymlink, Path.Combine(EngineRoot, "python_service"));
    }

    public void Dispose()
    {
        _listener?.Stop();
        _listener?.Close();

        // recursive:false matters: recursive deletion of a directory symlink can
        // follow it and wipe the real python_service directory.
        if (Directory.Exists(_pythonServiceSymlink))
            Directory.Delete(_pythonServiceSymlink, recursive: false);
    }

    private HttpListener StartStubPlannerService()
    {
        var listener = new HttpListener();
        listener.Prefixes.Add($"http://localhost:{PlannerPort}/");
        listener.Start();
        _listener = listener;

        _ = Task.Run(async () =>
        {
            while (listener.IsListening)
            {
                HttpListenerContext ctx;
                try
                {
                    ctx = await listener.GetContextAsync();
                }
                catch (Exception)
                {
                    return; // listener was stopped
                }

                try
                {
                    if (ctx.Request.Url?.AbsolutePath == "/plan")
                    {
                        string body = "{\"success\":true,\"plan\":\"this is not planner output, just a stub\",\"plannerUsed\":\"Stub\"}";
                        var bytes = Encoding.UTF8.GetBytes(body);
                        ctx.Response.ContentType = "application/json";
                        ctx.Response.StatusCode = 200;
                        await ctx.Response.OutputStream.WriteAsync(bytes);
                    }
                    else
                    {
                        ctx.Response.StatusCode = 200;
                    }
                }
                catch { /* best-effort stub */ }
                finally
                {
                    ctx.Response.Close();
                }
            }
        });

        return listener;
    }

    [SlowFact("boots a real HTTP stub and drives the full Demonstrator model through BehaviorTreeRunner")]
    public async Task ShippedDemonstratorModel_RunsHeadless_AgainstAStubPlanningService()
    {
        StartStubPlannerService();

        string modelPath = Path.Combine(EngineRoot, "src", "ModelLoader", "BehaviorTreeModel.json");
        string configPath = Path.Combine(EngineRoot, "src", "ModelLoader", "DemonstratorConfig.json");
        Assert.True(File.Exists(modelPath), $"shipped model not found at {modelPath}");
        Assert.True(File.Exists(configPath), $"shipped config not found at {configPath}");

        var config = BehaviorTreeConfiguration.LoadFromFile(configPath);
        // Empty plans complete each flow node within a tick or two; this is a generous cap.
        config.MaxTicks = 50;
        config.TickDelayMs = 0;

        // RunFromFiles' convention lookup is also AppDomain.BaseDirectory-relative,
        // so pass absolute paths and call Run() directly.
        config.SetupObjectsFile = Path.Combine(EngineRoot, "src", "ModelLoader", "DemonstratorSetupObjects.json");
        config.InitialStateFile = Path.Combine(EngineRoot, "src", "ModelLoader", "DemonstratorInitState.json");
        config.GoalStateFile = Path.Combine(EngineRoot, "src", "ModelLoader", "DemonstratorGoalState.json");
        config.ActionInstancesFile = Path.Combine(EngineRoot, "src", "InputInstances", "ActionInstances.txt");
        Assert.True(File.Exists(config.SetupObjectsFile), config.SetupObjectsFile);
        Assert.True(File.Exists(config.InitialStateFile), config.InitialStateFile);
        Assert.True(File.Exists(config.GoalStateFile), config.GoalStateFile);
        Assert.True(File.Exists(config.ActionInstancesFile), config.ActionInstancesFile);

        var runner = new BehaviorTreeRunner(modelPath, config);

        var exception = await Record.ExceptionAsync(() => runner.Run());

        Assert.Null(exception);
    }
}
