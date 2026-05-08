using System;
using System.Collections.Generic;
using ModelLoader.ParameterTypes;
using BehaviorTreeMainProject.Log.Services;
using RobotCommand;

/// <summary>
/// Combined low-level stack-release action.
/// Sends a single request to /stack_release which performs:
///   1. movej to the stack (place) position
///   2. open gripper
///   3. lift TCP 10 cm up
/// Replaces the three-node sequence (MoveToLL → OpenGripperLL → LiftLL) for
/// StackML and StackOnTwoML subtrees.
/// </summary>
public class StackReleaseLL : ExeAction, ILLInputBindable
{
    public MoveType MoveType { get; }
    public double Velocity { get; }
    public double Acceleration { get; }

    // Resolved from ML action inputs by DecoratorLLInputResolver
    public RobotPosition TargetPosition { get; set; }
    public Location TargetLocation { get; set; }

    public StackReleaseLL(
        string instanceName,
        Blackboard<FastName> blackboard,
        MoveType moveType = MoveType.MoveJ,
        double velocity = 1.0,
        double acceleration = 1.0,
        string flaskBaseUrl = null,
        string robotIp = null,
        IRobotCommandCommunicator communicator = null
    ) : base("StackReleaseLL", instanceName, blackboard, flaskBaseUrl, robotIp, communicator)
    {
        MoveType = moveType;
        Velocity = velocity;
        Acceleration = acceleration;
        LoggingService.LogInfo($"🤖 StackReleaseLL: Created '{instanceName}'");
    }

    public void BindInput(object value)
    {
        switch (value)
        {
            case RobotPosition rp: TargetPosition = rp; break;
            case Location loc:     TargetLocation = loc; break;
        }
    }

    protected override RobotCommandRequest BuildCommandRequest()
    {
        var request = new RobotCommandRequest
        {
            Endpoint = "/stack_release",
            CommandType = "stack_release",
            FinalPosition = "stackpos",
            RobotIp = RobotIp,
            Velocity = Velocity,
            Acceleration = Acceleration
        };

        if (TargetPosition != null)
        {
            if (TargetPosition.Joints != null)
                request.Joints = TargetPosition.Joints.Values;
            if (TargetPosition.TcpPose != null && TargetPosition.TcpOrinetation != null)
                request.Pose = new[]
                {
                    TargetPosition.TcpPose.X, TargetPosition.TcpPose.Y, TargetPosition.TcpPose.Z,
                    TargetPosition.TcpOrinetation.X, TargetPosition.TcpOrinetation.Y, TargetPosition.TcpOrinetation.Z
                };
            request.FinalPosition = TargetPosition.NameKey?.ToString() ?? "stackpos";
            ResolvedPosition = TargetPosition;
        }
        else if (TargetLocation is InitialLocation il && il.Position != null)
        {
            var ori = GetManipulateOrientationFromBlackboard();
            request.Pose = new[] { il.Position.X, il.Position.Y, il.Position.Z, ori[0], ori[1], ori[2] };
            request.FinalPosition = il.NameKey?.ToString() ?? "stackpos";
        }
        else if (TargetLocation is FinalLocation fl && fl.Position != null)
        {
            double[] ori;
            if (fl.Orientation != null)
            {
                double theta = Math.Atan2(fl.Orientation.Y, fl.Orientation.X) + Math.PI / 2.0;
                double halfTheta = theta / 2.0;
                ori = new[] { Math.PI * Math.Cos(halfTheta), Math.PI * Math.Sin(halfTheta), 0.0 };
            }
            else
            {
                ori = GetManipulateOrientationFromBlackboard();
            }
            request.Pose = new[] { fl.Position.X, fl.Position.Y, fl.Position.Z, ori[0], ori[1], ori[2] };
            request.FinalPosition = fl.NameKey?.ToString() ?? "stackpos";
        }

        return request;
    }
}
