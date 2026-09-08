using ModelLoader.ParameterTypes;
using ModelLoader.PredicateTypes;

namespace BehaviorTreeMainProject.Tests;

/// <summary>
/// Small, reusable object graph for predicate-store tests: two locations,
/// two elements, and two robots (names chosen so one is a string-prefix of
/// the other - "r1" / "r10" - which is exactly the case that exposes the
/// CleanupAtAgentPredicates divergence documented in PredicateStoreContractTests).
///
/// IMPORTANT: every object below is built with the parameterless constructor
/// plus an object initializer for NameKey, never the "(string name, ...)"
/// constructor overloads. Agent/Element/Location each redeclare their own
/// NameKey property, which HIDES (not overrides) CustomProperty.NameKey -
/// see the "hides inherited member" build warnings. Any "(string name, ...)"
/// constructor sets CustomProperty's NameKey slot via the base-class chain,
/// which is invisible through the derived type's own shadowing property, so
/// e.g. `new Robot("r1", ...)` silently produces a Robot whose NameKey is
/// null. Confirmed by direct reproduction while writing these tests - see
/// the write-up in PredicateStoreContractTests for the full explanation.
/// This is a real bug in the generated property-type classes, not a test
/// mistake; it's flagged here rather than fixed, since it touches the whole
/// generated Properties/ParameterTypes hierarchy.
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
