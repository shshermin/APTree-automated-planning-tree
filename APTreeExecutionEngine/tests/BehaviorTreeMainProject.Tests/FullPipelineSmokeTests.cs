using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using BehaviorTreeMainProject.ModelLoader;
using Xunit;

namespace BehaviorTreeMainProject.Tests;

/// <summary>
/// Full pipeline smoke test.
///
/// Tests one component at a time, mostly with fakes
/// standing in for anything cross-process (FakePlannerCommunicator instead of
/// a real network call, etc.). This test instead drives the actual, shipped
/// production entry point - BehaviorTreeRunner, loaded with the real
/// Demonstrator JSON model, the real DemonstratorConfig.json, and the real
/// PDDL files already checked into python_service/Plannerinputs - and lets it
/// make real HTTP calls across the same REST boundary RestPlannerCommunicator
/// uses in production. The only thing replaced is the planner process itself:
/// a plain HttpListener stands in for python_service, so this test needs no
/// Python, Java, or ENHSP installed.
///
/// Two real, verified findings came out of getting this to run at all:
///
/// 1. BehaviorTreeRunner.Validate() resolves the PDDL files referenced in the
///    model JSON via `AppDomain.CurrentDomain.BaseDirectory + "../../../python_service"`
///    (see ResolveLocalPddlPath in BehaviorTreeRunner.cs). That assumption -
///    "the running process's binary sits at APTreeExecutionEngine/bin/Debug/net8.0/"
///    - only holds when launched via `dotnet run` from the main project.
///    Launched from anywhere else (this test project's own bin folder, a
///    packaged Food4Rhino plugin install, ...), the same 3-levels-up math
///    lands in the wrong directory and Validate() throws
///    FileNotFoundException for files that actually exist on disk. Worked
///    around here with a throwaway symlink (see EnginePythonServiceSymlink)
///    rather than fixed in production code, per the project's policy of
///    documenting rather than silently patching findings made while writing
///    tests. This is directly relevant to Food4Rhino packaging (Phase 10):
///    whatever ships needs the exe to actually live where this math expects,
///    or the same failure will hit real users.
///
/// 2. BehaviorTreeConfiguration.ExecutionActive and ServiceLLSubtreeInject are
///    never wired together anywhere in BehaviorTreeRunner.cs or
///    ServiceSubtreeInject.cs (grep confirms ServiceLLSubtreeInject is only
///    constructed in src/Tests/DemonstratorTreeTest.cs, a separate hand-coded
///    `dotnet run --test` harness). So the actual production runner used for
///    a JSON model + config file can NEVER expand a plan into real robot
///    commands, regardless of ExecutionActive's value - it is a dead config
///    flag on this path, and "planning-only mode" is not actually a mode
///    switch, it is the only mode. Confirmed by grep, not just by reading the
///    config's doc comment.
///
/// The stub planner returns Success=true with unrecognized ("garbage")
/// Plan content for every /plan call. Per the Phase 4 finding
/// (GarbagePlanContent_IsSilentlyReportedAsSuccessWithAnEmptyNodeGraph in
/// ServicePDDLPlanningCommunicationTests.cs), that is reported as a
/// successful plan with zero actions - which would let every one of the
/// Demonstrator's 11 flow nodes complete instantly, without ever reaching
/// the ML-level subtree-injection planning call.
///
/// 3. It never gets that far. BehaviorTreeRunner.ExecuteTree (the tick loop
///    Run() awaits) calls Console.KeyAvailable unconditionally on every
///    single tick to support its interactive pause/quit (P / Q) controls,
///    with no headless/non-interactive mode. Confirmed empirically below,
///    not assumed: under a test host (stdin not an interactive console),
///    that throws `InvalidOperationException: Cannot see if a key has been
///    pressed when either application does not have a console or when
///    console input has been redirected from a file` on the very first
///    tick - before any planning HTTP call is even made. This means the
///    shipped BehaviorTreeRunner - the actual entry point for running a
///    JSON model + config, i.e. what a packaged Food4Rhino plugin would need
///    to invoke - cannot run at all in any host process without a real,
///    attached interactive console. That includes this test, a CI runner,
///    and plausibly a Rhino/Grasshopper-hosted process. This is the
///    headline finding of this "full pipeline smoke test": the pipeline
///    smoke-fails before planning, execution mode, or predicate store
///    choice ever come into play.
/// </summary>
public class FullPipelineSmokeTests : IDisposable
{
    private const int PlannerPort = 5000;

