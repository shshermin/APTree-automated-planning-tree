using System.Linq;
using BehaviorTreeMainProject.ModelLoader;
using BehaviorTreeMainProject.Services.FaultInjection;
using ModelLoader.ParameterTypes;
using ModelLoader.PredicateTypes;
using Xunit;

namespace BehaviorTreeMainProject.Tests;

/// <summary>DropAfterClose fault handling; ApplyDrop supports a null mlFlow.</summary>
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

        // SetPredicateSync's atagent cleanup removes the stale entry outright
        // (closed world: absent == false), so "negated" and "gone" both count.
        Assert.True(atOldLoc == null || atOldLoc.not, "robot should no longer be at its old location");
        Assert.NotNull(atNewLoc);
        Assert.False(atNewLoc.not, "robot should now be at the temp location");
        Assert.Single(atAgents, p => !p.not);
    }

    /// <summary>
    /// InitialLocation's (string name, ...) constructor leaves NameKey unset
    /// (Location hides CustomProperty.NameKey), but Blackboard.SetLocation
    /// assigns NameKey, so registered locations end up with the right name.
    /// </summary>
    [Fact]
    public void ApplyDrop_TheNewAtPlace_CorrectlyReferencesTheTempLocation_ThanksToSetLocationsNameKeyPatch()
    {
        var (bb, _, _, _) = SetUpSceneWithDroppedObjectAtInitLoc();
        var manager = new WorldStateManager(bb);

        manager.ApplyDrop(DropEffects(), mlFlow: null);

        // The original atplace(stick4, initloc) is negated, so this is the only true one.
        var newAtPlace = bb.GetAllPredicates().OfType<AtPlace>()
            .First(p => p.obj.NameKey.ToString() == "stick4" && !p.not);

        Assert.Equal("temploc1", newAtPlace.objLoc.NameKey?.ToString());
    }
}
