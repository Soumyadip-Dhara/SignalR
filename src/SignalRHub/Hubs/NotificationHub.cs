using Microsoft.AspNetCore.SignalR;

namespace SignalRHub.Hubs;

/// <summary>
/// Central SignalR hub that all applications connect to.
///
/// Clients call:
///   - <c>JoinChannel(channel)</c>   to subscribe to a topic / channel
///   - <c>LeaveChannel(channel)</c>  to unsubscribe
///
/// The hub pushes the event <c>ReceiveNotification</c> to every client that
/// has joined the relevant channel (or directly to a specific user).
/// </summary>
public class NotificationHub : Hub
{
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Subscribe the calling connection to a channel so that it receives all
    /// future notifications published on that channel.
    /// </summary>
    public async Task JoinChannel(string channel)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, channel);
        _logger.LogInformation(
            "Connection {ConnectionId} joined channel '{Channel}'",
            Context.ConnectionId, channel);

        await Clients.Caller.SendAsync("JoinedChannel", channel);
    }

    /// <summary>Unsubscribe the calling connection from a channel.</summary>
    public async Task LeaveChannel(string channel)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, channel);
        _logger.LogInformation(
            "Connection {ConnectionId} left channel '{Channel}'",
            Context.ConnectionId, channel);

        await Clients.Caller.SendAsync("LeftChannel", channel);
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation(
            "Client disconnected: {ConnectionId}. Reason: {Reason}",
            Context.ConnectionId, exception?.Message ?? "normal close");
        await base.OnDisconnectedAsync(exception);
    }
}
