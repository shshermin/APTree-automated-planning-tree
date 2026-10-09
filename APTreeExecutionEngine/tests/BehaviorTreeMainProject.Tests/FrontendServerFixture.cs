using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using Xunit;

namespace BehaviorTreeMainProject.Tests;

/// <summary>
/// Starts the real FrontendServer once per test run on a free port. It has no
/// shutdown hook, so it lives until the test process exits and its static
/// state is shared across tests.
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
