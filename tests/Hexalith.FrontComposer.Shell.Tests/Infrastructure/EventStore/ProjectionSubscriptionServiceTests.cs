using System.Security.Claims;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Infrastructure.EventStore;
using Hexalith.FrontComposer.Shell.Infrastructure.Tenancy;
using Hexalith.FrontComposer.Shell.Options;
using Hexalith.FrontComposer.Shell.Services.Auth;
using Hexalith.FrontComposer.Shell.State.PendingCommands;
using Hexalith.FrontComposer.Shell.State.ProjectionConnection;
using Hexalith.FrontComposer.Shell.State.ReconnectionReconciliation;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Infrastructure.EventStore;

public sealed class ProjectionSubscriptionServiceTests {
    [Fact]
    public async Task Subscribe_CommitsActiveGroupOnlyAfterJoinSucceeds_AndNotifiesOnNudge() {
        FakeProjectionHubConnection connection = new();
        TestNotifier notifier = new();
        ProjectionSubscriptionService sut = Create(connection, notifier);

        await sut.SubscribeAsync("orders", "acme", TestContext.Current.CancellationToken);
        await sut.SubscribeAsync("orders", "acme", TestContext.Current.CancellationToken);
        await connection.RaiseAsync("orders", "acme");

        connection.StartCount.ShouldBe(1);
        connection.JoinedGroups.ShouldBe(["orders:acme"]);
        notifier.Changed.ShouldBe(["orders"]);
    }

    [Fact]
    public async Task SubscribeScoped_JoinsScopedGroup() {
        FakeProjectionHubConnection connection = new();
        TestNotifier notifier = new();
        ProjectionSubscriptionService sut = Create(connection, notifier);

        await sut.SubscribeAsync("orders", "acme", "conv-1", TestContext.Current.CancellationToken);

        connection.JoinedGroups.ShouldBe(["orders:acme:conv-1"]);
    }

    [Fact]
    public async Task SubscribeScoped_NullScope_JoinsTenantWideGroup() {
        FakeProjectionHubConnection connection = new();
        TestNotifier notifier = new();
        ProjectionSubscriptionService sut = Create(connection, notifier);

        await sut.SubscribeAsync("orders", "acme", null, TestContext.Current.CancellationToken);

        connection.JoinedGroups.ShouldBe(["orders:acme"]);
    }

    [Fact]
    public async Task SubscribeScoped_AndTenantWide_AreIndependentGroups() {
        FakeProjectionHubConnection connection = new();
        TestNotifier notifier = new();
        ProjectionSubscriptionService sut = Create(connection, notifier);

        await sut.SubscribeAsync("orders", "acme", TestContext.Current.CancellationToken);
        await sut.SubscribeAsync("orders", "acme", "conv-1", TestContext.Current.CancellationToken);

        connection.JoinedGroups.ShouldBe(["orders:acme", "orders:acme:conv-1"]);
    }

    [Fact]
    public async Task UnsubscribeScoped_LeavesScopedGroup() {
        FakeProjectionHubConnection connection = new();
        TestNotifier notifier = new();
        ProjectionSubscriptionService sut = Create(connection, notifier);

        await sut.SubscribeAsync("orders", "acme", "conv-1", TestContext.Current.CancellationToken);
        await sut.UnsubscribeAsync("orders", "acme", "conv-1", TestContext.Current.CancellationToken);

        connection.LeftGroups.ShouldBe(["orders:acme:conv-1"]);
    }

    [Fact]
    public async Task Reconnect_RejoinsScopedGroupCarryingScope() {
        FakeProjectionHubConnection connection = new();
        TestNotifier notifier = new();
        ProjectionSubscriptionService sut = Create(connection, notifier);

        await sut.SubscribeAsync("orders", "acme", "conv-1", TestContext.Current.CancellationToken);
        await connection.RaiseStateAsync(new ProjectionHubConnectionStateChanged(ProjectionHubConnectionState.Reconnected));

        // Joined once on subscribe and once on reconnect rejoin — the scope survives reconnect.
        connection.JoinedGroups.ShouldBe(["orders:acme:conv-1", "orders:acme:conv-1"]);
    }

