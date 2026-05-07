using System;
using System.Collections.Generic;
using BehaviorTreeMainProject.Log.Services;
using RobotCommand;

/// <summary>
/// Low-level push-down action. Moves the TCP 2 mm down (-Z) from the current
/// position using movel (via the robot_service /lift endpoint with a negative height).
/// Used for nailing: after arriving at the nail location the nailgun tip is
/// pressed 2 mm into the surface before firing.
/// </summary>
public class PushDownLL : ExeAction, ILLInputBindable
{
    private const double PUSH_DEPTH_M = -0.002;   // 2 mm down

    public PushDownLL(
        string instanceName,
        Blackboard<FastName> blackboard,
        string flaskBaseUrl = null,
        string robotIp = null,
        IRobotCommandCommunicator communicator = null
    ) : base("PushDownLL", instanceName, blackboard, flaskBaseUrl, robotIp, communicator)
    {
        LoggingService.LogInfo($"🔨 PushDownLL: Created '{instanceName}'");
    }

    public void BindInput(object value) { }

    protected override RobotCommandRequest BuildCommandRequest()
    {
        return new RobotCommandRequest
        {
            Endpoint = "/lift",
            CommandType = "lift",
            Height = PUSH_DEPTH_M,
            RobotIp = RobotIp
        };
    }
}
