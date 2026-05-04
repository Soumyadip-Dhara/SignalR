using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SignalRHub.Hubs;
using SignalRHub.Models;

namespace SignalRHub.Controllers;

/// <summary>
/// REST endpoints that backend services call to publish real-time notifications
/// through the common SignalR hub.
///
/// Example flow for a document storage app:
///   POST /api/notifications/publish
///   {
///     "channel":   "document-upload",
///     "eventType": "upload-success",
///     "message":   "Invoice_Q1.pdf uploaded successfully.",
///     "payload":   { "documentId": "abc123", "fileName": "Invoice_Q1.pdf" }
///   }
///
/// All connected clients that have called <c>JoinChannel("document-upload")</c>
/// will instantly receive the <c>ReceiveNotification</c> hub event.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        IHubContext<NotificationHub> hubContext,
        ILogger<NotificationsController> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <summary>
    /// Sanitizes a user-supplied string so that it cannot be used to inject
    /// fake log entries (log-forging / CRLF injection).
    /// </summary>
    private static string S(string? value) =>
        value?.Replace("\r", "\\r", StringComparison.Ordinal)
              .Replace("\n", "\\n", StringComparison.Ordinal) ?? string.Empty;

    /// <summary>
    /// Publish a notification to all subscribers of a channel, or to a single
    /// user if <c>TargetUserId</c> is specified.
    /// </summary>
    [HttpPost("publish")]
    [ProducesResponseType(typeof(HubResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HubResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Publish([FromBody] NotificationMessage notification)
    {
        if (string.IsNullOrWhiteSpace(notification.Channel))
            return BadRequest(HubResponse.Fail("'channel' is required."));

        if (string.IsNullOrWhiteSpace(notification.EventType))
            return BadRequest(HubResponse.Fail("'eventType' is required."));

        notification.Timestamp = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(notification.TargetUserId))
        {
            // Send only to the specified user (all their connections).
            await _hubContext.Clients
                .User(notification.TargetUserId)
                .SendAsync("ReceiveNotification", notification);

            _logger.LogInformation(
                "Notification sent to user '{UserId}' on channel '{Channel}' (event: {EventType})",
                S(notification.TargetUserId), S(notification.Channel), S(notification.EventType));
        }
        else
        {
            // Broadcast to every connection that joined this channel.
            await _hubContext.Clients
                .Group(notification.Channel)
                .SendAsync("ReceiveNotification", notification);

            _logger.LogInformation(
                "Notification broadcast on channel '{Channel}' (event: {EventType})",
                S(notification.Channel), S(notification.EventType));
        }

        return Ok(HubResponse.Ok("Notification published.", new
        {
            notification.Channel,
            notification.EventType,
            notification.Timestamp
        }));
    }

    /// <summary>
    /// Broadcast a notification to ALL connected clients regardless of channel.
    /// Useful for system-wide announcements (e.g. maintenance windows).
    /// </summary>
    [HttpPost("broadcast")]
    [ProducesResponseType(typeof(HubResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HubResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Broadcast([FromBody] NotificationMessage notification)
    {
        if (string.IsNullOrWhiteSpace(notification.EventType))
            return BadRequest(HubResponse.Fail("'eventType' is required."));

        notification.Channel = "global";
        notification.Timestamp = DateTime.UtcNow;

        await _hubContext.Clients.All.SendAsync("ReceiveNotification", notification);

        _logger.LogInformation(
            "Global broadcast sent (event: {EventType})", S(notification.EventType));

        return Ok(HubResponse.Ok("Global broadcast sent.", new
        {
            notification.EventType,
            notification.Timestamp
        }));
    }
}
