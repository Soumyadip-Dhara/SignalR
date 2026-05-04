# Backend Integration Guide

How to publish real-time notifications to the hub **from your backend services**.

---

## How it works

```
Your Backend Service                    SignalR Hub
       │                                     │
       │── POST /api/notifications/publish ──►│
       │   X-Api-Key: <your-key>              │
       │   { channel, eventType, … }          │
       │                                      │
       │◄── 200 { success: true } ────────────│
       │                                      │  ──push──►  Browser clients
```

Your service makes a single **HTTP POST** request. The hub instantly pushes the notification to all browser clients that have joined the target channel. No SignalR SDK is needed on the backend.

---

## Prerequisites

1. The hub is running and reachable from your backend network.
2. If the hub has API keys configured, you need a valid key in `X-Api-Key`.

See the [Configuration guide](./configuration.md) for API key setup.

---

## Endpoints at a glance

| Method | Path | Purpose |
|---|---|---|
| `POST` | `/api/notifications/publish` | Push to a **channel** (or one user) |
| `POST` | `/api/notifications/broadcast` | Push to **all** connected clients |
| `GET` | `/health` | Liveness check |

---

## Request body fields

### `POST /api/notifications/publish`

| Field | Type | Required | Description |
|---|---|---|---|
| `channel` | `string` | ✅ | Topic name. Clients subscribe with `JoinChannel`. |
| `eventType` | `string` | ✅ | Machine-readable event (e.g. `upload-success`). |
| `message` | `string` | | Human-readable text for the UI. |
| `payload` | `object` | | Any JSON object with extra data. |
| `targetUserId` | `string` | | If set, delivered **only** to that user's connections. |

### `POST /api/notifications/broadcast`

| Field | Type | Required | Description |
|---|---|---|---|
| `eventType` | `string` | ✅ | Machine-readable event. |
| `message` | `string` | | Human-readable text. |
| `payload` | `object` | | Any JSON object. |

---

## Code examples

### C# / .NET (`HttpClient`)

Add the helper below once in your project (e.g. `SignalRPublisher.cs`):

```csharp
public class SignalRPublisher
{
    private readonly HttpClient _http;
    private readonly string _hubBaseUrl;
    private readonly string _apiKey;

    public SignalRPublisher(HttpClient http, string hubBaseUrl, string apiKey)
    {
        _http = http;
        _hubBaseUrl = hubBaseUrl.TrimEnd('/');
        _apiKey = apiKey;
    }

    public async Task PublishAsync(string channel, string eventType,
        string message = "", object? payload = null, string? targetUserId = null)
    {
        var body = new
        {
            channel,
            eventType,
            message,
            payload,
            targetUserId
        };

        using var request = new HttpRequestMessage(
            HttpMethod.Post, $"{_hubBaseUrl}/api/notifications/publish");

        request.Headers.Add("X-Api-Key", _apiKey);
        request.Content = JsonContent.Create(body);

        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }
}
```

**Usage in a document upload handler:**

```csharp
var publisher = new SignalRPublisher(httpClient,
    hubBaseUrl: "https://your-hub-host",
    apiKey: "my-secret-key");

// After a document is saved successfully:
await publisher.PublishAsync(
    channel:   "document-upload",
    eventType: "upload-success",
    message:   $"{fileName} uploaded successfully.",
    payload:   new { documentId, fileName, sizeKb }
);
```

**ASP.NET Core DI registration (`Program.cs`):**

```csharp
builder.Services.AddHttpClient<SignalRPublisher>();
builder.Services.AddSingleton(sp =>
    new SignalRPublisher(
        sp.GetRequiredService<IHttpClientFactory>().CreateClient(),
        hubBaseUrl: builder.Configuration["SignalRHub:BaseUrl"]!,
        apiKey:     builder.Configuration["SignalRHub:ApiKey"]!
    ));
```

Add to `appsettings.json`:

```json
{
  "SignalRHub": {
    "BaseUrl": "https://your-hub-host",
    "ApiKey": "my-secret-key"
  }
}
```

---

### Node.js (`node-fetch` / built-in `fetch`)

```js
const HUB_URL = 'https://your-hub-host';
const API_KEY  = 'my-secret-key';

async function publishNotification({ channel, eventType, message, payload, targetUserId }) {
  const res = await fetch(`${HUB_URL}/api/notifications/publish`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'X-Api-Key': API_KEY,
    },
    body: JSON.stringify({ channel, eventType, message, payload, targetUserId }),
  });

  if (!res.ok) {
    const body = await res.text();
    throw new Error(`Hub returned ${res.status}: ${body}`);
  }

  return res.json();
}

// Example: after a file upload completes
await publishNotification({
  channel:   'document-upload',
  eventType: 'upload-success',
  message:   'Invoice_Q1.pdf uploaded successfully.',
  payload:   { documentId: 'doc-001', fileName: 'Invoice_Q1.pdf', sizeKb: 142 },
});
```

**Express.js middleware example:**