    private static string EngineRoot => Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private readonly string _pythonServiceSymlink;
    private HttpListener? _listener;

    public FullPipelineSmokeTests()
    {
        // BehaviorTreeRunner.Validate() looks for python_service 3 levels
        // above AppDomain.CurrentDomain.BaseDirectory (this test binary's own
        // bin folder), not above the main project's - see finding 1 above.
        // A throwaway symlink there, pointing at the real directory, is the
        // least invasive way to let Validate() find the real PDDL files
        // without changing production path-resolution code.
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        _pythonServiceSymlink = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "python_service"));

        if (Directory.Exists(_pythonServiceSymlink) || File.Exists(_pythonServiceSymlink))
        {
            // Never remove a real directory here - only ever a symlink this
            // fixture itself created on a previous (crashed) run.
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

        // recursive:false is load-bearing: this path is a symlink, and
        // Directory.Delete(path, recursive:true) on a directory symlink
        // follows it and deletes the REAL target's contents on some
        // platforms. Only ever delete the link itself.
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
    public async Task ShippedDemonstratorModel_CannotRunHeadless_ConsoleKeyAvailableThrowsBeforeAnyTickCompletes()
    {
        StartStubPlannerService();

        string modelPath = Path.Combine(EngineRoot, "src", "ModelLoader", "BehaviorTreeModel.json");
        string configPath = Path.Combine(EngineRoot, "src", "ModelLoader", "DemonstratorConfig.json");
        Assert.True(File.Exists(modelPath), $"shipped model not found at {modelPath}");
        Assert.True(File.Exists(configPath), $"shipped config not found at {configPath}");

        var config = BehaviorTreeConfiguration.LoadFromFile(configPath);
        // Bound the tick loop tightly - with an all-empty-NodeGraph plan every
        // flow node completes in one or two ticks, so this is a generous cap,
        // not a real "let it run" budget.
        config.MaxTicks = 50;
        config.TickDelayMs = 0;

        // Set absolute paths directly instead of going through
        // BehaviorTreeRunner.RunFromFiles's convention resolution, which is
        // ALSO AppDomain.CurrentDomain.BaseDirectory-relative and would need
        // its own symlink (see ResolveConventionBlackboardPaths) - calling
        // Run() directly with a fully-populated config sidesteps that.
        config.SetupObjectsFile = Path.Combine(EngineRoot, "src", "ModelLoader", "DemonstratorSetupObjects.json");
        config.InitialStateFile = Path.Combine(EngineRoot, "src", "ModelLoader", "DemonstratorInitState.json");
        config.GoalStateFile = Path.Combine(EngineRoot, "src", "ModelLoader", "DemonstratorGoalState.json");
        config.ActionInstancesFile = Path.Combine(EngineRoot, "src", "InputInstances", "ActionInstances.txt");
        Assert.True(File.Exists(config.SetupObjectsFile), config.SetupObjectsFile);
        Assert.True(File.Exists(config.InitialStateFile), config.InitialStateFile);
        Assert.True(File.Exists(config.GoalStateFile), config.GoalStateFile);
        Assert.True(File.Exists(config.ActionInstancesFile), config.ActionInstancesFile);

        var runner = new BehaviorTreeRunner(modelPath, config);

        // BLOCKED, verified: BehaviorTreeRunner.Run() cannot complete outside
        // an interactive console - see finding 3 above. This assertion
        // documents that real, reproduced failure rather than asserting the
        // clean "runs to completion" outcome this test originally set out to
        // prove. If this ever starts failing because Run() no longer throws
        // here, that's good news - come back and change this test to assert
        // the tree actually reaches HasFinished(), which is what a true
        // full-pipeline smoke test should verify.
        var exception = await Record.ExceptionAsync(() => runner.Run());

        var invalidOp = Assert.IsType<InvalidOperationException>(exception);
        Assert.Contains("Cannot see if a key has been pressed", invalidOp.Message);
    }
}