    [Fact]
    public async Task Closed_WithFallbackEnabled_RestartsAndRejoinsActiveGroups() {
        FakeProjectionHubConnection connection = new();
        TestNotifier notifier = new();
        TestProjectionConnectionState state = new();
        TestRefreshScheduler refresh = new();
        FcShellOptions shellOptions = new() { ProjectionFallbackPollingIntervalSeconds = 15 };
        ProjectionFallbackPollingDriver fallbackDriver = new(
            state,
            refresh,
            new TestOptionsMonitor(shellOptions),
            NullLogger<ProjectionFallbackPollingDriver>.Instance);
        ProjectionSubscriptionService sut = new(
            global::Microsoft.Extensions.Options.Options.Create(new EventStoreOptions {
                BaseAddress = new Uri("https://eventstore.test"),
                RequireAccessToken = false,
                ProjectionChangesHubPath = "/hubs/projection-changes",
            }),
            new FakeProjectionHubConnectionFactory(connection, "https://eventstore.test/hubs/projection-changes"),
            state,
            refresh,
            notifier,
            NullLogger<ProjectionSubscriptionService>.Instance,
            fallbackDriver: fallbackDriver,
            shellOptions: global::Microsoft.Extensions.Options.Options.Create(shellOptions));

        try {
            await sut.SubscribeAsync("orders", "acme", TestContext.Current.CancellationToken);
            connection.JoinedGroups.Clear();

            await connection.RaiseStateAsync(new ProjectionHubConnectionStateChanged(
                ProjectionHubConnectionState.Closed,
                new IOException("transport")));

            connection.StartCount.ShouldBe(2);
            connection.JoinedGroups.ShouldBe(["orders:acme"]);
            state.Current.Status.ShouldBe(ProjectionConnectionStatus.Connected);
        }
        finally {
            await sut.DisposeAsync().ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task Closed_WithFallbackDisabled_DoesNotRestart() {
        FakeProjectionHubConnection connection = new();
        TestNotifier notifier = new();
        TestProjectionConnectionState state = new();
        TestRefreshScheduler refresh = new();
        FcShellOptions shellOptions = new() { ProjectionFallbackPollingIntervalSeconds = 0 };
        ProjectionFallbackPollingDriver fallbackDriver = new(
            state,
            refresh,
            new TestOptionsMonitor(shellOptions),
            NullLogger<ProjectionFallbackPollingDriver>.Instance);
        ProjectionSubscriptionService sut = new(
            global::Microsoft.Extensions.Options.Options.Create(new EventStoreOptions {
                BaseAddress = new Uri("https://eventstore.test"),
                RequireAccessToken = false,
                ProjectionChangesHubPath = "/hubs/projection-changes",
            }),
            new FakeProjectionHubConnectionFactory(connection, "https://eventstore.test/hubs/projection-changes"),
            state,
            refresh,
            notifier,
            NullLogger<ProjectionSubscriptionService>.Instance,
            fallbackDriver: fallbackDriver,
            shellOptions: global::Microsoft.Extensions.Options.Options.Create(shellOptions));

        try {
            await sut.SubscribeAsync("orders", "acme", TestContext.Current.CancellationToken);
            connection.JoinedGroups.Clear();

            await connection.RaiseStateAsync(new ProjectionHubConnectionStateChanged(
                ProjectionHubConnectionState.Closed,
                new IOException("transport")));

            connection.StartCount.ShouldBe(1);
            connection.JoinedGroups.ShouldBeEmpty();
            state.Current.Status.ShouldBe(ProjectionConnectionStatus.Disconnected);
        }
        finally {
            await sut.DisposeAsync().ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task Subscribe_CapturesCircuitTokenProvider_ForSignalRReconnectAfterCircuitContextClears() {
        DateTimeOffset now = new(2026, 7, 5, 12, 0, 0, TimeSpan.Zero);
        FrontComposerUserTokenStore store = new(new FixedTimeProvider(now));
        store.Set("user-1", "stored-token", now.AddMinutes(5));
        CircuitServicesAccessor circuitServices = new() {
            Services = new ServiceCollection()
                .AddScoped<AuthenticationStateProvider>(_ => new StubAuthenticationStateProvider(
                    new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "user-1")], "Test"))))
                .BuildServiceProvider(),
        };
        FrontComposerAuthenticationOptions authOptions = new();
        authOptions.CustomBrokered.Enabled = true;
        authOptions.UserClaimTypes.Add("sub");
        FrontComposerAccessTokenProvider tokenProvider = new(
            new HttpContextAccessor(),
            circuitServices,
            store,
            global::Microsoft.Extensions.Options.Options.Create(authOptions),
            NullLogger<FrontComposerAccessTokenProvider>.Instance);
        FakeProjectionHubConnection connection = new();
        ProjectionSubscriptionService sut = new(
            global::Microsoft.Extensions.Options.Options.Create(new EventStoreOptions {
                BaseAddress = new Uri("https://eventstore.test"),
                RequireAccessToken = true,
                ProjectionChangesHubPath = "/hubs/projection-changes",
                AccessTokenProvider = tokenProvider.GetAccessTokenAsync,
            }),
            new FakeProjectionHubConnectionFactory(connection, "https://eventstore.test/hubs/projection-changes"),
            new TestProjectionConnectionState(),
            new TestRefreshScheduler(),
            new TestNotifier(),
            NullLogger<ProjectionSubscriptionService>.Instance,
            frontComposerAccessTokenProvider: tokenProvider);

        await sut.SubscribeAsync("orders", "acme", TestContext.Current.CancellationToken);
        circuitServices.Services = null;

        string? token = await connection.AccessTokenProvider!(TestContext.Current.CancellationToken);

        (token == "stored-token").ShouldBeTrue("SignalR reconnect token acquisition should not depend on ambient circuit services");
    }

