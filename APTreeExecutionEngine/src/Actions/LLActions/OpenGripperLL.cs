using BehaviorTreeMainProject.Log.Services;
using RobotCommand;

/// <summary>
/// Low-level gripper open action. Inherits ExeAction.
/// Sends a tool-digital-output command to the robot via REST API
/// (set TDO0=True to open the gripper).
/// </summary>
public class OpenGripperLL : ExeAction
{
    public OpenGripperLL(
        string instanceName,
        Blackboard<FastName> blackboard,
        string flaskBaseUrl = null,
        string robotIp = null
    ) : base("OpenGripperLL", instanceName, blackboard, flaskBaseUrl, robotIp)
    {
        LoggingService.LogInfo($"🤖 OpenGripperLL: Created '{instanceName}'");
    }

    protected override RobotCommandRequest BuildCommandRequest()
    {
        return new RobotCommandRequest
        {
            Endpoint = "/gripper",
            CommandType = "open_gripper",
            RobotIp = RobotIp
        };
    }
}
