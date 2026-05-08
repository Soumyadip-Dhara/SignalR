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

    /// <summary>
    /// Subscribe the calling connection to a group inside a channel so that it
    /// receives only notifications published to that specific group.
    ///
    /// Internally the group key is <c>"{channel}:{group}"</c>, which keeps groups
    /// namespaced per channel and avoids accidental cross-channel collisions.
    /// </summary>
    public async Task JoinGroup(string channel, string group)
    {
        var groupKey = BuildGroupKey(channel, group);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupKey);
        _logger.LogInformation(
            "Connection {ConnectionId} joined group '{Group}' in channel '{Channel}'",
            Context.ConnectionId, group, channel);

        await Clients.Caller.SendAsync("JoinedGroup", channel, group);
    }

    /// <summary>Unsubscribe the calling connection from a channel group.</summary>
    public async Task LeaveGroup(string channel, string group)
    {
        var groupKey = BuildGroupKey(channel, group);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupKey);
        _logger.LogInformation(
            "Connection {ConnectionId} left group '{Group}' in channel '{Channel}'",
            Context.ConnectionId, group, channel);

        await Clients.Caller.SendAsync("LeftGroup", channel, group);
    }

    /// <summary>
    /// Builds the internal SignalR group key for a channel + group pair.
    /// Format: <c>"{channel}:{group}"</c>.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="channel"/> or <paramref name="group"/> is null,
    /// empty, or contains the <c>':'</c> delimiter (which would create an ambiguous key).
    /// </exception>
    public static string BuildGroupKey(string channel, string group)
    {
        if (string.IsNullOrWhiteSpace(channel))
            throw new ArgumentException("Channel must not be null or empty.", nameof(channel));
        if (string.IsNullOrWhiteSpace(group))
            throw new ArgumentException("Group must not be null or empty.", nameof(group));
        if (channel.Contains(':', StringComparison.Ordinal))
            throw new ArgumentException("Channel must not contain the ':' delimiter.", nameof(channel));
        if (group.Contains(':', StringComparison.Ordinal))
            throw new ArgumentException("Group must not contain the ':' delimiter.", nameof(group));

        return $"{channel}:{group}";
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
