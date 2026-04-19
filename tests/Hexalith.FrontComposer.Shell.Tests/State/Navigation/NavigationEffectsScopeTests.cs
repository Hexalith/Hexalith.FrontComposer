// ATDD RED PHASE — Story 3-2 Task 10.6
// Fails at compile until Task 3 (NavigationEffects + NavigationPersistenceBlob) lands.
// Asserts the fail-closed tenant scoping contract: null/empty/whitespace tenant or user
// MUST log HFC2105 AND MUST NOT call storage.SetAsync (BOTH assertions required —
// memory feedback `feedback_tenant_isolation_fail_closed.md` + Murat party-mode review).

using System.Collections.Immutable;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Diagnostics;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Contracts.Storage;
using Hexalith.FrontComposer.Shell.State;
using Hexalith.FrontComposer.Shell.State.Navigation;

using Microsoft.Extensions.Logging;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.State.Navigation;

/// <summary>
/// Story 3-2 Task 10.6 — fail-closed tenant/user scope guard for <see cref="NavigationEffects"/>.
/// Mirrors the existing <c>ThemeEffectsScopeTests</c> pattern so adopters reading one understand
/// the other. Decisions D12, D14, D23; ADR-037, ADR-038. AC2.
/// </summary>
public sealed class NavigationEffectsScopeTests
{
    [Fact]
    public async Task PersistsOnValidScope()
    {
        CancellationToken ct = Xunit.TestContext.Current.CancellationToken;
        var storage = new InMemoryStorageService();
        ILogger<NavigationEffects> logger = Substitute.For<ILogger<NavigationEffects>>();
        IUserContextAccessor accessor = MakeAccessor("acme", "alice");
        IState<FrontComposerNavigationState> state = FakeState(new(
            SidebarCollapsed: true,
            CollapsedGroups: ImmutableDictionary<string, bool>.Empty
                .WithComparers(StringComparer.Ordinal)
                .Add("Counter", true),
            CurrentViewport: ViewportTier.Desktop));

        var sut = new NavigationEffects(storage, accessor, logger, state);

        await sut.HandleSidebarToggled(new SidebarToggledAction("c1"), Substitute.For<IDispatcher>());

        string expectedKey = StorageKeys.BuildKey("acme", "alice", "nav");
        NavigationPersistenceBlob? stored = await storage.GetAsync<NavigationPersistenceBlob>(expectedKey, ct);
        stored.ShouldNotBeNull();
        stored!.SidebarCollapsed.ShouldBeTrue();
        stored.CollapsedGroups.ShouldContainKeyAndValue("Counter", true);
    }

    [Fact]
    public async Task SkipsOnNullTenant_LogsAndDoesNotCallStorage()
    {
        IStorageService storage = Substitute.For<IStorageService>();
        ILogger<NavigationEffects> logger = Substitute.For<ILogger<NavigationEffects>>();
        IUserContextAccessor accessor = MakeAccessor(tenantId: null, userId: "alice");
        IState<FrontComposerNavigationState> state = FakeState(DefaultState());

        var sut = new NavigationEffects(storage, accessor, logger, state);

        await sut.HandleSidebarToggled(new SidebarToggledAction("c1"), Substitute.For<IDispatcher>());

        _ = storage.DidNotReceiveWithAnyArgs().SetAsync(default!, default(NavigationPersistenceBlob)!, Arg.Any<CancellationToken>());
        AssertLoggedInformation(logger, FcDiagnosticIds.HFC2105_StoragePersistenceSkipped);
    }

    [Fact]
    public async Task SkipsOnNullUser_LogsAndDoesNotCallStorage()
    {
        IStorageService storage = Substitute.For<IStorageService>();
        ILogger<NavigationEffects> logger = Substitute.For<ILogger<NavigationEffects>>();
        IUserContextAccessor accessor = MakeAccessor(tenantId: "acme", userId: null);
        IState<FrontComposerNavigationState> state = FakeState(DefaultState());

        var sut = new NavigationEffects(storage, accessor, logger, state);

        await sut.HandleSidebarToggled(new SidebarToggledAction("c1"), Substitute.For<IDispatcher>());

        _ = storage.DidNotReceiveWithAnyArgs().SetAsync(default!, default(NavigationPersistenceBlob)!, Arg.Any<CancellationToken>());
        AssertLoggedInformation(logger, FcDiagnosticIds.HFC2105_StoragePersistenceSkipped);
    }

