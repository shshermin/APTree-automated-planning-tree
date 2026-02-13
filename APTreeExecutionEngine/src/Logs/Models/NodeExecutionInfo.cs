using System;

namespace BehaviorTreeMainProject.Log
{
    /// <summary>
    /// Information about node execution for tracking purposes
    /// </summary>
    public class NodeExecutionInfo
    {
        public string NodeName { get; set; }
        public string NodeType { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan CompletionTime { get; set; }
        public bool Success { get; set; }
        public bool Completed { get; set; }
        public string Status { get; set; }

        public NodeExecutionInfo(string nodeName, string nodeType, DateTime startTime)
        {
            NodeName = nodeName;
            NodeType = nodeType;
            StartTime = startTime;
            Completed = false;
            Status = "Running";
        }

        public void Complete(DateTime endTime, bool success)
        {
            EndTime = endTime;
            CompletionTime = endTime - StartTime;
            Success = success;
            Completed = true;
            Status = success ? "Success" : "Failed";
        }

        public void UpdateStatus(string status)
        {
            Status = status;
        }
    }
}
