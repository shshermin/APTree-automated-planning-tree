using System.Collections.Generic;
using ModelLoader.ParameterTypes;
using BehaviorTreeMainProject.Log.Services;
using RobotCommand;

/// <summary>
/// Low-level equip tool action. Calls /play_program with the appropriate .urp
/// program based on the tool type (Gripper → equipdemo.urp, NailGripper → equipdemonailgun.urp).
/// </summary>
public class EquipToolLL : ExeAction
{
    public EquipToolLL(
        string instanceName,
        Blackboard<FastName> blackboard,
        string flaskBaseUrl = null,
        string robotIp = null
    ) : base("EquipToolLL", instanceName, blackboard, flaskBaseUrl, robotIp)
    {
        LoggingService.LogInfo($"🔧 EquipToolLL: Created '{instanceName}'");
    }

    protected override RobotCommandRequest BuildCommandRequest()
    {
        var programName = ResolveProgramName();
        LoggingService.LogInfo($"🔧 EquipToolLL: Resolved program '{programName}' for '{InstanceName}'");

        return new RobotCommandRequest
        {
            Endpoint = "/play_program",
            CommandType = "play_program",
            ProgramName = programName,
            Speed = 10,
            RobotIp = RobotIp,
            Payload = ResolvePayload(),
            PayloadCog = ResolvePayloadCog(),
            Tcp = ResolveTcp()
        };
    }

    private string ResolveProgramName()
    {
        if (MLInputs != null)
        {
            foreach (var kv in MLInputs)
            {
                if (kv.Value is NailGripper or StaplerGun)
                    return "equipdemonailgun.urp";
                if (kv.Value is Gripper)
                    return "equipdemo.urp";
            }
        }

        LoggingService.LogWarning($"⚠️ EquipToolLL: Could not determine tool type for '{InstanceName}', defaulting to equipdemo.urp");
        return "equipdemo.urp";
    }

    private double? ResolvePayload()
    {
        if (MLInputs != null)
        {
            foreach (var kv in MLInputs)
            {
                if (kv.Value is NailGripper or StaplerGun)
                    return 8.0;
                if (kv.Value is Gripper)
                    return 1.0;
            }
        }
        return null;
    }

    private double[] ResolvePayloadCog()
    {
        if (MLInputs != null)
        {
            foreach (var kv in MLInputs)
            {
                if (kv.Value is NailGripper or StaplerGun)
                    return new[] { 0.0, 0.0, 0.12 };  // Cz = 120mm
            }
        }
        return null;
    }

    private double[] ResolveTcp()
    {
        if (MLInputs != null)
        {
            foreach (var kv in MLInputs)
            {
                if (kv.Value is NailGripper or StaplerGun)
                    return new[] { -0.095, 0.0, 0.3165, 0.0, 0.0, 0.0 };
                if (kv.Value is Gripper)
                    return new[] { 0.003, 0.0, 0.148, 0.0, 0.0, 0.0 };
            }
        }
        return null;
    }
}