    [Fact]
    public async Task OnProjectionChangedDetail_RaisesDetailNotifierOpaquely() {
        FakeProjectionHubConnection connection = new();
        DetailCapturingNotifier notifier = new();
        ProjectionSubscriptionService sut = Create(connection, notifier);

        await sut.SubscribeAsync("orders", "acme", "conv-1", TestContext.Current.CancellationToken);
        ProjectionChangedDetail detail = new(
            "orders",
            "acme",
            "conv-1",
            new Dictionary<string, string> { ["sequence"] = "7", ["state"] = "running" });

        await connection.RaiseDetailAsync(detail);

        ProjectionChangedDetail captured = notifier.Details.ShouldHaveSingleItem();
        captured.GroupScope.ShouldBe("conv-1");
        captured.Metadata["sequence"].ShouldBe("7");
    }

    [Fact]
    public async Task OnProjectionChangedDetail_MalformedRouting_IsIgnored() {
        FakeProjectionHubConnection connection = new();
        DetailCapturingNotifier notifier = new();
        _ = Create(connection, notifier);

        // A colon in projectionType fails routing validation; the detail is dropped, not surfaced.
        await connection.RaiseDetailAsync(new ProjectionChangedDetail(
            "orders:bad", "acme", null, new Dictionary<string, string>()));

        notifier.Details.ShouldBeEmpty();
    }

    [Fact]
    public async Task Subscribe_TenantMismatch_BlocksBeforeStartJoinAndActiveGroupRegistration() {
        FakeProjectionHubConnection connection = new();
        TestNotifier notifier = new();
        MutableUserContextAccessor userContext = new("tenant-a", "user-a");
        ProjectionSubscriptionService sut = new(
            global::Microsoft.Extensions.Options.Options.Create(new EventStoreOptions {
                BaseAddress = new Uri("https://eventstore.test"),
                RequireAccessToken = false,
                ProjectionChangesHubPath = "/hubs/projection-changes",
            }),
            new FakeProjectionHubConnectionFactory(connection, "https://eventstore.test/hubs/projection-changes"),
            new TestProjectionConnectionState(),
            new TestRefreshScheduler(),
            notifier,
            NullLogger<ProjectionSubscriptionService>.Instance,
            userContextAccessor: userContext,
            shellOptions: global::Microsoft.Extensions.Options.Options.Create(new FcShellOptions()));

        TenantContextException ex = await Should.ThrowAsync<TenantContextException>(
            async () => await sut.SubscribeAsync("orders", "tenant-b", TestContext.Current.CancellationToken).ConfigureAwait(true));

        ex.FailureCategory.ShouldBe(TenantContextFailureCategory.TenantMismatch);
        connection.StartCount.ShouldBe(0);
        connection.JoinedGroups.ShouldBeEmpty();
        await connection.RaiseAsync("orders", "tenant-b");
        notifier.Changed.ShouldBeEmpty();
    }

    [Fact]
    public async Task OnNudge_AfterTenantSwitch_SkipsNotifierRefreshAndPendingPolling() {
        FakeProjectionHubConnection connection = new();
        TestNotifier notifier = new();
        TestRefreshScheduler refresh = new();
        TestPendingPolling pending = new();
        MutableUserContextAccessor userContext = new("tenant-a", "user-a");
        ProjectionSubscriptionService sut = new(
            global::Microsoft.Extensions.Options.Options.Create(new EventStoreOptions {
                BaseAddress = new Uri("https://eventstore.test"),
                RequireAccessToken = false,
                ProjectionChangesHubPath = "/hubs/projection-changes",
            }),
            new FakeProjectionHubConnectionFactory(connection, "https://eventstore.test/hubs/projection-changes"),
            new TestProjectionConnectionState(),
            refresh,
            notifier,
            NullLogger<ProjectionSubscriptionService>.Instance,
            pendingCommandPolling: pending,
            userContextAccessor: userContext,
            shellOptions: global::Microsoft.Extensions.Options.Options.Create(new FcShellOptions()));

        await sut.SubscribeAsync("orders", "tenant-a", TestContext.Current.CancellationToken);
        userContext.TenantId = "tenant-b";

        await connection.RaiseAsync("orders", "tenant-a");

        notifier.Changed.ShouldBeEmpty();
        refresh.NudgeRefreshes.ShouldBeEmpty();
        pending.Calls.ShouldBe(0);
    }

    [Fact]
    public async Task OnNudge_TriggersVisibleLaneRefreshScheduler_WithTenantRoute() {
        FakeProjectionHubConnection connection = new();
        TestNotifier notifier = new();
        TestRefreshScheduler refresh = new();
        ProjectionSubscriptionService sut = Create(connection, notifier, refreshScheduler: refresh);

        await sut.SubscribeAsync("orders", "acme", TestContext.Current.CancellationToken);
        await connection.RaiseAsync("orders", "acme");

        refresh.NudgeRefreshes.ShouldBe([("orders", "acme")]);
    }

    [Fact]
    public async Task Reconnected_RejoinsActiveGroupsExactlyOnce_UsingExistingActiveSet() {
        FakeProjectionHubConnection connection = new();
        TestNotifier notifier = new();
        ProjectionSubscriptionService sut = Create(connection, notifier);

        await sut.SubscribeAsync("orders", "acme", TestContext.Current.CancellationToken);
        connection.JoinedGroups.Clear();

        await connection.RaiseStateAsync(new ProjectionHubConnectionStateChanged(ProjectionHubConnectionState.Reconnected));

        connection.JoinedGroups.ShouldBe(["orders:acme"]);
    }

