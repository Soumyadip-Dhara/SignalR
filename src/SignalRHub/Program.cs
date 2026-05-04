using SignalRHub.Hubs;
using SignalRHub.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ── Controllers ──────────────────────────────────────────────────────────────
builder.Services.AddControllers();

// ── SignalR ──────────────────────────────────────────────────────────────────
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
});

// ── CORS ─────────────────────────────────────────────────────────────────────
// Allow origins configured in appsettings.json so that browser clients from
// other applications can connect to the hub.
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("HubCorsPolicy", policy =>
    {
        if (allowedOrigins.Length > 0)
            policy.WithOrigins(allowedOrigins);
        else
            // In dev mode (no origins configured) accept any origin while still
            // supporting credentials (required for SignalR WebSocket connections).
            policy.SetIsOriginAllowed(_ => true);

        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// ── Health checks ─────────────────────────────────────────────────────────────
builder.Services.AddHealthChecks();

var app = builder.Build();

// ── Middleware pipeline ───────────────────────────────────────────────────────
app.UseCors("HubCorsPolicy");

app.UseMiddleware<ApiKeyMiddleware>();

app.MapHealthChecks("/health");
app.MapControllers();

// All SignalR connections go through /hubs/notifications.
// Clients from any application connect here to receive real-time events.
app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();

// Make Program accessible to the integration test project.
public partial class Program { }
