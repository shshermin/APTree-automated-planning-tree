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
        double velocity = 1.0,
        double acceleration = 1.0,
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
            LoggingService.LogInfo($"🧭 MoveToLL[{InstanceName}] InitialLocation: pos=({il.Position.X:F4},{il.Position.Y:F4},{il.Position.Z:F4}) ori(rppickup)=({ori[0]:F4},{ori[1]:F4},{ori[2]:F4}) moveType={MoveType}");
        }
        else if (TargetLocation is FinalLocation fl && fl.Position != null)
        {
            double[] ori;
            string oriSrc;
            if (fl.Orientation != null)
            {
                // Convert 2D stick direction to gripper yaw in URScript rotvec.
                // + π/2 corrects for MoveIt's base_link frame being 90° rotated
                // relative to URScript's base frame.
                double theta = Math.Atan2(fl.Orientation.Y, fl.Orientation.X) + Math.PI / 2.0;
                double halfTheta = theta / 2.0;
                ori = new[] { Math.PI * Math.Cos(halfTheta), Math.PI * Math.Sin(halfTheta), 0.0 };
                oriSrc = $"fl.Orientation=({fl.Orientation.X:F4},{fl.Orientation.Y:F4}) theta={theta:F4}rad";
            }
            else
            {
                ori = GetManipulateOrientationFromBlackboard();
                oriSrc = "fl.Orientation=null — using rppickup";
            }
            request.Pose = new[] { fl.Position.X, fl.Position.Y, fl.Position.Z, ori[0], ori[1], ori[2] };
            LoggingService.LogInfo($"🧭 MoveToLL[{InstanceName}] FinalLocation: pos=({fl.Position.X:F4},{fl.Position.Y:F4},{fl.Position.Z:F4}) ori=({ori[0]:F4},{ori[1]:F4},{ori[2]:F4}) [{oriSrc}] moveType={MoveType}");
        }

        return request;
    }
}
