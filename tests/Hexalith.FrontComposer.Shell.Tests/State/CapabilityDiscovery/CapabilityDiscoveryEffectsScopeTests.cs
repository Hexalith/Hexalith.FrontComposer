using System.Collections.Immutable;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Badges;
using Hexalith.FrontComposer.Contracts.Diagnostics;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Contracts.Storage;
using Hexalith.FrontComposer.Shell.Badges;
using Hexalith.FrontComposer.Shell.State;
using Hexalith.FrontComposer.Shell.State.CapabilityDiscovery;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.State.CapabilityDiscovery;

/// <summary>
/// Story 3-5 Task 7.5 — fail-closed tenant/user scope guard symmetry for hydrate AND persist
/// (AC11, L03 / feedback memory <c>feedback_tenant_isolation_fail_closed.md</c>). Both paths
/// MUST log <c>HFC2105</c> AND MUST NOT call storage when the scope is invalid.
/// </summary>
public sealed class CapabilityDiscoveryEffectsScopeTests {
    private sealed class StubCatalog : IActionQueueProjectionCatalog {
        public IReadOnlyList<Type> ActionQueueTypes { get; } = Array.Empty<Type>();
    }

    private static IUserContextAccessor MakeAccessor(string? tenantId, string? userId) {
        IUserContextAccessor accessor = Substitute.For<IUserContextAccessor>();
        accessor.TenantId.Returns(tenantId);
        accessor.UserId.Returns(userId);
        return accessor;
    }

    private static IState<FrontComposerCapabilityDiscoveryState> FakeState(FrontComposerCapabilityDiscoveryState value) {
        IState<FrontComposerCapabilityDiscoveryState> state = Substitute.For<IState<FrontComposerCapabilityDiscoveryState>>();
        state.Value.Returns(value);
        return state;
    }

    private static BadgeCountService MakeBadgeService() {
        return new BadgeCountService(
            new StubCatalog(),
            new NullActionQueueCountReader(),
            new ServiceCollection().BuildServiceProvider(),
            Substitute.For<ILogger<BadgeCountService>>(),
            new FakeTimeProvider());
    }

    private static CapabilityDiscoveryEffects MakeEffect(
        IStorageService storage,
        IUserContextAccessor accessor,
        IDispatcher dispatcher,
        ILogger<CapabilityDiscoveryEffects> logger,
        FrontComposerCapabilityDiscoveryState? state = null) {
        return new CapabilityDiscoveryEffects(
            dispatcher,
            storage,
            accessor,
            MakeBadgeService(),
            FakeState(state ?? FrontComposerCapabilityDiscoveryState.Empty),
            logger);
    }

    [Theory]
    [InlineData(null, "alice")]
    [InlineData("acme", null)]
    [InlineData("", "alice")]
    [InlineData("acme", "")]
    [InlineData("   ", "alice")]
    [InlineData("acme", "  ")]
    public async Task PersistEffect_InvalidScope_FailsClosed_NoStorageCall_LogsHFC2105_DirectionPersist(
        string? tenantId, string? userId) {
        IStorageService storage = Substitute.For<IStorageService>();
        ILogger<CapabilityDiscoveryEffects> logger = Substitute.For<ILogger<CapabilityDiscoveryEffects>>();
        IDispatcher dispatcher = Substitute.For<IDispatcher>();
        IUserContextAccessor accessor = MakeAccessor(tenantId, userId);
        using CapabilityDiscoveryEffects sut = MakeEffect(storage, accessor, dispatcher, logger);

        await sut.HandleCapabilityVisited(new CapabilityVisitedAction("bc:Counter"), dispatcher);

        await storage.DidNotReceiveWithAnyArgs().SetAsync<ImmutableHashSet<string>>(
            default!, default!, Arg.Any<CancellationToken>());
        AssertLoggedInformation(logger, FcDiagnosticIds.HFC2105_StoragePersistenceSkipped, "persist");
    }

