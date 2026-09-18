using System.Threading.Tasks;
using AIPlanning;

namespace BehaviorTreeMainProject.Tests;

/// <summary>Test double for IPlannerCommunicator - returns a fixed, pre-set
/// PlanningResult (or throws) instead of making a real HTTP call.</summary>
internal class FakePlannerCommunicator : IPlannerCommunicator
{
    private readonly PlanningResult _result;
    public int CallCount { get; private set; }

    public FakePlannerCommunicator(PlanningResult result)
    {
        _result = result;
    }

    public Task<PlanningResult> SendPlanningRequestAsync(IPlanningRequest request)
    {
        CallCount++;
        return Task.FromResult(_result);
    }

    public bool IsAvailable() => true;
    public string GetPlannerName() => "Fake";
}
