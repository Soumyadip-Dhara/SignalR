namespace SignalRHub.Client;

/// <summary>
/// Contract for publishing notifications through the Common SignalR Hub.
/// </summary>
public interface ISignalRHubClient
{
    /// <summary>
    /// Publish a notification to all subscribers of a channel, or to a single
    /// user when <see cref="NotificationMessage.TargetUserId"/> is set.
    /// </summary>
    /// <param name="notification">Notification payload to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// The <see cref="HubResponse"/> returned by the hub, or <see langword="null"/>
    /// if the response body could not be deserialised.
    /// </returns>
    Task<HubResponse?> PublishAsync(
        NotificationMessage notification,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Broadcast a notification to <b>all</b> connected clients regardless of
    /// channel.  Useful for system-wide announcements (e.g. maintenance windows).
    /// </summary>
    /// <param name="notification">Notification payload to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// The <see cref="HubResponse"/> returned by the hub, or <see langword="null"/>
    /// if the response body could not be deserialised.
    /// </returns>
    Task<HubResponse?> BroadcastAsync(
        NotificationMessage notification,
        CancellationToken cancellationToken = default);
}