    [Theory]
    [InlineData(null, "alice")]
    [InlineData("acme", null)]
    [InlineData("", "alice")]
    [InlineData("acme", "  ")]
    public async Task HydrateEffect_InvalidScope_FailsClosed_NoGetAsync_DispatchesEmptySeenSet_LogsHFC2105_DirectionHydrate(
        string? tenantId, string? userId) {
        IStorageService storage = Substitute.For<IStorageService>();
        ILogger<CapabilityDiscoveryEffects> logger = Substitute.For<ILogger<CapabilityDiscoveryEffects>>();
        IDispatcher dispatcher = Substitute.For<IDispatcher>();
        IUserContextAccessor accessor = MakeAccessor(tenantId, userId);
        using CapabilityDiscoveryEffects sut = MakeEffect(storage, accessor, dispatcher, logger);

        await sut.HandleAppInitialized(new AppInitializedAction("c-1"), dispatcher);

        await storage.DidNotReceiveWithAnyArgs()
            .GetAsync<ImmutableHashSet<string>>(default!, Arg.Any<CancellationToken>());
        AssertLoggedInformation(logger, FcDiagnosticIds.HFC2105_StoragePersistenceSkipped, "hydrate");
        dispatcher.Received().Dispatch(Arg.Is<SeenCapabilitiesHydratedAction>(a =>
            a.SeenCapabilities.IsEmpty));
    }

    [Fact]
    public async Task PersistEffect_StorageThrows_LogsHFC2112_InMemoryStateUnchanged() {
        // D13 — when persist throws after the user has navigated away, log HFC2112 and never
        // crash. The reducer state is already authoritative for the current circuit.
        IStorageService storage = Substitute.For<IStorageService>();
        _ = storage.SetAsync(Arg.Any<string>(), Arg.Any<ImmutableHashSet<string>>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("disk full"));
        ILogger<CapabilityDiscoveryEffects> logger = Substitute.For<ILogger<CapabilityDiscoveryEffects>>();
        IDispatcher dispatcher = Substitute.For<IDispatcher>();
        IUserContextAccessor accessor = MakeAccessor("acme", "alice");
        using CapabilityDiscoveryEffects sut = MakeEffect(storage, accessor, dispatcher, logger);

        await sut.HandleCapabilityVisited(new CapabilityVisitedAction("bc:Counter"), dispatcher);

        AssertLoggedAtLevel(logger, LogLevel.Warning, FcDiagnosticIds.HFC2112_BadgeInitialFetchFault);
    }

    [Fact]
    public async Task HydrateEffect_StorageThrows_DispatchesEmptySeenSet_LogsHFC2112_DoesNotCrash() {
        IStorageService storage = Substitute.For<IStorageService>();
        _ = storage.GetAsync<ImmutableHashSet<string>>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Task<ImmutableHashSet<string>?>>(_ => throw new InvalidOperationException("corrupt"));
        ILogger<CapabilityDiscoveryEffects> logger = Substitute.For<ILogger<CapabilityDiscoveryEffects>>();
        IDispatcher dispatcher = Substitute.For<IDispatcher>();
        IUserContextAccessor accessor = MakeAccessor("acme", "alice");
        using CapabilityDiscoveryEffects sut = MakeEffect(storage, accessor, dispatcher, logger);

        await sut.HandleAppInitialized(new AppInitializedAction("c-1"), dispatcher);

        AssertLoggedAtLevel(logger, LogLevel.Warning, FcDiagnosticIds.HFC2112_BadgeInitialFetchFault);
        dispatcher.Received().Dispatch(Arg.Is<SeenCapabilitiesHydratedAction>(a => a.SeenCapabilities.IsEmpty));
    }

    [Fact]
    public async Task PersistEffect_ValidScope_WritesUnderTenantUserKey() {
        CancellationToken ct = Xunit.TestContext.Current.CancellationToken;
        InMemoryStorageService storage = new();
        ILogger<CapabilityDiscoveryEffects> logger = Substitute.For<ILogger<CapabilityDiscoveryEffects>>();
        IDispatcher dispatcher = Substitute.For<IDispatcher>();
        IUserContextAccessor accessor = MakeAccessor("acme", "alice");
        ImmutableHashSet<string> seen = ImmutableHashSet<string>.Empty
            .WithComparer(StringComparer.Ordinal)
            .Add("bc:Counter")
            .Add("proj:Counter:Counter.Domain.CounterProjection");
        FrontComposerCapabilityDiscoveryState state = FrontComposerCapabilityDiscoveryState.Empty with {
            SeenCapabilities = seen,
        };
        using CapabilityDiscoveryEffects sut = MakeEffect(storage, accessor, dispatcher, logger, state);

        await sut.HandleCapabilityVisited(new CapabilityVisitedAction("bc:Counter"), dispatcher);

        string key = StorageKeys.BuildKey("acme", "alice", "capability-seen");
        ImmutableHashSet<string>? stored = await storage.GetAsync<ImmutableHashSet<string>>(key, ct);
        stored.ShouldNotBeNull();
        stored!.ShouldContain("bc:Counter");
        stored.ShouldContain("proj:Counter:Counter.Domain.CounterProjection");
    }

