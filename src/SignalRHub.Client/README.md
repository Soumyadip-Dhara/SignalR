# SignalRHub.Client

> **NuGet package** — the zero-boilerplate .NET client for the Common SignalR Hub.

## Installation

```bash
dotnet add package SignalRHub.Client
```

## Setup (one line in `Program.cs`)

```csharp
// Read hub URL and API key from appsettings.json → "SignalRHub" section
builder.Services.AddSignalRHubClient(builder.Configuration);

// — or configure in code —
builder.Services.AddSignalRHubClient(options =>
{
    options.HubBaseUrl = "https://your-hub-host";
    options.ApiKey     = "your-api-key";   // omit if hub runs without key enforcement
});
```

Add the matching section to `appsettings.json`:

```json
{
  "SignalRHub": {
    "HubBaseUrl": "https://your-hub-host",
    "ApiKey":     "your-api-key"
  }
}
```

## Publishing a notification

```csharp
public class DocumentService(ISignalRHubClient hub)
{
    public async Task NotifyUploadAsync(string fileName, string documentId)
    {
        await hub.PublishAsync(new NotificationMessage
        {
            Channel   = "document-upload",
            EventType = "upload-success",
            Message   = $"{fileName} uploaded successfully.",
            Payload   = new { documentId, fileName }
        });
    }
}
```

## Broadcasting a system-wide announcement

```csharp
await hub.BroadcastAsync(new NotificationMessage
{
    EventType = "maintenance-window",
    Message   = "Scheduled maintenance in 10 minutes."
});
```

## Target a specific user

```csharp
await hub.PublishAsync(new NotificationMessage
{
    Channel      = "orders",
    EventType    = "order-ready",
    Message      = "Your order #42 is ready.",
    TargetUserId = "user-42"   // only this user's connections receive the event
});
```

## API

| Method | Description |
|---|---|
| `PublishAsync(notification)` | Send to all channel subscribers (or a single user). |
| `BroadcastAsync(notification)` | Send to **all** connected clients. |

### `NotificationMessage` properties

| Property | Required | Description |
|---|---|---|
| `Channel` | ✅ (Publish) | Logical topic clients subscribe to. |
| `EventType` | ✅ | Machine-readable event identifier. |
| `Message` | | Human-readable text shown in the UI. |
| `Payload` | | Any JSON-serialisable object. |
| `TargetUserId` | | If set, only that user receives the notification. |
