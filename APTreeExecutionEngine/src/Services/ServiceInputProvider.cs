using System;
using ModelLoader.ParameterTypes;
using BehaviorTreeMainProject.Log.Services;

/// <summary>
/// Service that resolves symbolic position names to actual robot position data
/// from the blackboard. Attaches to every ExeAction and runs BEFORE ServiceRobotCommand
/// so that the command request contains real joint/pose arrays when sent to the Flask service.
///
/// Flow: ExeAction.CommandRequest.FinalPosition ("rpmanipulate")
///       → blackboard.GetLocation("rpmanipulate")
///       → RobotPosition with Joints, TcpPose, TcpOrinetation
///       → updates CommandRequest.Joints / .Pose
/// </summary>
public class ServiceInputProvider : Service
{
    private readonly ExeAction _action;
    private bool _resolved = false;

    public ServiceInputProvider(ExeAction action) : base(null)
    {
        _action = action ?? throw new ArgumentNullException(nameof(action));
    }

    public override bool OnEvaluate(float InDeltaTime)
    {
        if (_resolved) return true;

        var request = _action.CommandRequest;
        if (request == null) return true;

        var positionName = request.FinalPosition;
        if (string.IsNullOrEmpty(positionName) || positionName == "unknown")
            return true;

        try
        {
            var location = linkedBlackboard.GetLocation(new FastName(positionName));
            if (location is RobotPosition rp)
            {
                if (rp.Joints != null)
                    request.Joints = rp.Joints.Values;

                if (rp.TcpPose != null && rp.TcpOrinetation != null)
                    request.Pose = new[]
                    {
                        rp.TcpPose.X, rp.TcpPose.Y, rp.TcpPose.Z,
                        rp.TcpOrinetation.X, rp.TcpOrinetation.Y, rp.TcpOrinetation.Z
                    };

                _action.ResolvedPosition = rp;
                _resolved = true;

                LoggingService.LogInfo(
                    $"✅ ServiceInputProvider: Resolved '{positionName}' → Joints={rp.Joints}, " +
                    $"TcpPose={rp.TcpPose}, TcpOrientation={rp.TcpOrinetation}");
            }
            else
            {
                LoggingService.LogWarning(
                    $"⚠️ ServiceInputProvider: '{positionName}' is not a RobotPosition " +
                    $"(type: {location?.GetType().Name ?? "null"})");
            }
        }
        catch (Exception ex)
        {
            LoggingService.LogWarning(
                $"⚠️ ServiceInputProvider: Could not resolve '{positionName}': {ex.Message}");
        }

        return true;
    }
}
