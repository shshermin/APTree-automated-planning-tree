using System.Linq;
using BehaviorTreeMainProject.Graph;
using ModelLoader.ParameterTypes;
using ModelLoader.PredicateTypes;
using Xunit;

namespace BehaviorTreeMainProject.Tests;

/// <summary>Test-plan section 2, #30-31.</summary>
public class PddlExporterTests
{
    [Fact]
    public void GenerateProblem_ProducesSyntacticallyBalancedPddl()
    {
        using var blackboard = new Blackboard<FastName>(new DictionaryPredicateStore());
        var exporter = new PddlExporter(blackboard);

        string pddl = exporter.GenerateProblem("trussml", "empty_problem");

        Assert.StartsWith("(define (problem empty_problem)", pddl);
        Assert.Contains("(:domain trussml)", pddl);
        Assert.Contains("(:objects", pddl);
        Assert.Contains("(:init", pddl);
        Assert.Contains("(:goal", pddl);
        AssertBalancedParentheses(pddl);
    }

    [Fact]
    public void GenerateProblem_RoundTripsKnownObjectsAndPredicates()
    {
        using var blackboard = new Blackboard<FastName>(new DictionaryPredicateStore());
        // Object-initializer construction, not the "(string name, ...)" ctor
        // overloads - see PredicateStoreTestFixtures for why those silently
        // leave NameKey null (Agent/Element/Location each shadow their own
        // NameKey, hiding CustomProperty's).
        var fp1 = new FirstPos { NameKey = new FastName("fp1") };
        var beam1 = new Beam { NameKey = new FastName("beam1"), Loc = fp1 };
        var robot1 = new Robot { NameKey = new FastName("robot1") };

        blackboard.SetLocation(fp1.NameKey, fp1);
        blackboard.SetElement(beam1.NameKey, beam1);
        blackboard.SetAgent(robot1.NameKey, robot1);

        var initPredicate = new AtPlace(beam1, fp1, isNegated: false);
        blackboard.SetPredicateSync(initPredicate.PredicateName, initPredicate);

        var goalPredicate = new Holding(robot1, beam1, isNegated: false);
        blackboard.SetGoalStatePredicate(goalPredicate.PredicateName, goalPredicate);

        string pddl = new PddlExporter(blackboard).GenerateProblem("trussml", "roundtrip_problem");

        // Objects: beam1 grouped under its own type, robot1 under "robot" (see
        // PddlExporter.AgentPddlType), fp1 under "firstpos".
        Assert.Contains("beam1 - beam", pddl);
        Assert.Contains("robot1 - robot", pddl);
        Assert.Contains("fp1 - firstpos", pddl);

        // Init: the non-negated AtPlace predicate, rendered as (atplace beam1 fp1).
        Assert.Contains("(atplace beam1 fp1)", pddl);

        // Goal: wrapped in (:goal (and ...)) and contains the holding predicate.
        int goalIndex = pddl.IndexOf("(:goal", System.StringComparison.Ordinal);
        Assert.True(goalIndex >= 0);
        string goalSection = pddl[goalIndex..];
        Assert.Contains("(holding robot1 beam1)", goalSection);
    }

    [Fact]
    public void GenerateProblem_RendersNegatedPredicates_WrappedInNot()
    {
        using var blackboard = new Blackboard<FastName>(new DictionaryPredicateStore());
        var fp1 = new FirstPos { NameKey = new FastName("fp1") };
        var beam1 = new Beam { NameKey = new FastName("beam1"), Loc = fp1 };
        blackboard.SetLocation(fp1.NameKey, fp1);
        blackboard.SetElement(beam1.NameKey, beam1);

        // GetTruePredicates() only returns non-negated predicates (see
        // Blackboard.GetTruePredicates -> _initStore.AllTrue()), so a negated
        // predicate written directly via Upsert (bypassing SetPredicateSync's
        // negation-update path) should not appear in :init at all - only
        // GenerateGoalBlock's goal-predicate rendering actually emits `(not ...)`.
        var negatedGoal = new AtPlace(beam1, fp1, isNegated: true);
        blackboard.SetGoalStatePredicate(negatedGoal.PredicateName, negatedGoal);

        string pddl = new PddlExporter(blackboard).GenerateProblem("trussml", "negation_problem");

        Assert.Contains("(not (atplace beam1 fp1))", pddl);
    }

    private static void AssertBalancedParentheses(string pddl)
    {
        int depth = 0;
        foreach (char c in pddl)
        {
            if (c == '(') depth++;
            else if (c == ')') depth--;
            Assert.True(depth >= 0, "unbalanced parentheses: found a ')' with no matching '('");
        }
        Assert.Equal(0, depth);
    }
}
