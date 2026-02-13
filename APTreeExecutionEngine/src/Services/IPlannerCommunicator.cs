using System.Threading.Tasks;
using PlanningDataStructures;

namespace AIPlanning
{
    public interface IPlannerCommunicator
    {
        Task<PlanningResult> SendPlanningRequestAsync(IPlanningRequest request);
        bool IsAvailable();
        string GetPlannerName();
    }
}
