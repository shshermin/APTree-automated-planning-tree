using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ModelLoader.ParameterTypes;
using BehaviorTreeMainProject.Log.Services;
using RobotCommand;

/// <summary>
/// Abstract base for all executable (LL) action nodes that interact with the robot.
/// Inherits PActionNode and provides common infrastructure: robot IP, Flask URL,
/// empty PDDL states, position resolution from blackboard, and robot command execution.
/// Concrete subclasses (MoveToLL, CloseGripperLL, etc.) supply their specific command request.
///
/// Execution flow per tick:
///   OnEnter → operator confirmation MessageBox (blocking)
///   OnTick_NodeLogic → resolve position from blackboard, send REST command to robot
///   OnExit → operator verification MessageBox (blocking)
/// </summary>
public abstract class ExeAction : PActionNode
{
    protected static readonly string DEFAULT_FLASK_URL = "http://localhost:5001";
    protected static readonly string DEFAULT_ROBOT_IP = "192.168.1.100";

    // Win32 MessageBox via P/Invoke
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);

    private const uint MB_OKCANCEL = 0x00000001;
    private const uint MB_ICONWARNING = 0x00000030;
    private const uint MB_TOPMOST = 0x00040000;
    private const int IDOK = 1;

    public string RobotIp { get; }
    public string FlaskBaseUrl { get; }

    /// <summary>The command request built by the subclass.</summary>
    public RobotCommandRequest CommandRequest { get; protected set; }

    /// <summary>Populated after resolving the position from ML inputs.</summary>
    public RobotPosition ResolvedPosition { get; set; }

    /// <summary>
    /// Actual objects from the parent ML action, keyed by template parameter name.
    /// E.g. "target" → RobotPosition, "robot" → Robot.
    /// Set by ServiceLLSubtreeInject after creating the LL node.
    /// </summary>
    public Dictionary<string, object> MLInputs { get; set; }

    private bool _hasExecuted = false;

    // Empty PDDL states — LL execution nodes have no preconditions/effects
    private readonly State _emptyPreconditions;
    private readonly State _emptyEffects;

    protected override State Preconditions => _emptyPreconditions;
    protected override State Effects => _emptyEffects;

    protected ExeAction(
        string actionType,
        string instanceName,
        Blackboard<FastName> blackboard,
        string flaskBaseUrl = null,
        string robotIp = null
    ) : base(actionType, instanceName, blackboard)
    {
        FlaskBaseUrl = flaskBaseUrl ?? DEFAULT_FLASK_URL;
        RobotIp = robotIp ?? DEFAULT_ROBOT_IP;

        _emptyPreconditions = new State(StateType.Precondition, new FastName($"{instanceName}_pre"));
        _emptyEffects = new State(StateType.Effect, new FastName($"{instanceName}_eff"));
    }

    /// <summary>
    /// Shows a blocking confirmation dialog before the LL action executes.
    /// The BT pauses until the operator presses OK (proceed) or Cancel (abort).
    /// </summary>
    protected override void OnEnter()
    {
        base.OnEnter();

        // Build the command request on enter so subclass data is available
        CommandRequest = BuildCommandRequest();

        var actionName = GetType().Name;
        var details = CommandRequest != null
            ? $"Command: {CommandRequest.CommandType}\nTarget: {CommandRequest.FinalPosition}"
            : "No command details";

        var message = $"About to execute LL action:\n\n" +
                      $"  Action:   {actionName}\n" +
                      $"  Instance: {InstanceName}\n" +
                      $"  {details}\n\n" +
                      $"Press OK to proceed, Cancel to abort.";

        LoggingService.LogInfo($"⏸️ ExeAction: Waiting for operator confirmation for '{InstanceName}' ({actionName})");

        int result = MessageBoxW(IntPtr.Zero, message, $"Confirm: {actionName}", MB_OKCANCEL | MB_ICONWARNING | MB_TOPMOST);

        if (result != IDOK)
        {
            LoggingService.LogWarning($"🛑 ExeAction: Operator CANCELLED '{InstanceName}' ({actionName})");
            status = BTNodeResult.Failure;
            return;
        }

        LoggingService.LogSuccess($"▶️ ExeAction: Operator confirmed '{InstanceName}' ({actionName}) — proceeding");
    }

    /// <summary>
    /// Resolves position data from blackboard and sends the robot command via REST.
    /// Called after OnEnter (operator has already confirmed).
    /// </summary>
    protected override bool OnTick_NodeLogic(float InDeltaTime)
    {
        if (_hasExecuted)
        {
            status = BTNodeResult.Success;
            return true;
        }

        if (CommandRequest == null)
        {
            LoggingService.LogError($"❌ ExeAction: No command request for '{InstanceName}'");
            status = BTNodeResult.Failure;
            return false;
        }

        // Resolve inputs from ML action objects into the command request
        ResolveInputs();

        // Send the robot command
        var communicator = new RestRobotCommandCommunicator(FlaskBaseUrl);
        LoggingService.LogInfo($"🚀 ExeAction: Sending command for '{InstanceName}' — {CommandRequest.CommandType} → {CommandRequest.FinalPosition}");

        try
        {
            var commandResult = Task.Run(async () => await communicator.SendCommandAsync(CommandRequest)).Result;

            if (commandResult.Success)
            {
                LoggingService.LogSuccess($"✅ ExeAction: Command succeeded for '{InstanceName}'");
                _hasExecuted = true;
                status = BTNodeResult.Success;
                return true;
            }
            else
            {
                LoggingService.LogError($"❌ ExeAction: Command failed for '{InstanceName}': {commandResult.Error}");
                status = BTNodeResult.Failure;
                return false;
            }
        }
        catch (Exception ex)
        {
            LoggingService.LogError($"❌ ExeAction: Exception sending command for '{InstanceName}': {ex.Message}");
            status = BTNodeResult.Failure;
            return false;
        }
    }

    /// <summary>
    /// Resolves data from the parent ML action's actual objects into the command request.
    /// Looks through MLInputs for RobotPosition (joints/pose), Robot (IP), etc.
    /// Falls back to blackboard lookup if MLInputs is not set.
    /// </summary>
    private void ResolveInputs()
    {
        if (MLInputs == null || MLInputs.Count == 0)
        {
            LoggingService.LogInfo($"ℹ️ ExeAction: No ML inputs for '{InstanceName}', skipping resolution");
            return;
        }

        foreach (var kv in MLInputs)
        {
            switch (kv.Value)
            {
                case RobotPosition rp:
                    if (kv.Key.Equals("target", StringComparison.OrdinalIgnoreCase))
                    {
                        if (rp.Joints != null)
                            CommandRequest.Joints = rp.Joints.Values;

                        if (rp.TcpPose != null && rp.TcpOrinetation != null)
                            CommandRequest.Pose = new[]
                            {
                                rp.TcpPose.X, rp.TcpPose.Y, rp.TcpPose.Z,
                                rp.TcpOrinetation.X, rp.TcpOrinetation.Y, rp.TcpOrinetation.Z
                            };

                        ResolvedPosition = rp;
                        LoggingService.LogInfo($"✅ ExeAction: Resolved target '{rp.ID}' → Joints={rp.Joints}, TcpPose={rp.TcpPose}");
                    }
                    break;

                case Location loc:
                    LoggingService.LogInfo($"ℹ️ ExeAction: ML input '{kv.Key}' is Location '{loc.ID}'");
                    break;

                default:
                    if (kv.Value is CustomProperty cp)
                        LoggingService.LogInfo($"ℹ️ ExeAction: ML input '{kv.Key}' = {cp.GetType().Name} '{cp.ID}'");
                    break;
            }
        }
    }

    /// <summary>
    /// After the LL action finishes, shows a blocking dialog asking the operator
    /// whether the action executed successfully.
    /// </summary>
    protected override void OnExit()
    {
        var actionName = GetType().Name;
        var statusBefore = status;

        var message = $"LL action completed:\n\n" +
                      $"  Action:   {actionName}\n" +
                      $"  Instance: {InstanceName}\n" +
                      $"  Status:   {statusBefore}\n\n" +
                      $"Did the action execute successfully?\n" +
                      $"Press OK to proceed, Cancel to mark as FAILED.";

        LoggingService.LogInfo($"⏸️ ExeAction: Waiting for operator verification for '{InstanceName}' ({actionName}), status={statusBefore}");

        int result = MessageBoxW(IntPtr.Zero, message, $"Verify: {actionName}", MB_OKCANCEL | MB_ICONWARNING | MB_TOPMOST);

        if (result != IDOK)
        {
            LoggingService.LogWarning($"🛑 ExeAction: Operator marked '{InstanceName}' ({actionName}) as FAILED");
            status = BTNodeResult.Failure;
        }
        else
        {
            LoggingService.LogSuccess($"▶️ ExeAction: Operator verified '{InstanceName}' ({actionName}) — proceeding");
        }

        base.OnExit();
    }

    /// <summary>
    /// Build the command-specific request. Each subclass defines what data to send.
    /// </summary>
    protected abstract RobotCommandRequest BuildCommandRequest();
}
