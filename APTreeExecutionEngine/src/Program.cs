﻿using BehaviorTreeMainProject;

// Usage: dotnet run [--server | --test | --loadtest | --parityt | --run <model.json> [config.json] [--faults <faults.json>]]
//   --server    : frontend API server only  (default)
//   --test      : DemonstratorTreeTest only
//   --loadtest  : JsonBTLoadTest only
//   --parity    : PredicateStoreParityTest — diffs Dictionary vs. Sqlite store
//   --run       : Run BT from JSON model + optional config file
//   --faults    : Optional fault-injection config file

var mode = args.FirstOrDefault(a => a.StartsWith("--") && a != "--faults") ?? "--server";

// Extract --faults <path> if present
string faultsPath = null;
int faultsIdx = Array.IndexOf(args, "--faults");
if (faultsIdx >= 0 && faultsIdx + 1 < args.Length)
    faultsPath = args[faultsIdx + 1];

var remainingArgs = args
    .Where((a, i) => !a.StartsWith("--") && !(faultsIdx >= 0 && (i == faultsIdx || i == faultsIdx + 1)))
    .ToArray();

switch (mode)
{
    case "--test":
        await DemonstratorTreeTest.RunTest();
        break;
    case "--loadtest":
        await JsonBTLoadTest.RunTest();
        break;
    case "--parity":
        BehaviorTreeMainProject.Tests.PredicateStoreParityTest.Run();
        break;
    case "--run":
        var modelPath = remainingArgs.ElementAtOrDefault(0)
            ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "src", "ModelLoader", "BehaviorTreeModel.json");
        var configPath = remainingArgs.ElementAtOrDefault(1);
        await BehaviorTreeRunner.RunFromFiles(modelPath, configPath, faultsPath);
        break;
    default:
        await FrontendServer.Run(remainingArgs);
        break;
}
