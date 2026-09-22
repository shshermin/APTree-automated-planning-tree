using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using Xunit;

namespace BehaviorTreeMainProject.Tests;

/// <summary>
/// Starts the real FrontendServer once per test run on a free localhost port.
/// FrontendServer.Run builds and runs the whole WebApplication inside one
/// static method (no WebApplicationFactory seam, top-level-statement Program),
/// and offers no shutdown hook, so the server simply lives until the test
/// process exits. Static state inside FrontendServer (subscriber list, tick
/// log) is therefore shared across all tests using this fixture.
/// </summary>
public sealed class FrontendServerFixture : IAsyncLifetime
{
    public string BaseUrl { get; private set; } = "";
    public HttpClient Http { get; private set; } = new();

    public async Task InitializeAsync()
    {
        int port = GetFreePort();
        BaseUrl = $"http://127.0.0.1:{port}";
        Http = new HttpClient { BaseAddress = new Uri(BaseUrl), Timeout = TimeSpan.FromSeconds(5) };

        _ = Task.Run(() => FrontendServer.Run(new[] { "--urls", BaseUrl }));

        for (int i = 0; i < 50; i++)
        {
            try
            {
                var response = await Http.GetAsync("/health");
                if (response.StatusCode == HttpStatusCode.OK) return;
            }
            catch (HttpRequestException) { }
            await Task.Delay(100);
        }
        throw new TimeoutException($"FrontendServer did not become healthy on {BaseUrl} within 5s");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static int GetFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}

[CollectionDefinition("FrontendServer")]
public class FrontendServerCollection : ICollectionFixture<FrontendServerFixture> { }
