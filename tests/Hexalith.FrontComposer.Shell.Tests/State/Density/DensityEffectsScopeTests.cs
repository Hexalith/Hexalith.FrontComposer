// ATDD RED PHASE — Story 3-3 Task 10.5 (D7, D8, D18, D19; AC3, AC4; ADR-038, ADR-040)
// Fails at compile until:
//   Task 2.3 — UserPreferenceChangedAction / UserPreferenceClearedAction / DensityHydratedAction
//   Task 3.1 — DensityEffects constructor expansion (IState<FrontComposerNavigationState>, IOptions<FcShellOptions>)
//   Task 3.2 — HandleViewportTierChanged
//   Task 3.4 — HandleUserPreferenceChanged + HandleUserPreferenceCleared

using System.Collections.Immutable;
using System.Reflection;

using Fluxor;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Diagnostics;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Contracts.Storage;
using Hexalith.FrontComposer.Shell.State;
using Hexalith.FrontComposer.Shell.State.Density;
using Hexalith.FrontComposer.Shell.State.Navigation;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

using MsOptions = Microsoft.Extensions.Options.Options;

namespace Hexalith.FrontComposer.Shell.Tests.State.Density;

/// <summary>
/// Story 3-3 Task 10.5 (D8 / AC3 / ADR-029 + ADR-038 mirror) — fail-closed tenant/user scope
/// guard for the rewritten <see cref="DensityEffects"/>. Mirrors
/// <c>NavigationEffectsScopeTests</c> (Story 3-2). Adds reflective invariants for ADR-038
/// (hydrate is read-only) and ADR-037 mirror (viewport never persisted).
/// </summary>
public sealed class DensityEffectsScopeTests
{
    [Fact]
    public async Task PersistsOnValidScope_UserPreferenceChanged()
    {
        CancellationToken ct = Xunit.TestContext.Current.CancellationToken;
        InMemoryStorageService storage = new();
        ILogger<DensityEffects> logger = Substitute.For<ILogger<DensityEffects>>();
        IUserContextAccessor accessor = MakeAccessor("acme", "alice");
        IState<FrontComposerNavigationState> navState = FakeNavState(ViewportTier.Desktop);
        IOptions<FcShellOptions> options = MsOptions.Create(new FcShellOptions());

        IState<FrontComposerDensityState> densityState = FakeDensityState();
        DensityEffects sut = new(storage, accessor, logger, navState, options, densityState);

        await sut.HandleUserPreferenceChanged(
            new UserPreferenceChangedAction("c1", DensityLevel.Roomy, DensityLevel.Roomy),
            Substitute.For<IDispatcher>());

        string expectedKey = StorageKeys.BuildKey("acme", "alice", "density");
        DensityLevel? stored = await storage.GetAsync<DensityLevel?>(expectedKey, ct);
        stored.ShouldBe(DensityLevel.Roomy);
    }

    [Fact]
    public async Task SkipsOnNullTenant_UserPreferenceChanged_LogsAndDoesNotCallStorage()
    {
        IStorageService storage = Substitute.For<IStorageService>();
        ILogger<DensityEffects> logger = Substitute.For<ILogger<DensityEffects>>();
        IUserContextAccessor accessor = MakeAccessor(tenantId: null, userId: "alice");
        DensityEffects sut = MakeSut(storage, accessor, logger);

        await sut.HandleUserPreferenceChanged(
            new UserPreferenceChangedAction("c1", DensityLevel.Compact, DensityLevel.Compact),
            Substitute.For<IDispatcher>());

        _ = storage.DidNotReceiveWithAnyArgs().SetAsync(default!, default(DensityLevel?), Arg.Any<CancellationToken>());
        AssertLoggedInformation(logger, FcDiagnosticIds.HFC2105_StoragePersistenceSkipped);
    }

