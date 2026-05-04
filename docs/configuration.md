# Configuration Reference

All runtime configuration lives in `src/SignalRHub/appsettings.json` and can be overridden with environment variables (recommended for production).

---

## `appsettings.json` structure

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Cors": {
    "AllowedOrigins": []
  },
  "ApiKeys": []
}
```

---

## CORS (`Cors.AllowedOrigins`)

Controls which browser origins are allowed to open a WebSocket (or SSE/long-poll) connection to the hub.

| Value | Behaviour |
|---|---|
| Empty array `[]` | **Allow all origins** (development convenience). |
| One or more URLs | Only those exact origins are allowed. |

### Example – production setup

```json
{
  "Cors": {
    "AllowedOrigins": [
      "https://app-a.example.com",
      "https://app-b.example.com",
      "https://admin.example.com"
    ]
  }
}
```

### Environment variable override

```
Cors__AllowedOrigins__0=https://app-a.example.com
Cors__AllowedOrigins__1=https://app-b.example.com
```

> **Note:** Use double underscores (`__`) to navigate JSON levels in environment variables (.NET configuration convention).

---

## API Keys (`ApiKeys`)

The `X-Api-Key` header guards the REST endpoints (`POST /api/notifications/publish` and `POST /api/notifications/broadcast`).

| Value | Behaviour |
|---|---|
| Empty array `[]` | **No enforcement** (development convenience). |
| One or more strings | Requests must include a matching key in the `X-Api-Key` header. |

Each backend service that publishes notifications should have its own key so you can rotate or revoke individual service access without affecting others.

### Example – multiple service keys

```json
{
  "ApiKeys": [
    "key-for-document-storage-service",
    "key-for-order-management-service",
    "key-for-crm-integration"
  ]
}
```

### Environment variable override

```
ApiKeys__0=key-for-document-storage-service
ApiKeys__1=key-for-order-management-service
```

### Rotating a key

1. Add the new key to `ApiKeys` alongside the old one.
2. Update the backend service that uses the old key.
3. Remove the old key from `ApiKeys`.
4. Restart (or reload configuration of) the hub.

---

## Logging (`Logging.LogLevel`)

Standard .NET logging configuration.

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "SignalRHub": "Debug"
    }
  }
}
```

Set `SignalRHub` to `Debug` to see every connection, channel join/leave, and publish event.

---

## Ports and URLs

The default port is `5000`. Override it at startup without modifying any file:

```bash
# Command line
dotnet run --urls "http://0.0.0.0:8080"

# Environment variable
ASPNETCORE_URLS=http://0.0.0.0:8080 dotnet run

# Docker / container (see Deployment guide)
ENV ASPNETCORE_URLS=http://+:80
```

---

## SignalR hub options

Advanced SignalR settings are configured in `Program.cs`. Common overrides:

```csharp
builder.Services.AddSignalR(options =>
{
    // Maximum message size (bytes) the hub accepts from clients.
    // Default: 32 768 (32 KB).
    options.MaximumReceiveMessageSize = 65_536;

    // How long a client can go without sending a keep-alive ping.
    // Default: 30 s.
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);

    // Interval between server-to-client keep-alive pings.
    // Default: 15 s.
    options.KeepAliveInterval = TimeSpan.FromSeconds(20);
});
```

---

## Environment-specific overrides

.NET layered configuration means `appsettings.Development.json` values override `appsettings.json` when `ASPNETCORE_ENVIRONMENT=Development`.

Use this to keep your production config safe while keeping development convenient:

**`appsettings.json`** (production defaults)

```json
{
  "Cors": { "AllowedOrigins": [] },
  "ApiKeys": []
}
```

**`appsettings.Development.json`** (dev overrides — not committed to source control)

```json
{
  "Cors": { "AllowedOrigins": [] },
  "ApiKeys": []
}
```

For production secrets (API keys), prefer **environment variables** or a secrets manager over storing values in `appsettings.json`.

---

## Full environment-variable reference

| Environment Variable | JSON equivalent | Example |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | – | `Production` |
| `ASPNETCORE_URLS` | – | `http://+:80` |
| `Cors__AllowedOrigins__0` | `Cors.AllowedOrigins[0]` | `https://app.example.com` |
| `ApiKeys__0` | `ApiKeys[0]` | `my-secret-key` |
| `Logging__LogLevel__Default` | `Logging.LogLevel.Default` | `Warning` |
