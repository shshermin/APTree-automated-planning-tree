using System;
using System.Collections.Generic;
using System.Linq;
using ModelLoader;
using ModelLoader.ParameterTypes;
using ModelLoader.PredicateTypes;
using BehaviorTreeMainProject.Log.Services;
using RobotCommand;

/// <summary>
/// Combined low-level nailing action.
/// Sends a single request to /nail_and_retract which performs:
///   1. movej to the nail position
///   2. push down 2 mm
///   3. lift TCP back up
/// Replaces the three-node sequence (NailingLL → PushDownLL → LiftLL) for
/// NailingML subtrees.
/// Nail pose resolution logic mirrors NailingLL exactly.
/// </summary>
public class NailAndRetractLL : ExeAction, ILLInputBindable
{
    public NailLocation NailLoc { get; set; }
    public Element Obj { get; set; }
    public Element Base { get; set; }

    public NailAndRetractLL(
        string instanceName,
        Blackboard<FastName> blackboard,
        string flaskBaseUrl = null,
        string robotIp = null,
        IRobotCommandCommunicator communicator = null
    ) : base("NailAndRetractLL", instanceName, blackboard, flaskBaseUrl, robotIp, communicator)
    {
        LoggingService.LogInfo($"🔨 NailAndRetractLL: Created '{instanceName}'");
    }

    public void BindInput(object value)
    {
        if (value is NailLocation nl) NailLoc = nl;
    }

    protected override RobotCommandRequest BuildCommandRequest()
    {
        double[] pose = ResolveNailPose();
        LoggingService.LogInfo($"🔨 NailAndRetractLL: Nail pose=[{string.Join(", ", pose)}] for '{InstanceName}'");

        return new RobotCommandRequest
        {
            Endpoint = "/nail_and_retract",
            CommandType = "nail_and_retract",
            FinalPosition = NailLoc?.ID ?? ResolveNailLocationFromGoalState()?.ID ?? "unknown",
            RobotIp = RobotIp,
            Velocity = 0.3,
            Acceleration = 0.3,
            Pose = pose,
            EndEffectorType = "nailgun"
        };
    }

    private double[] ResolveNailPose()
    {
        if (NailLoc?.Position != null)
        {
            LoggingService.LogInfo($"🔨 NailAndRetractLL: Using NailLocation '{NailLoc.ID}' at ({NailLoc.Position.X}, {NailLoc.Position.Y}, {NailLoc.Position.Z})");
            return new[]
            {
                NailLoc.Position.X, NailLoc.Position.Y, NailLoc.Position.Z,
                Math.PI / Math.Sqrt(2), -Math.PI / Math.Sqrt(2), 0.0
            };
        }

        var nailLocFromGoal = ResolveNailLocationFromGoalState();
        if (nailLocFromGoal?.Position != null)
        {
            LoggingService.LogWarning($"⚠️ NailAndRetractLL: NailLoc not set by decorator, resolved from goal state: '{nailLocFromGoal.ID}'");
            return new[]
            {
                nailLocFromGoal.Position.X, nailLocFromGoal.Position.Y, nailLocFromGoal.Position.Z,
                Math.PI / Math.Sqrt(2), -Math.PI / Math.Sqrt(2), 0.0
            };
        }

        LoggingService.LogWarning($"⚠️ NailAndRetractLL: Could not resolve nail position for '{InstanceName}', using zero pose");
        return new[] { 0.0, 0.0, 0.0, 0.0, 0.0, 0.0 };
    }

    private NailLocation ResolveNailLocationFromGoalState()
    {
        if (Obj == null || Base == null) return null;

        var goalPredicates = Blackboard?.GetGoalStatePredicates();
        if (goalPredicates == null) return null;

        var matchingNailed = goalPredicates
            .OfType<Nailed>()
            .FirstOrDefault(n =>
                n.obj1?.NameKey?.ToString()?.Equals(Obj.NameKey?.ToString(), StringComparison.OrdinalIgnoreCase) == true &&
                n.obj2?.NameKey?.ToString()?.Equals(Base.NameKey?.ToString(), StringComparison.OrdinalIgnoreCase) == true);

        return matchingNailed?.nailloc as NailLocation;
    }
}
