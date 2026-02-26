namespace AIPlanning
{
    public class PDDLPlanningRequest : IPlanningRequest
    {
        public string PlanningType => "PDDL";
        public string DomainFile { get; set; }
        public string ProblemFile { get; set; }
        public string ProblemFileContent { get; set; }  // Inline content so the VM doesn't need to read a local path
        public string PlannerPath { get; set; }
        public string PlannerName { get; set; } = "ENHSP";  // New: planner selection
        public int TimeoutSeconds { get; set; } = 30;
        public int MaxPlanLength { get; set; } = 20;
        
        // New constructor with planner selection
        public PDDLPlanningRequest(string InDomainFile, string InProblemFile, string InPlannerPath, string InPlannerName, int InTimeoutSeconds = 30, int InMaxPlanLength = 20)
        {
            DomainFile = InDomainFile;
            ProblemFile = InProblemFile;
            PlannerPath = InPlannerPath;
            PlannerName = InPlannerName;
            TimeoutSeconds = InTimeoutSeconds;
            MaxPlanLength = InMaxPlanLength;
        }
    }
}
