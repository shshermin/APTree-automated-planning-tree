using System.Threading.Tasks;

namespace RobotCommand
{
    /// <summary>
    /// Abstracts how robot commands are sent.
    /// Mirrors IPlannerCommunicator for the robot command layer.
    /// Production: RestRobotCommandCommunicator (HTTP to Flask).
    /// Testing:    swap in a mock implementation.
    /// </summary>
    public interface IRobotCommandCommunicator
    {
        Task<RobotCommandResult> SendCommandAsync(RobotCommandRequest request);
    }
}
