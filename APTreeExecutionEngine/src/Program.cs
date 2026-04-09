﻿using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using BehaviorTreeMainProject;

// Run the behavior tree test (disabled — uncomment to run before server starts)
//await FullTreeTest.RunTest();

// To run the JSON BT load test:
//await JsonBTLoadTest.RunTest();

// To run the full behavior tree test:
//await FullTreeTest.RunTest();

// Start the frontend API server (runs in background so mock ticks can run in parallel)
var serverTask = FrontendServer.Run(args);

await Task.Delay(2000); // wait for server to start

// Mock tick generator for frontend testing without a real robot
_ = Task.Run(async () =>
{
    var nodes = new[]
    {
        "Layers1_2",
        "PickUpHL_stick4_initlocstick4_robot1_gripper1",
        "StackHL_stick4_table1_robot1_finallocstick4_gripper1",
        "PickUpHL_stick5_initlocstick5_robot1_gripper1",
        "StackHL_stick5_table1_robot1_finallocstick5_gripper1",
    };
    while (true)
    {
        foreach (var name in nodes)
        {
            BTNode.FireNodeTicked(name, "Running");
            await Task.Delay(800);
            BTNode.FireNodeTicked(name, "Success");
            await Task.Delay(400);
        }
    }
});

await serverTask;