    [Theory]
    [InlineData("   ", "alice")]
    [InlineData("acme", " ")]
    [InlineData("", "alice")]
    [InlineData("acme", "")]
    public async Task SkipsOnWhitespaceUserContext_LogsAndDoesNotCallStorage(string tenantId, string userId)
    {
        IStorageService storage = Substitute.For<IStorageService>();
        ILogger<NavigationEffects> logger = Substitute.For<ILogger<NavigationEffects>>();
        IUserContextAccessor accessor = MakeAccessor(tenantId, userId);
        IState<FrontComposerNavigationState> state = FakeState(DefaultState());

        var sut = new NavigationEffects(storage, accessor, logger, state);

        await sut.HandleSidebarToggled(new SidebarToggledAction("c1"), Substitute.For<IDispatcher>());

        _ = storage.DidNotReceiveWithAnyArgs().SetAsync(default!, default(NavigationPersistenceBlob)!, Arg.Any<CancellationToken>());
        AssertLoggedInformation(logger, FcDiagnosticIds.HFC2105_StoragePersistenceSkipped);
    }

    [Fact]
    public void NoEffectMethodAcceptsViewportTierChangedAction()
    {
        // F4 — intent-accurate reflection: any [EffectMethod] whose first parameter is
        // ViewportTierChangedAction violates ADR-037 (viewport is NEVER persisted), regardless
        // of method name. The previous test filtered by name-pattern and could silently miss a
        // newly-named persist method (e.g., HandleNavStateChanged).
        System.Reflection.MethodInfo? offender = typeof(NavigationEffects)
            .GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)
            .FirstOrDefault(m =>
                m.GetCustomAttributes(typeof(EffectMethodAttribute), inherit: false).Length > 0 &&
                m.GetParameters().FirstOrDefault()?.ParameterType == typeof(ViewportTierChangedAction));

