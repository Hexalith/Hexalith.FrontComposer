using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Shell.Infrastructure.EventStore;
using Hexalith.FrontComposer.Shell.Services;
using Hexalith.FrontComposer.Shell.Services.Auth;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Hexalith.FrontComposer.Shell.Extensions;

/// <summary>
/// EventStore-backed communication registration for FrontComposer Shell.
/// </summary>
public static class EventStoreServiceExtensions {
    /// <summary>
    /// Registers EventStore-backed command, query, and projection subscription services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional EventStore options callback.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddHexalithEventStore(
        this IServiceCollection services,
        Action<EventStoreOptions>? configure = null) {
        ArgumentNullException.ThrowIfNull(services);

        _ = services.AddOptions<EventStoreOptions>()
            .Configure(options => configure?.Invoke(options))
            .ValidateOnStart();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<EventStoreOptions>, EventStoreOptionsValidator>());

        _ = services.AddHttpClient(EventStoreCommandClient.HttpClientName, ConfigureHttpClient);
        _ = services.AddHttpClient(EventStoreQueryClient.HttpClientName, ConfigureHttpClient);

        // Story 5-2 T6 — singleton classifier (stateless apart from logger).
        services.TryAddSingleton<EventStoreResponseClassifier>();

        services.TryAddScoped<EventStoreCommandClient>();
        services.TryAddScoped<EventStoreQueryClient>();
        services.TryAddScoped<IAuthRedirector, NoOpAuthRedirector>();
        services.TryAddScoped<ProjectionSubscriptionService>();
        services.TryAddScoped<IProjectionHubConnectionFactory, SignalRProjectionHubConnectionFactory>();
        services.TryAddScoped<ProjectionChangeNotifier>();
        services.TryAddScoped<IProjectionChangeNotifier>(sp => sp.GetRequiredService<ProjectionChangeNotifier>());
        services.TryAddScoped<IProjectionChangeNotifierWithTenant>(sp => sp.GetRequiredService<ProjectionChangeNotifier>());

        // Story 5-2 T4 — replace the NullActionQueueCountReader default with the EventStore-
        // backed reader so badge counts share the same response classifier + ETag cache seam
        // as projection page queries (AC7).
        ReplaceActionQueueCountReader(services);

        RemoveStubCommandService(services);
        services.TryAddScoped<ICommandService>(sp => sp.GetRequiredService<EventStoreCommandClient>());
        services.TryAddScoped<ICommandServiceWithLifecycle>(sp => sp.GetRequiredService<EventStoreCommandClient>());
        services.TryAddScoped<IQueryService>(sp => sp.GetRequiredService<EventStoreQueryClient>());
        services.TryAddScoped<IProjectionSubscription>(sp => sp.GetRequiredService<ProjectionSubscriptionService>());

        return services;
    }

    private static void ReplaceActionQueueCountReader(IServiceCollection services) {
        for (int i = services.Count - 1; i >= 0; i--) {
            ServiceDescriptor descriptor = services[i];
            if (descriptor.ServiceType == typeof(Hexalith.FrontComposer.Contracts.Badges.IActionQueueCountReader)
                && descriptor.ImplementationType == typeof(Hexalith.FrontComposer.Shell.Badges.NullActionQueueCountReader)) {
                services.RemoveAt(i);
            }
        }

        services.TryAddScoped<Hexalith.FrontComposer.Contracts.Badges.IActionQueueCountReader, Hexalith.FrontComposer.Shell.Badges.EventStoreActionQueueCountReader>();
    }

    private static void ConfigureHttpClient(IServiceProvider serviceProvider, HttpClient client) {
        EventStoreOptions options = serviceProvider.GetRequiredService<IOptions<EventStoreOptions>>().Value;
        client.BaseAddress = options.BaseAddress;
        client.Timeout = options.Timeout;
    }

    private static void RemoveStubCommandService(IServiceCollection services) {
        for (int i = services.Count - 1; i >= 0; i--) {
            ServiceDescriptor descriptor = services[i];
            if (descriptor.ServiceType == typeof(ICommandService)
                && descriptor.ImplementationType == typeof(StubCommandService)) {
                services.RemoveAt(i);
            }
        }
    }
}
