namespace AIPlanning
{
    // Response classes for receiving from external planner
    public class PlanningResult
    {
        public bool Success { get; set; }
        public string Plan { get; set; } // Plan as string (like NodeGraph format)
        public string Error { get; set; } // Error as string
        public double PlanningTimeSeconds { get; set; }
        public int PlanLength { get; set; }
        public string PlannerUsed { get; set; }
    }
}
