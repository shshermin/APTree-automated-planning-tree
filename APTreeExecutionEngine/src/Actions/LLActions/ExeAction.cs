using System;
using System.Runtime.InteropServices;
using ModelLoader.ParameterTypes;
using BehaviorTreeMainProject.Log.Services;
using RobotCommand;

/// <summary>
/// Abstract base for all executable (LL) action nodes that interact with the robot.
/// Inherits PActionNode and provides common infrastructure: robot IP, Flask URL,
/// empty PDDL states, ServiceInputProvider (resolves position names from blackboard),
/// and ServiceRobotCommand attachment.
/// Concrete subclasses (MoveToLL, GripLL, etc.) supply their specific command request.
/// </summary>
public abstract class ExeAction : PActionNode
{
    protected static readonly string DEFAULT_FLASK_URL = "http://localhost:5001";
    protected static readonly string DEFAULT_ROBOT_IP = "192.168.1.100";

    // Win32 MessageBox via P/Invoke — works on any .NET Windows app without WinForms
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);

    // MB_OKCANCEL = 0x01, MB_ICONWARNING = 0x30, MB_TOPMOST = 0x40000
    private const uint MB_OKCANCEL = 0x00000001;
    private const uint MB_ICONWARNING = 0x00000030;
    private const uint MB_TOPMOST = 0x00040000;
    private const int IDOK = 1;

    public string RobotIp { get; }
    public string FlaskBaseUrl { get; }

    /// <summary>Shared reference to the command request — updated by ServiceInputProvider before sending.</summary>
    public RobotCommandRequest CommandRequest { get; protected set; }

    /// <summary>Populated by ServiceInputProvider after resolving the position from the blackboard.</summary>
    public RobotPosition ResolvedPosition { get; set; }

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
    /// After the LL action finishes, shows a blocking dialog asking the operator
    /// whether the action executed successfully. Only proceeds on OK; Cancel
    /// overrides the status to Failure.
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

    /// <summary>
    /// Creates a ServiceInputProvider (resolves position names → actual joint/pose data
    /// from the blackboard) and a ServiceRobotCommand, then attaches both to this node.
    /// ServiceInputProvider is added first so it ticks before the command is sent.
    /// Call this at the end of the subclass constructor.
    /// </summary>
    protected void InitializeRobotCommandService()
    {
        try
        {
            CommandRequest = BuildCommandRequest();

            // ServiceInputProvider resolves position names → actual joint/pose data.
            // Must be added BEFORE ServiceRobotCommand so it ticks first.
            AddService(new ServiceInputProvider(this), false);

            var communicator = new RestRobotCommandCommunicator(FlaskBaseUrl);
            var service = new ServiceRobotCommand(communicator, CommandRequest);
            AddService(service, false);

            LoggingService.LogSuccess($"✅ ExeAction: Attached ServiceInputProvider + ServiceRobotCommand to '{InstanceName}'");
        }
        catch (Exception ex)
        {
            LoggingService.LogError($"❌ ExeAction: Failed to attach services to '{InstanceName}': {ex.Message}");
        }
    }
}
