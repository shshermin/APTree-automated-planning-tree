using System;
using System.Collections.Generic;
using BehaviorTreeMainProject.Log.Services;
using RobotCommand;

/// <summary>
/// Low-level gripper open action. Inherits ExeAction.
/// Sends a tool-digital-output command to the robot via REST API
/// (set TDO0=True to open the gripper).
/// </summary>
public class OpenGripperLL : ExeAction, ILLInputBindable
{
    public OpenGripperLL(
        string instanceName,
        Blackboard<FastName> blackboard,
        string flaskBaseUrl = null,
        string robotIp = null,
        IRobotCommandCommunicator communicator = null
    ) : base("OpenGripperLL", instanceName, blackboard, flaskBaseUrl, robotIp, communicator)
    {
        LoggingService.LogInfo($"🤖 OpenGripperLL: Created '{instanceName}'");
    }

    public void BindInput(object value) { }

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
