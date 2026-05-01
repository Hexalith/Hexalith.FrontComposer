using Hexalith.FrontComposer.Shell.Services.DevMode;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Hexalith.FrontComposer.Shell.Extensions;

/// <summary>
/// Registers development-only FrontComposer overlay services.
/// </summary>
public static class AddFrontComposerDevModeExtensions {
    /// <summary>
    /// Adds the FrontComposer development overlay services when the host environment is Development.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddFrontComposerDevMode(this IServiceCollection services) {
        ArgumentNullException.ThrowIfNull(services);

#if DEBUG
        IHostEnvironment? environment = FindRegisteredEnvironment(services);
        if (environment is not null) {
            _ = services.AddFrontComposerDevMode(environment);
        }
#endif
        return services;
    }

    /// <summary>
    /// Adds the FrontComposer development overlay services for an explicit host environment.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="environment">The host environment used to gate dev-mode registration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddFrontComposerDevMode(this IServiceCollection services, IHostEnvironment environment) {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(environment);

#if DEBUG
        if (environment.IsDevelopment()) {
            services.TryAddScoped<IDevModeOverlayController, DevModeOverlayController>();
            services.TryAddScoped<IRazorEmitter, RazorEmitter>();
            services.TryAddScoped<IClipboardJSModule, ClipboardJSModule>();
            services.TryAddScoped<IDevModeAnnotationSnapshotVisitor, DevModeAnnotationSnapshotVisitor>();
            services.TryAddSingleton<DevModeRegistrationLogMarker>();
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, DevModeRegistrationLogger>());
        }
        else {
            // HFC2010 — defensive log: AddFrontComposerDevMode invoked outside Development.
            // Should not fire in practice; production callers should not reach this overload.
            services.TryAddSingleton<DevModeNonDevelopmentMarker>();
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, DevModeNonDevelopmentLogger>());
        }
#endif
        return services;
    }

#if DEBUG
    private static IHostEnvironment? FindRegisteredEnvironment(IServiceCollection services) {
        for (int i = services.Count - 1; i >= 0; i--) {
            ServiceDescriptor descriptor = services[i];
            if (descriptor.ServiceType == typeof(IHostEnvironment)
                && descriptor.ImplementationInstance is IHostEnvironment environment) {
                return environment;
            }
        }

        // Fallback: ASP.NET Core's host builder registers IHostEnvironment via instance, but custom
        // test hosts may use a factory. Adopters using factory registration must call the explicit
        // AddFrontComposerDevMode(services, env) overload — the no-arg path fails closed (no-op).
        return null;
    }

    private sealed class DevModeRegistrationLogMarker {
        public int Logged;
    }

    private sealed class DevModeRegistrationLogger(
        ILogger<DevModeRegistrationLogger> logger,
        DevModeRegistrationLogMarker marker,
        IHostEnvironment environment) : IHostedService {
        public Task StartAsync(CancellationToken cancellationToken) {
            if (Interlocked.Exchange(ref marker.Logged, 1) == 0) {
                logger.LogInformation(
                    "FrontComposer dev-mode overlay registered. Environment={EnvironmentName} OverlayVersion={OverlayVersion} GradientLevels={GradientLevels}",
                    environment.EnvironmentName,
                    "6.5",
                    "Default,Level1,Level2,Level3,Level4");
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class DevModeNonDevelopmentMarker {
        public int Logged;
    }

    private sealed class DevModeNonDevelopmentLogger(
        ILogger<DevModeNonDevelopmentLogger> logger,
        DevModeNonDevelopmentMarker marker,
        IHostEnvironment environment) : IHostedService {
        public Task StartAsync(CancellationToken cancellationToken) {
            if (Interlocked.Exchange(ref marker.Logged, 1) == 0) {
                logger.LogInformation(
                    "HFC2010: FrontComposer dev-mode AddFrontComposerDevMode invoked outside Development; overlay services were not registered. Environment={EnvironmentName}",
                    environment.EnvironmentName);
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
#endif
}
