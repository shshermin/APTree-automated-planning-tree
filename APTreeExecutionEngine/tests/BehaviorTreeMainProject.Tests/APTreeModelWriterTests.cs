using System;
using System.IO;
using System.Runtime.CompilerServices;
using BehaviorTreeMainProject.Services.AIPlanning;
using Xunit;

namespace BehaviorTreeMainProject.Tests;

/// <summary>
/// Points the model writer's working copy at a per-run temp file for the whole test
/// process, so tests that plan never touch a developer's live model in the system temp folder.
/// </summary>
internal static class ModelWriterTestSetup
{
    [ModuleInitializer]
    internal static void UseTempWorkingCopy()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(APTreeModelWriter.WorkingCopyPathEnvVar)))
        {
            Environment.SetEnvironmentVariable(APTreeModelWriter.WorkingCopyPathEnvVar,
                Path.Combine(Path.GetTempPath(), $"aptree-tests-{Guid.NewGuid():N}", "live-model.bt"));
        }
    }
}

[CollectionDefinition("ModelWriterEnv", DisableParallelization = true)]
public class ModelWriterEnvCollection
{
}

/// <summary>
/// The planner writes generated NodeGraphs into a working copy of the .bt model;
/// the authored model it starts from is never written.
/// </summary>
[Collection("ModelWriterEnv")]
public class APTreeModelWriterTests : IDisposable
{
    private const string Template =
        "BehaviorTree Main {\n" +
        "    Root FlowNode cassette1 {\n" +
        "        All\n" +
        "        AllAction\n" +
        "        NodeGraph { }\n" +
        "    }\n" +
        "}\n";

    private readonly string _dir = Path.Combine(Path.GetTempPath(), $"aptree-writer-{Guid.NewGuid():N}");
    private readonly string _templatePath;
    private readonly string? _oldTemplateEnv;

    public APTreeModelWriterTests()
    {
        Directory.CreateDirectory(_dir);
        _templatePath = Path.Combine(_dir, "template.bt");
        File.WriteAllText(_templatePath, Template);
        _oldTemplateEnv = Environment.GetEnvironmentVariable(APTreeModelWriter.TemplatePathEnvVar);
        Environment.SetEnvironmentVariable(APTreeModelWriter.TemplatePathEnvVar, _templatePath);
        APTreeModelWriter.ResetWorkingCopy();
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(APTreeModelWriter.TemplatePathEnvVar, _oldTemplateEnv);
        try { Directory.Delete(_dir, recursive: true); } catch { }
    }

    [Fact]
    public void WritingAPlan_ChangesTheWorkingCopy_NotTheTemplate()
    {
        APTreeModelWriter.UpdateCassetteNodeGraph("cassette1", "NodeGraph {\n    Action TravelML t1 (r1)\n}");

        Assert.Equal(Template, File.ReadAllText(_templatePath));
        Assert.Contains("Action TravelML t1 (r1)", File.ReadAllText(APTreeModelWriter.ActiveBtFilePath));
        Assert.NotEqual(Path.GetFullPath(_templatePath), APTreeModelWriter.ActiveBtFilePath);
    }

    [Fact]
    public void ResetWorkingCopy_DropsThePlansOfThePreviousRun()
    {
        APTreeModelWriter.UpdateCassetteNodeGraph("cassette1", "NodeGraph {\n    Action TravelML t1 (r1)\n}");

        APTreeModelWriter.ResetWorkingCopy();

        Assert.Equal(Template, File.ReadAllText(APTreeModelWriter.ActiveBtFilePath));
    }

    [Fact]
    public void EnsureWorkingCopy_KeepsAnExistingWorkingCopy()
    {
        APTreeModelWriter.UpdateCassetteNodeGraph("cassette1", "NodeGraph {\n    Action TravelML t1 (r1)\n}");

        Assert.True(APTreeModelWriter.EnsureWorkingCopy());

        Assert.Contains("Action TravelML t1 (r1)", File.ReadAllText(APTreeModelWriter.ActiveBtFilePath));
    }

    [Fact]
    public void WithoutAnOverride_TheTemplateIsFoundInTheRepositorysAPTreeDSL()
    {
        Environment.SetEnvironmentVariable(APTreeModelWriter.TemplatePathEnvVar, null);

        var template = APTreeModelWriter.TemplateBtFilePath;

        Assert.True(File.Exists(template), template);
        Assert.EndsWith(Path.Combine("APTreeDSL", "src", "test", "resources", "valid", "behavior_trees", "DemonstratorFinal.bt"), template);
    }
}
