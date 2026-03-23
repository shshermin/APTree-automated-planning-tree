namespace RobotCommand
{
    public class RobotCommandResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string Error { get; set; }
        public double ExecutionTimeSeconds { get; set; }
    }
}
