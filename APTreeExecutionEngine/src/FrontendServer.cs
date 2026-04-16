using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Channels;

namespace BehaviorTreeMainProject
{
    public static class FrontendServer
    {
        // One channel writer per connected WebSocket client
        private static readonly object _subscribersLock = new();
        private static readonly List<ChannelWriter<string>> _tickSubscribers = new();

        // Cached content-root path set during startup — used by BroadcastModelUpdatedAsync
        // to locate the MontiCore jar without needing IWebHostEnvironment.
        private static string? _contentRootPath;

        // Circular buffer for tick diagnostics (last 2000 ticks)
        private const int TickLogCapacity = 2000;
        private static readonly object _tickLogLock = new();
        private static readonly List<TickLogEntry> _tickLog = new(TickLogCapacity);
        private static int _tickLogIndex = 0;

        private record TickLogEntry(string Timestamp, string NodeName, string Status, string Source);

        private static void LogTick(string nodeName, string status, string source)
        {
            var entry = new TickLogEntry(
                DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.fffZ"),
                nodeName, status, source);
            lock (_tickLogLock)
            {
                if (_tickLog.Count < TickLogCapacity)
                    _tickLog.Add(entry);
                else
                    _tickLog[_tickLogIndex % TickLogCapacity] = entry;
                _tickLogIndex++;
            }
        }

        /// <summary>
        /// Runs the MontiCore jar on the current .bt file, embeds the resulting graph JSON
        /// directly in the WebSocket message, and broadcasts it to all connected clients.
        /// Falls back to a plain { type: "modelUpdated" } message when the jar is unavailable
        /// or fails so the frontend can still trigger its HTTP-based fallback path.
        /// </summary>
        private static async Task BroadcastModelUpdatedAsync()
        {
            string message;
            try
            {
                message = await BuildModelUpdatedMessageAsync();
            }
            catch
            {
                // Jar not found, parse error, etc. — fall back to plain notification so the
                // frontend can still attempt the HTTP-based validate as a safety net.
                message = JsonSerializer.Serialize(new { type = "modelUpdated" });
            }

            lock (_subscribersLock)
            {
                foreach (var writer in _tickSubscribers)
                    writer.TryWrite(message);
            }
        }

        /// <summary>
        /// Runs the jar on the active .bt file and returns a JSON string of the form
        /// { "type": "modelUpdated", "graphPayload": { ...graph... } }.
        /// Throws if the jar or .bt file cannot be found or the jar produces non-JSON output.
        /// </summary>
        private static async Task<string> BuildModelUpdatedMessageAsync()
        {
            if (_contentRootPath == null)
                throw new InvalidOperationException("Content root path not initialised.");

            var montiCoreDir = FindMontiCoreDir(_contentRootPath)
                ?? throw new DirectoryNotFoundException("APTreeDSL directory not found.");

            // Locate the tool jar (same logic as the /api/aptree/validate endpoint).
            var libsDir = Path.Combine(montiCoreDir, "target", "libs");
            var jarPath = Path.Combine(libsDir, "automaton-tool.jar");
            if (!File.Exists(jarPath) && Directory.Exists(libsDir))
            {
                jarPath = Directory
                    .GetFiles(libsDir, "*-tool.jar")
                    .OrderByDescending(File.GetLastWriteTimeUtc)
                    .FirstOrDefault() ?? jarPath;
            }
            if (!File.Exists(jarPath))
                throw new FileNotFoundException("MontiCore tool jar not found.", jarPath);

            var btPath = BehaviorTreeMainProject.Services.AIPlanning.APTreeModelWriter.ActiveBtFilePath;
            if (!File.Exists(btPath))
                throw new FileNotFoundException(".bt model file not found.", btPath);

            var modelText = await File.ReadAllTextAsync(btPath);

            var tmpDir = Path.Combine(montiCoreDir, "target", "tmp");
            Directory.CreateDirectory(tmpDir);
            var modelFile = Path.Combine(tmpDir, $"aptree_{Guid.NewGuid():N}.bt");
            var utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
            await File.WriteAllTextAsync(modelFile, StripUtf8Bom(modelText), utf8NoBom);

            try
            {
                var result = await RunProcessAsync(
                    fileName: "java",
                    arguments: BuildArguments(jarPath, modelFile, null),
                    workingDirectory: montiCoreDir,
                    ct: CancellationToken.None);

                if (!TryNormalizeJson(result.StdOut, out var graphJson, out _))
                    throw new InvalidDataException("MontiCore jar returned non-JSON output.");

                // Inline the graph JSON so the frontend can use it without extra HTTP round-trips.
                return $"{{\"type\":\"modelUpdated\",\"graphPayload\":{graphJson}}}";
            }
            finally
            {
                TryDelete(modelFile);
            }
        }