    [Fact]
    public async Task Reconnected_StartsReconciliationAfterRejoinCompletes() {
        FakeProjectionHubConnection connection = new();
        TestNotifier notifier = new();
        TestReconciliationCoordinator reconciliation = new();
        ProjectionSubscriptionService sut = new(
            global::Microsoft.Extensions.Options.Options.Create(new EventStoreOptions {
                BaseAddress = new Uri("https://eventstore.test"),
                RequireAccessToken = false,
                ProjectionChangesHubPath = "/hubs/projection-changes",
            }),
            new FakeProjectionHubConnectionFactory(connection, "https://eventstore.test/hubs/projection-changes"),
            new TestProjectionConnectionState(),
            new TestRefreshScheduler(),
            notifier,
            NullLogger<ProjectionSubscriptionService>.Instance,
            fallbackDriver: null,
            reconciliation);

        await sut.SubscribeAsync("orders", "acme", TestContext.Current.CancellationToken);
        connection.JoinedGroups.Clear();

        await connection.RaiseStateAsync(new ProjectionHubConnectionStateChanged(ProjectionHubConnectionState.Reconnected));

        connection.JoinedGroups.ShouldBe(["orders:acme"]);
        reconciliation.Calls.ShouldBe(1);
    }

    [Fact]
    public async Task Reconnected_AppliesConnectedStateAfterRejoin_PerOrderingContract() {
        // P9 / DN5=a — the observable transition order on Reconnected is: rejoin completes → state
        // applies Connected → reconciliation fires. This locks the ordering contract so adopters
        // observing IProjectionConnectionState transitions cannot see Connected before rejoin.
        FakeProjectionHubConnection connection = new();
        TestNotifier notifier = new();
        TestRefreshScheduler refresh = new();
        TestReconciliationCoordinator reconciliation = new();
        TestProjectionConnectionState state = new();
        ProjectionSubscriptionService sut = new(
            global::Microsoft.Extensions.Options.Options.Create(new EventStoreOptions {
                BaseAddress = new Uri("https://eventstore.test"),
                RequireAccessToken = false,
                ProjectionChangesHubPath = "/hubs/projection-changes",
            }),
            new FakeProjectionHubConnectionFactory(connection, "https://eventstore.test/hubs/projection-changes"),
            state,
            refresh,
            notifier,
            NullLogger<ProjectionSubscriptionService>.Instance,
            fallbackDriver: null,
            reconciliation);

        await sut.SubscribeAsync("orders", "acme", TestContext.Current.CancellationToken);
        await sut.SubscribeAsync("billing", "acme", TestContext.Current.CancellationToken);
        connection.JoinedGroups.Clear();

        await connection.RaiseStateAsync(new ProjectionHubConnectionStateChanged(ProjectionHubConnectionState.Reconnected));

        // P49 — multi-group at-most-once join per epoch. Two distinct groups → exactly two joins.
        connection.JoinedGroups.OrderBy(static g => g, StringComparer.Ordinal)
            .ShouldBe(new[] { "billing:acme", "orders:acme" });
        // DN5=a — Connected applied unconditionally after rejoin completes (here both rejoins
        // succeed; per-group degradation in the failed-rejoin scenario is covered by
        // FailedRejoin_MarksGroupDegraded below).
        state.Current.Status.ShouldBe(ProjectionConnectionStatus.Connected);
        reconciliation.Calls.ShouldBe(1);
    }

    [Fact]
    public async Task InitialStartFailure_SurfacesDisconnectedState_WithoutActiveGroup() {
        FakeProjectionHubConnection connection = new() { StartException = new InvalidOperationException("token expired") };
        TestNotifier notifier = new();
        TestProjectionConnectionState state = new();
        ProjectionSubscriptionService sut = Create(connection, notifier, "/hubs/projection-changes", state);

        _ = await Should.ThrowAsync<InvalidOperationException>(
            async () => await sut.SubscribeAsync("orders", "acme", TestContext.Current.CancellationToken).ConfigureAwait(true)).ConfigureAwait(true);

        state.Current.Status.ShouldBe(ProjectionConnectionStatus.Disconnected);
        state.Current.LastFailureCategory.ShouldBe("InitialStartFailed");
        connection.JoinedGroups.ShouldBeEmpty();
    }

