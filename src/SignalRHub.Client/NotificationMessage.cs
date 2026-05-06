namespace SignalRHub.Client;

/// <summary>
/// Payload sent to the hub to publish a real-time notification.
/// Mirrors the model used internally by the hub server.
/// </summary>
public class NotificationMessage
{
    /// <summary>
    /// Logical channel / topic (e.g. "document-upload", "order-status").
    /// Clients subscribe to a channel to receive its events.
    /// </summary>
    public string Channel { get; set; } = string.Empty;

    /// <summary>
    /// Short machine-readable event type (e.g. "upload-success", "upload-failed").
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable message shown in the UI.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Arbitrary JSON-serialisable payload attached to the event.
    /// </summary>
    public object? Payload { get; set; }

    /// <summary>
    /// Optional target user identifier.  When set, the message is delivered
    /// only to connections belonging to that user; otherwise it is broadcast
    /// to all channel subscribers.
    /// </summary>
    public string? TargetUserId { get; set; }
}
