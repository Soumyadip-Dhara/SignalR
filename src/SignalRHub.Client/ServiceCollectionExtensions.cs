using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SignalRHub.Client;

/// <summary>
/// Extension methods for registering <see cref="ISignalRHubClient"/> in the
/// ASP.NET Core / Generic Host dependency-injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    private const string HttpClientName = "SignalRHubClient";

    /// <summary>
    /// Registers <see cref="ISignalRHubClient"/> and reads its configuration
    /// from the <c>SignalRHub</c> section of <c>appsettings.json</c>.
    /// </summary>
    /// <example>
    /// <code>
    /// // appsettings.json
    /// {
    ///   "SignalRHub": {
    ///     "HubBaseUrl": "https://your-hub-host",
    ///     "ApiKey":     "your-api-key"
    ///   }
    /// }
    ///
    /// // Program.cs
    /// builder.Services.AddSignalRHubClient(builder.Configuration);
    ///
    /// // Usage
    /// public class MyService(ISignalRHubClient hub) { ... }
    /// </code>
    /// </example>
    public static IServiceCollection AddSignalRHubClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<SignalRHubClientOptions>(
            configuration.GetSection(SignalRHubClientOptions.SectionName));

        RegisterHttpClient(services);
        return services;
    }

    /// <summary>
    /// Registers <see cref="ISignalRHubClient"/> using the supplied
    /// <paramref name="configure"/> delegate.
    /// </summary>
    /// <example>
    /// <code>
    /// builder.Services.AddSignalRHubClient(options =>
    /// {
    ///     options.HubBaseUrl = "https://your-hub-host";
    ///     options.ApiKey     = "your-api-key";
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddSignalRHubClient(
        this IServiceCollection services,
        Action<SignalRHubClientOptions> configure)
    {
        services.Configure(configure);
        RegisterHttpClient(services);
        return services;
    }

    // ── Shared registration ───────────────────────────────────────────────────

    private static void RegisterHttpClient(IServiceCollection services)
    {
        services
            .AddHttpClient<ISignalRHubClient, SignalRHubClient>(HttpClientName,
                (sp, client) =>
                {
                    var opts = sp
                        .GetRequiredService<Microsoft.Extensions.Options.IOptions<SignalRHubClientOptions>>()
                        .Value;

                    if (!string.IsNullOrWhiteSpace(opts.HubBaseUrl))
                        client.BaseAddress = new Uri(opts.HubBaseUrl.TrimEnd('/') + "/");

                    if (!string.IsNullOrWhiteSpace(opts.ApiKey))
                        client.DefaultRequestHeaders.Add("X-Api-Key", opts.ApiKey);
                });
    }
}
