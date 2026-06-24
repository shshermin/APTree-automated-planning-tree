using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using BehaviorTreeMainProject;

// Run the behavior tree test
 await FullTreeTest.RunTest();

// Toggle: set to false to skip starting the editor/frontend backend after the test.
const bool runEditorBackend = true;
if (!runEditorBackend) return;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend-dev", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5173",
                "http://127.0.0.1:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

var utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

app.UseCors("frontend-dev");

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/health", () => Results.Ok(new { ok = true }));

app.MapGet("/api/catalog/decorators", () =>
    Results.Ok(BuildNodeCatalog(typeof(Decorator), kind: "decorator", typeLabel: "Decorator")))
    .WithName("GetDecoratorCatalog");

app.MapGet("/api/catalog/services", () =>
    Results.Ok(BuildNodeCatalog(typeof(Service), kind: "service", typeLabel: "Service")))
    .WithName("GetServiceCatalog");

app.MapGet("/api/catalog/flows", () =>
    Results.Ok(BuildNodeCatalog(typeof(FlowNode), kind: "flow", typeLabel: "Flow")))
    .WithName("GetFlowCatalog");

app.MapPost("/api/aptree/validate", async (APTreeValidateRequest request, IWebHostEnvironment env, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request.ModelText))
    {
        return Results.BadRequest(new { ok = false, errors = new[] { "ModelText is required" } });
    }

    var montiCoreDir = FindMontiCoreDir(env.ContentRootPath);
    if (montiCoreDir == null)
    {
        return Results.Problem(
            title: "APTreeDSL directory not found",
            detail: $"Searched from ContentRootPath: {env.ContentRootPath}. Expected a folder named APTreeDSL next to the solution or within the backend folder.");
    }
    var jarPath = request.JarPath;
    if (string.IsNullOrWhiteSpace(jarPath))
    {
        // Prefer a stable default, but fall back to the actual built *-tool.jar name.
        var libsDir = Path.Combine(montiCoreDir, "target", "libs");
        jarPath = Path.Combine(libsDir, "automaton-tool.jar");
        if (!File.Exists(jarPath) && Directory.Exists(libsDir))
        {
            jarPath = Directory
                .GetFiles(libsDir, "*-tool.jar")
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault() ?? jarPath;
        }
    }

    if (!File.Exists(jarPath))
    {
        return Results.Problem(
            title: "MontiCore tool jar not found",
            detail: $"Expected jar at: {jarPath}. Build it via: (cd APTreeDSL && gradle shadowJar)");
    }

    Directory.CreateDirectory(Path.Combine(montiCoreDir, "target", "tmp"));
    var modelFile = Path.Combine(montiCoreDir, "target", "tmp", $"aptree_{Guid.NewGuid():N}.bt");
    await File.WriteAllTextAsync(modelFile, StripUtf8Bom(request.ModelText), utf8NoBom, ct);

    string? instancesFile = null;
    if (!string.IsNullOrWhiteSpace(request.InstancesText))
    {
        instancesFile = Path.Combine(montiCoreDir, "target", "tmp", $"instances_{Guid.NewGuid():N}.bt");
        await File.WriteAllTextAsync(instancesFile, StripUtf8Bom(request.InstancesText), utf8NoBom, ct);
    }

    try
    {
        var result = await RunProcessAsync(
            fileName: "java",
            arguments: BuildArguments(jarPath, modelFile, instancesFile),
            workingDirectory: montiCoreDir,
            ct: ct);

        // Tool contract: stdout should be one JSON object.
        // In practice, MontiCore logging may print before the JSON payload; extract the JSON safely.
        if (TryNormalizeJson(result.StdOut, out var normalizedJson, out var stdoutNonJson))
        {
            // Optionally attach stderr for debugging (without breaking JSON contract)
            if (!string.IsNullOrWhiteSpace(result.StdErr))
            {
                normalizedJson = AttachStderr(normalizedJson, result.StdErr);
            }
            if (!string.IsNullOrWhiteSpace(stdoutNonJson))
            {
                normalizedJson = AttachToolLogs(normalizedJson, stdoutNonJson);
            }
            return Results.Text(normalizedJson, "application/json", Encoding.UTF8);
        }

        return Results.Problem(
            title: "MontiCore tool returned non-JSON output",
            detail: $"ExitCode={result.ExitCode}\nSTDOUT:\n{result.StdOut}\nSTDERR:\n{result.StdErr}");
    }
    finally
    {
        TryDelete(modelFile);
        if (instancesFile != null) TryDelete(instancesFile);
    }
})
.WithName("ValidateAptree")
;

app.Run();


static IReadOnlyList<NodeCatalogEntry> BuildNodeCatalog(Type baseType, string kind, string typeLabel)
{
    var assembly = baseType.Assembly;

    return assembly
        .GetTypes()
        .Where(type =>
            type is { IsClass: true, IsAbstract: false } &&
            baseType.IsAssignableFrom(type))
        .Select(type =>
        {
            var rawName = type.Name;
            var display = ToDisplayName(rawName);
            return new NodeCatalogEntry(
                Id: rawName,
                Label: display,
                TypeLabel: typeLabel,
                Kind: kind,
                Description: null);
        })
        .OrderBy(entry => entry.Label, StringComparer.OrdinalIgnoreCase)
        .ToList();
}

