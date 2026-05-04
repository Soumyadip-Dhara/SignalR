namespace SignalRHub.Middleware;

/// <summary>
/// Simple API-key middleware that protects the REST endpoints used by backend
/// services (e.g. document storage) to publish notifications.
///
/// The key is read from the <c>X-Api-Key</c> request header and compared
/// against the <c>ApiKeys</c> list in <c>appsettings.json</c>.
///
/// SignalR WebSocket/long-polling connections (<c>/hubs/*</c>) are exempt so
/// that browser clients do not need an API key.
/// </summary>
public class ApiKeyMiddleware
{
    private const string ApiKeyHeader = "X-Api-Key";

    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ApiKeyMiddleware> _logger;

    public ApiKeyMiddleware(
        RequestDelegate next,
        IConfiguration configuration,
        ILogger<ApiKeyMiddleware> logger)
    {
        _next = next;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Let SignalR negotiate and WebSocket connections through without a key.
        if (context.Request.Path.StartsWithSegments("/hubs"))
        {
            await _next(context);
            return;
        }

        // Health-check is also public.
        if (context.Request.Path.StartsWithSegments("/health"))
        {
            await _next(context);
            return;
        }

        var validKeys = _configuration.GetSection("ApiKeys").Get<string[]>() ?? [];

        // If no keys are configured, skip enforcement (dev-mode convenience).
        if (validKeys.Length == 0)
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(ApiKeyHeader, out var providedKey)
            || !validKeys.Contains(providedKey.ToString()))
        {
            _logger.LogWarning(
                "Unauthorized API access attempt from {RemoteIp}",
                context.Connection.RemoteIpAddress);

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid or missing API key." });
            return;
        }

        await _next(context);
    }
}
