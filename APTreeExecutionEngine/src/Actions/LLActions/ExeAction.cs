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

    // LL actions never have children — short-circuit the inherited PActionNode logic
    public override bool HasChildren => false;
    protected override bool OnTick_Children(float InDeltaTime) => true;

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

        // Debug: log what we're about to send
        LoggingService.LogInfo($"🔍 ExeAction: Pre-send state for '{InstanceName}': " +
            $"Pose={CommandRequest.Pose?.Length ?? 0} elements, " +
            $"Joints={CommandRequest.Joints?.Length ?? 0} elements, " +
            $"MLInputs count={MLInputs?.Count ?? 0}");
        if (MLInputs != null)
            foreach (var kv in MLInputs)
                LoggingService.LogInfo($"   MLInput: '{kv.Key}' = {kv.Value?.GetType().Name ?? "null"} ({kv.Value})");

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
                    if (kv.Key.Equals("target", StringComparison.OrdinalIgnoreCase) && loc is InitialLocation il && il.Position != null)
                    {
                        var ori = GetManipulateOrientation();
                        CommandRequest.Pose = new[] { il.Position.X, il.Position.Y, il.Position.Z, ori[0], ori[1], ori[2] };
                        LoggingService.LogInfo($"✅ ExeAction: Resolved InitialLocation target '{il.ID}' → Pose=[{il.Position.X}, {il.Position.Y}, {il.Position.Z}, {ori[0]}, {ori[1]}, {ori[2]}]");
                    }
                    else if (kv.Key.Equals("target", StringComparison.OrdinalIgnoreCase) && loc is FinalLocation fl && fl.Position != null)
                    {
                        // Convert piece direction vector (dx,dy,0) to UR TCP rotation vector.
                        // Base orientation is Rot(X, π) (tool pointing down). To rotate by θ around Z,
                        // the combined rotation vector is (π·cos(θ/2), π·sin(θ/2), 0)
                        // where θ = atan2(dy, dx).
                        double[] ori;
                        if (fl.Orientation != null)
                        {
                            double theta = Math.Atan2(fl.Orientation.Y, fl.Orientation.X) - Math.PI / 2.0;
                            double halfTheta = theta / 2.0;
                            ori = new[] { Math.PI * Math.Cos(halfTheta), Math.PI * Math.Sin(halfTheta), 0.0 };
                        }
                        else
                        {
                            ori = GetManipulateOrientation();
                        }
                        CommandRequest.Pose = new[] { fl.Position.X, fl.Position.Y, fl.Position.Z, ori[0], ori[1], ori[2] };
                        LoggingService.LogInfo($"✅ ExeAction: Resolved FinalLocation target '{fl.ID}' → Pose=[{fl.Position.X}, {fl.Position.Y}, {fl.Position.Z}, {ori[0]}, {ori[1]}, {ori[2]}] (orientation source: {(fl.Orientation != null ? "FinalLocation (piece dir → TCP)" : "rpmanipulate")})");
                    }
                    else
                    {
                        LoggingService.LogInfo($"ℹ️ ExeAction: ML input '{kv.Key}' is Location '{loc.ID}'");
                    }
                    break;

                default:
                    if (kv.Value is NailGripper or StaplerGun)
                    {
                        CommandRequest.EndEffectorType = "nailgun";
                        LoggingService.LogInfo($"✅ ExeAction: Detected end effector type 'nailgun' from {kv.Value.GetType().Name} param '{kv.Key}'");
                    }
                    else if (kv.Value is Gripper)
                    {
                        CommandRequest.EndEffectorType = "gripper";
                        LoggingService.LogInfo($"✅ ExeAction: Detected end effector type 'gripper' from Gripper param '{kv.Key}'");
                    }
                    else if (kv.Value is Robot robot && robot.Tool != null)
                    {
                        if (robot.Tool is NailGripper or StaplerGun)
                            CommandRequest.EndEffectorType = "nailgun";
                        else if (robot.Tool is Gripper)
                            CommandRequest.EndEffectorType = "gripper";
                        LoggingService.LogInfo($"✅ ExeAction: Detected end effector type '{CommandRequest.EndEffectorType}' from Robot '{robot.ID}'");
                    }
                    else if (kv.Value is CustomProperty cp)
                        LoggingService.LogInfo($"ℹ️ ExeAction: ML input '{kv.Key}' = {cp.GetType().Name} '{cp.ID}'");
                    break;
            }
        }

        // Default to gripper if no end effector type was detected (e.g. StackML only has Robot param without Tool set)
        if (CommandRequest.EndEffectorType == null && CommandRequest.CommandType == "planned")
        {
            CommandRequest.EndEffectorType = "gripper";
            LoggingService.LogInfo($"ℹ️ ExeAction: No end effector detected, defaulting to 'gripper' for planned move");
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

    /// <summary>
    /// Gets the TCP orientation (rx, ry, rz) from rpmanipulate.
    /// Used so that InitialLocation/FinalLocation moves keep the same end-effector orientation.
    /// </summary>
    private double[] GetManipulateOrientation()
    {
        try
        {
            var bb = OwningTree?.linkedBlackboard;
            if (bb != null)
            {
                var manip = bb.GetLocation(new FastName("rpmanipulate")) as RobotPosition;
                if (manip?.TcpOrinetation != null)
                    return new[] { manip.TcpOrinetation.X, manip.TcpOrinetation.Y, manip.TcpOrinetation.Z };
            }
        }
        catch { }
        // Fallback: rpmanipulate orientation from DemonstratorSetupObjects
        return new[] {0.0, -3.14159, 0.0};
    }

    /// <summary>
    /// Converts a piece direction vector (dx, dy) to the equivalent pendant Rz angle in degrees.
    /// theta = atan2(dy, dx) - 90°, i.e. the gripper Z-rotation perpendicular to the stick.
    /// </summary>
    private static double OrientationToDegrees(double dx, double dy)
    {
        double theta = Math.Atan2(dy, dx) - Math.PI / 2.0;
        return theta * (180.0 / Math.PI);
    }
}
