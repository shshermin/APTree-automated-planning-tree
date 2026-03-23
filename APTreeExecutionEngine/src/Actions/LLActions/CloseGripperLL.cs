using BehaviorTreeMainProject.Log.Services;
using RobotCommand;

/// <summary>
/// Low-level gripper close action. Inherits ExeAction.
/// Sends a digital-output sequence to the robot via REST API
/// (set DO1=True, wait 2s, set DO0=True).
/// </summary>
public class CloseGripperLL : ExeAction
{
    public CloseGripperLL(
        string instanceName,
        Blackboard<FastName> blackboard,
        string flaskBaseUrl = null,
        string robotIp = null
    ) : base("CloseGripperLL", instanceName, blackboard, flaskBaseUrl, robotIp)
    {
        LoggingService.LogInfo($"🤖 CloseGripperLL: Created '{instanceName}'");
    }

    protected override RobotCommandRequest BuildCommandRequest()
    {
        return new RobotCommandRequest
        {
            Endpoint = "/gripper",
            CommandType = "close_gripper",
            RobotIp = RobotIp
        };
    }
}
