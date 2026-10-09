using System.Linq;
using ModelLoader.PredicateTypes;
using Xunit;

namespace BehaviorTreeMainProject.Tests;

/// <summary>
/// Regression: the atagent cleanup in SetPredicateSync compared types
/// case-sensitively and never ran, leaving a moved robot at two locations.
/// </summary>
public class BlackboardSetPredicateSyncTests
{
    [Fact]
    public void SetPredicateSync_MovingARobot_ShouldRemoveTheStaleAtAgentPredicate()
    {
        using var blackboard = new Blackboard<FastName>(new DictionaryPredicateStore());
        var robot = PredicateStoreTestFixtures.R1;
        var fp1 = PredicateStoreTestFixtures.Fp1;
        var fp2 = PredicateStoreTestFixtures.Fp2;

        var atOldLocation = new AtAgent(robot, fp1, isNegated: false);
        blackboard.SetPredicateSync(atOldLocation.PredicateName, atOldLocation);

        var atNewLocation = new AtAgent(robot, fp2, isNegated: false);
        blackboard.SetPredicateSync(atNewLocation.PredicateName, atNewLocation);

        var atAgentPredicatesForRobot = blackboard.GetTruePredicates()
            .Where(p => p.PredicateTypeName == "atagent")
            .ToList();

        Assert.Single(atAgentPredicatesForRobot);
    }
}
