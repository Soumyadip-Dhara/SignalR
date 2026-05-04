namespace SignalRHub.Models;

/// <summary>Standard envelope returned by all hub REST endpoints.</summary>
public class HubResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public object? Data { get; set; }

    public static HubResponse Ok(string message, object? data = null) =>
        new() { Success = true, Message = message, Data = data };

    public static HubResponse Fail(string message) =>
        new() { Success = false, Message = message };
}
