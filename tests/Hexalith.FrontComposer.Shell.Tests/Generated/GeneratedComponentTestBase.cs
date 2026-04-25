using System.Reflection;

using Bunit;

using Fluxor;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Services;
using Hexalith.FrontComposer.Shell.Services.Lifecycle;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.FluentUI.AspNetCore.Components;

using NSubstitute;

namespace Hexalith.FrontComposer.Shell.Tests.Generated;

public abstract class GeneratedComponentTestBase : BunitContext
{
    private bool _storeInitialized;

    protected GeneratedComponentTestBase(params Assembly[] scanAssemblies)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        _ = Services.AddFluentUIComponents();
        Services.Replace(ServiceDescriptor.Scoped<IThemeService>(_ => Substitute.For<IThemeService>()));
        _ = Services.AddLogging();
        _ = Services.AddLocalization();
        _ = Services.AddFluxor(o =>
        {
            foreach (Assembly assembly in scanAssemblies)
            {
                _ = o.ScanAssemblies(assembly);
            }
        });

        // Story 4-4 — generated grid views inject IState<LoadedPageState>. Register the
        // LoadedPageFeature directly (Shell-assembly scan would duplicate FrontComposerTheme via
        // TestableFrontComposerThemeFeature in Shell.Tests). Fluxor's State<TState> resolves via
        // IFeature<TState>; both descriptors point at the same singleton instance.
        _ = Services.AddSingleton<Hexalith.FrontComposer.Shell.State.DataGridNavigation.LoadedPageFeature>();
        _ = Services.AddSingleton<Fluxor.IFeature<Hexalith.FrontComposer.Shell.State.DataGridNavigation.LoadedPageState>>(
            sp => sp.GetRequiredService<Hexalith.FrontComposer.Shell.State.DataGridNavigation.LoadedPageFeature>());
        _ = Services.AddSingleton<Fluxor.IFeature>(
            sp => sp.GetRequiredService<Hexalith.FrontComposer.Shell.State.DataGridNavigation.LoadedPageFeature>());

        // Zero-delay stub so bUnit tests stay deterministic under CI load.
        _ = Services.Configure<StubCommandServiceOptions>(o =>
        {
            o.AcknowledgeDelayMs = 0;
            o.SyncingDelayMs = 0;
            o.ConfirmDelayMs = 0;
        });
        _ = Services.AddScoped<ICommandService, StubCommandService>();

        // Story 2-2 — renderers resolve these from DI.
        _ = Services.AddOptions<FcShellOptions>();
        _ = Services.AddScoped<InlinePopoverRegistry>();
        _ = Services.AddScoped<ILastUsedSubscriberRegistry, NoopLastUsedSubscriberRegistry>();
        _ = Services.AddScoped<ICommandPageContext, NullCommandPageContext>();

        // Story 2-3 — generated forms resolve ILifecycleBridgeRegistry + IUlidFactory; stub out so bUnit tests
        // don't activate the actual bridge (which requires a fully-wired Fluxor subscriber).
        _ = Services.AddScoped<ILifecycleBridgeRegistry, NoopLifecycleBridgeRegistry>();
        _ = Services.AddSingleton<IUlidFactory, UlidFactory>();
        _ = Services.AddOptions<LifecycleOptions>();
        _ = Services.AddScoped<ILifecycleStateService, LifecycleStateService>();

        // Story 2-4 — FcLifecycleWrapper injects TimeProvider; use the system clock by default
        // so generated-form rendering doesn't block on a fake clock that never ticks.
        _ = Services.AddSingleton(TimeProvider.System);

        // Story 4-4 T2.1 / T2.5 — generated grid views inject DataGridScrollInterop and
        // IProjectionPageLoader. Loose JS interop mode swallows the JS calls; the loader
        // returns an empty page by default (Story 4-4 NullProjectionPageLoader).
        _ = Services.AddScoped<DataGridScrollInterop>();
        _ = Services.AddScoped<
            Hexalith.FrontComposer.Shell.State.DataGridNavigation.IProjectionPageLoader,
            Hexalith.FrontComposer.Shell.State.DataGridNavigation.NullProjectionPageLoader>();
    }

    protected async Task InitializeStoreAsync()
    {
        if (_storeInitialized)
        {
            return;
        }

        IStore store = Services.GetRequiredService<IStore>();
        await store.InitializeAsync().ConfigureAwait(false);
        _storeInitialized = true;
    }

    private sealed class NoopLastUsedSubscriberRegistry : ILastUsedSubscriberRegistry
    {
        public void Ensure<TSubscriber>() where TSubscriber : class, IDisposable
        {
        }
    }

    private sealed class NoopLifecycleBridgeRegistry : ILifecycleBridgeRegistry
    {
        public void Ensure<TBridge>() where TBridge : class, IDisposable
        {
        }
    }
}
