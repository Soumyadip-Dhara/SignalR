namespace SignalRHub.Models;

/// <summary>
/// Payload sent by a backend service (e.g. document storage app) to publish
/// a real-time notification through the common SignalR hub.
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
    /// Arbitrary JSON payload attached to the event (document metadata, order details, …).
    /// </summary>
    public object? Payload { get; set; }

    /// <summary>
    /// Optional group identifier within a channel (e.g. a document ID, team name, or
    /// tenant ID). When set together with <see cref="Channel"/>, the message is
    /// delivered only to connections that have called
    /// <c>JoinGroup(channel, group)</c> on the hub, giving fine-grained segregation
    /// inside a channel.  When omitted the message goes to all channel subscribers.
    ///
    /// <para>
    /// <b>Precedence:</b> when both <see cref="TargetUserId"/> and <see cref="Group"/>
    /// are set, <see cref="TargetUserId"/> takes priority and <see cref="Group"/> is
    /// ignored. Set at most one of the two properties.
    /// </para>
    ///
    /// <para>
    /// The value must not contain the <c>':'</c> character, as that is used
    /// internally as the channel/group delimiter.
    /// </para>
    /// </summary>
    public string? Group { get; set; }

    /// <summary>
    /// Optional target user identifier. When set, the message is delivered only to
    /// connections belonging to that user; otherwise it is broadcast to all channel
    /// subscribers (or to the <see cref="Group"/> sub-group when that is also set).
    ///
    /// <para>
    /// <b>Precedence:</b> <see cref="TargetUserId"/> takes priority over
    /// <see cref="Group"/>. Set at most one of the two properties.
    /// </para>
    /// </summary>
    public string? TargetUserId { get; set; }

    /// <summary>
    /// UTC timestamp set automatically by the hub before forwarding the message.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