    [Fact]
    public async Task Subscribe_WhenAccessTokenProviderReturnsEmpty_DoesNotStartSignalRConnection() {
        FakeProjectionHubConnection connection = new();
        TestNotifier notifier = new();
        ProjectionSubscriptionService sut = new(
            global::Microsoft.Extensions.Options.Options.Create(new EventStoreOptions {
                BaseAddress = new Uri("https://eventstore.test"),
                RequireAccessToken = true,
                AccessTokenProvider = _ => ValueTask.FromResult<string?>(null),
                ProjectionChangesHubPath = "/hubs/projection-changes",
            }),
            new FakeProjectionHubConnectionFactory(connection, "https://eventstore.test/hubs/projection-changes"),
            new TestProjectionConnectionState(),
            new TestRefreshScheduler(),
            notifier,
            NullLogger<ProjectionSubscriptionService>.Instance);

        _ = await Should.ThrowAsync<InvalidOperationException>(
            async () => await sut.SubscribeAsync("orders", "acme", TestContext.Current.CancellationToken).ConfigureAwait(true)).ConfigureAwait(true);

        connection.StartCount.ShouldBe(0);
        connection.JoinedGroups.ShouldBeEmpty();
    }

    [Fact]
    public async Task Subscribe_WhenTokenAcquisitionIsCanceled_DoesNotStartSignalRConnection() {
        FakeProjectionHubConnection connection = new();
        TestNotifier notifier = new();
        using CancellationTokenSource cts = new();
        await cts.CancelAsync();
        ProjectionSubscriptionService sut = new(
            global::Microsoft.Extensions.Options.Options.Create(new EventStoreOptions {
                BaseAddress = new Uri("https://eventstore.test"),
                RequireAccessToken = true,
                AccessTokenProvider = token => {
                    token.ThrowIfCancellationRequested();
                    return ValueTask.FromResult<string?>("token");
                },
                ProjectionChangesHubPath = "/hubs/projection-changes",
            }),
            new FakeProjectionHubConnectionFactory(connection, "https://eventstore.test/hubs/projection-changes"),
            new TestProjectionConnectionState(),
            new TestRefreshScheduler(),
            notifier,
            NullLogger<ProjectionSubscriptionService>.Instance);

        _ = await Should.ThrowAsync<OperationCanceledException>(
            async () => await sut.SubscribeAsync("orders", "acme", cts.Token).ConfigureAwait(true)).ConfigureAwait(true);

        connection.StartCount.ShouldBe(0);
        connection.JoinedGroups.ShouldBeEmpty();
    }

    [Fact]
    public async Task Subscribe_WhenJoinFails_DoesNotLeaveStaleActiveGroup() {
        FakeProjectionHubConnection connection = new() { JoinException = new InvalidOperationException("join failed") };
        TestNotifier notifier = new();
        ProjectionSubscriptionService sut = Create(connection, notifier);

        _ = await Should.ThrowAsync<InvalidOperationException>(
            async () => await sut.SubscribeAsync("orders", "acme", TestContext.Current.CancellationToken).ConfigureAwait(true)).ConfigureAwait(true);
        await sut.UnsubscribeAsync("orders", "acme", TestContext.Current.CancellationToken);

        connection.LeftGroups.ShouldBeEmpty();
    }

    [Fact]
    public async Task Unsubscribe_LeavesOnlyActiveGroups_AndDisposeSuppressesCallbacks() {
        FakeProjectionHubConnection connection = new();
        TestNotifier notifier = new();
        ProjectionSubscriptionService sut = Create(connection, notifier);

        await sut.SubscribeAsync("orders", "acme", TestContext.Current.CancellationToken);
        await sut.UnsubscribeAsync("orders", "acme", TestContext.Current.CancellationToken);
        await connection.RaiseAsync("orders", "acme");
        await sut.DisposeAsync();
        await connection.RaiseAsync("orders", "acme");

        connection.LeftGroups.ShouldBe(["orders:acme"]);
        connection.StopCount.ShouldBe(1);
        notifier.Changed.ShouldBeEmpty();
    }

    [Fact]
    public async Task Subscribe_HonorsConfiguredHubPath() {
        FakeProjectionHubConnection connection = new();
        TestNotifier notifier = new();
        ProjectionSubscriptionService sut = Create(connection, notifier, "/custom-hub");

        await sut.SubscribeAsync("orders", "acme", TestContext.Current.CancellationToken);

        connection.StartCount.ShouldBe(1);
    }

    [Fact]
    public async Task Subscribe_PropagatesCancellationToken_ToJoin() {
        // P7 (AC8): cancellation must reach SignalR JoinGroup.
        FakeProjectionHubConnection connection = new();
        TestNotifier notifier = new();
        ProjectionSubscriptionService sut = Create(connection, notifier);

        await sut.SubscribeAsync("orders", "acme", TestContext.Current.CancellationToken);

        connection.LastJoinToken.CanBeCanceled.ShouldBeTrue();
    }

    [Fact]
    public async Task Unsubscribe_KeepsActiveGroup_WhenLeaveGroupThrows() {
        // P3: leaving must not be observed before the server actually acknowledges.
        FakeProjectionHubConnection connection = new();
        TestNotifier notifier = new();
        ProjectionSubscriptionService sut = Create(connection, notifier);

        await sut.SubscribeAsync("orders", "acme", TestContext.Current.CancellationToken);
        connection.LeaveException = new InvalidOperationException("transient hub error");

        _ = await Should.ThrowAsync<InvalidOperationException>(
            async () => await sut.UnsubscribeAsync("orders", "acme", TestContext.Current.CancellationToken).ConfigureAwait(true)).ConfigureAwait(true);

        // The group is still on the server; client view must agree so that a retry can leave it.
        connection.LeaveException = null;
        await sut.UnsubscribeAsync("orders", "acme", TestContext.Current.CancellationToken);

        // The first leave threw before the fake recorded it; the retry succeeds and records once.
        connection.LeftGroups.ShouldBe(["orders:acme"]);
    }

