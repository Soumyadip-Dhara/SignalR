# SignalR – Common Real-Time Notification Hub

A centralised **ASP.NET Core SignalR** service that acts as the single real-time communication bridge for all your applications.

## Overview

```
┌─────────────────────┐        POST /api/notifications/publish
│  Document Storage   │──────────────────────────────────────────┐
│  App (backend)      │                                          │
└─────────────────────┘                                          ▼
                                                   ┌────────────────────────┐
┌─────────────────────┐   WebSocket / SSE / LP     │                        │
│  App A  (browser)   │◄──────────────────────────►│  Common SignalR Hub    │
└─────────────────────┘                            │  /hubs/notifications   │
                                                   │                        │
┌─────────────────────┐   WebSocket / SSE / LP     │  POST /api/            │
│  App B  (browser)   │◄──────────────────────────►│  notifications/publish │
└─────────────────────┘                            │                        │
                                                   │  POST /api/            │
┌─────────────────────┐                            │  notifications/        │
│  Any other App      │◄──────────────────────────►│  broadcast             │
└─────────────────────┘                            └────────────────────────┘
```

**Flow example – document upload:**

1. A user uploads a document in the *Document Storage App*.
2. The storage app sends `POST /api/notifications/publish` with an API key to this hub.
3. The hub instantly pushes the event to every browser client that joined the `document-upload` channel.
4. All connected UIs update in real time — no polling required.

## Project structure

```
SignalR.sln
├── src/
│   ├── SignalRHub/                  # The hub application (ASP.NET Core 8)
│   │   ├── Hubs/
│   │   │   └── NotificationHub.cs  # SignalR hub: JoinChannel / LeaveChannel
│   │   ├── Controllers/
│   │   │   └── NotificationsController.cs  # REST API for backend services
│   │   ├── Middleware/
│   │   │   └── ApiKeyMiddleware.cs  # X-Api-Key header auth for REST endpoints
│   │   ├── Models/
│   │   │   ├── NotificationMessage.cs
│   │   │   └── HubResponse.cs
│   │   ├── Program.cs
│   │   └── appsettings.json
│   └── SignalRHub.Tests/            # xUnit integration & unit tests
└── demo/
    └── index.html                   # Browser demo client (no build step)
```

## Running the hub

```bash
cd src/SignalRHub
dotnet run
# Hub is now listening on http://localhost:5000
```

## REST API (for backend services)

### `POST /api/notifications/publish`

Publishes a notification to all clients subscribed to a **channel**, or to a single user.

```json
{
  "channel":      "document-upload",
  "eventType":    "upload-success",
  "message":      "Invoice_Q1.pdf uploaded successfully.",
  "payload":      { "documentId": "doc-001", "fileName": "Invoice_Q1.pdf" },
  "targetUserId": null
}
```

| Field | Required | Description |
|---|---|---|
| `channel` | ✅ | Logical topic. Clients subscribe to this. |
| `eventType` | ✅ | Machine-readable event identifier. |
| `message` | | Human-readable text for the UI. |
| `payload` | | Any JSON object with extra data. |
| `targetUserId` | | If set, message is delivered **only** to that user. |

### `POST /api/notifications/broadcast`

Sends a notification to **all** connected clients regardless of channel.  
Useful for system-wide announcements.

```json
{
  "eventType": "maintenance-window",
  "message":   "Scheduled maintenance in 10 minutes."
}
```

### `GET /health`

Returns `200 Healthy` — use this for liveness/readiness probes.

## SignalR hub endpoint

All browser/native clients connect to:

```
ws://your-host/hubs/notifications
```

### Client-side methods (invoked by the client)

| Method | Parameters | Description |
|---|---|---|
| `JoinChannel` | `channel: string` | Subscribe to a channel's notifications. |
| `LeaveChannel` | `channel: string` | Unsubscribe from a channel. |

### Server-side events (pushed to the client)

| Event | Payload | Description |
|---|---|---|
| `ReceiveNotification` | `NotificationMessage` | A notification was published to a joined channel or sent directly to the user. |
| `JoinedChannel` | `channel: string` | Confirmation that the client joined a channel. |
| `LeftChannel` | `channel: string` | Confirmation that the client left a channel. |

## JavaScript client example

```js
import * as signalR from '@microsoft/signalr';

const connection = new signalR.HubConnectionBuilder()
  .withUrl('http://your-hub-host/hubs/notifications')
  .withAutomaticReconnect()
  .build();

// Receive any notification published on joined channels
connection.on('ReceiveNotification', (notification) => {
  console.log(`[${notification.channel}] ${notification.eventType}:`, notification.message);
  // notification.payload contains the arbitrary JSON sent by the backend
});

await connection.start();

// Subscribe to the document-upload channel
await connection.invoke('JoinChannel', 'document-upload');
```

## Configuration

Edit `appsettings.json` (or use environment variables / secrets):

```json
{
  "Cors": {
    "AllowedOrigins": [
      "https://app-a.example.com",
      "https://app-b.example.com"
    ]
  },
  "ApiKeys": [
    "my-secret-key-for-document-storage-app",
    "another-key-for-order-app"
  ]
}
```

- **`Cors.AllowedOrigins`** – List the origins of all browser applications that connect to the hub. Leave empty to allow any origin (development only).
- **`ApiKeys`** – Keys that backend services must send in the `X-Api-Key` header when calling the REST endpoints. Leave empty to disable enforcement (development only).

## Running the tests

```bash
dotnet test src/SignalRHub.Tests
```

## Demo client

Open `demo/index.html` in a browser while the hub is running to:

- Connect to the hub via WebSocket
- Join / leave channels
- Publish a test notification through the REST API
- Watch events appear in real time
