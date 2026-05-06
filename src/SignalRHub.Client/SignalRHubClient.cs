using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SignalRHub.Client;

/// <summary>
/// Default implementation of <see cref="ISignalRHubClient"/> that communicates
/// with the hub over HTTP using a named <see cref="HttpClient"/>.
/// </summary>
public sealed class SignalRHubClient : ISignalRHubClient
{
    private readonly HttpClient _http;
    private readonly ILogger<SignalRHubClient> _logger;

    /// <summary>Initialises a new instance of <see cref="SignalRHubClient"/>.</summary>
    public SignalRHubClient(
        HttpClient http,
        IOptions<SignalRHubClientOptions> options,
        ILogger<SignalRHubClient> logger)
    {
        _http = http;
        _logger = logger;

        // Ensure the base address is always set even when the caller injects
        // a pre-configured client (e.g. in tests).
        var baseUrl = options.Value.HubBaseUrl.TrimEnd('/');
        if (!string.IsNullOrWhiteSpace(baseUrl) && _http.BaseAddress is null)
            _http.BaseAddress = new Uri(baseUrl + "/");
    }

    /// <inheritdoc/>
    public async Task<HubResponse?> PublishAsync(
        NotificationMessage notification,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Publishing notification to channel '{Channel}' (event: {EventType})",
            notification.Channel, notification.EventType);

        var response = await _http.PostAsJsonAsync(
            "api/notifications/publish", notification, cancellationToken);

        return await ReadResponseAsync(response);
    }

    /// <inheritdoc/>
    public async Task<HubResponse?> BroadcastAsync(
        NotificationMessage notification,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Broadcasting notification (event: {EventType})", notification.EventType);

        var response = await _http.PostAsJsonAsync(
            "api/notifications/broadcast", notification, cancellationToken);

        return await ReadResponseAsync(response);
    }

    private async Task<HubResponse?> ReadResponseAsync(HttpResponseMessage response)
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<HubResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Could not deserialise hub response (status: {StatusCode})",
                (int)response.StatusCode);
            return null;
        }
    }
}
