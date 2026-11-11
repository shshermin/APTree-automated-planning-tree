using System.Collections.Generic;
using BehaviorTreeMainProject.Services.AIPlanning;
using Xunit;

namespace BehaviorTreeMainProject.Tests;

public class ServiceSubtreeInjectSubtreeConfigTests
{
    [Fact]
    public void CreatePlannerSubtree_ThreadsTheConfiguredPlannerSettingsOntoTheNewPlanner()
    {
        var blackboard = new Blackboard<FastName>(new DictionaryPredicateStore());
        var tree = new BehaviorTree();
        tree.Initialise(blackboard, "Test");
        var hlAction = new TestAction("hlAction", blackboard);

        var service = new ServiceSubtreeInject(hlAction);
        service.SetOwiningTree(tree);

        var config = new ServiceSubtreeInject.SubtreeConfiguration("TestConfig", "Enhsp");
        config.PlannerParameters["domainFile"] = "domain.pddl";
        config.PlannerParameters["problemFile"] = "problem.pddl";
        config.PlannerParameters["plannerPath"] = "enhsp.jar";
        config.PlannerParameters["timeoutSeconds"] = 30;
        config.PlannerParameters["maxPlanLength"] = 20;

        var subtree = service.CreatePlannerSubtree(config, "instance1", new Dictionary<string, object>());

        var planner = Assert.IsType<ServicePDDLPlanning>(subtree.ServicePlanning);
        Assert.Equal("domain.pddl", planner.PlanningRequest.DomainFile);
        Assert.Equal("problem.pddl", planner.PlanningRequest.ProblemFile);
        Assert.Equal(30, planner.PlanningRequest.TimeoutSeconds);
    }
}