    [Fact]
    public async Task SkipsOnNullUser_UserPreferenceChanged_LogsAndDoesNotCallStorage()
    {
        IStorageService storage = Substitute.For<IStorageService>();
        ILogger<DensityEffects> logger = Substitute.For<ILogger<DensityEffects>>();
        IUserContextAccessor accessor = MakeAccessor(tenantId: "acme", userId: null);
        DensityEffects sut = MakeSut(storage, accessor, logger);

        await sut.HandleUserPreferenceChanged(
            new UserPreferenceChangedAction("c1", DensityLevel.Compact, DensityLevel.Compact),
            Substitute.For<IDispatcher>());

        _ = storage.DidNotReceiveWithAnyArgs().SetAsync(default!, default(DensityLevel?), Arg.Any<CancellationToken>());
        AssertLoggedInformation(logger, FcDiagnosticIds.HFC2105_StoragePersistenceSkipped);
    }

    [Theory]
    [InlineData("   ", "alice")]
    [InlineData("acme", " ")]
    [InlineData("", "alice")]
    [InlineData("acme", "")]
    public async Task SkipsOnWhitespaceUserContext_UserPreferenceChanged(string tenantId, string userId)
    {
        IStorageService storage = Substitute.For<IStorageService>();
        ILogger<DensityEffects> logger = Substitute.For<ILogger<DensityEffects>>();
        IUserContextAccessor accessor = MakeAccessor(tenantId, userId);
        DensityEffects sut = MakeSut(storage, accessor, logger);

        await sut.HandleUserPreferenceChanged(
            new UserPreferenceChangedAction("c1", DensityLevel.Compact, DensityLevel.Compact),
            Substitute.For<IDispatcher>());

        _ = storage.DidNotReceiveWithAnyArgs().SetAsync(default!, default(DensityLevel?), Arg.Any<CancellationToken>());
        AssertLoggedInformation(logger, FcDiagnosticIds.HFC2105_StoragePersistenceSkipped);
    }

    [Fact]
    public async Task PersistsNullOnUserPreferenceCleared()
    {
        // D8 — clear path writes a literal `null` JSON value (DensityLevel? round-trip).
        CancellationToken ct = Xunit.TestContext.Current.CancellationToken;
        InMemoryStorageService storage = new();
        // Pre-seed a non-null value so we can detect the null overwrite.
        string key = StorageKeys.BuildKey("acme", "alice", "density");
        await storage.SetAsync<DensityLevel?>(key, DensityLevel.Compact, ct);

        ILogger<DensityEffects> logger = Substitute.For<ILogger<DensityEffects>>();
        IUserContextAccessor accessor = MakeAccessor("acme", "alice");
        DensityEffects sut = MakeSut(storage, accessor, logger);

        await sut.HandleUserPreferenceCleared(
            new UserPreferenceClearedAction("c1", DensityLevel.Comfortable),
            Substitute.For<IDispatcher>());

        DensityLevel? stored = await storage.GetAsync<DensityLevel?>(key, ct);
        stored.ShouldBeNull("UserPreferenceCleared must persist the literal null value (D8).");
    }

    [Fact]
    public async Task HydrateDoesNotRePersist()
    {
        // ADR-038 mirror — DensityHydratedAction must NOT trigger a storage write.
        InMemoryStorageService inner = new();
        ObservingStorage spy = new(inner);
        ILogger<DensityEffects> logger = Substitute.For<ILogger<DensityEffects>>();
        IUserContextAccessor accessor = MakeAccessor("acme", "alice");
        DensityEffects sut = MakeSut(spy, accessor, logger);

        // If the dev added a HandleDensityHydrated [EffectMethod], it must not write.
        // Drive every persist-prefixed [EffectMethod] handler exposed by DensityEffects with the
        // hydrated action via reflection-aware invocation (defensive — we don't pre-name the method).
        MethodInfo? hydrateHandler = typeof(DensityEffects)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .FirstOrDefault(m =>
                m.GetCustomAttributes(typeof(EffectMethodAttribute), inherit: false).Length > 0 &&
                m.GetParameters().Length == 2 &&
                m.GetParameters()[0].ParameterType == typeof(DensityHydratedAction) &&
                m.GetParameters()[1].ParameterType == typeof(IDispatcher));

        if (hydrateHandler is not null)
        {
            object? task = hydrateHandler.Invoke(sut, [
                new DensityHydratedAction(DensityLevel.Compact, DensityLevel.Compact),
                Substitute.For<IDispatcher>()]);
            if (task is Task t)
            {
                await t.ConfigureAwait(true);
            }
        }

        spy.SetAsyncCallCount.ShouldBe(0, "ADR-038 mirror — hydrate is read-only from the storage perspective.");
    }

