using System.Collections.Generic;
using ModelLoader.ParameterTypes;
using BehaviorTreeMainProject.Log.Services;
using RobotCommand;

/// <summary>
/// Low-level deequip tool action. Calls /play_program with the appropriate .urp
/// program based on the tool type (Gripper → deequipdemo.urp, NailGripper → deequipnaildemo.urp).
/// </summary>
public class DeequipToolLL : ExeAction
{
    public DeequipToolLL(
        string instanceName,
        Blackboard<FastName> blackboard,
        string flaskBaseUrl = null,
        string robotIp = null
    ) : base("DeequipToolLL", instanceName, blackboard, flaskBaseUrl, robotIp)
    {
        LoggingService.LogInfo($"🔧 DeequipToolLL: Created '{instanceName}'");
    }

    protected override RobotCommandRequest BuildCommandRequest()
    {
        var programName = ResolveProgramName();
        LoggingService.LogInfo($"🔧 DeequipToolLL: Resolved program '{programName}' for '{InstanceName}'");

        return new RobotCommandRequest
        {
            Endpoint = "/play_program",
            CommandType = "play_program",
            ProgramName = programName,
            Speed = 10,
            RobotIp = RobotIp,
            Payload = ResolvePayload()
        };
    }

    private string ResolveProgramName()
    {
        if (MLInputs != null)
        {
            foreach (var kv in MLInputs)
            {
                if (kv.Value is NailGripper or StaplerGun)
                    return "deequipnaildemo.urp";
                if (kv.Value is Gripper)
                    return "deequipdemo.urp";
            }
        }

        LoggingService.LogWarning($"⚠️ DeequipToolLL: Could not determine tool type for '{InstanceName}', defaulting to deequipdemo.urp");
        return "deequipdemo.urp";
    }

    private double? ResolvePayload()
    {
        if (MLInputs != null)
        {
            foreach (var kv in MLInputs)
            {
                if (kv.Value is NailGripper or StaplerGun)
                    return 0.5;  // Reset to default after removing nailgun
                if (kv.Value is Gripper)
                    return 0.5;  // Reset to default after removing gripper
            }
        }
        return null;
    }
}
