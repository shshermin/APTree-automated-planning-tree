using BehaviorTreeMainProject.Log.Services;
using RobotCommand;

/// <summary>
/// Low-level move action. Inherits ExeAction.
/// Takes an initial and final position name. ServiceInputProvider (from ExeAction)
/// resolves names to actual joint/pose data from the blackboard, then
/// ServiceRobotCommand sends the move command to the robot via REST API.
/// </summary>
public class MoveToLL : ExeAction
{
    public string InitialPosition { get; }
    public string FinalPosition { get; }
    public double Velocity { get; }
    public double Acceleration { get; }
    public MoveType MoveType { get; }

    public MoveToLL(
        string instanceName,
        string initialPosition,
        string finalPosition,
        Blackboard<FastName> blackboard,
        MoveType moveType = MoveType.MoveJ,
        string flaskBaseUrl = null,
        string robotIp = null,
        double velocity = 0.3,
        double acceleration = 0.3
    ) : base("MoveToLL", instanceName, blackboard, flaskBaseUrl, robotIp)
    {
        InitialPosition = initialPosition;
        FinalPosition = finalPosition;
        Velocity = velocity;
        Acceleration = acceleration;
        MoveType = moveType;

        LoggingService.LogInfo($"🤖 MoveToLL: Created '{instanceName}' — {initialPosition} → {finalPosition}");
    }

    protected override RobotCommandRequest BuildCommandRequest()
    {
        return new RobotCommandRequest
        {
            CommandType = MoveType.ToString().ToLower(),
            InitialPosition = InitialPosition,
            FinalPosition = FinalPosition,
            RobotIp = RobotIp,
            Velocity = Velocity,
            Acceleration = Acceleration
        };
    }
}