```js
// middleware/notifyHub.js
const HUB_URL = process.env.SIGNALR_HUB_URL;
const API_KEY  = process.env.SIGNALR_API_KEY;

async function notifyHub(channel, eventType, message, payload) {
  const res = await fetch(`${HUB_URL}/api/notifications/publish`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', 'X-Api-Key': API_KEY },
    body: JSON.stringify({ channel, eventType, message, payload }),
  });
  if (!res.ok) console.error('Hub publish failed:', await res.text());
}

module.exports = { notifyHub };
```

---

### Python (`requests` / `httpx`)

#### Using `requests`

```bash
pip install requests
```

```python
import requests

HUB_URL = "https://your-hub-host"
API_KEY  = "my-secret-key"

def publish_notification(channel, event_type, message="", payload=None, target_user_id=None):
    response = requests.post(
        f"{HUB_URL}/api/notifications/publish",
        json={
            "channel":      channel,
            "eventType":    event_type,
            "message":      message,
            "payload":      payload,
            "targetUserId": target_user_id,
        },
        headers={"X-Api-Key": API_KEY},
        timeout=10,
    )
    response.raise_for_status()
    return response.json()


# Example: after processing an order
publish_notification(
    channel="order-status",
    event_type="order-shipped",
    message="Your order #ORD-42 has shipped.",
    payload={"orderId": "ORD-42", "trackingNumber": "1Z999AA1"},
)
```

#### Using `httpx` (async)

```bash
pip install httpx
```

```python
import httpx

HUB_URL = "https://your-hub-host"
API_KEY  = "my-secret-key"

async def publish_notification(channel, event_type, message="", payload=None):
    async with httpx.AsyncClient() as client:
        resp = await client.post(
            f"{HUB_URL}/api/notifications/publish",
            json={"channel": channel, "eventType": event_type,
                  "message": message, "payload": payload},
            headers={"X-Api-Key": API_KEY},
            timeout=10,
        )
        resp.raise_for_status()
        return resp.json()
```

---

### Java (`OkHttp`)

```java
OkHttpClient client = new OkHttpClient();
MediaType JSON = MediaType.get("application/json");

String body = """
    {
      "channel": "order-status",
      "eventType": "order-shipped",
      "message": "Order #ORD-42 has shipped.",
      "payload": { "orderId": "ORD-42" }
    }
    """;

Request request = new Request.Builder()
    .url("https://your-hub-host/api/notifications/publish")
    .addHeader("X-Api-Key", "my-secret-key")
    .post(RequestBody.create(body, JSON))
    .build();

try (Response response = client.newCall(request).execute()) {
    System.out.println(response.body().string());
}
```

---

### PHP (`Guzzle`)

```php
use GuzzleHttp\Client;

$client = new Client(['base_uri' => 'https://your-hub-host']);

$response = $client->post('/api/notifications/publish', [
    'headers' => [
        'X-Api-Key'    => 'my-secret-key',
        'Content-Type' => 'application/json',
    ],
    'json' => [
        'channel'   => 'document-upload',
        'eventType' => 'upload-success',
        'message'   => 'Invoice_Q1.pdf uploaded successfully.',
        'payload'   => ['documentId' => 'doc-001', 'fileName' => 'Invoice_Q1.pdf'],
    ],
]);

$result = json_decode($response->getBody(), true);
```

---

### `curl` (testing / scripting)

```bash
# Publish to a channel
curl -X POST https://your-hub-host/api/notifications/publish \
  -H "Content-Type: application/json" \
  -H "X-Api-Key: my-secret-key" \
  -d '{
    "channel":   "document-upload",
    "eventType": "upload-success",
    "message":   "Invoice_Q1.pdf uploaded.",
    "payload":   { "documentId": "doc-001" }
  }'

# Broadcast to all clients
curl -X POST https://your-hub-host/api/notifications/broadcast \
  -H "Content-Type: application/json" \
  -H "X-Api-Key: my-secret-key" \
  -d '{
    "eventType": "maintenance-window",
    "message":   "Scheduled maintenance in 10 minutes."
  }'
```

---

## Sending to a specific user

If your hub authenticates users (via JWT or cookies), set `targetUserId` to the user's identifier. Only that user's connections receive the notification:

```json
{
  "channel":      "order-status",
  "eventType":    "order-ready",
  "message":      "Your order is ready for pickup.",
  "targetUserId": "user-abc-123"
}
```

When `targetUserId` is set, the `channel` field is still required and is forwarded in the notification payload (so the client knows the topic), but channel-group filtering is **not** applied.

---

## Expected responses

### Success (`200 OK`)

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

### Validation error (`400 Bad Request`)

```json
{
  "success": false,
  "message": "'channel' is required."
}
```

### Missing / invalid API key (`401 Unauthorized`)

```json
{
  "error": "Invalid or missing API key."
}
```

---

## Troubleshooting

| Symptom | Likely cause | Fix |
|---|---|---|
| `401 Unauthorized` | Missing or wrong `X-Api-Key` | Check the key matches one in `ApiKeys` in `appsettings.json` |
| `400 Bad Request` | Missing required field | Ensure `channel` and `eventType` are non-empty strings |
| Clients don't receive the event | Client hasn't joined the channel | Make sure `connection.invoke('JoinChannel', channel)` was called |
| Connection refused | Hub not running or wrong URL | Verify `dotnet run` is active and URL is correct |
