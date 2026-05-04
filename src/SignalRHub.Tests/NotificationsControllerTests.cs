using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using SignalRHub.Models;

namespace SignalRHub.Tests;

/// <summary>
/// Integration tests for <see cref="Controllers.NotificationsController"/>.
/// Uses <see cref="WebApplicationFactory{TEntryPoint}"/> so the full ASP.NET
/// Core pipeline (middleware, routing, serialisation) is exercised.
/// </summary>
public class NotificationsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public NotificationsControllerTests(WebApplicationFactory<Program> factory)
    {
        // No API keys configured → middleware is open (dev convenience mode).
        _client = factory.CreateClient();
    }

    // ── /api/notifications/publish ────────────────────────────────────────────

    [Fact]
    public async Task Publish_ValidPayload_Returns200WithSuccessTrue()
    {
        var message = new NotificationMessage
        {
            Channel = "document-upload",
            EventType = "upload-success",
            Message = "Invoice.pdf uploaded successfully.",
            Payload = new { documentId = "doc-001" }
        };

        var response = await _client.PostAsJsonAsync("/api/notifications/publish", message);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<HubResponse>();
        Assert.NotNull(body);
        Assert.True(body.Success);
    }

    [Fact]
    public async Task Publish_MissingChannel_Returns400()
    {
        var message = new NotificationMessage
        {
            Channel = "",           // intentionally blank
            EventType = "upload-success",
            Message = "Test"
        };

        var response = await _client.PostAsJsonAsync("/api/notifications/publish", message);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<HubResponse>();
        Assert.NotNull(body);
        Assert.False(body.Success);
    }

    [Fact]
    public async Task Publish_MissingEventType_Returns400()
    {
        var message = new NotificationMessage
        {
            Channel = "document-upload",
            EventType = "",         // intentionally blank
            Message = "Test"
        };

        var response = await _client.PostAsJsonAsync("/api/notifications/publish", message);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<HubResponse>();
        Assert.NotNull(body);
        Assert.False(body.Success);
    }

    [Fact]
    public async Task Publish_WithTargetUserId_Returns200()
    {
        var message = new NotificationMessage
        {
            Channel = "document-upload",
            EventType = "upload-success",
            Message = "File ready.",
            TargetUserId = "user-42"
        };

        var response = await _client.PostAsJsonAsync("/api/notifications/publish", message);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ── /api/notifications/broadcast ─────────────────────────────────────────

    [Fact]
    public async Task Broadcast_ValidPayload_Returns200WithSuccessTrue()
    {
        var message = new NotificationMessage
        {
            EventType = "maintenance",
            Message = "Scheduled maintenance in 10 minutes."
        };

        var response = await _client.PostAsJsonAsync("/api/notifications/broadcast", message);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<HubResponse>();
        Assert.NotNull(body);
        Assert.True(body.Success);
    }

    [Fact]
    public async Task Broadcast_MissingEventType_Returns400()
    {
        var message = new NotificationMessage
        {
            EventType = "",         // intentionally blank
            Message = "Test"
        };

        var response = await _client.PostAsJsonAsync("/api/notifications/broadcast", message);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── Health check ──────────────────────────────────────────────────────────

    [Fact]
    public async Task HealthCheck_Returns200()
    {
        var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