    [Fact]
    public async Task OnNudge_RaisesTenantAwareEvent_WhenNotifierImplementsCompanionInterface() {
        // DN3: tenant-carrying notifier surface for Stories 5-3/5-4 consumers.
        FakeProjectionHubConnection connection = new();
        TenantAwareNotifier notifier = new();
        ProjectionSubscriptionService sut = Create(connection, notifier);

        await sut.SubscribeAsync("orders", "acme", TestContext.Current.CancellationToken);
        await connection.RaiseAsync("orders", "acme");

        notifier.TenantChanged.ShouldBe([("orders", "acme")]);
        notifier.Changed.ShouldBe(["orders"]);
    }

    [Fact]
    public async Task OnNudge_DoesNotPropagateSubscriberException_ToSignalRDispatcher() {
        // P8: a buggy subscriber must not kill the SignalR callback dispatcher.
        FakeProjectionHubConnection connection = new();
        ThrowingNotifier notifier = new();
        ProjectionSubscriptionService sut = Create(connection, notifier);

        await sut.SubscribeAsync("orders", "acme", TestContext.Current.CancellationToken);
        // Must not throw.
        await connection.RaiseAsync("orders", "acme");
    }

    [Fact]
    public async Task FailedRejoin_MarksGroupDegraded_AndSkipsNudgeRefresh_UntilNextSuccessfulRejoin() {
        // DN2: per-group degraded marker. After a rejoin failure, nudges for that group must
        // not trigger refresh until a subsequent rejoin succeeds.
        FakeProjectionHubConnection connection = new();
        TestNotifier notifier = new();
        TestRefreshScheduler refresh = new();
        ProjectionSubscriptionService sut = Create(connection, notifier, refreshScheduler: refresh);

        await sut.SubscribeAsync("orders", "acme", TestContext.Current.CancellationToken);
        connection.JoinedGroups.Clear();
        refresh.NudgeRefreshes.Clear();

        // Cause rejoin to fail.
        connection.JoinException = new InvalidOperationException("transient join failure");
        await connection.RaiseStateAsync(new ProjectionHubConnectionStateChanged(ProjectionHubConnectionState.Reconnected));

        // Nudge while degraded: must not trigger refresh.
        await connection.RaiseAsync("orders", "acme");
        refresh.NudgeRefreshes.ShouldBeEmpty();
        notifier.Changed.ShouldBeEmpty();

        // Recover on next successful rejoin.
        connection.JoinException = null;
        await connection.RaiseStateAsync(new ProjectionHubConnectionStateChanged(ProjectionHubConnectionState.Reconnected));
        await connection.RaiseAsync("orders", "acme");
        refresh.NudgeRefreshes.ShouldBe([("orders", "acme")]);
    }

    [Fact]
    public async Task RejoinFailure_LogsRedactedFailureCategory_NotRawExceptionMessage() {
        // P5 / D11: rejoin warning must log the bounded exception type name only — never the
        // raw exception message (which can carry group/tenant/payload arguments).
        FakeProjectionHubConnection connection = new();
        TestNotifier notifier = new();
        CapturingLogger<ProjectionSubscriptionService> logger = new();
        const string sensitive = "tenant=acme group=orders:acme token=Bearer-secret";
        connection.JoinException = new InvalidOperationException(sensitive);

        ProjectionSubscriptionService sut = new(
            Microsoft.Extensions.Options.Options.Create(new EventStoreOptions {
                BaseAddress = new Uri("https://eventstore.test"),
                RequireAccessToken = false,
                ProjectionChangesHubPath = "/hubs/projection-changes",
            }),
            new FakeProjectionHubConnectionFactory(connection, "https://eventstore.test/hubs/projection-changes"),
            new TestProjectionConnectionState(),
            new TestRefreshScheduler(),
            notifier,
            logger);

        connection.JoinException = null;
        await sut.SubscribeAsync("orders", "acme", TestContext.Current.CancellationToken);
        connection.JoinException = new InvalidOperationException(sensitive);

        await connection.RaiseStateAsync(new ProjectionHubConnectionStateChanged(ProjectionHubConnectionState.Reconnected));

        logger.Entries.ShouldNotBeEmpty();
        foreach (CapturingLogger<ProjectionSubscriptionService>.Entry entry in logger.Entries) {
            entry.Message.ShouldNotContain("acme");
            entry.Message.ShouldNotContain("orders:acme");
            entry.Message.ShouldNotContain("Bearer-secret");
            entry.Message.ShouldNotContain(sensitive);
        }

        logger.Entries.ShouldContain(e => e.Message.Contains("InvalidOperationException", StringComparison.Ordinal));
    }

