using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ModelLoader.ParameterTypes;
using RobotCommand;
using Xunit;

namespace BehaviorTreeMainProject.Tests;

/// <summary>Records every robot command instead of sending it.</summary>
public class FakeRobotCommunicator : IRobotCommandCommunicator
{
    public List<RobotCommandRequest> Requests { get; } = new();
    public Func<RobotCommandRequest, RobotCommandResult> Respond { get; set; } =
        _ => new RobotCommandResult { Success = true };

    public Task<RobotCommandResult> SendCommandAsync(RobotCommandRequest request)
    {
        Requests.Add(request);
        return Task.FromResult(Respond(request));
    }
}

/// <summary>ML action type used only here, so its LL template doesn't collide with other tests.</summary>
public class TemplateTestML : PActionNode
{
    protected override State Preconditions => null;
    protected override State Effects => null;

    public TemplateTestML(string instanceName, Blackboard<FastName> blackboard)
        : base("TemplateTestML", instanceName, blackboard)
    {
    }
}

public class LLSubtreeTemplateTests
{
    private static string TemplateJson(string moveType) => $$"""
        { "llSubtrees": [ { "mlAction": "TemplateTestML", "steps": [
            { "actionType": "MoveToLL", "instanceName": "approach",
              "contParams": { "moveType": "{{moveType}}" }, "paramBindings": { "target": "somewhere" } }
        ] } ] }
        """;

    private static (TemplateTestML action, ServiceLLSubtreeInject service) NewMLAction()
    {
        var blackboard = new Blackboard<FastName>(new DictionaryPredicateStore());
        var tree = new BehaviorTree();
        tree.Initialise(blackboard, "Test");
        blackboard.SetBool(new FastName("ExecutionActive"), true);

        var action = new TemplateTestML("mlAction", blackboard);
        action.SetOwiningTree(tree);
        action.SetTreeForAllServices(tree);
        return (action, action.GetLLSubtreeInjectionService());
    }

    [Theory]
    [InlineData("plannedl", MoveType.PlannedL)]
    [InlineData("PlannedJ", MoveType.PlannedJ)]
    [InlineData("movel", MoveType.MoveL)]
    public void KnownMoveTypes_AreParsed(string text, MoveType expected)
    {
        ServiceLLSubtreeInject.LoadTemplatesFromJson(TemplateJson(text));

        var step = ServiceLLSubtreeInject.GetTemplate("TemplateTestML").Steps.Single();
        Assert.Equal(expected, step.MoveType);
        Assert.Null(step.InvalidMoveType);
    }

    /// <summary>
    /// Regression: an unknown moveType used to fall back to MoveJ, turning a
    /// planned, collision-checked, speed-capped move into a raw movej.
    /// </summary>
    [Fact]
    public void UnknownMoveType_MakesTheMLActionFail_InsteadOfFallingBackToMoveJ()
    {
        ServiceLLSubtreeInject.LoadTemplatesFromJson(TemplateJson("planed_l"));
        var (action, service) = NewMLAction();

        bool result = service.OnEvaluate(0.1f);

        Assert.False(result);
        Assert.False(action.IsHighLevelAction);
        Assert.Null(action.HighLevelSubtree);
    }

    [Fact]
    public void ShippedLLSubtreeTemplates_UseOnlyKnownMoveTypes()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "ModelLoader", "DemonstratorLLSubtrees.json");
        string json = File.ReadAllText(path);

        int count = ServiceLLSubtreeInject.LoadTemplatesFromJson(json);

        Assert.True(count > 0);
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        foreach (var subtree in doc.RootElement.GetProperty("llSubtrees").EnumerateArray())
        {
            string mlAction = subtree.GetProperty("mlAction").GetString()!;
            var invalid = ServiceLLSubtreeInject.GetTemplate(mlAction).Steps.Where(s => s.InvalidMoveType != null).ToList();
            Assert.True(invalid.Count == 0, $"{mlAction}: unknown moveType '{invalid.FirstOrDefault()?.InvalidMoveType}'");
        }
    }
}

public class ExeActionCommandTests
{
    private static (Blackboard<FastName> bb, BehaviorTree tree) NewTree()
    {
        var bb = new Blackboard<FastName>(new DictionaryPredicateStore());
        var tree = new BehaviorTree();
        tree.Initialise(bb, "Test");
        return (bb, tree);
    }

    private static MoveToLL NewMove(BehaviorTree tree, Blackboard<FastName> bb, FakeRobotCommunicator comm, MoveType type = MoveType.PlannedL)
    {
        var move = new MoveToLL("move", "", "target", bb, type, velocity: 0.2, acceleration: 0.3, communicator: comm);
        move.SetOwiningTree(tree);
        return move;
    }

