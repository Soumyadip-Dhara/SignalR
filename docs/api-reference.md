# API Reference

Complete reference for all hub endpoints and real-time events.

---

## REST API

All REST endpoints are protected by the `X-Api-Key` header when `ApiKeys` is configured. Browser clients connecting via WebSocket do **not** need an API key.

### Base URL

```
https://<your-hub-host>
```

---

### `POST /api/notifications/publish`

Publish a notification to all clients subscribed to a channel, or to a single user.

**Headers**

| Header | Value |
|---|---|
| `Content-Type` | `application/json` |
| `X-Api-Key` | Your API key (required when `ApiKeys` is configured) |

**Request body**

```json
{
  "channel":      "document-upload",
  "eventType":    "upload-success",
  "message":      "Invoice_Q1.pdf uploaded successfully.",
  "payload":      { "documentId": "doc-001", "fileName": "Invoice_Q1.pdf" },
  "targetUserId": null
}
```

| Field | Type | Required | Description |
|---|---|---|---|
| `channel` | `string` | ✅ | Logical topic (e.g. `document-upload`, `order-status`). Clients subscribe to this. |
| `eventType` | `string` | ✅ | Machine-readable event identifier. |
| `message` | `string` | | Human-readable description shown in the UI. |
| `payload` | `object` | | Any JSON object with extra context (document metadata, order details, etc.). |
| `targetUserId` | `string` | | When set, the message is delivered **only** to connections belonging to that user. |

**Success response – `200 OK`**

```json
{
  "success":  true,
  "message":  "Notification published.",
  "data": {
    "channel":   "document-upload",
    "eventType": "upload-success",
    "timestamp": "2024-06-01T12:00:00.000Z"
  }
}
```

**Error responses**

| Status | `message` | Reason |
|---|---|---|
| `400 Bad Request` | `'channel' is required.` | `channel` is missing or blank |
| `400 Bad Request` | `'eventType' is required.` | `eventType` is missing or blank |
| `401 Unauthorized` | `Invalid or missing API key.` | `X-Api-Key` header missing or wrong |

---

### `POST /api/notifications/broadcast`

Send a notification to **all** connected clients regardless of their channel subscriptions.

**Headers**

| Header | Value |
|---|---|
| `Content-Type` | `application/json` |
| `X-Api-Key` | Your API key (required when `ApiKeys` is configured) |

**Request body**

```json
{
  "eventType": "maintenance-window",
  "message":   "Scheduled maintenance in 10 minutes.",
  "payload":   { "startsAt": "2024-06-01T22:00:00Z" }
}
```

| Field | Type | Required | Description |
|---|---|---|---|
| `eventType` | `string` | ✅ | Machine-readable event identifier. |
| `message` | `string` | | Human-readable text. |
| `payload` | `object` | | Any additional JSON data. |

> `channel` is set automatically to `"global"` by the hub.

**Success response – `200 OK`**

```json
{
  "success":  true,
  "message":  "Global broadcast sent.",
  "data": {
    "eventType": "maintenance-window",
    "timestamp": "2024-06-01T12:00:00.000Z"
  }
}
```

---

### `GET /health`

Liveness and readiness check. No authentication required.

**Response – `200 OK`**

```
Healthy
```

Use this endpoint in Docker health checks, Kubernetes probes, or load-balancer health checks.

---

## SignalR hub

**Endpoint:** `wss://<your-hub-host>/hubs/notifications`

The hub uses the standard SignalR negotiation protocol. Clients connect via:
- WebSocket (preferred)
- Server-Sent Events (SSE) fallback
- Long-polling fallback

---

### Client → Server methods

Methods the client can **invoke** on the hub.

#### `JoinChannel(channel: string)`

Subscribe to a channel. After joining, the client receives all `ReceiveNotification` events published to that channel.

```js
await connection.invoke('JoinChannel', 'document-upload');
```

On success the hub sends back a `JoinedChannel` event.

#### `LeaveChannel(channel: string)`

Unsubscribe from a channel. The client stops receiving events for that channel.

```js
await connection.invoke('LeaveChannel', 'document-upload');
```

On success the hub sends back a `LeftChannel` event.

---

### Server → Client events

Events the hub **pushes** to connected clients.

#### `ReceiveNotification`

Fired when a notification is published to a channel the client has joined, or sent directly to the client's user.

**Payload**

```ts
interface Notification {
  channel:      string;        // e.g. "document-upload"
  eventType:    string;        // e.g. "upload-success"
  message:      string;        // human-readable text
  payload:      object | null; // arbitrary JSON from the backend
  targetUserId: string | null; // set when targeted at a specific user
  timestamp:    string;        // ISO-8601 UTC timestamp
}
```

**Example**

```js
connection.on('ReceiveNotification', (n) => {
  console.log(n.channel, n.eventType, n.message, n.payload);
});
```

#### `JoinedChannel`

Confirmation that `JoinChannel` succeeded.

**Payload:** `channel: string`

```js
connection.on('JoinedChannel', (channel) => {
  console.log(`Subscribed to "${channel}"`);
});
```

#### `LeftChannel`

Confirmation that `LeaveChannel` succeeded.

**Payload:** `channel: string`

```js
connection.on('LeftChannel', (channel) => {
  console.log(`Unsubscribed from "${channel}"`);
});
```

---

## Standard `HubResponse` envelope

All REST endpoints return this JSON wrapper:

```ts
interface HubResponse {
  success: boolean;
  message: string;
  data?:   object; // present on success only
}
```
