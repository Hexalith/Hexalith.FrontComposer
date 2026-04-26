using System.Reflection;

using Bunit;

using Fluxor;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Services;
using Hexalith.FrontComposer.Shell.Services.Lifecycle;
using Hexalith.FrontComposer.Shell.State.ProjectionConnection;
using Hexalith.FrontComposer.Shell.State.ReconnectionReconciliation;

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

        // Story 4-4 — generated grid views also inject IState<DataGridNavigationState>. Register
        // the feature alongside LoadedPageFeature so the Counter / Status projection bUnit tests
        // can resolve the dependency without scanning the Shell assembly.
        _ = Services.AddSingleton<Hexalith.FrontComposer.Shell.State.DataGridNavigation.DataGridNavigationFeature>();
        _ = Services.AddSingleton<Fluxor.IFeature<Hexalith.FrontComposer.Shell.State.DataGridNavigation.DataGridNavigationState>>(
            sp => sp.GetRequiredService<Hexalith.FrontComposer.Shell.State.DataGridNavigation.DataGridNavigationFeature>());
        _ = Services.AddSingleton<Fluxor.IFeature>(
            sp => sp.GetRequiredService<Hexalith.FrontComposer.Shell.State.DataGridNavigation.DataGridNavigationFeature>());

        // Story 4-5 T1 / D2 — generated grid views inject IState<ExpandedRowState>. Register
        // the ephemeral feature alongside LoadedPageFeature so bUnit tests covering Default /
        // ActionQueue / Dashboard strategies can resolve the dependency.
        _ = Services.AddSingleton<Hexalith.FrontComposer.Shell.State.ExpandedRow.ExpandedRowFeature>();
        _ = Services.AddSingleton<Fluxor.IFeature<Hexalith.FrontComposer.Shell.State.ExpandedRow.ExpandedRowState>>(
            sp => sp.GetRequiredService<Hexalith.FrontComposer.Shell.State.ExpandedRow.ExpandedRowFeature>());
        _ = Services.AddSingleton<Fluxor.IFeature>(
            sp => sp.GetRequiredService<Hexalith.FrontComposer.Shell.State.ExpandedRow.ExpandedRowFeature>());

        // Story 5-4 Pass-2 — generated grid views inject IState<ReconciliationSweepState>
        // and IProjectionFallbackRefreshScheduler for visible-lane reconciliation. Register
        // the feature directly so generated bUnit tests can render without scanning Shell.
        _ = Services.AddSingleton<ReconciliationSweepFeature>();
        _ = Services.AddSingleton<Fluxor.IFeature<ReconciliationSweepState>>(
            sp => sp.GetRequiredService<ReconciliationSweepFeature>());
        _ = Services.AddSingleton<Fluxor.IFeature>(
            sp => sp.GetRequiredService<ReconciliationSweepFeature>());
        _ = Services.AddScoped<IProjectionFallbackRefreshScheduler, NoopProjectionFallbackRefreshScheduler>();

        // Story 4-5 T1 / D7 — generated views inject IExpandInRowJSModule for the FcExpandInRowDetail
        // component. Loose JS interop mode swallows the underlying JS calls; tests that need to
        // assert the scroll-stabilizer call use NSubstitute to verify dispatch counts.
        _ = Services.AddScoped<
            Hexalith.FrontComposer.Shell.Services.IExpandInRowJSModule,
            Hexalith.FrontComposer.Shell.Services.ExpandInRowJSModule>();

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
        _ = Services.AddScoped<IProjectionConnectionState, ProjectionConnectionStateService>();

        // Story 4-4 T2.1 / T2.5 — generated grid views inject DataGridScrollInterop and
        // IProjectionPageLoader. Loose JS interop mode swallows the JS calls; generated tests that
        // cross the server-side threshold must replace NullProjectionPageLoader with a real stub.
        _ = Services.AddScoped<DataGridScrollInterop>();
        _ = Services.AddScoped<
            Hexalith.FrontComposer.Shell.State.DataGridNavigation.IProjectionPageLoader,
            Hexalith.FrontComposer.Shell.State.DataGridNavigation.NullProjectionPageLoader>();

        // Story 5-2 — generated forms inject ICommandFeedbackPublisher (warning channel)
        // and IAuthRedirector (401 redirect seam).
        _ = Services.AddScoped<
            Hexalith.FrontComposer.Shell.Services.Feedback.ICommandFeedbackPublisher,
            Hexalith.FrontComposer.Shell.Services.Feedback.CommandFeedbackPublisher>();
        _ = Services.AddScoped<
            Hexalith.FrontComposer.Contracts.Communication.IAuthRedirector,
            Hexalith.FrontComposer.Shell.Services.Auth.NoOpAuthRedirector>();
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

    private sealed class NoopProjectionFallbackRefreshScheduler : IProjectionFallbackRefreshScheduler
    {
        public IDisposable RegisterLane(ProjectionFallbackLane lane) => NoopDisposable.Instance;

        public Task<int> TriggerFallbackOnceAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);

        public Task<int> TriggerNudgeRefreshAsync(
            string projectionType,
            string tenantId,
            CancellationToken cancellationToken = default) => Task.FromResult(0);

        private sealed class NoopDisposable : IDisposable
        {
            public static NoopDisposable Instance { get; } = new();

            public void Dispose()
            {
            }
        }
    }
}
