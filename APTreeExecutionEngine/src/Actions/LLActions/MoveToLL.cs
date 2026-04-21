using System;
using System.Collections.Generic;
using ModelLoader.ParameterTypes;
using BehaviorTreeMainProject.Log.Services;
using RobotCommand;

/// <summary>
/// Low-level move action. Inherits ExeAction.
/// Takes an initial and final position name. The DecoratorLLInputResolver
/// resolves typed properties from the parent ML action before the first tick,
/// then BuildCommandRequest() maps them to the robot command.
/// </summary>
public class MoveToLL : ExeAction, ILLInputBindable
{
    public string InitialPosition { get; }
    public string FinalPosition { get; }
    public double Velocity { get; }
    public double Acceleration { get; }
    public MoveType MoveType { get; }

    // Typed properties resolved by DecoratorLLInputResolver
    public RobotPosition TargetPosition { get; set; }
    public Location TargetLocation { get; set; }

    public MoveToLL(
        string instanceName,
        string initialPosition,
        string finalPosition,
        Blackboard<FastName> blackboard,
        MoveType moveType = MoveType.MoveJ,
        string flaskBaseUrl = null,
        string robotIp = null,
        double velocity = 0.3,
        double acceleration = 0.3,
        IRobotCommandCommunicator communicator = null
    ) : base("MoveToLL", instanceName, blackboard, flaskBaseUrl, robotIp, communicator)
    {
        InitialPosition = initialPosition;
        FinalPosition = finalPosition;
        Velocity = velocity;
        Acceleration = acceleration;
        MoveType = moveType;

        LoggingService.LogInfo($"🤖 MoveToLL: Created '{instanceName}' — {initialPosition} → {finalPosition}");
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
            CommandType = MoveType.ToString().ToLower(),
            InitialPosition = InitialPosition,
            FinalPosition = FinalPosition,
            RobotIp = RobotIp,
            Velocity = Velocity,
            Acceleration = Acceleration
        };

        // Populate joints/pose from resolved typed properties
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
            ResolvedPosition = TargetPosition;
        }
        else if (TargetLocation is InitialLocation il && il.Position != null)
        {
            var ori = GetManipulateOrientationFromBlackboard();
            request.Pose = new[] { il.Position.X, il.Position.Y, il.Position.Z, ori[0], ori[1], ori[2] };
        }
        else if (TargetLocation is FinalLocation fl && fl.Position != null)
        {
            double[] ori;
            if (fl.Orientation != null)
            {
                double theta = Math.Atan2(fl.Orientation.Y, fl.Orientation.X) - Math.PI / 2.0;
                double halfTheta = theta / 2.0;
                ori = new[] { Math.PI * Math.Cos(halfTheta), Math.PI * Math.Sin(halfTheta), 0.0 };
            }
            else
            {
                ori = GetManipulateOrientationFromBlackboard();
            }
            request.Pose = new[] { fl.Position.X, fl.Position.Y, fl.Position.Z, ori[0], ori[1], ori[2] };
        }

        return request;
    }
}
