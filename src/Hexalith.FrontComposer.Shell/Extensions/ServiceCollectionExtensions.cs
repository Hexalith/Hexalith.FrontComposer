namespace Hexalith.FrontComposer.Shell.Extensions;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Storage;
using Hexalith.FrontComposer.Shell.State.Theme;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering FrontComposer Shell services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers Fluxor state management, storage services, and all FrontComposer Shell dependencies.
    /// <para>
    /// <b>Important:</b> The consuming application must place
    /// <c>&lt;Fluxor.Blazor.Web.StoreInitializer /&gt;</c> in its root layout component.
    /// The Shell is a Razor Class Library and cannot place it automatically.
    /// </para>
    /// </summary>
    /// <remarks>
    /// DI scope divergence: on Blazor Server, services are scoped per circuit;
    /// on Blazor WebAssembly, services are scoped per application instance.
    /// </remarks>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddHexalithFrontComposer(this IServiceCollection services)
    {
        services.AddFluxor(o => o.ScanAssemblies(typeof(FrontComposerThemeState).Assembly));
        services.AddSingleton<IStorageService, InMemoryStorageService>();
        return services;
    }
}
