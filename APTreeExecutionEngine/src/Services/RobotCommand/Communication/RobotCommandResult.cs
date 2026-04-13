namespace RobotCommand
{
    public class RobotCommandResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string Error { get; set; }
        public double ExecutionTimeSeconds { get; set; }

        /// <summary>Time spent in motion planning (MoveIt) for planned moves. 0 for direct URScript moves.</summary>
        public double PlanningTimeSeconds { get; set; }
    }
}
