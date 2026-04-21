using System;
using System.Collections.Generic;
using BehaviorTreeMainProject.Log.Services;
using RobotCommand;

/// <summary>
/// Low-level lift action. Sends a command to move the TCP 10 cm up (+Z)
/// from the robot's current position using movel.
/// The actual lifting is handled robot-side via URScript that reads the
/// current TCP pose, so no position resolution is needed.
/// </summary>
public class LiftLL : ExeAction, ILLInputBindable
{
    public LiftLL(
        string instanceName,
        Blackboard<FastName> blackboard,
        string flaskBaseUrl = null,
        string robotIp = null,
        IRobotCommandCommunicator communicator = null
    ) : base("LiftLL", instanceName, blackboard, flaskBaseUrl, robotIp, communicator)
    {
        LoggingService.LogInfo($"🤖 LiftLL: Created '{instanceName}'");
    }

    public void BindInput(object value) { }

    protected override RobotCommandRequest BuildCommandRequest()
    {
        return new RobotCommandRequest
        {
            Endpoint = "/lift",
            CommandType = "lift",
            RobotIp = RobotIp
        };
    }
}
