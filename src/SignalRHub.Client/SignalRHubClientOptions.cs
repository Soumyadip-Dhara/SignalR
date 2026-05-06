namespace SignalRHub.Client;

/// <summary>
/// Configuration options for <see cref="SignalRHubClient"/>.
/// Bind this from <c>appsettings.json</c> or set values directly.
/// </summary>
/// <example>
/// <code>
/// // appsettings.json
/// {
///   "SignalRHub": {
///     "HubBaseUrl": "https://your-hub-host",
///     "ApiKey":     "your-api-key"
///   }
/// }
/// </code>
/// </example>
public class SignalRHubClientOptions
{
    /// <summary>Configuration section name used when binding from <c>appsettings.json</c>.</summary>
    public const string SectionName = "SignalRHub";

    /// <summary>
    /// Base URL of the hub server (e.g. <c>https://signalrhub.example.com</c>).
    /// Do <b>not</b> include a trailing slash.
    /// </summary>
    public string HubBaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// API key sent in the <c>X-Api-Key</c> request header.
    /// Leave empty if the hub is running without API-key enforcement (development).
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
}
