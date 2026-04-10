﻿using BehaviorTreeMainProject;

// Usage: dotnet run [--server | --test | --loadtest]
//   --server   : frontend API server only  (default)
//   --test     : FullTreeTest only
//   --loadtest : JsonBTLoadTest only

var mode = args.FirstOrDefault(a => a.StartsWith("--")) ?? "--server";
var remainingArgs = args.Where(a => !a.StartsWith("--")).ToArray();

switch (mode)
{
    case "--test":
        await FullTreeTest.RunTest();
        break;
    case "--loadtest":
        await JsonBTLoadTest.RunTest();
        break;
    default:
        await FrontendServer.Run(remainingArgs);
        break;
}
