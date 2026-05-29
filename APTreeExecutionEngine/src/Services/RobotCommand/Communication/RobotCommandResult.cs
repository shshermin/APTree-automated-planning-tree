namespace RobotCommand
{
    public class RobotCommandStep
    {
        public string Name { get; set; } = "";
        public double DurationSec { get; set; }
        public double PlanningSec { get; set; }
        public int PointCount { get; set; }
        public double NominalSec { get; set; }
    }

    public class RobotCommandResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string Error { get; set; }
        public double ExecutionTimeSeconds { get; set; }

        /// <summary>Time spent in motion planning (MoveIt) for planned moves. 0 for direct URScript moves.</summary>
        public double PlanningTimeSeconds { get; set; }

        /// <summary>Number of trajectory points in the planned motion. 0 for direct URScript moves.</summary>
        public int PointCount { get; set; }

        /// <summary>Nominal trajectory duration as computed by the planner (seconds). 0 for direct URScript moves.</summary>
        public double NominalDurationSeconds { get; set; }

        /// <summary>Sub-steps for compound endpoints (nail_and_retract, stack_release). Null for single-step moves.</summary>
        public System.Collections.Generic.List<RobotCommandStep>? Steps { get; set; }
    }
}
