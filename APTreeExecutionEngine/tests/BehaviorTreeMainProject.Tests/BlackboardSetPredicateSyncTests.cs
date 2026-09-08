using System.Linq;
using ModelLoader.PredicateTypes;
using Xunit;

namespace BehaviorTreeMainProject.Tests;

/// <summary>
/// Found while writing the predicate-store parity tests, not part of the
/// original test-plan list, but too significant to leave undocumented:
/// Blackboard.SetPredicateSync (Blackboard.cs ~line 552) has:
///
///   if (predicate.GetPredicateType() == "atAgent" && !predicate.not)
///
/// Every generated AtAgent predicate sets PredicateType = new FastName
/// ("atagent") - lowercase - matching every other reference to it in the
/// codebase (WorldStateManager.cs, ServicePDDLPlanning.cs). This one
/// comparison uses "atAgent" (capital A), so it can never match and the
/// "clean up conflicting atAgent predicates when updating location" branch
/// is dead code. Net effect: moving a robot from one location to another
/// via SetPredicateSync leaves the stale atAgent predicate at the old
/// location in place alongside the new one - the world state ends up
/// claiming the robot is in two places at once.
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
