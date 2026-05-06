using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using SignalRHub.Client;

namespace SignalRHub.Client.Tests;

/// <summary>
/// Unit tests for <see cref="ServiceCollectionExtensions"/> and
/// <see cref="SignalRHubClient"/>.
/// </summary>
public class SignalRHubClientTests
{
    // ── ServiceCollectionExtensions ───────────────────────────────────────────

    [Fact]
    public void AddSignalRHubClient_WithDelegate_RegistersISignalRHubClient()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddSignalRHubClient(opts =>
        {
            opts.HubBaseUrl = "http://localhost:5000";
            opts.ApiKey = "test-key";
        });

        var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<ISignalRHubClient>();

        Assert.NotNull(client);
    }

    [Fact]
    public void AddSignalRHubClient_RegistersAsTransient()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddSignalRHubClient(opts => opts.HubBaseUrl = "http://localhost:5000");

        var provider = services.BuildServiceProvider();

        // Typed HttpClients are transient by design — each resolution returns a new wrapper
        // (the underlying connection pool is shared).
        var a = provider.GetRequiredService<ISignalRHubClient>();
        var b = provider.GetRequiredService<ISignalRHubClient>();

        Assert.NotNull(a);
        Assert.NotNull(b);
    }

    // ── PublishAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task PublishAsync_SendsPostToCorrectEndpoint()
    {
        var (client, handler) = BuildClient("http://hub/");

        await client.PublishAsync(new NotificationMessage
        {
            Channel = "ch",
            EventType = "ev",
            Message = "msg"
        });

        handler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(r =>
                r.Method == HttpMethod.Post &&
                r.RequestUri!.AbsolutePath.EndsWith("api/notifications/publish")),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task PublishAsync_ReturnsDeserializedHubResponse()
    {
        var expected = new HubResponse { Success = true, Message = "OK" };
        var (client, _) = BuildClient("http://hub/", JsonSerializer.Serialize(expected));

        var result = await client.PublishAsync(new NotificationMessage
        {
            Channel = "ch",
            EventType = "ev"
        });

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal("OK", result.Message);
    }

    // ── BroadcastAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task BroadcastAsync_SendsPostToCorrectEndpoint()
    {
        var (client, handler) = BuildClient("http://hub/");

        await client.BroadcastAsync(new NotificationMessage { EventType = "ev" });

        handler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(r =>
                r.Method == HttpMethod.Post &&
                r.RequestUri!.AbsolutePath.EndsWith("api/notifications/broadcast")),
            ItExpr.IsAny<CancellationToken>());
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (ISignalRHubClient client, Mock<HttpMessageHandler> handler)
        BuildClient(string baseUrl, string? responseJson = null)
    {
        responseJson ??= JsonSerializer.Serialize(new HubResponse { Success = true, Message = "OK" });

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson,
                    System.Text.Encoding.UTF8, "application/json")
            });

        var http = new HttpClient(handler.Object) { BaseAddress = new Uri(baseUrl) };

        var opts = Options.Create(new SignalRHubClientOptions
        {
            HubBaseUrl = baseUrl,
            ApiKey = string.Empty
        });

        var logger = NullLogger<SignalRHubClient>.Instance;
        ISignalRHubClient client = new SignalRHubClient(http, opts, logger);
        return (client, handler);
    }
}
