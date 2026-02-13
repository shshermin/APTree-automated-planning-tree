using System;

namespace BehaviorTreeMainProject.Log
{
    /// <summary>
    /// Information about planning service execution for tracking purposes
    /// </summary>
    public class PlanningServiceInfo
    {
        public string ServiceName { get; set; }
        public string PlannerType { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan PlanningTime { get; set; }
        public bool Success { get; set; }
        public int ActionsGenerated { get; set; }
        public bool Completed { get; set; }
        public string Status { get; set; }

        public PlanningServiceInfo(string serviceName, string plannerType, DateTime startTime, int actionsGenerated = 0)
        {
            ServiceName = serviceName;
            PlannerType = plannerType;
            StartTime = startTime;
            ActionsGenerated = actionsGenerated;
            Completed = false;
            Status = "Running";
        }

        public void Complete(DateTime endTime, bool success)
        {
            EndTime = endTime;
            PlanningTime = endTime - StartTime;
            Success = success;
            Completed = true;
            Status = success ? "Success" : "Failed";
        }

        public void UpdateStatus(string status)
        {
            Status = status;
        }

        public void SetActionsGenerated(int count)
        {
            ActionsGenerated = count;
        }
    }
}
