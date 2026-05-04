# Deployment Guide

How to run the Common SignalR Hub in production.

---

## Option 1 – Docker (recommended)

### 1. Create a `Dockerfile`

Place this file at the root of the repository:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/SignalRHub/SignalRHub.csproj SignalRHub/
RUN dotnet restore SignalRHub/SignalRHub.csproj
COPY src/SignalRHub/ SignalRHub/
RUN dotnet publish SignalRHub/SignalRHub.csproj -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SignalRHub.dll"]
```

### 2. Build and run

```bash
docker build -t signalr-hub .

docker run -d \
  -p 80:80 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ASPNETCORE_URLS=http://+:80 \
  -e Cors__AllowedOrigins__0=https://app-a.example.com \
  -e Cors__AllowedOrigins__1=https://app-b.example.com \
  -e ApiKeys__0=my-secret-key-for-doc-service \
  -e ApiKeys__1=my-secret-key-for-order-service \
  --name signalr-hub \
  signalr-hub
```

### 3. Verify

```bash
curl http://localhost/health
# → Healthy
```

---

## Option 2 – Docker Compose

Create `docker-compose.yml` at the root of the repository:

```yaml
version: "3.9"
services:
  signalr-hub:
    build: .
    ports:
      - "80:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:80
      - Cors__AllowedOrigins__0=https://app-a.example.com
      - ApiKeys__0=my-secret-key
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost/health"]
      interval: 30s
      timeout: 5s
      retries: 3
```

```bash
docker compose up -d
```

---

## Option 3 – Azure App Service

### Using the Azure CLI

```bash
# 1. Create a resource group and an App Service plan (Linux, free tier for testing)
az group create --name rg-signalr-hub --location eastus

az appservice plan create \
  --name plan-signalr-hub \
  --resource-group rg-signalr-hub \
  --sku B1 --is-linux

# 2. Create the web app
az webapp create \
  --name my-signalr-hub \
  --resource-group rg-signalr-hub \
  --plan plan-signalr-hub \
  --runtime "DOTNETCORE:8.0"

# 3. Set environment variables (app settings)
az webapp config appsettings set \
  --name my-signalr-hub \
  --resource-group rg-signalr-hub \
  --settings \
    Cors__AllowedOrigins__0="https://app-a.example.com" \
    ApiKeys__0="my-secret-key"

# 4. Enable WebSockets (required for SignalR)
az webapp config set \
  --name my-signalr-hub \
  --resource-group rg-signalr-hub \
  --web-sockets-enabled true

# 5. Deploy from the local folder
dotnet publish src/SignalRHub -c Release -o publish/
cd publish && zip -r ../hub.zip .
az webapp deploy \
  --name my-signalr-hub \
  --resource-group rg-signalr-hub \
  --src-path ../hub.zip
```

Your hub is now available at `https://my-signalr-hub.azurewebsites.net`.

> **Tip:** For high-scale scenarios, consider using [Azure SignalR Service](https://learn.microsoft.com/en-us/azure/azure-signalr/signalr-overview) as the transport layer. Add the `Microsoft.Azure.SignalR` NuGet package and call `.AddAzureSignalR()` in `Program.cs`.

---

## Option 4 – Linux server (`systemd`)

### 1. Publish the app

```bash
dotnet publish src/SignalRHub -c Release -o /opt/signalr-hub
```

### 2. Create a systemd service

```bash
sudo nano /etc/systemd/system/signalr-hub.service
```

```ini
[Unit]
Description=Common SignalR Hub
After=network.target

[Service]
WorkingDirectory=/opt/signalr-hub
ExecStart=/usr/bin/dotnet /opt/signalr-hub/SignalRHub.dll
Restart=always
RestartSec=10
SyslogIdentifier=signalr-hub
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://localhost:5000
Environment=Cors__AllowedOrigins__0=https://app-a.example.com
Environment=ApiKeys__0=my-secret-key

[Install]
WantedBy=multi-user.target
```

```bash
sudo systemctl daemon-reload
sudo systemctl enable --now signalr-hub
sudo systemctl status signalr-hub
```

### 3. Reverse proxy with nginx

```nginx
server {
    listen 443 ssl;
    server_name hub.example.com;

    # SSL certificate (e.g. from Let's Encrypt)
    ssl_certificate     /etc/letsencrypt/live/hub.example.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/hub.example.com/privkey.pem;

    location / {
        proxy_pass         http://localhost:5000;
        proxy_http_version 1.1;

        # Required for WebSocket upgrades
        proxy_set_header   Upgrade    $http_upgrade;
        proxy_set_header   Connection "upgrade";

        proxy_set_header   Host              $host;
        proxy_set_header   X-Real-IP         $remote_addr;
        proxy_set_header   X-Forwarded-For   $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;

        # Long timeout for persistent WebSocket connections
        proxy_read_timeout 3600s;
        proxy_send_timeout 3600s;
    }
}
```

---

## Production checklist

- [ ] Set `ASPNETCORE_ENVIRONMENT=Production`
- [ ] List all browser app origins in `Cors.AllowedOrigins` (do not leave empty)
- [ ] Add at least one API key in `ApiKeys` (do not leave empty)
- [ ] Enable WebSocket support in your reverse proxy or cloud platform
- [ ] Configure HTTPS (TLS termination at the proxy or app level)
- [ ] Set a long `proxy_read_timeout` (≥ 3600 s) for WebSocket connections
- [ ] Add a health-check probe pointing at `GET /health`
- [ ] Rotate API keys per service so you can revoke individual ones
- [ ] Store API keys in a secrets manager (not in source control)
