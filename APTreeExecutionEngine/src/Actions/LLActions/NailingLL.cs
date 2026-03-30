using System.Collections.Generic;
using ModelLoader.ParameterTypes;
using BehaviorTreeMainProject.Log.Services;
using RobotCommand;

/// <summary>
/// Low-level nailing action. Sends a planned move (MoveIt) to the nail position
/// with end_effector_type = "nailgun". The nail position is derived from the
/// FinalLocation of the Element (obj1) being nailed.
/// </summary>
public class NailingLL : ExeAction
{
    public NailingLL(
        string instanceName,
        Blackboard<FastName> blackboard,
        string flaskBaseUrl = null,
        string robotIp = null
    ) : base("NailingLL", instanceName, blackboard, flaskBaseUrl, robotIp)
    {
        LoggingService.LogInfo($"🔨 NailingLL: Created '{instanceName}'");
    }

    protected override RobotCommandRequest BuildCommandRequest()
    {
        // Find the nail position from an Element's FinalLoc
        double[] pose = ResolveNailPose();

        LoggingService.LogInfo($"🔨 NailingLL: Nail pose=[{string.Join(", ", pose)}] for '{InstanceName}'");

        return new RobotCommandRequest
        {
            Endpoint = "/move",
            CommandType = "planned",
            FinalPosition = ResolveFinalPositionName(),
            RobotIp = RobotIp,
            Velocity = 0.3,
            Acceleration = 0.3,
            Pose = pose,
            EndEffectorType = "nailgun"
        };
    }

    private double[] ResolveNailPose()
    {
        // First try: use the explicit nail coordinate from MLInputs (set by NailingML via NailCoordinates lookup)
        if (MLInputs != null && MLInputs.TryGetValue("coordinate", out var coordObj) && coordObj is Coordinate coord)
        {
            LoggingService.LogInfo($"🔨 NailingLL: Using nail coordinate ({coord.X}, {coord.Y}, {coord.Z})");
            return new[]
            {
                coord.X,
                coord.Y,
                coord.Z,
                0.0, 0.0, 0.0  // nailgun doesn't use orientation/yaw
            };
        }

        // Fallback: use Element's FinalLoc
        if (MLInputs != null)
        {
            foreach (var kv in MLInputs)
            {
                if (kv.Value is Element elem)
                {
                    var finalLoc = GetFinalLocation(elem);
                    if (finalLoc?.Position != null)
                    {
                        LoggingService.LogWarning($"⚠️ NailingLL: No nail coordinate found, falling back to FinalLoc of '{kv.Key}'");
                        return new[]
                        {
                            finalLoc.Position.X,
                            finalLoc.Position.Y,
                            finalLoc.Position.Z,
                            0.0, 0.0, 0.0
                        };
                    }
                }
            }
        }

        LoggingService.LogWarning($"⚠️ NailingLL: Could not resolve nail position for '{InstanceName}', using zero pose");
        return new[] { 0.0, 0.0, 0.0, 0.0, 0.0, 0.0 };
    }

    private FinalLocation GetFinalLocation(Element elem)
    {
        // Stick and Cube both have a FinalLoc property
        var prop = elem.GetType().GetProperty("FinalLoc");
        if (prop != null)
        {
            var loc = prop.GetValue(elem);
            if (loc is FinalLocation fl)
                return fl;
        }
        return null;
    }

    private string ResolveFinalPositionName()
    {
        if (MLInputs != null)
        {
            foreach (var kv in MLInputs)
            {
                if (kv.Value is Element elem)
                {
                    var finalLoc = GetFinalLocation(elem);
                    if (finalLoc != null)
                        return finalLoc.ID ?? "unknown";
                }
            }
        }
        return "unknown";
    }
}