    [Fact]
    public void RobotPositionTarget_SendsJointsAndPoseWithTheRequestedMoveAndSpeed()
    {
        var (bb, tree) = NewTree();
        var comm = new FakeRobotCommunicator();
        var move = NewMove(tree, bb, comm);
        move.BindInput(new RobotPosition
        {
            Joints = new RobotJoints(new[] { 1.0, 2.0, 3.0, 4.0, 5.0, 6.0 }),
            TcpPose = new Coordinate(0.1, 0.2, 0.3),
            TcpOrinetation = new Coordinate(0.0, 3.14, 0.0),
        });

        Assert.Equal(BTNodeResult.Success, move.Tick(0.1f));

        var request = Assert.Single(comm.Requests);
        Assert.Equal("plannedl", request.CommandType);
        Assert.Equal(new[] { 1.0, 2.0, 3.0, 4.0, 5.0, 6.0 }, request.Joints);
        Assert.Equal(new[] { 0.1, 0.2, 0.3, 0.0, 3.14, 0.0 }, request.Pose);
        Assert.Equal(0.2, request.Velocity);
        Assert.Equal(0.3, request.Acceleration);
    }

    [Fact]
    public void InitialLocationTarget_UsesTheLocationPositionWithThePickupOrientation()
    {
        var (bb, tree) = NewTree();
        var pickup = new RobotPosition { TcpOrinetation = new Coordinate(0.5, -3.0, 0.1) };
        bb.SetLocation(new FastName("rppickup"), pickup);
        var comm = new FakeRobotCommunicator();
        var move = NewMove(tree, bb, comm);
        move.BindInput(new InitialLocation { Position = new Coordinate(1.0, 2.0, 3.0) });

        move.Tick(0.1f);

        Assert.Equal(new[] { 1.0, 2.0, 3.0, 0.5, -3.0, 0.1 }, Assert.Single(comm.Requests).Pose);
    }

    [Fact]
    public void FinalLocationOrientation_IsConvertedToAToolDownRotationVector()
    {
        var (bb, tree) = NewTree();
        var comm = new FakeRobotCommunicator();
        var move = NewMove(tree, bb, comm);
        // Stick along +X: theta = atan2(0, 1) + pi/2 = pi/2, rotvec = pi * (cos(pi/4), sin(pi/4), 0)
        move.BindInput(new FinalLocation { Position = new Coordinate(1.0, 2.0, 3.0), Orientation = new Coordinate(1.0, 0.0, 0.0) });

        move.Tick(0.1f);

        var pose = Assert.Single(comm.Requests).Pose;
        Assert.Equal(new[] { 1.0, 2.0, 3.0 }, pose.Take(3));
        Assert.Equal(Math.PI * Math.Cos(Math.PI / 4), pose[3], 9);
        Assert.Equal(Math.PI * Math.Sin(Math.PI / 4), pose[4], 9);
        Assert.Equal(0.0, pose[5], 9);
        // The rotation vector's length must be pi (tool pointing straight down).
        Assert.Equal(Math.PI, Math.Sqrt(pose[3] * pose[3] + pose[4] * pose[4] + pose[5] * pose[5]), 9);
    }

    [Fact]
    public void ACompletedMove_IsNotResentOnLaterTicks()
    {
        var (bb, tree) = NewTree();
        var comm = new FakeRobotCommunicator();
        var move = NewMove(tree, bb, comm);
        move.BindInput(new InitialLocation { Position = new Coordinate(1, 2, 3) });

        move.Tick(0.1f);
        move.Tick(0.1f);
        move.Tick(0.1f);

        Assert.Single(comm.Requests);
    }

    [Fact]
    public void AFailedRobotCommand_FailsTheNode()
    {
        var (bb, tree) = NewTree();
        var comm = new FakeRobotCommunicator { Respond = _ => new RobotCommandResult { Success = false, Error = "protective stop" } };
        var move = NewMove(tree, bb, comm);
        move.BindInput(new InitialLocation { Position = new Coordinate(1, 2, 3) });

        Assert.Equal(BTNodeResult.Failure, move.Tick(0.1f));
    }

    [Fact]
    public void ACommunicationException_FailsTheNode()
    {
        var (bb, tree) = NewTree();
        var comm = new FakeRobotCommunicator { Respond = _ => throw new InvalidOperationException("connection refused") };
        var move = NewMove(tree, bb, comm);
        move.BindInput(new InitialLocation { Position = new Coordinate(1, 2, 3) });

        Assert.Equal(BTNodeResult.Failure, move.Tick(0.1f));
    }

    public static IEnumerable<object[]> KnownTools => new[]
    {
        new object[] { new Gripper(), "equipdemo.urp", 0.95, "gripper" },
        new object[] { new StaplerGun(), "equipdemonailgun.urp", 8.25, "nailgun" },
        new object[] { new NailGripper(), "equipdemonailgun.urp", 8.25, "nailgun" },
    };

