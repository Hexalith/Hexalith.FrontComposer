using System.Globalization;

using Bunit;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Contracts.Badges;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Contracts.Storage;
using Hexalith.FrontComposer.Shell.Components.Rendering;
using Hexalith.FrontComposer.Shell.Extensions;
using Hexalith.FrontComposer.Shell.State.Theme;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.FluentUI.AspNetCore.Components;

using NSubstitute;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Rendering;

/// <summary>
/// Story 4-1 T6.7 / D9 / Murat review (NFR anchor) — subscription-leak gate.
/// Renders 100 <see cref="FcProjectionSubtitle"/> instances bound to a stub
/// <see cref="IBadgeCountService"/> that exposes a <c>SubscriberCount</c> property
/// and disposes them all (via <see cref="BunitContext"/> teardown which invokes
/// the component lifecycle's <see cref="IAsyncDisposable.DisposeAsync"/>);
/// asserts <c>SubscriberCount</c> returns to baseline. Catches circuit-teardown
/// leaks that would otherwise manifest as production GC pressure or stale
/// <c>StateHasChanged</c> calls into disposed components.
/// </summary>
public sealed class FcProjectionSubtitleLeakTests {
    private sealed class OrderProjection { }

    private sealed class CountingBadgeCountService : IBadgeCountService {
        private readonly CountingObservable _changed = new();

        public IReadOnlyDictionary<Type, int> Counts { get; } = new Dictionary<Type, int>();

        public IObservable<BadgeCountChangedArgs> CountChanged => _changed;

        public int TotalActionableItems => 0;

        public int SubscriberCount => _changed.SubscriberCount;

        private sealed class CountingObservable : IObservable<BadgeCountChangedArgs> {
            private readonly object _gate = new();
            private int _subscribers;

            public int SubscriberCount {
                get {
                    lock (_gate) {
                        return _subscribers;
                    }
                }
            }

            public IDisposable Subscribe(IObserver<BadgeCountChangedArgs> observer) {
                lock (_gate) {
                    _subscribers++;
                }

                return new Subscription(this);
            }

            private void Decrement() {
                lock (_gate) {
                    if (_subscribers > 0) {
                        _subscribers--;
                    }
                }
            }

            private sealed class Subscription : IDisposable {
                private readonly CountingObservable _owner;
                private bool _disposed;

                public Subscription(CountingObservable owner) {
                    _owner = owner;
                }

                public void Dispose() {
                    if (_disposed) {
                        return;
                    }

                    _disposed = true;
                    _owner.Decrement();
                }
            }
        }
    }

    [Fact]
    public void Rendering100SubtitlesAndDisposingReturnsSubscriberCountToBaseline() {
        CultureInfo.CurrentUICulture = new CultureInfo("en");
        CultureInfo.CurrentCulture = new CultureInfo("en");

        CountingBadgeCountService stub = new();

        // Render 100 subtitle components inside a scoped BunitContext so we can
        // dispose the entire context (which triggers IAsyncDisposable.DisposeAsync
        // on every rendered component via the test renderer's teardown).
        int contextBaseline;
        int peakAfter100Renders;
        using (BunitContext ctx = ConfigureContext(stub)) {
            // Other Shell consumers of IBadgeCountService (e.g., FcPaletteResultList
            // from Story 3-4 / FcHomeBadgeCountTile from Story 3-5) may subscribe
            // when the container resolves their dependency graphs. Capture the
            // context-baseline AFTER ConfigureContext so the assertion measures the
            // 100-component delta against what the context already had subscribed.
            contextBaseline = stub.SubscriberCount;

            for (int i = 0; i < 100; i++) {
                IRenderedComponent<FcProjectionSubtitle> _ = ctx.Render<FcProjectionSubtitle>(parameters => parameters
                    .Add(p => p.ProjectionType, typeof(OrderProjection))
                    .Add(p => p.Role, ProjectionRole.ActionQueue)
                    .Add(p => p.FallbackCount, i));
            }

            peakAfter100Renders = stub.SubscriberCount;
            (peakAfter100Renders - contextBaseline).ShouldBe(100,
                customMessage: "Each rendered FcProjectionSubtitle must subscribe exactly once to IBadgeCountService.CountChanged (D21 + D9).");
        }

        // After BunitContext disposal the test renderer has torn down all rendered
        // components — each component's IAsyncDisposable.DisposeAsync ran via the
        // dispatcher. SubscriberCount MUST return to ZERO (the stub outlives the
        // context, so any leftover subscription proves a leak — both 4-1 components
        // AND any context-baseline consumer are torn down with the renderer).
        stub.SubscriberCount.ShouldBe(0,
            customMessage:
                $"All FcProjectionSubtitle instances must release their CountChanged subscription on DisposeAsync (D9 disposal discipline) — leak found. "
                + $"Context baseline at start was {contextBaseline}; peak after 100 renders was {peakAfter100Renders}; current count = {stub.SubscriberCount}.");
    }

    private static BunitContext ConfigureContext(IBadgeCountService stub) {
        BunitContext ctx = new();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ = ctx.Services.AddLogging();
        _ = ctx.Services.AddFluentUIComponents();
        _ = ctx.Services.AddHexalithFrontComposerQuickstart();
        ctx.Services.Replace(ServiceDescriptor.Scoped<IStorageService, InMemoryStorageService>());
        ctx.Services.Replace(ServiceDescriptor.Scoped<IUserContextAccessor>(_ => {
            IUserContextAccessor accessor = Substitute.For<IUserContextAccessor>();
            accessor.TenantId.Returns("test-tenant");
            accessor.UserId.Returns("test-user");
            return accessor;
        }));
        IThemeService themeService = Substitute.For<IThemeService>();
        ctx.Services.Replace(ServiceDescriptor.Scoped<IThemeService>(_ => themeService));
        ctx.Services.Replace(ServiceDescriptor.Scoped<IBadgeCountService>(_ => stub));

        // Initialize Fluxor store BEFORE rendering.
        IStore store = ctx.Services.GetRequiredService<IStore>();
        store.InitializeAsync().GetAwaiter().GetResult();

        return ctx;
    }
}
