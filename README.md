# SignalR – Common Real-Time Notification Hub

A centralised **ASP.NET Core SignalR** service that acts as the single real-time communication bridge for all your applications.

## 📚 Documentation

| Guide | Description |
|---|---|
| [Quick Start](docs/quick-start.md) | Run the hub and verify it in under 5 minutes |
| [Frontend Integration](docs/integrate-frontend.md) | Connect React, Vue, Angular, or vanilla JS to the hub |
| [Backend Integration](docs/integrate-backend.md) | Publish notifications from C#, Node.js, Python, Java, PHP |
| [Configuration](docs/configuration.md) | CORS, API keys, ports, logging, environment variables |
| [Deployment](docs/deployment.md) | Docker, Docker Compose, Azure App Service, Linux + nginx |
| [API Reference](docs/api-reference.md) | Full REST and hub event reference |

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
SignalR/
├── docs/                        # 📚 Integration & deployment guides
│   ├── quick-start.md
│   ├── integrate-frontend.md
│   ├── integrate-backend.md
│   ├── configuration.md
│   ├── deployment.md
│   └── api-reference.md
├── src/
│   ├── SignalRHub/              # The hub application (ASP.NET Core 8)
│   │   ├── Hubs/
│   │   │   └── NotificationHub.cs
│   │   ├── Controllers/
│   │   │   └── NotificationsController.cs
│   │   ├── Middleware/
│   │   │   └── ApiKeyMiddleware.cs
│   │   ├── Models/
│   │   │   ├── NotificationMessage.cs
│   │   │   └── HubResponse.cs
│   │   ├── Program.cs
│   │   └── appsettings.json
│   └── SignalRHub.Tests/        # xUnit integration & unit tests
└── demo/
    └── index.html               # Browser demo client (no build step)
```

## Running the hub

```bash
cd src/SignalRHub
dotnet run
# Hub is now listening on http://localhost:5000
```

> See the [Quick Start guide](docs/quick-start.md) for a full walkthrough including how to verify the hub and open the demo client.

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

> See the [API Reference](docs/api-reference.md) for full details including response shapes and error codes.

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

> See [Frontend Integration](docs/integrate-frontend.md) for React, Vue, and Angular recipes.

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

> See the [Configuration guide](docs/configuration.md) for the full reference including environment variable names and log-level settings.

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

