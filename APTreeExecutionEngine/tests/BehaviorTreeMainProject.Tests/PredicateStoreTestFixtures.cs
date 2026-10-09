using ModelLoader.ParameterTypes;
using ModelLoader.PredicateTypes;

namespace BehaviorTreeMainProject.Tests;

/// <summary>
/// Shared objects for predicate-store tests. "r1"/"r10" are chosen so one robot
/// name is a prefix of the other. Objects use object initializers for NameKey:
/// the "(string name, ...)" constructors leave it null, because Agent/Element/
/// Location hide CustomProperty.NameKey instead of overriding it.
/// </summary>
internal static class PredicateStoreTestFixtures
{
    public static FirstPos Fp1 { get; } = new() { NameKey = new FastName("fp1") };
    public static FirstPos Fp2 { get; } = new() { NameKey = new FastName("fp2") };

    public static Beam Beam1 { get; } = new() { NameKey = new FastName("beam1"), Loc = Fp1 };
    public static Beam Beam2 { get; } = new() { NameKey = new FastName("beam2"), Loc = Fp2 };

    public static Robot R1 { get; } = new() { NameKey = new FastName("r1") };
    public static Robot R10 { get; } = new() { NameKey = new FastName("r10") };

    public static AtPlace AtPlace(bool negated = false) => new(Beam1, Fp1, negated);
    public static Holding Holding(bool negated = false) => new(R1, Beam1, negated);
    public static AtAgent AtAgentOf(Robot robot, Location loc, bool negated = false) => new(robot, loc, negated);
}
