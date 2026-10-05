using System;
using System.IO;
using Xunit;

namespace BehaviorTreeMainProject.Tests;

/// <summary>
///
/// FrontendServer.FindMontiCoreDir / the /api/aptree/validate endpoint both
/// need APTreeDSL/target/libs/automaton-*-tool.jar to exist - it is the
/// compiled MontiCore grammar tool that parses .bt model text. The Dockerfile
/// copies the APTreeDSL *source* folder into the image
/// (`COPY APTreeDSL /app/APTreeDSL`) but never builds it - there is no
/// `gradle shadowJar` (or any gradle invocation at all) anywhere in the
/// Dockerfile, and the runtime stage only installs a JRE (openjdk-17-jre,
/// not a JDK + Gradle). APTreeDSL/.gitignore excludes `target/` entirely, so
/// the jar is also not checked into git as a fallback.
///
/// Net effect, confirmed by reading both files rather than assumed: a Docker
/// image built from this repo's Dockerfile today has an APTreeDSL folder
/// with no `target/libs/*.jar` in it at all. /api/aptree/validate - the
/// endpoint behind the live editor's tree validation - would fail every
/// call with "MontiCore tool jar not found" (see FrontendServer.cs's
/// FindMontiCoreDir/jarPath handling), and the WebSocket model
/// auto-refresh (BroadcastModelUpdatedAsync) would silently fall back to
/// its plain "modelUpdated" notification instead of embedding the parsed
/// graph, same root cause.
///
/// This was not built end-to-end in a real `docker build` here (would need
/// to pull the base images and have no faster, more direct way to prove the
/// same gap - the Dockerfile's own content plus the gitignore rule already
/// establish it conclusively). Left undocumented in the Dockerfile and
/// unfixed here, per the project's policy of documenting findings rather
/// than silently patching them - the fix is a `RUN cd APTreeDSL && gradle
/// shadowJar` build stage (with a JDK, not just a JRE) before the runtime
/// stage copies APTreeDSL in.
/// </summary>
public class DockerImageBuildTests
{
    private static string RepoRoot => Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".."));

    [Fact]
    public void Dockerfile_NeverBuildsTheMontiCoreJar_SoApiAptreeValidateCannotWorkAsShipped()
    {
        string dockerfilePath = Path.Combine(RepoRoot, "Dockerfile");
        Assert.True(File.Exists(dockerfilePath), $"Dockerfile not found at {dockerfilePath}");
        string dockerfile = File.ReadAllText(dockerfilePath);

        // If this assertion starts failing, someone added a gradle build
        // step for APTreeDSL - good news, but then this test (and the
        // gitignore-based fallback check below) need re-verifying and this
        // class-level writeup needs updating, not just deleting.
        Assert.DoesNotContain("gradle", dockerfile, StringComparison.OrdinalIgnoreCase);

        string gitignorePath = Path.Combine(RepoRoot, "APTreeDSL", ".gitignore");
        Assert.True(File.Exists(gitignorePath), $".gitignore not found at {gitignorePath}");
        string gitignore = File.ReadAllText(gitignorePath);

        // Confirms there is also no committed fallback jar a `COPY` could pick up.
        Assert.Contains("target/", gitignore);
    }

    [Fact]
    public void Dockerfile_InstallsOnlyAJre_NotAJdk_SoItCouldNotRunGradleEvenIfAskedTo()
    {
        string dockerfilePath = Path.Combine(RepoRoot, "Dockerfile");
        string dockerfile = File.ReadAllText(dockerfilePath);

        Assert.Contains("openjdk-17-jre", dockerfile);
        // A JRE (runtime only) cannot run Gradle/javac - confirms that even
        // adding a build step to the wrong (runtime) stage wouldn't help;
        // the fix needs to happen in the `build` stage, which already has a
        // full .NET SDK but no JDK at all. Checking for the "-jdk" package
        // suffix specifically, not the substring "jdk" - "openjdk-17-jre"
        // itself contains "jdk" as a substring, which would make a naive
        // DoesNotContain("jdk") check fail on a correct assertion.
        Assert.DoesNotContain("-jdk", dockerfile, StringComparison.OrdinalIgnoreCase);
    }
}