static string? FindMontiCoreDir(string contentRoot)
{
    // Common layouts:
    // 1) <repo>/APTreeExecutionEngine (contentRoot) + ../APTreeDSL
    // 2) <repo>/APTreeExecutionEngine (contentRoot) + ./APTreeDSL (if copied)
    // 3) When run from subfolder, climb up a few levels to find APTreeDSL
    var candidates = new[]
    {
        Path.Combine(contentRoot, "APTreeDSL"),
        Path.GetFullPath(Path.Combine(contentRoot, "..", "APTreeDSL")),
        Path.GetFullPath(Path.Combine(contentRoot, "..", "..", "APTreeDSL")),
        Path.GetFullPath(Path.Combine(contentRoot, "..", "..", "..", "APTreeDSL")),
    };

    foreach (var candidate in candidates)
    {
        if (Directory.Exists(candidate))
        {
            return candidate;
        }
    }

    return null;
}

static string ToDisplayName(string rawName)
{
    if (string.IsNullOrWhiteSpace(rawName)) return rawName;

    // Strip common prefixes, then prettify.
    var name = Regex.Replace(rawName, "^(Decorator|BTService|ServicePlanning|BTFlowNode|Call)", "");
    name = name.Replace('_', ' ');
    name = Regex.Replace(name, @"(?<=[a-z0-9])([A-Z])", " $1");
    name = Regex.Replace(name, @"(?<=[A-Z])([A-Z][a-z])", " $1");
    return name.Trim();
}

static string BuildArguments(string jarPath, string modelFile, string? instancesFile)
{
    var sb = new StringBuilder();
    sb.Append("-jar ").Append(Quote(jarPath));
    sb.Append(" --model ").Append(Quote(modelFile));
    if (!string.IsNullOrWhiteSpace(instancesFile))
    {
        sb.Append(" --instances ").Append(Quote(instancesFile));
    }
    return sb.ToString();
}

static string Quote(string s) => $"\"{s.Replace("\"", "\\\"")}\"";

static string StripUtf8Bom(string s)
{
    if (string.IsNullOrEmpty(s)) return s;
    return s[0] == '\uFEFF' ? s[1..] : s;
}

static void TryDelete(string path)
{
    try { File.Delete(path); } catch { /* ignore */ }
}

static bool TryNormalizeJson(string? output, out string normalized, out string? stdoutNonJson)
{
    normalized = string.Empty;
    stdoutNonJson = null;
    if (string.IsNullOrWhiteSpace(output)) return false;

    if (TryParseAndNormalize(output, out normalized))
    {
        return true;
    }

    var trimmed = output.Trim();
    var lines = trimmed.Split('\n', StringSplitOptions.RemoveEmptyEntries);
    for (var i = lines.Length - 1; i >= 0; i--)
    {
        var candidate = lines[i].Trim();
        if (!candidate.StartsWith('{') && !candidate.StartsWith('['))
        {
            continue;
        }

        if (TryParseAndNormalize(candidate, out normalized))
        {
            var prefix = string.Join('\n', lines.Take(i)).Trim();
            stdoutNonJson = string.IsNullOrWhiteSpace(prefix) ? null : prefix;
            return true;
        }
    }

    var lastObjectStart = trimmed.LastIndexOf('{');
    if (lastObjectStart >= 0)
    {
        var candidate = trimmed[lastObjectStart..].Trim();
        if (TryParseAndNormalize(candidate, out normalized))
        {
            var prefix = trimmed[..lastObjectStart].Trim();
            stdoutNonJson = string.IsNullOrWhiteSpace(prefix) ? null : prefix;
            return true;
        }
    }

    return false;
}

static bool TryParseAndNormalize(string json, out string normalized)
{
    normalized = string.Empty;
    try
    {
        using var doc = JsonDocument.Parse(json);
        normalized = JsonSerializer.Serialize(doc.RootElement);
        return true;
    }
    catch
    {
        return false;
    }
}

static string AttachStderr(string json, string stderr)
{
    try
    {
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.ValueKind != JsonValueKind.Object)
        {
            return json;
        }

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                prop.WriteTo(writer);
            }
            writer.WriteString("stderr", stderr);
            writer.WriteEndObject();
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }
    catch
    {
        return json;
    }
}

static string AttachToolLogs(string json, string stdoutNonJson)
{
    try
    {
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.ValueKind != JsonValueKind.Object)
        {
            return json;
        }

        // Avoid huge payloads (keep the last chunk, which usually contains the relevant error).
        var logs = TruncateFromEnd(stdoutNonJson, 20_000);

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                prop.WriteTo(writer);
            }
            writer.WriteString("toolLogs", logs);
            writer.WriteEndObject();
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }
    catch
    {
        return json;
    }
}

static string TruncateFromEnd(string value, int maxChars)
{
    if (value.Length <= maxChars) return value;
    return value[(value.Length - maxChars)..];
}

static async Task<ProcessResult> RunProcessAsync(string fileName, string arguments, string workingDirectory, CancellationToken ct)
{
    var psi = new ProcessStartInfo
    {
        FileName = fileName,
        Arguments = arguments,
        WorkingDirectory = workingDirectory,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true,
    };

    using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };

    var stdout = new StringBuilder();
    var stderr = new StringBuilder();

    process.OutputDataReceived += (_, e) => { if (e.Data != null) stdout.AppendLine(e.Data); };
    process.ErrorDataReceived += (_, e) => { if (e.Data != null) stderr.AppendLine(e.Data); };

    if (!process.Start())
    {
        return new ProcessResult(-1, string.Empty, "Failed to start process");
    }

    process.BeginOutputReadLine();
    process.BeginErrorReadLine();

    await process.WaitForExitAsync(ct);

    return new ProcessResult(process.ExitCode, stdout.ToString().Trim(), stderr.ToString().Trim());
}

record APTreeValidateRequest(
    string ModelText,
    string? InstancesText,
    string? JarPath
);

record ProcessResult(int ExitCode, string StdOut, string StdErr);

record NodeCatalogEntry(
    string Id,
    string Label,
    string TypeLabel,
    string Kind,
    string? Description
);
