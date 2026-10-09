using Xunit;

namespace BehaviorTreeMainProject.Tests;

/// <summary>
/// Test classes that bind real ports or spawn processes run sequentially;
/// e.g. FullPipelineSmokeTests' stub listener on port 5000 would otherwise
/// collide with DockerEntrypointBindingTests.
/// </summary>
[CollectionDefinition("NetworkIntegrationTests", DisableParallelization = true)]
public class NetworkIntegrationTestCollection
{
}