    [Fact]
    public async Task ViewportTierChangedDoesNotPersist()
    {
        // D7 / D8 — the cross-feature viewport handler is a pure compute path; no storage write.
        IStorageService storage = Substitute.For<IStorageService>();
        ILogger<DensityEffects> logger = Substitute.For<ILogger<DensityEffects>>();
        IUserContextAccessor accessor = MakeAccessor("acme", "alice");
        DensityEffects sut = MakeSut(storage, accessor, logger);

        await sut.HandleViewportTierChanged(
            new ViewportTierChangedAction(ViewportTier.Tablet),
            Substitute.For<IDispatcher>());

        _ = storage.DidNotReceiveWithAnyArgs().SetAsync(default!, default(DensityLevel?), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void NoHandlePersistEffectMethodAcceptsDensityHydratedAction()
    {
        // ADR-038 mirror — borrow the Story 3-2 NavigationEffectsScopeTests F4 reflective invariant.
        MethodInfo? offender = typeof(DensityEffects)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(m => m.GetCustomAttributes(typeof(EffectMethodAttribute), inherit: false).Length > 0)
            .Where(m => m.GetParameters().FirstOrDefault()?.ParameterType == typeof(DensityHydratedAction))
            .FirstOrDefault(m => m.Name.StartsWith("HandlePersist", StringComparison.Ordinal));

        offender.ShouldBeNull(
            $"ADR-038 mirror: no HandlePersist* [EffectMethod] may accept DensityHydratedAction. Found: {offender?.Name ?? "<none>"}");
    }

    [Fact]
    public void NoHandlePersistEffectMethodAcceptsViewportTierChangedAction()
    {
        // ADR-037 mirror (viewport is observation, never persisted).
        MethodInfo? offender = typeof(DensityEffects)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(m => m.GetCustomAttributes(typeof(EffectMethodAttribute), inherit: false).Length > 0)
            .Where(m => m.GetParameters().FirstOrDefault()?.ParameterType == typeof(ViewportTierChangedAction))
            .FirstOrDefault(m => m.Name.StartsWith("HandlePersist", StringComparison.Ordinal));

        offender.ShouldBeNull(
            $"ADR-037 mirror: no HandlePersist* [EffectMethod] may accept ViewportTierChangedAction. Found: {offender?.Name ?? "<none>"}");
    }

    private static DensityEffects MakeSut(
        IStorageService storage,
        IUserContextAccessor accessor,
        ILogger<DensityEffects> logger)
    {
        IState<FrontComposerNavigationState> navState = FakeNavState(ViewportTier.Desktop);
        IOptions<FcShellOptions> options = MsOptions.Create(new FcShellOptions());
        IState<FrontComposerDensityState> densityState = FakeDensityState();
        return new DensityEffects(storage, accessor, logger, navState, options, densityState);
    }

    private static IState<FrontComposerNavigationState> FakeNavState(ViewportTier tier)
    {
        IState<FrontComposerNavigationState> state = Substitute.For<IState<FrontComposerNavigationState>>();
        state.Value.Returns(new FrontComposerNavigationState(
            SidebarCollapsed: false,
            CollapsedGroups: ImmutableDictionary<string, bool>.Empty.WithComparers(StringComparer.Ordinal),
            CurrentViewport: tier));
        return state;
    }

    private static IState<FrontComposerDensityState> FakeDensityState(
        DensityLevel? userPreference = null,
        DensityLevel effective = DensityLevel.Comfortable)
    {
        IState<FrontComposerDensityState> state = Substitute.For<IState<FrontComposerDensityState>>();
        state.Value.Returns(new FrontComposerDensityState(userPreference, effective));
        return state;
    }

    private static IUserContextAccessor MakeAccessor(string? tenantId, string? userId)
    {
        IUserContextAccessor accessor = Substitute.For<IUserContextAccessor>();
        accessor.TenantId.Returns(tenantId);
        accessor.UserId.Returns(userId);
        return accessor;
    }

    private static void AssertLoggedInformation(ILogger<DensityEffects> logger, string diagnosticId)
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
