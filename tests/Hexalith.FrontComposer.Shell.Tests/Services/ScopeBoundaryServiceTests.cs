using System.Collections.Immutable;
using System.Security.Claims;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Badges;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Infrastructure.Tenancy;
using Hexalith.FrontComposer.Shell.Services;
using Hexalith.FrontComposer.Shell.State.DataGridNavigation;
using Hexalith.FrontComposer.Shell.State.CommandPalette;
using Hexalith.FrontComposer.Shell.State.Navigation;
using Hexalith.FrontComposer.Shell.State.PendingCommands;
using Hexalith.FrontComposer.Shell.Tests.Infrastructure.Tenancy;
using Hexalith.FrontComposer.Shell.Tests;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Services;

public sealed class ScopeBoundaryServiceTests {
    [Fact]
    public void AuthChange_BlocksRenderThroughClear_ThenAllowsB_AndBlocksOnLoss() {
        TestTenantContextAccessor scope = new();
        TestAuthProvider auth = new();
        IDispatcher dispatcher = Substitute.For<IDispatcher>();
        IScopeReadinessGate readiness = Substitute.For<IScopeReadinessGate>();
        _ = readiness.EvaluateAsync(Arg.Any<IDispatcher>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        IPendingCommandStateService pending = Substitute.For<IPendingCommandStateService>();
        INewItemIndicatorStateService newItems = Substitute.For<INewItemIndicatorStateService>();
        IBadgeCountService badges = Substitute.For<IBadgeCountService>();
        ICommandExecutionAdmissionGate admission = Substitute.For<ICommandExecutionAdmissionGate>();
        using ServiceProvider services = new ServiceCollection().BuildServiceProvider();
        using ScopeBoundaryService sut = new(auth, scope, dispatcher, readiness, pending, newItems, badges, admission, services);
        List<bool> blockedDuringClear = [];
        pending.When(x => x.Clear(Arg.Any<string>())).Do(_ => blockedDuringClear.Add(!sut.IsCurrent));
        newItems.When(x => x.Clear(Arg.Any<string>())).Do(_ => blockedDuringClear.Add(!sut.IsCurrent));
        sut.Start();
        sut.IsCurrent.ShouldBeTrue();
        sut.Generation.ShouldBe(0);

        scope.TenantId = "tenant-b";
        scope.UserId = "user-b";
        auth.Raise();
        blockedDuringClear.ShouldBe([true, true]);
        sut.IsCurrent.ShouldBeTrue();
        sut.Generation.ShouldBe(1);

        scope.TenantId = null;
        auth.Raise();
        blockedDuringClear.ShouldBe([true, true, true, true]);
        sut.IsCurrent.ShouldBeFalse();
        sut.Generation.ShouldBe(2);
        pending.Received(2).Clear("TenantOrUserTransition");
        newItems.Received(2).Clear("TenantOrUserTransition");
        dispatcher.Received(2).Dispatch(Arg.Any<ScopeChangedAction>());
        dispatcher.Received(2).Dispatch(Arg.Any<PaletteScopeChangedAction>());
        _ = readiness.Received(1).EvaluateAsync(dispatcher, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void OlderFaultingAuthEvent_DoesNotBlockNewerValidScope() {
        TestTenantContextAccessor scope = new();
        TestAuthProvider auth = new();
        IDispatcher dispatcher = Substitute.For<IDispatcher>();
        IScopeReadinessGate readiness = Substitute.For<IScopeReadinessGate>();
        using ServiceProvider services = new ServiceCollection().BuildServiceProvider();
        using ScopeBoundaryService sut = new(auth, scope, dispatcher, readiness,
            Substitute.For<IPendingCommandStateService>(), Substitute.For<INewItemIndicatorStateService>(),
            Substitute.For<IBadgeCountService>(), Substitute.For<ICommandExecutionAdmissionGate>(), services);
        sut.Start();
        TaskCompletionSource<AuthenticationState> old = new();
        auth.Raise(old.Task);

        scope.TenantId = "tenant-b";
        scope.UserId = "user-b";
        auth.Raise();
        sut.IsCurrent.ShouldBeTrue();
        sut.Generation.ShouldBe(1);

        old.SetException(new InvalidOperationException("old event failed"));
        sut.IsCurrent.ShouldBeTrue();
        sut.Generation.ShouldBe(1);
        dispatcher.Received(1).Dispatch(Arg.Any<ScopeChangedAction>());
    }

    [Fact]
    public void OwnerRearmsRealReadinessGate_OnTenantSwitch() {
        TestTenantContextAccessor scope = new() { TenantId = null };
        TestAuthProvider auth = new();
        IDispatcher dispatcher = Substitute.For<IDispatcher>();
        IUserContextAccessor user = Substitute.For<IUserContextAccessor>();
        user.TenantId.Returns(_ => scope.TenantId);
        user.UserId.Returns(_ => scope.UserId);
        IState<FrontComposerNavigationState> navigation = Substitute.For<IState<FrontComposerNavigationState>>();
        navigation.Value.Returns(new FrontComposerNavigationState(
            false, ImmutableDictionary<string, bool>.Empty, ViewportTier.Desktop, StorageReady: false));
        ScopeReadinessGate readiness = new(navigation, user, null, NullLogger<ScopeReadinessGate>.Instance);
        using ServiceProvider services = new ServiceCollection().BuildServiceProvider();
        using ScopeBoundaryService sut = new(auth, scope, dispatcher, readiness,
            Substitute.For<IPendingCommandStateService>(), Substitute.For<INewItemIndicatorStateService>(),
            Substitute.For<IBadgeCountService>(), Substitute.For<ICommandExecutionAdmissionGate>(), services);
        sut.Start();

        scope.TenantId = "tenant-a";
        auth.Raise();
        dispatcher.Received(1).Dispatch(Arg.Any<StorageReadyAction>());

        scope.TenantId = "tenant-b";
        scope.UserId = "user-b";
        auth.Raise();
        dispatcher.Received(2).Dispatch(Arg.Any<StorageReadyAction>());
        sut.IsCurrent.ShouldBeTrue();
    }

    [Fact]
    public void ScopeChangedReducers_ClearPreferencesPagesAndPendingProviders() {
        ScopeChangedAction action = new();
        FrontComposerNavigationState navigation = new(
            true, ImmutableDictionary<string, bool>.Empty.Add("Orders", true), ViewportTier.Desktop,
            LastActiveRoute: "/orders", StorageReady: true, HydrationState: HydrationState.Hydrated);
        FrontComposerNavigationState nextNavigation = NavigationReducers.ReduceScopeChanged(navigation, action);
        nextNavigation.StorageReady.ShouldBeFalse();
        nextNavigation.HydrationState.ShouldBe(HydrationState.Idle);
        nextNavigation.SidebarCollapsed.ShouldBeFalse();
        nextNavigation.CollapsedGroups.ShouldBeEmpty();
        nextNavigation.LastActiveRoute.ShouldBeNull();

        TaskCompletionSource<object> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        LoadedPageState pages = new() {
            PagesByKey = ImmutableDictionary<(string ViewKey, int Skip), IReadOnlyList<object>>.Empty
                .Add(("Orders.Row", 0), [new object()]),
            TotalCountByKey = ImmutableDictionary<string, int>.Empty.Add("Orders.Row", 1),
            PendingCompletionsByKey = ImmutableDictionary<(string ViewKey, int Skip), TaskCompletionSource<object>>.Empty
                .Add(("Orders.Row", 25), completion),
            LaneByKey = ImmutableDictionary<string, VirtualizationLane>.Empty.Add("Orders.Row", VirtualizationLane.ServerSide),
        };
        LoadedPageState nextPages = LoadedPageReducers.ReduceScopeChanged(pages, action);
        nextPages.PagesByKey.ShouldBeEmpty();
        nextPages.TotalCountByKey.ShouldBeEmpty();
        nextPages.PendingCompletionsByKey.ShouldBeEmpty();
        nextPages.LaneByKey.ShouldBeEmpty();
        completion.Task.IsCanceled.ShouldBeTrue();
    }

    [Fact]
    public void TenantSwitchAndLoss_ClearPendingAdmissionAndFreshRowState() {
        TestTenantContextAccessor scope = new();
        IUserContextAccessor user = Substitute.For<IUserContextAccessor>();
        user.TenantId.Returns(_ => scope.TenantId);
        user.UserId.Returns(_ => scope.UserId);
        ValidatedPendingScope validated = new(scope);
        PendingCommandStateService pending = new(
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions()),
            Substitute.For<ILifecycleStateService>(), user, validatedScope: validated);
        CommandExecutionAdmissionGate gate = new(pending, validatedScope: validated);
        using NewItemIndicatorStateService newItems = new(TimeProvider.System, user,
            NullLogger<NewItemIndicatorStateService>.Instance);
        using CommandExecutionAdmission admissionA = gate.TryAcquire(new CommandExecutionAdmissionRequest("Counter.Create", "Create"));
        admissionA.IsAdmitted.ShouldBeTrue();
        const string messageId = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
        pending.Register(new PendingCommandRegistration(
            "01CPZ3NDEKTSV4RRFFQ69G5FAV", messageId, "Counter.Create"))
            .Status.ShouldBe(PendingCommandRegistrationStatus.Registered);
        newItems.Add(new NewItemIndicatorEntry("Counter.View", "row-a", messageId, DateTimeOffset.UtcNow));
        newItems.Snapshot("Counter.View").Count.ShouldBe(1);

        scope.TenantId = "tenant-b";
        scope.UserId = "user-b";
        pending.Snapshot().ShouldBeEmpty();
        pending.GetByMessageId(messageId).ShouldBeNull();
        newItems.Snapshot("Counter.View").ShouldBeEmpty();
        gate.ResetScope();
        using CommandExecutionAdmission admissionB = gate.TryAcquire(new CommandExecutionAdmissionRequest("Counter.Create", "Create"));
        admissionB.IsAdmitted.ShouldBeTrue();

        scope.TenantId = null;
        pending.Snapshot().ShouldBeEmpty();
        newItems.Snapshot("Counter.View").ShouldBeEmpty();
        using CommandExecutionAdmission admissionNone = gate.TryAcquire(new CommandExecutionAdmissionRequest("Counter.Create", "Create"));
        admissionNone.DenialReason.ShouldBe(CommandExecutionAdmissionDenialReason.ScopeUnavailable);
        pending.Register(new PendingCommandRegistration(
            "01DPZ3NDEKTSV4RRFFQ69G5FAV", "01BRZ3NDEKTSV4RRFFQ69G5FAV", "Counter.Create"))
            .Status.ShouldBe(PendingCommandRegistrationStatus.ScopeUnavailable);
    }

    [Fact]
    public void ThrowingCanonicalAccessor_BlocksStartRenderAndSynchronization() {
        using ServiceProvider services = new ServiceCollection().BuildServiceProvider();
        IScopeReadinessGate readiness = Substitute.For<IScopeReadinessGate>();
        using ScopeBoundaryService sut = new(new TestAuthProvider(), new ThrowingScopeAccessor(),
            Substitute.For<IDispatcher>(), readiness,
            Substitute.For<IPendingCommandStateService>(), Substitute.For<INewItemIndicatorStateService>(),
            Substitute.For<IBadgeCountService>(), Substitute.For<ICommandExecutionAdmissionGate>(), services);

        sut.Start();
        sut.IsCurrent.ShouldBeFalse();
        sut.Synchronize();
        sut.IsCurrent.ShouldBeFalse();
    }

    private sealed class TestAuthProvider : AuthenticationStateProvider {
        private readonly AuthenticationState _state = new(new ClaimsPrincipal(new ClaimsIdentity()));

        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(_state);

        public void Raise() => Raise(GetAuthenticationStateAsync());

        public void Raise(Task<AuthenticationState> state) => NotifyAuthenticationStateChanged(state);
    }

    private sealed class ThrowingScopeAccessor : IFrontComposerTenantContextAccessor {
        public TenantContextResult TryGetContext(string? requestedTenant = null, string operationKind = "tenant-scoped")
            => throw new InvalidOperationException("scope unavailable");

        public TenantContextResult Revalidate(TenantContextSnapshot snapshot, string operationKind = "tenant-scoped")
            => throw new InvalidOperationException("scope unavailable");
    }
}

public sealed class ScopeBoundaryFluxorDispatchTests : FrontComposerTestBase {
    [Fact]
    public void RegisteredScopeReducer_ClearsLoadedPagesAndCancelsProvider() {
        TaskCompletionSource<object> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        LoadedPageState seeded = new() {
            PagesByKey = ImmutableDictionary<(string ViewKey, int Skip), IReadOnlyList<object>>.Empty
                .Add(("Orders.Row", 0), [new object()]),
            PendingCompletionsByKey = ImmutableDictionary<(string ViewKey, int Skip), TaskCompletionSource<object>>.Empty
                .Add(("Orders.Row", 25), completion),
        };
        Services.GetRequiredService<LoadedPageFeature>().RestoreState(seeded);
        IState<LoadedPageState> loaded = Services.GetRequiredService<IState<LoadedPageState>>();
        loaded.Value.PagesByKey.Count.ShouldBe(1);
        loaded.Value.PendingCompletionsByKey.Count.ShouldBe(1);

        Services.GetRequiredService<IDispatcher>().Dispatch(new ScopeChangedAction());

        loaded.Value.PagesByKey.ShouldBeEmpty();
        loaded.Value.PendingCompletionsByKey.ShouldBeEmpty();
        completion.Task.IsCanceled.ShouldBeTrue();
    }
}
