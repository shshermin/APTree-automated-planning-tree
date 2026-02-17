using System.Threading.Tasks;
namespace AIPlanning
{
    public interface IPlannerCommunicator
    {
        Task<PlanningResult> SendPlanningRequestAsync(IPlanningRequest request);
        bool IsAvailable();
        string GetPlannerName();
    }
}
