namespace AIPlanning
{
    /// <summary>Outcome of a planner run, as reported by the planner service (`status`).</summary>
    public enum PlanStatus
    {
        Solved,
        Unsolvable,
        Error
    }

    // Response classes for receiving from external planner
    public class PlanningResult
    {
        public bool Success { get; set; }
        public string Plan { get; set; } // Plan as string (like NodeGraph format)
        public string Error { get; set; } // Error as string
        public double PlanningTimeSeconds { get; set; }
        public int PlanLength { get; set; }
        public string PlannerUsed { get; set; }

        /// <summary>Null when the service sent no status (older service versions).</summary>
        public PlanStatus? Status { get; set; }

        /// <summary>The reported status, or one derived from Success when none was sent.</summary>
        public PlanStatus EffectiveStatus => Status ?? (Success ? PlanStatus.Solved : PlanStatus.Error);
    }
}