    private static ProjectionSubscriptionService Create(
        FakeProjectionHubConnection connection,
        IProjectionChangeNotifier notifier,
        string hubPath = "/hubs/projection-changes",
        IProjectionFallbackRefreshScheduler? refreshScheduler = null)
        => Create(connection, notifier, hubPath, new TestProjectionConnectionState(), refreshScheduler ?? new TestRefreshScheduler());

    private static ProjectionSubscriptionService Create(
        FakeProjectionHubConnection connection,
        IProjectionChangeNotifier notifier,
        string hubPath,
        IProjectionConnectionState connectionState,
        IProjectionFallbackRefreshScheduler? refreshScheduler = null)
        => new(
            global::Microsoft.Extensions.Options.Options.Create(new EventStoreOptions {
                BaseAddress = new Uri("https://eventstore.test"),
                RequireAccessToken = false,
                ProjectionChangesHubPath = hubPath,
            }),
            new FakeProjectionHubConnectionFactory(connection, $"https://eventstore.test{hubPath}"),
            connectionState,
            refreshScheduler ?? new TestRefreshScheduler(),
            notifier,
            NullLogger<ProjectionSubscriptionService>.Instance);

    private sealed class FakeProjectionHubConnectionFactory(FakeProjectionHubConnection connection, string expectedHubUrl) : IProjectionHubConnectionFactory {
        public IProjectionHubConnection Create(Uri hubUri, Func<CancellationToken, ValueTask<string?>>? accessTokenProvider) {
            hubUri.ToString().ShouldBe(expectedHubUrl);
            connection.AccessTokenProvider = accessTokenProvider;
            return connection;
        }
    }

    private sealed class FakeProjectionHubConnection : IProjectionHubConnection {
        private Func<string, string, Task>? _handler;
        private Func<ProjectionChangedDetail, Task>? _detailHandler;
        private Func<ProjectionHubConnectionStateChanged, Task>? _stateHandler;

        public bool IsConnected { get; private set; }
        public int StartCount { get; private set; }
        public int StopCount { get; private set; }
        public Func<CancellationToken, ValueTask<string?>>? AccessTokenProvider { get; set; }
        public Exception? StartException { get; set; }
        public Exception? JoinException { get; set; }
        public Exception? LeaveException { get; set; }
        public List<string> JoinedGroups { get; } = [];
        public List<string> LeftGroups { get; } = [];
        public CancellationToken LastJoinToken { get; private set; }

        public IDisposable OnProjectionChanged(Func<string, string, Task> handler) {
            _handler = handler;
            return new Registration(() => _handler = null);
        }

        public IDisposable OnProjectionChangedDetail(Func<ProjectionChangedDetail, Task> handler) {
            _detailHandler = handler;
            return new Registration(() => _detailHandler = null);
        }

        public IDisposable OnConnectionStateChanged(Func<ProjectionHubConnectionStateChanged, Task> handler) {
            _stateHandler = handler;
            return new Registration(() => _stateHandler = null);
        }

        public async Task StartAsync(CancellationToken cancellationToken) {
            StartCount++;
            if (StartException is not null) {
                throw StartException;
            }

            IsConnected = true;
            if (_stateHandler is not null) {
                await _stateHandler(new ProjectionHubConnectionStateChanged(ProjectionHubConnectionState.Connected)).ConfigureAwait(false);
            }
        }

        public Task JoinGroupAsync(string projectionType, string tenantId, CancellationToken cancellationToken)
            => JoinGroupAsync(projectionType, tenantId, scope: null, cancellationToken);

        public Task JoinGroupAsync(string projectionType, string tenantId, string? scope, CancellationToken cancellationToken) {
            LastJoinToken = cancellationToken;
            if (JoinException is not null) {
                throw JoinException;
            }

            JoinedGroups.Add(string.IsNullOrWhiteSpace(scope)
                ? $"{projectionType}:{tenantId}"
                : $"{projectionType}:{tenantId}:{scope}");
            return Task.CompletedTask;
        }

        public Task LeaveGroupAsync(string projectionType, string tenantId, CancellationToken cancellationToken)
            => LeaveGroupAsync(projectionType, tenantId, scope: null, cancellationToken);

        public Task LeaveGroupAsync(string projectionType, string tenantId, string? scope, CancellationToken cancellationToken) {
            if (LeaveException is not null) {
                throw LeaveException;
            }

            LeftGroups.Add(string.IsNullOrWhiteSpace(scope)
                ? $"{projectionType}:{tenantId}"
                : $"{projectionType}:{tenantId}:{scope}");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) {
            StopCount++;
            IsConnected = false;
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public Task RaiseAsync(string projectionType, string tenantId)
            => _handler?.Invoke(projectionType, tenantId) ?? Task.CompletedTask;

        public Task RaiseDetailAsync(ProjectionChangedDetail detail)
            => _detailHandler?.Invoke(detail) ?? Task.CompletedTask;

        public Task RaiseStateAsync(ProjectionHubConnectionStateChanged change) {
            IsConnected = change.State is ProjectionHubConnectionState.Connected or ProjectionHubConnectionState.Reconnected;
            return _stateHandler?.Invoke(change) ?? Task.CompletedTask;
        }
    }

