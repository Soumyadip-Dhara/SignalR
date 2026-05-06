# signalrhub-angular

> **npm package** — zero-boilerplate Angular integration for the Common SignalR Hub.

## Installation

```bash
npm install signalrhub-angular @microsoft/signalr
```

> **Requires Angular ≥ 19.2.20.** Earlier versions of Angular (≤ 18.x) contain unpatched
> XSS and XSRF vulnerabilities — see the [Angular security advisories](https://github.com/angular/angular/security/advisories).

## Setup (one call in `AppModule` or `bootstrapApplication`)

### NgModule approach

```ts
// app.module.ts
import { SignalRHubModule } from 'signalrhub-angular';

@NgModule({
  imports: [
    SignalRHubModule.forRoot({
      hubUrl: 'https://your-hub-host/hubs/notifications'
    })
  ]
})
export class AppModule {}
```

### Standalone / `bootstrapApplication` approach

```ts
// main.ts
import { bootstrapApplication } from '@angular/platform-browser';
import { SignalRHubModule } from 'signalrhub-angular';
import { AppComponent } from './app/app.component';

bootstrapApplication(AppComponent, {
  providers: [
    ...SignalRHubModule.forRoot({
      hubUrl: 'https://your-hub-host/hubs/notifications'
    }).providers!
  ]
});
```

## Receiving notifications in a component

```ts
import { Component, OnInit } from '@angular/core';
import { SignalRHubService } from 'signalrhub-angular';

@Component({ selector: 'app-notifications', template: '' })
export class NotificationsComponent implements OnInit {
  constructor(private hub: SignalRHubService) {}

  ngOnInit() {
    // Join the channel
    this.hub.joinChannel('document-upload');

    // React to every notification on that channel
    this.hub.onNotification('document-upload').subscribe(notification => {
      console.log(`[${notification.eventType}]`, notification.message, notification.payload);
    });
  }
}
```

## Configuration options

| Property | Required | Default | Description |
|---|---|---|---|
| `hubUrl` | ✅ | — | Full URL of the hub WebSocket endpoint. |
| `autoConnect` | | `true` | Start the connection automatically on first injection. |
| `logLevel` | | `Warning` | SignalR client log verbosity (`LogLevel` from `@microsoft/signalr`). |

## `SignalRHubService` API

### Connection management

| Method | Description |
|---|---|
| `connect()` | Start the connection (called automatically unless `autoConnect: false`). |
| `disconnect()` | Stop the connection. |
| `state` | Snapshot of the current state: `'Disconnected' \| 'Connecting' \| 'Connected' \| 'Reconnecting'`. |
| `state$` | `Observable<HubConnectionState>` — emits on every state change. |

### Channel management

| Method | Description |
|---|---|
| `joinChannel(channel)` | Subscribe to a channel. |
| `leaveChannel(channel)` | Unsubscribe from a channel. |

### Observables

| Method | Description |
|---|---|
| `onNotification(channel?)` | Emits every `NotificationMessage`. Pass a channel name to filter. |
| `onJoinedChannel()` | Emits channel name on successful join confirmation. |
| `onLeftChannel()` | Emits channel name on successful leave confirmation. |

## `NotificationMessage` shape

```ts
interface NotificationMessage {
  channel:       string;
  eventType:     string;
  message:       string;
  payload?:      unknown;
  targetUserId?: string;
  timestamp:     string;  // ISO 8601 UTC
}
```
