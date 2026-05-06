namespace SignalRHub.Client;

/// <summary>
/// Standard envelope returned by all hub REST endpoints.
/// </summary>
public class HubResponse
{
    /// <summary>Whether the operation succeeded.</summary>
    public bool Success { get; set; }

    /// <summary>Human-readable status message.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Optional additional data returned by the hub.</summary>
    public object? Data { get; set; }
}
