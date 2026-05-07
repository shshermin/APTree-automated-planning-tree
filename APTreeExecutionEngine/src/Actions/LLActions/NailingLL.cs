using System;
using System.Collections.Generic;
using System.Linq;
using ModelLoader;
using ModelLoader.ParameterTypes;
using ModelLoader.PredicateTypes;
using BehaviorTreeMainProject.Log.Services;
using RobotCommand;

/// <summary>
/// Low-level nailing action. Sends a planned move (MoveIt) to the nail position
/// with end_effector_type = "nailgun". The nail position is derived from the
/// NailLocation resolved via the Nailed predicate in the goal state.
/// </summary>
public class NailingLL : ExeAction, ILLInputBindable
{
    // Typed properties resolved by DecoratorLLInputResolver
    public NailLocation NailLoc { get; set; }
    public Element Obj { get; set; }
    public Element Base { get; set; }

    public NailingLL(
        string instanceName,
        Blackboard<FastName> blackboard,
        string flaskBaseUrl = null,
        string robotIp = null,
        IRobotCommandCommunicator communicator = null
    ) : base("NailingLL", instanceName, blackboard, flaskBaseUrl, robotIp, communicator)
    {
        LoggingService.LogInfo($"🔨 NailingLL: Created '{instanceName}'");
    }

    public void BindInput(object value)
    {
        if (value is NailLocation nl) NailLoc = nl;
    }

    protected override RobotCommandRequest BuildCommandRequest()
    {
        double[] pose = ResolveNailPose();
        LoggingService.LogInfo($"🔨 NailingLL: Nail pose=[{string.Join(", ", pose)}] for '{InstanceName}'");

        return new RobotCommandRequest
        {
            Endpoint = "/move",
            CommandType = "movej",
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
        // First try: use the NailLocation resolved by the decorator
        if (NailLoc?.Position != null)
        {
            LoggingService.LogInfo($"🔨 NailingLL: Using NailLocation '{NailLoc.ID}' at ({NailLoc.Position.X}, {NailLoc.Position.Y}, {NailLoc.Position.Z})");
            return new[]
            {
                NailLoc.Position.X, NailLoc.Position.Y, NailLoc.Position.Z,
                Math.PI / Math.Sqrt(2), -Math.PI / Math.Sqrt(2), 0.0
            };
        }

        // Fallback: look up the NailLocation from the Nailed predicate in the goal state
        var nailLocFromGoal = ResolveNailLocationFromGoalState();
        if (nailLocFromGoal?.Position != null)
        {
            LoggingService.LogWarning($"⚠️ NailingLL: NailLoc not set by decorator, resolved from goal state: '{nailLocFromGoal.ID}'");
            return new[]
            {
                nailLocFromGoal.Position.X, nailLocFromGoal.Position.Y, nailLocFromGoal.Position.Z,
                Math.PI / Math.Sqrt(2), -Math.PI / Math.Sqrt(2), 0.0
            };
        }

        LoggingService.LogWarning($"⚠️ NailingLL: Could not resolve nail position for '{InstanceName}', using zero pose");
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