    [Theory]
    [MemberData(nameof(KnownTools))]
    public void EquipTool_SetsTheProgramAndPayloadOfTheBoundTool(Tool tool, string program, double payload, string endEffector)
    {
        var (bb, tree) = NewTree();
        var comm = new FakeRobotCommunicator();
        var equip = new EquipToolLL("equip", bb, communicator: comm);
        equip.SetOwiningTree(tree);
        equip.BindInput(tool);

        Assert.Equal(BTNodeResult.Success, equip.Tick(0.1f));

        var request = Assert.Single(comm.Requests);
        Assert.Equal(program, request.ProgramName);
        Assert.Equal(payload, request.Payload);
        Assert.Equal(endEffector, request.EndEffectorType);
    }

    public static IEnumerable<object?[]> ToolsWithoutAProgram => new[]
    {
        new object?[] { null },
        new object?[] { new GlueGun() },
        new object?[] { new VacGripper() },
    };

    /// <summary>
    /// Regression: these used to fall through to the gripper program, setting the
    /// gripper's 0.95 kg payload and TCP whatever tool was actually mounted.
    /// </summary>
    [Theory]
    [MemberData(nameof(ToolsWithoutAProgram))]
    public void EquipAndDeequip_WithoutAKnownProgram_SendNothingAndFail(Tool? tool)
    {
        var (bb, tree) = NewTree();
        var comm = new FakeRobotCommunicator();
        var equip = new EquipToolLL("equip", bb, communicator: comm);
        var deequip = new DeequipToolLL("deequip", bb, communicator: comm);
        equip.SetOwiningTree(tree);
        deequip.SetOwiningTree(tree);
        if (tool != null)
        {
            equip.BindInput(tool);
            deequip.BindInput(tool);
        }

        Assert.Equal(BTNodeResult.Failure, equip.Tick(0.1f));
        Assert.Equal(BTNodeResult.Failure, deequip.Tick(0.1f));
        Assert.Empty(comm.Requests);
    }

    public static IEnumerable<object[]> NailActions => new[]
    {
        new object[] { (Func<Blackboard<FastName>, FakeRobotCommunicator, ExeAction>)((bb, c) => new NailingLL("nail", bb, communicator: c)) },
        new object[] { (Func<Blackboard<FastName>, FakeRobotCommunicator, ExeAction>)((bb, c) => new NailAndRetractLL("nail", bb, communicator: c)) },
    };

    [Theory]
    [MemberData(nameof(NailActions))]
    public void NailActions_SendTheNailLocationPose(Func<Blackboard<FastName>, FakeRobotCommunicator, ExeAction> create)
    {
        var (bb, tree) = NewTree();
        var comm = new FakeRobotCommunicator();
        var nail = create(bb, comm);
        nail.SetOwiningTree(tree);
        ((ILLInputBindable)nail).BindInput(new NailLocation { Position = new Coordinate(0.4, 0.5, 0.2) });

        Assert.Equal(BTNodeResult.Success, nail.Tick(0.1f));

        var pose = Assert.Single(comm.Requests).Pose;
        Assert.Equal(new[] { 0.4, 0.5, 0.2 }, pose.Take(3));
        Assert.Equal("nailgun", comm.Requests[0].EndEffectorType);
    }

    /// <summary>
    /// Regression: an unresolved nail location used to send pose [0,0,0,0,0,0],
    /// i.e. drive the nail gun to the robot base origin.
    /// </summary>
    [Theory]
    [MemberData(nameof(NailActions))]
    public void NailActions_WithoutANailLocation_SendNothingAndFail(Func<Blackboard<FastName>, FakeRobotCommunicator, ExeAction> create)
    {
        var (bb, tree) = NewTree();
        var comm = new FakeRobotCommunicator();
        var nail = create(bb, comm);
        nail.SetOwiningTree(tree);

        Assert.Equal(BTNodeResult.Failure, nail.Tick(0.1f));
        Assert.Empty(comm.Requests);
    }

    [Fact]
    public void AnUnresolvedTarget_SendsNoPoseOrJoints_WhichTheRobotServiceRejectsForMovej()
    {
        // robot_service's /move returns 400 for movej/movel without pose or joints,
        // so an unresolvable target never turns into a motion (see test_robot_service_contract.py).
        var (bb, tree) = NewTree();
        var comm = new FakeRobotCommunicator();
        var move = NewMove(tree, bb, comm, MoveType.MoveJ);

        move.Tick(0.1f);

        var request = Assert.Single(comm.Requests);
        Assert.Null(request.Pose);
        Assert.Null(request.Joints);
    }
}
