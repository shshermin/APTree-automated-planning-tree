using System;
using Xunit;

namespace BehaviorTreeMainProject.Tests;

/// <summary>Opt-in slow test: skipped unless APTREE_RUN_SLOW_TESTS is set.</summary>
public sealed class SlowFactAttribute : FactAttribute
{
    public SlowFactAttribute(string reason)
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APTREE_RUN_SLOW_TESTS")))
            Skip = $"Slow ({reason}); set APTREE_RUN_SLOW_TESTS=1 to run.";
    }
}
