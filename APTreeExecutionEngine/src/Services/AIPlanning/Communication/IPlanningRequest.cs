namespace AIPlanning
{
    // Base interface for all planning requests
    public interface IPlanningRequest
    {
        string PlanningType { get; }
    }
}
