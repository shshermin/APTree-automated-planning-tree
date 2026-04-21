﻿using BehaviorTreeMainProject;

// Usage: dotnet run [--server | --test | --loadtest | --run <model.json> [config.json]]
//   --server   : frontend API server only  (default)
//   --test     : DemonstratorTreeTest only
//   --loadtest : JsonBTLoadTest only
//   --run      : Run BT from JSON model + optional config file

var mode = args.FirstOrDefault(a => a.StartsWith("--")) ?? "--server";
var remainingArgs = args.Where(a => !a.StartsWith("--")).ToArray();

switch (mode)
{
    case "--test":
        await DemonstratorTreeTest.RunTest();
        break;
    case "--loadtest":
        await JsonBTLoadTest.RunTest();
        break;
    case "--run":
        var modelPath = remainingArgs.ElementAtOrDefault(0)
            ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "src", "ModelLoader", "BehaviorTreeModel.json");
        var configPath = remainingArgs.ElementAtOrDefault(1);
        await BehaviorTreeRunner.RunFromFiles(modelPath, configPath);
        break;
    default:
        await FrontendServer.Run(remainingArgs);
        break;
}
