# Frontend Integration Guide

How to connect **any browser application** to the Common SignalR Hub so it can receive real-time notifications.

---

## How it works

```
Browser App                      SignalR Hub
    │                                 │
    │── WebSocket /hubs/notifications ►│
    │◄── ReceiveNotification ─────────│
    │                                 │
    │── JoinChannel("order-status") ──►│
    │◄── JoinedChannel ───────────────│
```

1. The browser opens a **WebSocket** (or falls back to SSE/long-polling automatically).
2. The browser calls `JoinChannel` to subscribe to one or more named channels.
3. Whenever a backend service publishes to that channel, the hub pushes a `ReceiveNotification` event to every subscriber.

---

## Step 1 – Install the SignalR client library

Choose the method that matches your project.

### npm (React, Vue, Angular, Svelte, etc.)

```bash
npm install @microsoft/signalr
```

### CDN (Vanilla HTML page)

```html
<script src="https://cdn.jsdelivr.net/npm/@microsoft/signalr@8/dist/browser/signalr.min.js"></script>
```

### Copy the bundled file (no internet access)

The repository ships a pre-built copy at `demo/signalr.min.js`:

```html
<script src="signalr.min.js"></script>
```

---

## Step 2 – Create the connection

### ES module / bundler (React, Vue, Angular, etc.)

```js
import * as signalR from '@microsoft/signalr';

const HUB_URL = 'https://your-hub-host/hubs/notifications';

const connection = new signalR.HubConnectionBuilder()
  .withUrl(HUB_URL)
  .withAutomaticReconnect()        // reconnect on network disruption
  .configureLogging(signalR.LogLevel.Warning)
  .build();
```

### Vanilla `<script>` tag

```html
<script src="signalr.min.js"></script>
<script>
  const connection = new signalR.HubConnectionBuilder()
    .withUrl('http://localhost:5000/hubs/notifications')
    .withAutomaticReconnect()
    .build();
</script>
```

---

## Step 3 – Register event handlers

Register handlers **before** calling `connection.start()`:

```js
// Fired when a notification is pushed to a channel you joined,
// or directly to your user ID.
connection.on('ReceiveNotification', (notification) => {
  console.log('Channel:  ', notification.channel);
  console.log('Event:    ', notification.eventType);
  console.log('Message:  ', notification.message);
  console.log('Payload:  ', notification.payload);  // arbitrary JSON
  console.log('Timestamp:', notification.timestamp);
});

// Confirmation that JoinChannel succeeded.
connection.on('JoinedChannel', (channel) => {
  console.log(`Subscribed to "${channel}"`);
});

// Confirmation that LeaveChannel succeeded.
connection.on('LeftChannel', (channel) => {
  console.log(`Unsubscribed from "${channel}"`);
});

// Connection lifecycle
connection.onclose((error) => {
  console.warn('Disconnected:', error?.message);
});

connection.onreconnecting((error) => {
  console.warn('Reconnecting…', error?.message);
});

connection.onreconnected((connectionId) => {
  console.info('Reconnected. ID:', connectionId);
});
```

---

## Step 4 – Start the connection and join channels

```js
async function startConnection() {
  try {
    await connection.start();
    console.info('Connected to hub');

    // Subscribe to the channels your UI cares about.
    await connection.invoke('JoinChannel', 'document-upload');
    await connection.invoke('JoinChannel', 'order-status');
  } catch (err) {
    console.error('Connection failed:', err);
    // Optional: retry after a delay
    setTimeout(startConnection, 5000);
  }
}

startConnection();
```

---

## Step 5 – Leave channels when no longer needed

```js
await connection.invoke('LeaveChannel', 'document-upload');
```

Call this when a component unmounts or the user navigates away from a view that no longer needs the channel.

---

## Framework recipes

### React hook

```jsx
// useSignalR.js
import { useEffect, useRef } from 'react';
import * as signalR from '@microsoft/signalr';

export function useSignalR(hubUrl, channels, onNotification) {
  const connectionRef = useRef(null);

  useEffect(() => {
    const conn = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl)
      .withAutomaticReconnect()
      .build();

    conn.on('ReceiveNotification', onNotification);

    conn.start()
      .then(() => {
        channels.forEach(ch => conn.invoke('JoinChannel', ch));
      })
      .catch(console.error);

    connectionRef.current = conn;

    return () => {
      conn.stop();
    };
  }, [hubUrl]);   // re-run only if the hub URL changes

  return connectionRef;
}
```

Usage:

```jsx
import { useSignalR } from './useSignalR';

function Dashboard() {
  useSignalR(
    'https://your-hub/hubs/notifications',
    ['document-upload', 'order-status'],
    (notification) => {
      console.log('Got notification:', notification);
    }
  );

  return <div>…</div>;
}
```

### Vue 3 composable

```js
// useSignalR.js
import { onMounted, onUnmounted } from 'vue';
import * as signalR from '@microsoft/signalr';

export function useSignalR(hubUrl, channels, onNotification) {
  let connection;

  onMounted(async () => {
    connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl)
      .withAutomaticReconnect()
      .build();

    connection.on('ReceiveNotification', onNotification);
    await connection.start();
    for (const ch of channels) {
      await connection.invoke('JoinChannel', ch);
    }
  });

  onUnmounted(() => {
    connection?.stop();
  });
}
```

### Angular service

```ts
// signalr.service.ts
import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class SignalRService {
  private connection!: signalR.HubConnection;
  readonly notifications$ = new Subject<any>();

  async connect(hubUrl: string, channels: string[]) {
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl)
      .withAutomaticReconnect()
      .build();

    this.connection.on('ReceiveNotification', (n) => this.notifications$.next(n));
    await this.connection.start();
    for (const ch of channels) {
      await this.connection.invoke('JoinChannel', ch);
    }
  }

  async disconnect() {
    await this.connection?.stop();
  }
}
```

---

## Notification payload shape

Every `ReceiveNotification` event delivers an object with this structure:

```ts
interface Notification {
  channel:    string;       // e.g. "document-upload"
  eventType:  string;       // e.g. "upload-success"
  message:    string;       // human-readable text
  payload:    object | null; // arbitrary JSON from the backend
  targetUserId: string | null;
  timestamp:  string;       // ISO-8601 UTC, e.g. "2024-06-01T12:00:00Z"
}
```

---

## Troubleshooting

| Symptom | Likely cause | Fix |
|---|---|---|
| Connection rejected with CORS error | Hub's `AllowedOrigins` doesn't include your app's origin | Add your origin to `Cors.AllowedOrigins` in `appsettings.json` |
| Connection stuck at "Connecting…" | Hub is not running or wrong URL | Check `dotnet run` output; confirm URL |
| Events arrive but `notification.payload` is `null` | Backend didn't send a payload | Normal — payload is optional |
| Reconnect never fires | `withAutomaticReconnect()` not added | Add it to `HubConnectionBuilder` |
