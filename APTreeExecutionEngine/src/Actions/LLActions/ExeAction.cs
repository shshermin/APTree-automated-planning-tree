using System;
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

    /// <summary>Shared default communicator — avoids creating a new HttpClient per tick.</summary>
    private static readonly IRobotCommandCommunicator _defaultCommunicator =
        new RestRobotCommandCommunicator(DEFAULT_FLASK_URL);

    /// <summary>The communicator used to send robot commands. Injected or shared default.</summary>
    protected readonly IRobotCommandCommunicator _communicator;

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

    private bool _hasExecuted = false;

    public override void Reset()
    {
        base.Reset();
        _hasExecuted = false;
    }

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
        string robotIp = null,
        IRobotCommandCommunicator communicator = null
    ) : base(actionType, instanceName, blackboard)
    {
        FlaskBaseUrl = flaskBaseUrl ?? DEFAULT_FLASK_URL;
        RobotIp = robotIp ?? DEFAULT_ROBOT_IP;
        _communicator = communicator ?? _defaultCommunicator;

        _emptyPreconditions = new State(StateType.Precondition, new FastName($"{instanceName}_pre"));
        _emptyEffects = new State(StateType.Effect, new FastName($"{instanceName}_eff"));
    }

    protected override void OnEnter()
    {
        base.OnEnter();

        // Build the command request on enter so subclass data is available
        CommandRequest = BuildCommandRequest();

        var actionName = GetType().Name;
        LoggingService.LogInfo($"▶️ ExeAction: Entering '{InstanceName}' ({actionName})");
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

        // Debug: log what we're about to send
        LoggingService.LogInfo($"🔍 ExeAction: Pre-send state for '{InstanceName}': " +
            $"Pose={CommandRequest.Pose?.Length ?? 0} elements, " +
            $"Joints={CommandRequest.Joints?.Length ?? 0} elements");

        // Send the robot command via the injected communicator
        LoggingService.LogInfo($"🚀 ExeAction: Sending command for '{InstanceName}' — {CommandRequest.CommandType} → {CommandRequest.FinalPosition}");

        // Log command start for paper metrics
        var cmdId = RobotCommandLogger.LogCommandStart(
            llActionType: GetType().Name,
            instanceName: InstanceName.ToString(),
            commandType: CommandRequest.CommandType ?? "",
            targetPosition: CommandRequest.FinalPosition ?? "",
            pose: CommandRequest.Pose,
            joints: CommandRequest.Joints,
            endEffectorType: CommandRequest.EndEffectorType,
            velocity: CommandRequest.Velocity,
            acceleration: CommandRequest.Acceleration,
            parentMLAction: GetParentMLActionName());

        try
        {
            var commandResult = Task.Run(async () => await _communicator.SendCommandAsync(CommandRequest)).Result;

            if (commandResult.Success)
            {
                LoggingService.LogSuccess($"✅ ExeAction: Command succeeded for '{InstanceName}'");
                RobotCommandLogger.LogCommandEnd(cmdId, true, commandResult.ExecutionTimeSeconds, commandResult.PlanningTimeSeconds, pointCount: commandResult.PointCount, nominalDurationSeconds: commandResult.NominalDurationSeconds);

                // For compound endpoints (nail_and_retract, stack_release), Python returns
                // per-step timings. Emit one sub-step record per step so internal gaps and
                // planning details are visible in the per-command CSV.
                if (commandResult.Steps != null && commandResult.Steps.Count > 0)
                {
                    double offset = 0.0;
                    foreach (var step in commandResult.Steps)
                    {
                        RobotCommandLogger.LogSubStep(
                            parentCommandId: cmdId,
                            stepName: step.Name,
                            durationSec: step.DurationSec,
                            offsetFromParentStartSec: offset,
                            planningSec: step.PlanningSec,
                            pointCount: step.PointCount,
                            nominalSec: step.NominalSec);
                        offset += step.DurationSec;
                    }
                }
                HierarchicalTraceLogger.LogLLStep(GetType().Name, InstanceName.ToString(), CommandRequest.CommandType, CommandRequest.FinalPosition, true, commandResult.ExecutionTimeSeconds * 1000.0);
                NotifyParentMLStep(true);
                _hasExecuted = true;
                status = BTNodeResult.Success;
                return true;
            }
            else
            {
                LoggingService.LogError($"❌ ExeAction: Command failed for '{InstanceName}': {commandResult.Error}");
                RobotCommandLogger.LogCommandFailed(cmdId, commandResult.ExecutionTimeSeconds, commandResult.Error ?? "Unknown");
                HierarchicalTraceLogger.LogLLStep(GetType().Name, InstanceName.ToString(), CommandRequest.CommandType, CommandRequest.FinalPosition, false, commandResult.ExecutionTimeSeconds * 1000.0);
                NotifyParentMLStep(false);
                status = BTNodeResult.Failure;
                return false;
            }
        }
        catch (Exception ex)
        {
            LoggingService.LogError($"❌ ExeAction: Exception sending command for '{InstanceName}': {ex.Message}");
            RobotCommandLogger.LogCommandFailed(cmdId, 0, ex.Message);
            HierarchicalTraceLogger.LogLLStep(GetType().Name, InstanceName.ToString(), CommandRequest.CommandType, CommandRequest.FinalPosition, false, 0);
            NotifyParentMLStep(false);
            status = BTNodeResult.Failure;
            return false;
        }
    }

    protected override void OnExit()
    {
        var actionName = GetType().Name;
        LoggingService.LogInfo($"✅ ExeAction: Exiting '{InstanceName}' ({actionName}), status={status}");
        base.OnExit();
    }

    /// <summary>
    /// Build the command-specific request. Each subclass defines what data to send.
    /// </summary>
    protected abstract RobotCommandRequest BuildCommandRequest();

    /// <summary>
    /// Gets the TCP orientation (rx, ry, rz) from rppickup.
    /// Used so that InitialLocation/FinalLocation moves keep the same end-effector orientation.
    /// Available to subclasses for use in BuildCommandRequest().
    /// </summary>
    protected double[] GetManipulateOrientationFromBlackboard()
    {
        try
        {
            var bb = OwningTree?.linkedBlackboard;
            if (bb != null)
            {
                var pickup = bb.GetLocation(new FastName("rppickup")) as RobotPosition;
                if (pickup?.TcpOrinetation != null)
                    return new[] { pickup.TcpOrinetation.X, pickup.TcpOrinetation.Y, pickup.TcpOrinetation.Z };
            }
        }
        catch { }
        // Fallback: rppickup orientation from DemonstratorSetupObjects
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

    /// <summary>
    /// Notify the parent ML-level PActionNode that an LL step has completed,
    /// so it can track the count for the hierarchical trace summary.
    /// </summary>
    private void NotifyParentMLStep(bool success)
    {
        // Walk up the tree to find the parent ML action (PActionNode that isn't ExeAction)
        var node = this.ParentNode;
        while (node != null)
        {
            if (node is PActionNode pAction && !(node is ExeAction))
            {
                pAction._mlLLStepCount++;
                if (success)
                    pAction._mlLLStepSucceeded++;
                break;
            }
            node = node.ParentNode;
        }
    }

    /// <summary>
    /// Walk up the tree to find the nearest parent ML action name (PActionNode that isn't ExeAction).
    /// </summary>
    private string GetParentMLActionName()
    {
        var node = this.ParentNode;
        while (node != null)
        {
            if (node is PActionNode pAction && !(node is ExeAction))
                return pAction.InstanceName.ToString();
            node = node.ParentNode;
        }
        return "";
    }
}
