using System.Linq;
using BehaviorTreeMainProject.ModelLoader;
using BehaviorTreeMainProject.Services.FaultInjection;
using ModelLoader.ParameterTypes;
using ModelLoader.PredicateTypes;
using Xunit;

namespace BehaviorTreeMainProject.Tests;

/// <summary>Test the DropAfterClose fault - the only fault type
/// with a full ApplyX implementation reachable without a live DynamicFlowNode
/// planning hierarchy; mlFlow is passed null here, which ApplyDrop explicitly
/// supports - see its `if (mlFlow != null)` guards).</summary>
public class WorldStateManagerFaultTests
{
    private static (Blackboard<FastName> bb, Beam dropped, Robot robot, FirstPos initLoc) SetUpSceneWithDroppedObjectAtInitLoc()
    {
        var bb = new Blackboard<FastName>(new DictionaryPredicateStore());

        var initLoc = new FirstPos { NameKey = new FastName("initloc") };
        var dropped = new Beam { NameKey = new FastName("stick4"), Loc = initLoc };
        var robot = new Robot { NameKey = new FastName("robot1") };

        bb.SetLocation(initLoc.NameKey, initLoc);
        bb.SetElement(dropped.NameKey, dropped);
        bb.SetAgent(robot.NameKey, robot);

        var atPlace = new AtPlace(dropped, initLoc, isNegated: false);
        bb.SetPredicateSync(atPlace.PredicateName, atPlace);

        var atAgent = new AtAgent(robot, initLoc, isNegated: false);
        bb.SetPredicateSync(atAgent.PredicateName, atAgent);

        return (bb, dropped, robot, initLoc);
    }

    private static FaultEffects DropEffects() => new()
    {
        DroppedObject = "stick4",
        Robot = "robot1",
        Gripper = "gripper1",
        TempLocationName = "temploc1",
        TempLocationPddlType = "firstposition",
        RobotFromLocation = "initloc",
        RobotToLocation = "temploc1"
    };

    [Fact]
    public void ApplyDrop_NegatesTheOldAtPlace_AndFreesTheSourceLocation()
    {
        var (bb, _, _, _) = SetUpSceneWithDroppedObjectAtInitLoc();
        var manager = new WorldStateManager(bb);

        manager.ApplyDrop(DropEffects(), mlFlow: null);

        var oldAtPlace = bb.GetAllPredicates().OfType<AtPlace>()
            .First(p => p.obj.NameKey.ToString() == "stick4" && p.objLoc.NameKey.ToString() == "initloc");
        Assert.True(oldAtPlace.not, "the object's original atplace should be negated after it's dropped");

        var positionFree = bb.GetAllPredicates().OfType<PositionFree>()
            .FirstOrDefault(p => p.loc.NameKey.ToString() == "initloc");
        Assert.NotNull(positionFree);
        Assert.False(positionFree.not, "the vacated source location should become positionfree=true");
    }

    [Fact]
    public void ApplyDrop_MovesTheRobot_FromOldLocationToTempLocation()
    {
        var (bb, _, _, _) = SetUpSceneWithDroppedObjectAtInitLoc();
        var manager = new WorldStateManager(bb);

        manager.ApplyDrop(DropEffects(), mlFlow: null);

        var atAgents = bb.GetAllPredicates().OfType<AtAgent>().Where(p => p.client.NameKey.ToString() == "robot1").ToList();
        var atOldLoc = atAgents.FirstOrDefault(p => p.agentLoc.NameKey.ToString() == "initloc");
        var atNewLoc = atAgents.FirstOrDefault(p => p.agentLoc.NameKey.ToString() == "temploc1");

        Assert.NotNull(atOldLoc);
        Assert.True(atOldLoc.not, "robot should no longer be at its old location");
        Assert.NotNull(atNewLoc);
        Assert.False(atNewLoc.not, "robot should now be at the temp location");
    }

    /// <summary>
    /// A hypothesis worth recording even though it turned out false, since
    /// it clarifies the scope of the Phase 2 NameKey-shadowing finding:
    /// ApplyDrop constructs the temp Location via `new InitialLocation(name,
    /// position, orientation)` - one of the "(string name, ...)" constructors
    /// that PredicateStoreTestFixtures documented as leaving NameKey unset
    /// due to Location's own NameKey hiding CustomProperty.NameKey.
    /// Reasonable to expect the same failure here. It does NOT happen:
    /// verified directly (isolated repro, no Blackboard involved) that
    /// `new InitialLocation("x", ...).NameKey` is indeed null - but ApplyDrop
    /// immediately calls `bb.SetLocation(key, tempLoc)`, and
    /// Blackboard.SetLocation explicitly does `value.NameKey = key` right
    /// after storing (Blackboard.cs ~line 256, same for SetAgent/SetElement).
    /// That assignment's parameter is typed `Location value`, the same
    /// hiding level predicates later read through - so it correctly patches
    /// the exact slot the constructor missed. The constructor bug is real,
    /// but every real construction path found so far runs through one of
    /// these Set* calls, which neutralizes it. The risk is latent, not
    /// active: any future code that constructs one of these property types
    /// and uses it WITHOUT registering it on the blackboard first would hit
    /// the real bug.
    /// </summary>
    [Fact]
    public void ApplyDrop_TheNewAtPlace_CorrectlyReferencesTheTempLocation_ThanksToSetLocationsNameKeyPatch()
    {
        var (bb, _, _, _) = SetUpSceneWithDroppedObjectAtInitLoc();
        var manager = new WorldStateManager(bb);

        manager.ApplyDrop(DropEffects(), mlFlow: null);

        // The original atplace(stick4, initloc) gets negated by the first
        // test above; the freshly-added one (this fault's whole point) is
        // the only non-negated stick4 atplace left.
        var newAtPlace = bb.GetAllPredicates().OfType<AtPlace>()
            .First(p => p.obj.NameKey.ToString() == "stick4" && !p.not);

        Assert.Equal("temploc1", newAtPlace.objLoc.NameKey?.ToString());
    }
}
