using System;
using System.Collections.Generic;
using ModelLoader.ParameterTypes;
using BehaviorTreeMainProject.Log.Services;
using RobotCommand;

/// <summary>
/// Low-level equip tool action. Calls /play_program with the appropriate .urp
/// program based on the tool type (Gripper → equipdemo.urp, NailGripper → equipdemonailgun.urp).
/// </summary>
public class EquipToolLL : ExeAction, ILLInputBindable
{
    // Typed property resolved by DecoratorLLInputResolver
    public Tool EquippedTool { get; set; }

    public EquipToolLL(
        string instanceName,
        Blackboard<FastName> blackboard,
        string flaskBaseUrl = null,
        string robotIp = null,
        IRobotCommandCommunicator communicator = null
    ) : base("EquipToolLL", instanceName, blackboard, flaskBaseUrl, robotIp, communicator)
    {
        LoggingService.LogInfo($"🔧 EquipToolLL: Created '{instanceName}'");
    }

    public void BindInput(object value)
    {
        switch (value)
        {
            case Tool t:  EquippedTool = t; break;
            case Robot r when r.Tool != null: EquippedTool ??= r.Tool; break;
        }
    }

    protected override RobotCommandRequest BuildCommandRequest()
    {
        bool isNailTool = EquippedTool is NailGripper or StaplerGun;
        var programName = isNailTool ? "equipdemonailgun.urp" : "equipdemo.urp";
        LoggingService.LogInfo($"🔧 EquipToolLL: Resolved program '{programName}' for '{InstanceName}'");

        return new RobotCommandRequest
        {
            Endpoint = "/play_program",
            CommandType = "play_program",
            ProgramName = programName,
            Speed = 10,
            RobotIp = RobotIp,
            Payload = isNailTool ? 8.25 : 0.95,
            PayloadCog = isNailTool
                ? new[] { -0.013, 0.001, 0.151 }
                : new[] { -0.001, 0.015, 0.028 },
            Tcp = isNailTool
                ? new[] { -0.09515, -0.00026, 0.3165, 0.0, 0.0, 0.0 }
                : new[] { 0.00723, 0.00095, 0.148, 0.0, 0.0, 0.0 },
            EndEffectorType = isNailTool ? "nailgun" : "gripper"
        };
    }
}
