using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace BehaviorTreeMainProject.Tests;

/// <summary>Test against the real FrontendServer.</summary>
[Collection("FrontendServer")]
public class FrontendServerApiTests
{
    private readonly FrontendServerFixture _server;
    public FrontendServerApiTests(FrontendServerFixture server) => _server = server;

    [Fact]
    public async Task Health_Returns200_WithOkTrue()
    {
        var response = await _server.Http.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(doc.RootElement.GetProperty("ok").GetBoolean());
    }

    [Theory]
    [InlineData("/api/catalog/decorators", "decorator", "DecoratorRecovery")]
    [InlineData("/api/catalog/services", "service", "ServiceLLSubtreeInject")]
    [InlineData("/api/catalog/flows", "flow", "DynamicFlowNode")]
    public async Task Catalog_ListsConcreteTypesOfTheRequestedKind(string route, string kind, string knownTypeId)
    {
        var response = await _server.Http.GetAsync(route);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var entries = doc.RootElement.EnumerateArray().ToList();
        Assert.NotEmpty(entries);
        Assert.All(entries, e => Assert.Equal(kind, e.GetProperty("kind").GetString()));
        Assert.Contains(entries, e => e.GetProperty("id").GetString() == knownTypeId);
    }

    [Fact]
    public async Task PostTick_WithMissingStatus_Returns400()
    {
        var response = await _server.Http.PostAsJsonAsync("/api/tick", new { nodeName = "n1" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostTick_IsRecordedInTheTickLog()
    {
        string node = $"logged-{Guid.NewGuid():N}";

        var post = await _server.Http.PostAsJsonAsync("/api/tick", new { nodeName = node, status = "Running" });
        Assert.Equal(HttpStatusCode.OK, post.StatusCode);

        var log = await _server.Http.GetStringAsync("/api/tick/log");
        Assert.Contains(node, log);
    }

    [Fact]
    public async Task WebSocketEndpoint_RejectsPlainHttpRequests_With400()
    {
        var response = await _server.Http.GetAsync("/ws/tick");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task WebSocket_ReceivesTicksPostedToTheApi()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var ws = new ClientWebSocket();
        await ws.ConnectAsync(new Uri(_server.BaseUrl.Replace("http://", "ws://") + "/ws/tick"), cts.Token);

        string node = $"ws-{Guid.NewGuid():N}";
        // The server registers the subscriber after accepting the socket; retry the
        // POST until the message shows up rather than guessing a fixed delay.
        var buffer = new byte[4096];
        string? received = null;
        for (int attempt = 0; attempt < 20 && received == null; attempt++)
        {
            await _server.Http.PostAsJsonAsync("/api/tick", new { nodeName = node, status = "Success" }, cts.Token);
            using var receiveCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token);
            receiveCts.CancelAfter(TimeSpan.FromMilliseconds(250));
            try
            {
                var result = await ws.ReceiveAsync(buffer, receiveCts.Token);
                var text = Encoding.UTF8.GetString(buffer, 0, result.Count);
                if (text.Contains(node)) received = text;
            }
            catch (OperationCanceledException) when (!cts.IsCancellationRequested) { }
        }

        Assert.NotNull(received);
        using var doc = JsonDocument.Parse(received!);
        Assert.Equal("tick", doc.RootElement.GetProperty("type").GetString());
        Assert.Equal(node, doc.RootElement.GetProperty("nodeName").GetString());
        Assert.Equal("Success", doc.RootElement.GetProperty("status").GetString());
    }
}
