# Quick Start

Get the hub running locally in under 5 minutes.

## Prerequisites

| Tool | Version |
|---|---|
| [.NET SDK](https://dotnet.microsoft.com/download) | 8.0 or later |
| Any modern browser | – |

---

## Step 1 – Clone the repository

```bash
git clone https://github.com/Soumyadip-Dhara/SignalR.git
cd SignalR
```

## Step 2 – Start the hub

```bash
cd src/SignalRHub
dotnet run
```

You should see output similar to:

```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[0]
      Application started.
```

> **Tip:** The hub binds to `http://localhost:5000` by default.
> Change the port with `dotnet run --urls "http://localhost:7000"`.

## Step 3 – Verify the hub is alive

```bash
curl http://localhost:5000/health
# → 200 Healthy
```

## Step 4 – Open the demo client

Open `demo/index.html` in your browser (double-click the file).

1. The **Hub URL** field is pre-filled with `http://localhost:5000/hubs/notifications`.
2. Click **Connect** — the status dot turns green.
3. Type `document-upload` in the Channel field and click **Join**.
4. Click **Publish notification** — you will see the event appear in the Event log immediately.

> **CORS note:** The demo page is loaded from `file://`, which is not a normal origin.
> During development, leave `Cors.AllowedOrigins` empty in `appsettings.json` so the
> hub accepts any origin. In production, add your app's URL. See the
> [Configuration guide](./configuration.md) for details.

---

## Next steps

| Goal | Guide |
|---|---|
| Connect a web frontend (React, Vue, Vanilla JS…) | [Frontend integration](./integrate-frontend.md) |
| Publish notifications from a backend service | [Backend integration](./integrate-backend.md) |
| Configure CORS, API keys, and ports | [Configuration](./configuration.md) |
| Deploy to Docker / Azure | [Deployment](./deployment.md) |
| Full API reference | [API reference](./api-reference.md) |