    [Fact]
    public async Task HydrateEffect_ValidScopeEmptyStorage_DispatchesEmptySeenSet() {
        InMemoryStorageService storage = new();
        ILogger<CapabilityDiscoveryEffects> logger = Substitute.For<ILogger<CapabilityDiscoveryEffects>>();
        IDispatcher dispatcher = Substitute.For<IDispatcher>();
        IUserContextAccessor accessor = MakeAccessor("acme", "alice");
        using CapabilityDiscoveryEffects sut = MakeEffect(storage, accessor, dispatcher, logger);

        await sut.HandleAppInitialized(new AppInitializedAction("c-1"), dispatcher);

        dispatcher.Received().Dispatch(Arg.Is<SeenCapabilitiesHydratedAction>(a => a.SeenCapabilities.IsEmpty));
    }

    [Fact]
    public async Task HydrateEffect_ValidScopeWithStoredSet_DispatchesHydratedSet() {
        CancellationToken ct = Xunit.TestContext.Current.CancellationToken;
        InMemoryStorageService storage = new();
        ImmutableHashSet<string> seeded = ImmutableHashSet<string>.Empty
            .WithComparer(StringComparer.Ordinal)
            .Add("bc:Counter");
        await storage.SetAsync(
            StorageKeys.BuildKey("acme", "alice", "capability-seen"),
            seeded,
            ct);
        ILogger<CapabilityDiscoveryEffects> logger = Substitute.For<ILogger<CapabilityDiscoveryEffects>>();
        IDispatcher dispatcher = Substitute.For<IDispatcher>();
        IUserContextAccessor accessor = MakeAccessor("acme", "alice");
        using CapabilityDiscoveryEffects sut = MakeEffect(storage, accessor, dispatcher, logger);

        await sut.HandleAppInitialized(new AppInitializedAction("c-1"), dispatcher);

        dispatcher.Received().Dispatch(Arg.Is<SeenCapabilitiesHydratedAction>(a =>
            a.SeenCapabilities.Contains("bc:Counter")));
    }

    private static void AssertLoggedAtLevel(ILogger logger, LogLevel level, string fragment) {
        bool found = false;
        foreach (NSubstitute.Core.ICall call in logger.ReceivedCalls()) {
            if (!string.Equals(call.GetMethodInfo().Name, nameof(ILogger.Log), StringComparison.Ordinal)) {
                continue;
            }

            object?[] args = call.GetArguments();
            bool levelMatch = args.Any(a => a is LogLevel lvl && lvl == level);
            bool fragmentMatch = args.Any(a => a is not null && a.ToString()?.Contains(fragment, StringComparison.Ordinal) == true);
            if (levelMatch && fragmentMatch) {
                found = true;
                break;
            }
        }

        found.ShouldBeTrue($"Expected ILogger.Log({level}) referencing '{fragment}'.");
    }

    private static void AssertLoggedInformation(ILogger logger, string diagnosticId, string direction) {
        bool found = false;
        foreach (NSubstitute.Core.ICall call in logger.ReceivedCalls()) {
            if (!string.Equals(call.GetMethodInfo().Name, nameof(ILogger.Log), StringComparison.Ordinal)) {
                continue;
            }

            object?[] args = call.GetArguments();
            bool isInformation = args.Any(a => a is LogLevel lvl && lvl == LogLevel.Information);
            bool mentionsId = args.Any(a => a is not null && a.ToString()?.Contains(diagnosticId, StringComparison.Ordinal) == true);
            bool mentionsDirection = args.Any(a =>
                a is not null
                && (a.ToString()?.Contains(direction, StringComparison.Ordinal) == true
                    || (a is System.Collections.IEnumerable formatted
                        && formatted.Cast<object?>().Any(item => item?.ToString()?.Contains(direction, StringComparison.Ordinal) == true))));
            if (isInformation && mentionsId && mentionsDirection) {
                found = true;
                break;
            }
        }

        found.ShouldBeTrue($"Expected ILogger.Log(Information) referencing '{diagnosticId}' AND '{direction}'.");
    }
}