    private sealed class StubAuthenticationStateProvider(ClaimsPrincipal principal) : AuthenticationStateProvider {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(principal));
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class TestNotifier : IProjectionChangeNotifier {
        public event Action<string>? ProjectionChanged;
        public List<string> Changed { get; } = [];

        public void NotifyChanged(string projectionType) {
            Changed.Add(projectionType);
            ProjectionChanged?.Invoke(projectionType);
        }
    }

    private sealed class DetailCapturingNotifier : IProjectionChangeNotifier, IProjectionChangeDetailNotifier {
        public event Action<string>? ProjectionChanged;
        public event Func<ProjectionChangedDetail, Task>? ProjectionChangedDetail;
        public List<ProjectionChangedDetail> Details { get; } = [];

        public void NotifyChanged(string projectionType) => ProjectionChanged?.Invoke(projectionType);

        public Task NotifyDetailAsync(ProjectionChangedDetail detail, CancellationToken cancellationToken = default) {
            Details.Add(detail);
            return ProjectionChangedDetail?.Invoke(detail) ?? Task.CompletedTask;
        }
    }

    private sealed class TenantAwareNotifier : IProjectionChangeNotifierWithTenant {
        public event Action<string>? ProjectionChanged;
        public event Action<string, string>? ProjectionChangedForTenant;
        public List<string> Changed { get; } = [];
        public List<(string Projection, string Tenant)> TenantChanged { get; } = [];

        public void NotifyChanged(string projectionType) {
            Changed.Add(projectionType);
            ProjectionChanged?.Invoke(projectionType);
        }

        public void NotifyChanged(string projectionType, string tenantId) {
            Changed.Add(projectionType);
            TenantChanged.Add((projectionType, tenantId));
            ProjectionChanged?.Invoke(projectionType);
            ProjectionChangedForTenant?.Invoke(projectionType, tenantId);
        }
    }

    private sealed class ThrowingNotifier : IProjectionChangeNotifier {
        public event Action<string>? ProjectionChanged {
            add { }
            remove { }
        }

        public void NotifyChanged(string projectionType)
            => throw new InvalidOperationException("subscriber blew up");
    }

    private sealed class Registration(Action dispose) : IDisposable {
        public void Dispose() => dispose();
    }

    private sealed class TestProjectionConnectionState : IProjectionConnectionState {
        public ProjectionConnectionSnapshot Current { get; private set; } = new(
            ProjectionConnectionStatus.Connected,
            DateTimeOffset.UtcNow,
            ReconnectAttempt: 0,
            LastFailureCategory: null);

        public IDisposable Subscribe(Action<ProjectionConnectionSnapshot> handler, bool replay = true) {
            if (replay) {
                handler(Current);
            }

            return new Registration(() => { });
        }

        public void Apply(ProjectionConnectionTransition transition)
            => Current = new ProjectionConnectionSnapshot(
                transition.Status,
                DateTimeOffset.UtcNow,
                transition.ReconnectAttempt,
                transition.FailureCategory);
    }

    private sealed class TestRefreshScheduler : IProjectionFallbackRefreshScheduler {
        public List<(string ProjectionType, string TenantId)> NudgeRefreshes { get; } = [];

        public IDisposable RegisterLane(ProjectionFallbackLane lane)
            => new Registration(() => { });

        public Task<int> TriggerFallbackOnceAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(0);

        public Task<int> TriggerNudgeRefreshAsync(string projectionType, string tenantId, CancellationToken cancellationToken = default) {
            NudgeRefreshes.Add((projectionType, tenantId));
            return Task.FromResult(1);
        }
    }

    private sealed class TestReconciliationCoordinator : IReconnectionReconciliationCoordinator {
        public int Calls { get; private set; }

        public Task<ProjectionReconciliationRefreshResult> ReconcileAsync(CancellationToken cancellationToken = default) {
            Calls++;
            return Task.FromResult(ProjectionReconciliationRefreshResult.Empty);
        }
    }

    private sealed class TestPendingPolling : IPendingCommandPollingCoordinator {
        public int Calls { get; private set; }

        public Task<int> PollOnceAsync(CancellationToken cancellationToken = default) {
            Calls++;
            return Task.FromResult(1);
        }
    }

    private sealed class TestOptionsMonitor(FcShellOptions value) : IOptionsMonitor<FcShellOptions> {
        public FcShellOptions CurrentValue => value;

        public FcShellOptions Get(string? name) => value;

        public IDisposable? OnChange(Action<FcShellOptions, string?> listener) => new Registration(() => { });
    }

    private sealed class MutableUserContextAccessor(string? tenantId, string? userId) : IUserContextAccessor {
        public string? TenantId { get; set; } = tenantId;
        public string? UserId { get; set; } = userId;
    }

    private sealed class CapturingLogger<T> : ILogger<T> {
        public List<Entry> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) => Entries.Add(new Entry(logLevel, formatter(state, exception), exception?.GetType().Name));

        public sealed record Entry(LogLevel Level, string Message, string? ExceptionType);
    }
}