        public static async Task Run(string[] args)
        {
            // Subscribe to BTNode tick events and broadcast to all WebSocket clients
            BTNode.NodeTicked += (nodeName, status) =>
            {
                LogTick(nodeName, status, "local");
                var message = JsonSerializer.Serialize(new { type = "tick", nodeName, status });
                lock (_subscribersLock)
                {
                    foreach (var writer in _tickSubscribers)
                        writer.TryWrite(message);
                }
            };

            // Subscribe to model file updates: run the jar and push the graph to all WebSocket clients.
            BehaviorTreeMainProject.Services.AIPlanning.APTreeModelWriter.ModelUpdated += () =>
                _ = BroadcastModelUpdatedAsync();

            // Watch the .bt file on disk so that changes from external processes
            // (e.g. dotnet run --test) also trigger live frontend updates.
            var btFilePath = BehaviorTreeMainProject.Services.AIPlanning.APTreeModelWriter.ActiveBtFilePath;
            FileSystemWatcher? btFileWatcher = null;
            if (File.Exists(btFilePath))
            {
                var dir = Path.GetDirectoryName(btFilePath)!;
                var fileName = Path.GetFileName(btFilePath);
                btFileWatcher = new FileSystemWatcher(dir, fileName)
                {
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
                    EnableRaisingEvents = true,
                };

                // Debounce: FileSystemWatcher often fires multiple events for a single write.
                Timer? debounceTimer = null;
                var debounceLock = new object();
                btFileWatcher.Changed += (_, _) =>
                {
                    lock (debounceLock)
                    {
                        debounceTimer?.Dispose();
                        debounceTimer = new Timer(_ => _ = BroadcastModelUpdatedAsync(), null, 300, Timeout.Infinite);
                    }
                };
            }

            var builder = WebApplication.CreateBuilder(args);

            // Cache the content-root path so BroadcastModelUpdatedAsync can locate the jar
            // without needing IWebHostEnvironment outside of a request context.
            _contentRootPath = builder.Environment.ContentRootPath;

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

            app.UseWebSockets();
            app.UseCors("frontend-dev");

            app.UseSwagger();
            app.UseSwaggerUI();

            app.MapGet("/health", () => Results.Ok(new { ok = true }));

            // Accepts tick events from external processes (e.g. dotnet run --test)
            // and broadcasts them to all connected WebSocket clients.
            app.MapPost("/api/tick", (TickMessage tick) =>
            {
                if (string.IsNullOrEmpty(tick.NodeName) || string.IsNullOrEmpty(tick.Status))
                    return Results.BadRequest();

                LogTick(tick.NodeName, tick.Status, "remote");
                var message = JsonSerializer.Serialize(new { type = "tick", nodeName = tick.NodeName, status = tick.Status });
                lock (_subscribersLock)
                {
                    foreach (var writer in _tickSubscribers)
                        writer.TryWrite(message);
                }
                return Results.Ok();
            }).WithName("PostTick");

            // Returns recent tick log entries for diagnostic purposes.
            app.MapGet("/api/tick/log", () =>
            {
                List<TickLogEntry> snapshot;
                lock (_tickLogLock)
                {
                    snapshot = new List<TickLogEntry>(_tickLog);
                }
                return Results.Ok(snapshot);
            }).WithName("GetTickLog");

            // Returns the current .bt model file text so the frontend can re-validate after live updates.
            app.MapGet("/api/tree/model", () =>
            {
                var path = BehaviorTreeMainProject.Services.AIPlanning.APTreeModelWriter.ActiveBtFilePath;
                if (!File.Exists(path))
                    return Results.NotFound(new { error = $"Model file not found: {path}" });
                var text = File.ReadAllText(path);
                return Results.Text(text, "text/plain", Encoding.UTF8);
            }).WithName("GetTreeModel");

            // WebSocket endpoint: streams real-time BTNode tick events to the frontend
            app.Map("/ws/tick", async (HttpContext context) =>
            {
                if (!context.WebSockets.IsWebSocketRequest)
                {
                    context.Response.StatusCode = 400;
                    return;
                }

                using var ws = await context.WebSockets.AcceptWebSocketAsync();
                var channel = Channel.CreateBounded<string>(new BoundedChannelOptions(200)
                {
                    FullMode = BoundedChannelFullMode.DropOldest,
                });

                lock (_subscribersLock)
                    _tickSubscribers.Add(channel.Writer);

                using var cts = new CancellationTokenSource();

                // Detect client disconnect by reading incoming frames
                _ = Task.Run(async () =>
                {
                    var buf = new byte[256];
                    try
                    {
                        while (ws.State == WebSocketState.Open)
                        {
                            var result = await ws.ReceiveAsync(new ArraySegment<byte>(buf), cts.Token);
                            if (result.MessageType == WebSocketMessageType.Close)
                                break;
                        }
                    }
                    catch { }
                    finally { cts.Cancel(); }
                });

                try
                {
                    await foreach (var message in channel.Reader.ReadAllAsync(cts.Token))
                    {
                        if (ws.State != WebSocketState.Open) break;
                        var bytes = Encoding.UTF8.GetBytes(message);
                        await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                }
                catch (OperationCanceledException) { }
                finally
                {
                    lock (_subscribersLock)
                        _tickSubscribers.Remove(channel.Writer);
                    channel.Writer.TryComplete();

                    if (ws.State == WebSocketState.Open || ws.State == WebSocketState.CloseReceived)
                    {
                        try { await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Done", CancellationToken.None); }
                        catch { }
                    }
                }
            });

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

                    if (TryNormalizeJson(result.StdOut, out var normalizedJson, out var stdoutNonJson))
                    {
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
            .WithName("ValidateAptree");

            await app.RunAsync();
            btFileWatcher?.Dispose();
        }

        private static IReadOnlyList<NodeCatalogEntry> BuildNodeCatalog(Type baseType, string kind, string typeLabel)
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

        private static string? FindMontiCoreDir(string contentRoot)
        {
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

        private static string ToDisplayName(string rawName)
        {
            if (string.IsNullOrWhiteSpace(rawName)) return rawName;

            var name = Regex.Replace(rawName, "^(Decorator|BTService|ServicePlanning|BTFlowNode|Call)", "");
            name = name.Replace('_', ' ');
            name = Regex.Replace(name, @"(?<=[a-z0-9])([A-Z])", " $1");
            name = Regex.Replace(name, @"(?<=[A-Z])([A-Z][a-z])", " $1");
            return name.Trim();
        }

        private static string BuildArguments(string jarPath, string modelFile, string? instancesFile)
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

        private static string Quote(string s) => $"\"{s.Replace("\"", "\\\"")}\"";

        private static string StripUtf8Bom(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return s[0] == '\uFEFF' ? s[1..] : s;
        }

        private static void TryDelete(string path)
        {
            try { File.Delete(path); } catch { /* ignore */ }
        }

        private static bool TryNormalizeJson(string? output, out string normalized, out string? stdoutNonJson)
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

        private static bool TryParseAndNormalize(string json, out string normalized)
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

        private static string AttachStderr(string json, string stderr)
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

        private static string AttachToolLogs(string json, string stdoutNonJson)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind != JsonValueKind.Object)
                {
                    return json;
                }

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

        private static string TruncateFromEnd(string value, int maxChars)
        {
            if (value.Length <= maxChars) return value;
            return value[(value.Length - maxChars)..];
        }

        private static async Task<ProcessResult> RunProcessAsync(string fileName, string arguments, string workingDirectory, CancellationToken ct)
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

        private record APTreeValidateRequest(
            string ModelText,
            string? InstancesText,
            string? JarPath
        );

        private record ProcessResult(int ExitCode, string StdOut, string StdErr);

        private record TickMessage(
            [property: System.Text.Json.Serialization.JsonPropertyName("nodeName")] string NodeName,
            [property: System.Text.Json.Serialization.JsonPropertyName("status")] string Status
        );

        private record NodeCatalogEntry(
            string Id,
            string Label,
            string TypeLabel,
            string Kind,
            string? Description
        );
    }
}