        offender.ShouldBeNull(
            $"ADR-037: no [EffectMethod] may accept ViewportTierChangedAction. Found: {offender?.Name ?? "<none>"}");
    }

    [Fact]
    public void NoEffectMethodAcceptsNavigationHydratedAction()
    {
        // F4 — companion invariant for ADR-038: hydrate is read-only (no persist trigger).
        System.Reflection.MethodInfo? offender = typeof(NavigationEffects)
            .GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)
            .Where(m => m.GetCustomAttributes(typeof(EffectMethodAttribute), inherit: false).Length > 0)
            .Where(m => m.GetParameters().FirstOrDefault()?.ParameterType == typeof(NavigationHydratedAction))
            // HandleNavigationHydrated is allowed if it does NOT write to storage — that's a
            // behavioral assertion handled by HydrateDoesNotRePersist. This reflection check
            // covers the effect-subscription level only.
            .FirstOrDefault(m => m.Name.StartsWith("HandlePersist", StringComparison.Ordinal));

        offender.ShouldBeNull(
            $"ADR-038: no HandlePersist* [EffectMethod] may accept NavigationHydratedAction. Found: {offender?.Name ?? "<none>"}");
    }

    [Fact]
    public async Task HydrateDoesNotRePersist()
    {
        // ADR-038 (companion to D23) — NavigationHydratedAction was REMOVED from the persist
        // trigger set on 2026-04-18 to eliminate the pre-hydration SSR ordering surface.
        CancellationToken ct = Xunit.TestContext.Current.CancellationToken;
        var storage = new InMemoryStorageService();
        string key = StorageKeys.BuildKey("acme", "alice", "nav");
        // Pre-seed storage so we can detect re-writes via a distinct observer.
        NavigationPersistenceBlob seeded = new(
            SidebarCollapsed: true,
            CollapsedGroups: new Dictionary<string, bool>(StringComparer.Ordinal) { ["Counter"] = true });
        await storage.SetAsync(key, seeded, ct);

        ILogger<NavigationEffects> logger = Substitute.For<ILogger<NavigationEffects>>();
        IUserContextAccessor accessor = MakeAccessor("acme", "alice");
        IState<FrontComposerNavigationState> state = FakeState(DefaultState());

        // A spying decorator so we can assert no SetAsync fires for hydrate.
        ObservingStorage spy = new(storage);
        var sut = new NavigationEffects(spy, accessor, logger, state);

        await sut.HandleNavigationHydrated(
            new NavigationHydratedAction(SidebarCollapsed: true, CollapsedGroups: ImmutableDictionary<string, bool>.Empty),
            Substitute.For<IDispatcher>());

        spy.SetAsyncCallCount.ShouldBe(0, "ADR-038: hydrate is read-only from the storage perspective.");
    }

    [Fact]
    public async Task HandleAppInitialized_ValidContextEmptyStorage_DoesNotDispatch()
    {
        var storage = new InMemoryStorageService();
        ILogger<NavigationEffects> logger = Substitute.For<ILogger<NavigationEffects>>();
        IUserContextAccessor accessor = MakeAccessor("acme", "alice");
        IState<FrontComposerNavigationState> state = FakeState(DefaultState());
        IDispatcher dispatcher = Substitute.For<IDispatcher>();
        var sut = new NavigationEffects(storage, accessor, logger, state);

        await sut.HandleAppInitialized(new AppInitializedAction("c1"), dispatcher);

        dispatcher.DidNotReceiveWithAnyArgs().Dispatch(default!);
    }

    [Fact]
    public async Task HandleAppInitialized_ValidContextWithBlob_DispatchesNavigationHydrated()
    {
        CancellationToken ct = Xunit.TestContext.Current.CancellationToken;
        var storage = new InMemoryStorageService();
        ILogger<NavigationEffects> logger = Substitute.For<ILogger<NavigationEffects>>();
        IUserContextAccessor accessor = MakeAccessor("acme", "alice");
        IState<FrontComposerNavigationState> state = FakeState(DefaultState());
        IDispatcher dispatcher = Substitute.For<IDispatcher>();
        string key = StorageKeys.BuildKey("acme", "alice", "nav");
        NavigationPersistenceBlob blob = new(
            SidebarCollapsed: true,
            CollapsedGroups: new Dictionary<string, bool>(StringComparer.Ordinal) { ["Counter"] = true });
        await storage.SetAsync(key, blob, ct);
        var sut = new NavigationEffects(storage, accessor, logger, state);

        await sut.HandleAppInitialized(new AppInitializedAction("c1"), dispatcher);

        dispatcher.Received(1).Dispatch(Arg.Is<NavigationHydratedAction>(a =>
            a.SidebarCollapsed == true && a.CollapsedGroups.ContainsKey("Counter")));
    }

    private static FrontComposerNavigationState DefaultState() => new(
        SidebarCollapsed: false,
        CollapsedGroups: ImmutableDictionary<string, bool>.Empty.WithComparers(StringComparer.Ordinal),
        CurrentViewport: ViewportTier.Desktop);

    private static IUserContextAccessor MakeAccessor(string? tenantId, string? userId)
    {
        IUserContextAccessor accessor = Substitute.For<IUserContextAccessor>();
        accessor.TenantId.Returns(tenantId);
        accessor.UserId.Returns(userId);
        return accessor;
    }

    private static IState<FrontComposerNavigationState> FakeState(FrontComposerNavigationState value)
    {
        IState<FrontComposerNavigationState> state = Substitute.For<IState<FrontComposerNavigationState>>();
        state.Value.Returns(value);
        return state;
    }

    private static void AssertLoggedInformation(ILogger<NavigationEffects> logger, string diagnosticId)
    {
        bool found = false;
        foreach (NSubstitute.Core.ICall call in logger.ReceivedCalls())
        {
            if (!string.Equals(call.GetMethodInfo().Name, nameof(ILogger.Log), StringComparison.Ordinal))
            {
                continue;
            }

            object?[] args = call.GetArguments();
            bool isInformation = args.Any(a => a is LogLevel lvl && lvl == LogLevel.Information);
            bool mentionsId = args.Any(a => a is not null && a.ToString()?.Contains(diagnosticId, StringComparison.Ordinal) == true);
            if (isInformation && mentionsId)
            {
                found = true;
                break;
            }
        }

        found.ShouldBeTrue($"Expected ILogger.Log call with LogLevel.Information referencing '{diagnosticId}'.");
    }

    private sealed class ObservingStorage(IStorageService inner) : IStorageService
    {
        public int SetAsyncCallCount { get; private set; }

        public Task FlushAsync(CancellationToken cancellationToken = default)
            => inner.FlushAsync(cancellationToken);

        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
            => inner.GetAsync<T>(key, cancellationToken);

        public Task<IReadOnlyList<string>> GetKeysAsync(string prefix, CancellationToken cancellationToken = default)
            => inner.GetKeysAsync(prefix, cancellationToken);

        public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
            => inner.RemoveAsync(key, cancellationToken);

        public Task SetAsync<T>(string key, T value, CancellationToken cancellationToken = default)
        {
            SetAsyncCallCount++;
            return inner.SetAsync(key, value, cancellationToken);
        }
    }
}

