using System;
using System.Collections.Generic;
using ModelLoader.ParameterTypes;
using BehaviorTreeMainProject.Log.Services;
using RobotCommand;

/// <summary>
/// Low-level deequip tool action. Calls /play_program with the appropriate .urp
/// program based on the tool type (Gripper → deequipdemo.urp, NailGripper → deequipnaildemo.urp).
/// </summary>
public class DeequipToolLL : ExeAction, ILLInputBindable
{
    // Typed property resolved by DecoratorLLInputResolver
    public Tool EquippedTool { get; set; }

    public DeequipToolLL(
        string instanceName,
        Blackboard<FastName> blackboard,
        string flaskBaseUrl = null,
        string robotIp = null,
        IRobotCommandCommunicator communicator = null
    ) : base("DeequipToolLL", instanceName, blackboard, flaskBaseUrl, robotIp, communicator)
    {
        LoggingService.LogInfo($"🔧 DeequipToolLL: Created '{instanceName}'");
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
        var programName = isNailTool ? "deequipnaildemo.urp" : "deequipdemo.urp";
        LoggingService.LogInfo($"🔧 DeequipToolLL: Resolved program '{programName}' for '{InstanceName}'");

        return new RobotCommandRequest
        {
            Endpoint = "/play_program",
            CommandType = "play_program",
            ProgramName = programName,
            Speed = 10,
            RobotIp = RobotIp,
            Payload = 0.5  // Reset to default after removing tool
        };
    }
}
