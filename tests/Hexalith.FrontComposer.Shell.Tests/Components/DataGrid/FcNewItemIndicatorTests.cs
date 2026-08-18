using System.Globalization;

using Bunit;

using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Components.DataGrid;
using Hexalith.FrontComposer.Shell.Infrastructure.Telemetry;
using Hexalith.FrontComposer.Shell.State.PendingCommands;
using Hexalith.FrontComposer.Shell.Tests.Components.Layout;
using Hexalith.FrontComposer.Shell.Tests.Infrastructure.Telemetry;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.DataGrid;

public sealed class FcNewItemIndicatorTests : LayoutComponentTestBase {
    public FcNewItemIndicatorTests() {
        CultureInfo.CurrentUICulture = new CultureInfo("en");
        CultureInfo.CurrentCulture = new CultureInfo("en");
        EnsureStoreInitialized();
    }

    [Fact]
    public void RendersAccessiblePoliteIndicatorCopy() {
        IRenderedComponent<FcNewItemIndicator> cut = Render<FcNewItemIndicator>();

        cut.Markup.ShouldContain("New item. It may not match current filters yet.");
        cut.Markup.ShouldContain("aria-live=\"polite\"");
        cut.Markup.ShouldContain("aria-label=\"New item added outside current filters\"");
        cut.Markup.ShouldContain("role=\"status\"");
    }

    [Fact]
    public void AcceptsAdopterOverrideForVisibleTextAndAriaLabel() {
        IRenderedComponent<FcNewItemIndicator> cut = Render<FcNewItemIndicator>(parameters => parameters
            .Add(p => p.Text, "Custom row arrived")
            .Add(p => p.AriaLabelOverride, "Custom row label"));

        cut.Markup.ShouldContain("Custom row arrived");
        cut.Markup.ShouldContain("aria-label=\"Custom row label\"");
    }

    [Fact]
    public void State_EffectiveMutations_NotifyEachAffectedLaneExactlyOnce() {
        FakeTimeProvider time = new(new DateTimeOffset(2026, 4, 26, 12, 0, 0, TimeSpan.Zero));
        using NewItemIndicatorStateService sut = new(time);
        int viewOneNotifications = 0;
        int viewTwoNotifications = 0;
        using IDisposable viewOneSubscription = sut.Subscribe("view-1", () => viewOneNotifications++);
        using IDisposable viewTwoSubscription = sut.Subscribe("view-2", () => viewTwoNotifications++);

        sut.Add(new NewItemIndicatorEntry("view-1", "counter-1", "message-1", time.GetUtcNow()));
        sut.Add(new NewItemIndicatorEntry("view-1", "counter-2", "message-2", time.GetUtcNow()));
        sut.Add(new NewItemIndicatorEntry("view-2", "counter-3", "message-3", time.GetUtcNow()));
        viewOneNotifications.ShouldBe(2);
        viewTwoNotifications.ShouldBe(1);

        viewOneNotifications = 0;
        viewTwoNotifications = 0;
        sut.DismissForFilterChange("view-1");

        viewOneNotifications.ShouldBe(1);
        viewTwoNotifications.ShouldBe(0);
        sut.Snapshot("view-1").ShouldBeEmpty();

        sut.Clear("test-clear");

        viewOneNotifications.ShouldBe(1);
        viewTwoNotifications.ShouldBe(1);
        sut.Snapshot("view-2").ShouldBeEmpty();

        sut.Clear("empty-clear");
        viewOneNotifications.ShouldBe(1);
        viewTwoNotifications.ShouldBe(1);
    }

    [Fact]
    public void State_NonFatalTimerDisposalFailure_DisposesRemainingTimersAndPublishesOnce() {
        RecordingTimerTimeProvider time = new(true, false);
        using NewItemIndicatorStateService sut = new(time);
        int viewOneNotifications = 0;
        int viewTwoNotifications = 0;
        using IDisposable viewOneSubscription = sut.Subscribe("view-1", () => viewOneNotifications++);
        using IDisposable viewTwoSubscription = sut.Subscribe("view-2", () => viewTwoNotifications++);

        sut.Add(new NewItemIndicatorEntry("view-1", "counter-1", "message-1", time.GetUtcNow()));
        sut.Add(new NewItemIndicatorEntry("view-2", "counter-2", "message-2", time.GetUtcNow()));
        viewOneNotifications = 0;
        viewTwoNotifications = 0;

        Should.NotThrow(() => sut.Clear("timer-disposal-failure"));

        time.Timers.Count.ShouldBe(2);
        time.Timers[0].DisposeCount.ShouldBe(1);
        time.Timers[1].DisposeCount.ShouldBe(1);
        viewOneNotifications.ShouldBe(1);
        viewTwoNotifications.ShouldBe(1);
        sut.Snapshot("view-1").ShouldBeEmpty();
        sut.Snapshot("view-2").ShouldBeEmpty();
    }

    [Fact]
    public void State_MaterializationAndTtl_NotifyOnlyEffectiveRemoval() {
        FakeTimeProvider time = new(new DateTimeOffset(2026, 4, 26, 12, 0, 0, TimeSpan.Zero));
        using NewItemIndicatorStateService sut = new(time);
        int notifications = 0;
        using IDisposable subscription = sut.Subscribe("view-1", () => notifications++);

        sut.Add(new NewItemIndicatorEntry("view-1", "counter-1", "message-1", time.GetUtcNow()));
        notifications = 0;
        sut.DismissMaterialized("view-1", "missing");
        notifications.ShouldBe(0);

        sut.DismissMaterialized("view-1", "counter-1");
        notifications.ShouldBe(1);
        sut.DismissMaterialized("view-1", "counter-1");
        notifications.ShouldBe(1);

        sut.Add(new NewItemIndicatorEntry("view-1", "counter-2", "message-2", time.GetUtcNow()));
        notifications = 0;

        time.Advance(TimeSpan.FromSeconds(10));

        sut.Snapshot("view-1").ShouldBeEmpty();
        notifications.ShouldBe(1);
    }

    [Fact]
    public void State_SecondMessageForActiveRow_KeepsFirstProvenanceAndOriginalExpiry() {
        // Story 9.6 — first-wins: a later distinct material command targeting an already-active
        // (ViewKey, EntityKey) must not replace the incumbent MessageId/CreatedAt, must not notify
        // the view, and must not reset or extend the ten-second TTL. Before 9.6 this row was
        // last-wins and expired at t+15s.
        FakeTimeProvider time = new(new DateTimeOffset(2026, 4, 26, 12, 0, 0, TimeSpan.Zero));
        using NewItemIndicatorStateService sut = new(time);
        DateTimeOffset firstCreatedAt = time.GetUtcNow();
        int notifications = 0;
        using IDisposable subscription = sut.Subscribe("view-1", () => notifications++);

        sut.Add(new NewItemIndicatorEntry("view-1", "counter-1", "message-1", firstCreatedAt));
        notifications.ShouldBe(1);

        time.Advance(TimeSpan.FromSeconds(5));
        sut.Add(new NewItemIndicatorEntry("view-1", "counter-1", "message-2", time.GetUtcNow()));

        NewItemIndicatorEntry incumbent = sut.Snapshot("view-1").ShouldHaveSingleItem();
        incumbent.MessageId.ShouldBe("message-1");
        incumbent.CreatedAt.ShouldBe(firstCreatedAt);
        notifications.ShouldBe(1);

        // The original expiry instant still governs: t+10s, not the last-wins t+15s.
        time.Advance(TimeSpan.FromSeconds(5));

        sut.Snapshot("view-1").ShouldBeEmpty();
        notifications.ShouldBe(2);
    }

    [Fact]
    public void State_SuppressedAddWithFailingTimerDisposal_SwallowsTheFaultAndIgnoresTheLateFire() {
        // The suppressed Add speculatively created a timer before the occupancy test; it must be
        // disposed, the incumbent timer must be left alone, and a non-fatal disposal fault must not
        // escape. A failed disposal leaves that timer armed, and it carries a NEWER generation than
        // the incumbent, so OnTimerFired's generation guard is the only thing standing between a late
        // fire and a wrongly evicted incumbent. Pin that guard from the exact production shape.
        RecordingTimerTimeProvider time = new(false, true);
        using NewItemIndicatorStateService sut = new(time);
        DateTimeOffset createdAt = time.GetUtcNow();
        int notifications = 0;
        using IDisposable subscription = sut.Subscribe("view-1", () => notifications++);

        sut.Add(new NewItemIndicatorEntry("view-1", "counter-1", "message-1", createdAt));
        notifications = 0;

        Should.NotThrow(() => sut.Add(
            new NewItemIndicatorEntry("view-1", "counter-1", "message-2", time.GetUtcNow())));

        time.Timers.Count.ShouldBe(2);
        time.Timers[0].DisposeCount.ShouldBe(0);
        time.Timers[1].DisposeCount.ShouldBe(1);
        notifications.ShouldBe(0);
        sut.Snapshot("view-1").ShouldHaveSingleItem().MessageId.ShouldBe("message-1");

        // The speculative timer survived its failed disposal; firing it must change nothing.
        Should.NotThrow(time.Timers[1].Fire);

        NewItemIndicatorEntry incumbent = sut.Snapshot("view-1").ShouldHaveSingleItem();
        incumbent.MessageId.ShouldBe("message-1");
        incumbent.CreatedAt.ShouldBe(createdAt);
        notifications.ShouldBe(0);

        // The incumbent's own timer still expires the row exactly once.
        time.Timers[0].Fire();

        sut.Snapshot("view-1").ShouldBeEmpty();
        notifications.ShouldBe(1);
    }

    [Fact]
    public void State_SuppressedAdd_EmitsOneDigestedSuppressionDiagnosticForTheTargetRow() {
        FakeTimeProvider time = new(new DateTimeOffset(2026, 4, 26, 12, 0, 0, TimeSpan.Zero));
        CapturingLogger<NewItemIndicatorStateService> logger = new();
        using NewItemIndicatorStateService sut = new(time, userContext: null, logger);

        sut.Add(new NewItemIndicatorEntry("view-1", "counter-1", "message-1", time.GetUtcNow()));
        logger.Entries.ShouldBeEmpty();

        sut.Add(new NewItemIndicatorEntry("view-1", "counter-1", "message-2", time.GetUtcNow()));

        CapturedLogEntry suppression = logger.Entries.ShouldHaveSingleItem();
        suppression.Level.ShouldBe(LogLevel.Debug);
        suppression.EventId.Id.ShouldBe(5784);
        suppression.EventId.Name.ShouldBe("NewItemIndicatorSuppressed");
        suppression.State["MessageId"].ShouldBe(FrontComposerHotPathLog.DigestIdentifier("message-2"));
        suppression.State["ViewKey"].ShouldBe(FrontComposerHotPathLog.DigestIdentifier("view-1"));
        suppression.State["EntityKey"].ShouldBe(FrontComposerHotPathLog.DigestIdentifier("counter-1"));
        suppression.Message.ShouldNotContain("message-2");
        suppression.Message.ShouldNotContain("view-1");
        suppression.Message.ShouldNotContain("counter-1");
        suppression.Exception.ShouldBeNull();
    }

    [Fact]
    public void State_DistinctRows_StillPublishIndependently() {
        FakeTimeProvider time = new(new DateTimeOffset(2026, 4, 26, 12, 0, 0, TimeSpan.Zero));
        using NewItemIndicatorStateService sut = new(time);
        int notifications = 0;
        using IDisposable subscription = sut.Subscribe("view-1", () => notifications++);

        sut.Add(new NewItemIndicatorEntry("view-1", "counter-1", "message-1", time.GetUtcNow()));
        sut.Add(new NewItemIndicatorEntry("view-1", "counter-2", "message-2", time.GetUtcNow()));
        sut.Add(new NewItemIndicatorEntry("view-2", "counter-1", "message-3", time.GetUtcNow()));

        notifications.ShouldBe(2);
        sut.Snapshot("view-1").Select(entry => entry.MessageId).ShouldBe(["message-1", "message-2"], ignoreOrder: true);
        sut.Snapshot("view-2").ShouldHaveSingleItem().MessageId.ShouldBe("message-3");
    }

    [Fact]
    public void State_RowReopensAfterEveryLocalRemovalPath_WithAFreshTenSecondLifetime() {
        FakeTimeProvider time = new(new DateTimeOffset(2026, 4, 26, 12, 0, 0, TimeSpan.Zero));
        using NewItemIndicatorStateService sut = new(time);

        // TTL expiry re-opens the row.
        SeedActiveRow("message-1");
        time.Advance(TimeSpan.FromSeconds(10));
        sut.Snapshot("view-1").ShouldBeEmpty();
        ProveReopenedRowGetsAFreshTenSecondLifetime("message-2");

        // Materialization re-opens the row.
        SeedActiveRow("message-3");
        sut.DismissMaterialized("view-1", "counter-1");
        sut.Snapshot("view-1").ShouldBeEmpty();
        ProveReopenedRowGetsAFreshTenSecondLifetime("message-4");

        // Filter/re-query dismissal re-opens the row.
        SeedActiveRow("message-5");
        sut.DismissForFilterChange("view-1");
        sut.Snapshot("view-1").ShouldBeEmpty();
        ProveReopenedRowGetsAFreshTenSecondLifetime("message-6");

        // Explicit clear re-opens the row.
        SeedActiveRow("message-7");
        sut.Clear("test-clear");
        sut.Snapshot("view-1").ShouldBeEmpty();
        ProveReopenedRowGetsAFreshTenSecondLifetime("message-8");

        void SeedActiveRow(string messageId) =>
            sut.Add(new NewItemIndicatorEntry("view-1", "counter-1", messageId, time.GetUtcNow()));

        void ProveReopenedRowGetsAFreshTenSecondLifetime(string messageId) {
            DateTimeOffset reopenedAt = time.GetUtcNow();
            sut.Add(new NewItemIndicatorEntry("view-1", "counter-1", messageId, reopenedAt));

            NewItemIndicatorEntry reopened = sut.Snapshot("view-1").ShouldHaveSingleItem();
            reopened.MessageId.ShouldBe(messageId);
            reopened.CreatedAt.ShouldBe(reopenedAt);

            // The window is measured from this entry, not inherited from the removed one: alive at
            // +9s and gone at exactly +10s.
            time.Advance(TimeSpan.FromSeconds(9));
            sut.Snapshot("view-1").ShouldHaveSingleItem().MessageId.ShouldBe(messageId);
            time.Advance(TimeSpan.FromSeconds(1));
            sut.Snapshot("view-1").ShouldBeEmpty();
        }
    }

    [Fact]
    public void State_ScopeTransition_ReopensTheSameRowBeforeTheOccupancyTest() {
        FakeTimeProvider time = new(new DateTimeOffset(2026, 4, 26, 12, 0, 0, TimeSpan.Zero));
        MutableUserContextAccessor accessor = new("tenant-1", "user-1");
        using NewItemIndicatorStateService sut = CreateScopedState(time, accessor);
        DateTimeOffset firstCreatedAt = time.GetUtcNow();

        sut.Add(new NewItemIndicatorEntry("view-1", "counter-1", "message-1", firstCreatedAt));
        time.Advance(TimeSpan.FromSeconds(4));
        sut.Add(new NewItemIndicatorEntry("view-1", "counter-1", "message-2", time.GetUtcNow()));
        sut.Snapshot("view-1").ShouldHaveSingleItem().MessageId.ShouldBe("message-1");

        accessor.TenantId = "tenant-2";
        DateTimeOffset reopenedAt = time.GetUtcNow();
        sut.Add(new NewItemIndicatorEntry("view-1", "counter-1", "message-3", reopenedAt));

        NewItemIndicatorEntry reopened = sut.Snapshot("view-1").ShouldHaveSingleItem();
        reopened.MessageId.ShouldBe("message-3");
        reopened.CreatedAt.ShouldBe(reopenedAt);

        // The re-opened entry owns a fresh ten-second lifetime measured from its own creation.
        time.Advance(TimeSpan.FromSeconds(9));
        sut.Snapshot("view-1").ShouldHaveSingleItem().MessageId.ShouldBe("message-3");
        time.Advance(TimeSpan.FromSeconds(1));
        sut.Snapshot("view-1").ShouldBeEmpty();
    }

    [Fact]
    public void State_ConcurrentAddsForOneRow_LeaveExactlyOneEntryAndOneNotification() {
        // The producer publishes outside its own lock, so decision order and Add arrival order can
        // invert. Two threads aligned on a Barrier race distinct message IDs at the same
        // (ViewKey, EntityKey). The invariant — one entry, one surviving timer, one notification,
        // provenance matching whichever call won — holds on every legal interleaving, so correct
        // code never fails while a non-atomic regression fails under repeated load.
        const int iterations = 1000;
        CancellationToken cancellationToken = Xunit.TestContext.Current.CancellationToken;
        DateTimeOffset firstInstant = new(2026, 4, 26, 12, 0, 0, TimeSpan.Zero);
        DateTimeOffset secondInstant = firstInstant.AddSeconds(1);

        for (int iteration = 0; iteration < iterations; iteration++) {
            RecordingTimerTimeProvider time = new(false, false);
            using NewItemIndicatorStateService sut = new(time);
            int notifications = 0;
            using IDisposable subscription = sut.Subscribe(
                "view-1",
                () => Interlocked.Increment(ref notifications));
            using Barrier gate = new(2);

            Exception? firstFault = null;
            Exception? secondFault = null;
            var first = new Thread(() => {
                try {
                    gate.SignalAndWait(cancellationToken);
                    sut.Add(new NewItemIndicatorEntry("view-1", "counter-1", "message-1", firstInstant));
                }
                catch (Exception exception) {
                    // An escaping exception on a raw thread would tear down the test host instead of
                    // failing the test, so every fault is carried back to the assertion thread.
                    firstFault = exception;
                }
            }) { IsBackground = true };
            var second = new Thread(() => {
                try {
                    gate.SignalAndWait(cancellationToken);
                    sut.Add(new NewItemIndicatorEntry("view-1", "counter-1", "message-2", secondInstant));
                }
                catch (Exception exception) {
                    secondFault = exception;
                }
            }) { IsBackground = true };

            first.Start();
            second.Start();

            // Join BOTH threads before asserting: an assertion between the joins would throw out of
            // the loop body and dispose the barrier, subscription, and service under a live thread.
            bool firstJoined = first.Join(TimeSpan.FromSeconds(5));
            bool secondJoined = second.Join(TimeSpan.FromSeconds(5));
            firstJoined.ShouldBeTrue($"Iteration {iteration}: first Add did not complete.");
            secondJoined.ShouldBeTrue($"Iteration {iteration}: second Add did not complete.");
            firstFault.ShouldBeNull($"Iteration {iteration}: first Add threw.");
            secondFault.ShouldBeNull($"Iteration {iteration}: second Add threw.");

            NewItemIndicatorEntry winner = sut.Snapshot("view-1").ShouldHaveSingleItem();
            winner.CreatedAt.ShouldBe(
                winner.MessageId == "message-1" ? firstInstant : secondInstant,
                $"Iteration {iteration}: the surviving entry mixed provenance from both calls.");
            Volatile.Read(ref notifications).ShouldBe(1, $"Iteration {iteration}: notification count.");
            time.Timers.Count.ShouldBe(2, $"Iteration {iteration}: timer creation count.");
            time.Timers.Count(timer => timer.DisposeCount == 0)
                .ShouldBe(1, $"Iteration {iteration}: exactly one timer must survive.");
            time.Timers.Count(timer => timer.DisposeCount == 1)
                .ShouldBe(1, $"Iteration {iteration}: the losing speculative timer must be disposed.");
        }
    }

    [Fact]
    public void State_FaultingSubscriber_DoesNotBlockHealthySubscriber() {
        FakeTimeProvider time = new(new DateTimeOffset(2026, 4, 26, 12, 0, 0, TimeSpan.Zero));
        using NewItemIndicatorStateService sut = new(time);
        int notifications = 0;
        using IDisposable faulting = sut.Subscribe("view-1", () => throw new InvalidOperationException("subscriber fault"));
        using IDisposable healthy = sut.Subscribe("view-1", () => notifications++);

        Should.NotThrow(() => sut.Add(
            new NewItemIndicatorEntry("view-1", "counter-1", "message-1", time.GetUtcNow())));

        notifications.ShouldBe(1);
    }

    [Fact]
    public void State_ScopeChangeBeforeSnapshot_ClearsAllPreviousScopeLanesAtomically() {
        FakeTimeProvider time = new(new DateTimeOffset(2026, 4, 26, 12, 0, 0, TimeSpan.Zero));
        MutableUserContextAccessor accessor = new("tenant-1", "user-1");
        using NewItemIndicatorStateService sut = CreateScopedState(time, accessor);
        int viewOneNotifications = 0;
        int viewTwoNotifications = 0;
        using IDisposable viewOneSubscription = sut.Subscribe("view-1", () => viewOneNotifications++);
        using IDisposable viewTwoSubscription = sut.Subscribe("view-2", () => viewTwoNotifications++);

        sut.Add(new NewItemIndicatorEntry("view-1", "counter-1", "message-1", time.GetUtcNow()));
        sut.Add(new NewItemIndicatorEntry("view-2", "counter-2", "message-2", time.GetUtcNow()));
        viewOneNotifications = 0;
        viewTwoNotifications = 0;
        accessor.TenantId = "tenant-2";

        sut.Snapshot("view-1").ShouldBeEmpty();

        viewOneNotifications.ShouldBe(1);
        viewTwoNotifications.ShouldBe(1);
        sut.Snapshot("view-2").ShouldBeEmpty();
        viewOneNotifications.ShouldBe(1);
        viewTwoNotifications.ShouldBe(1);

        sut.Add(new NewItemIndicatorEntry("view-1", "counter-3", "message-3", time.GetUtcNow()));
        sut.Snapshot("view-1").Single().EntityKey.ShouldBe("counter-3");
    }

    [Fact]
    public void State_ScopeChangeBeforeAdd_ReplacesOldLanesAndDeduplicatesSharedLaneNotification() {
        FakeTimeProvider time = new(new DateTimeOffset(2026, 4, 26, 12, 0, 0, TimeSpan.Zero));
        MutableUserContextAccessor accessor = new("tenant-1", "user-1");
        using NewItemIndicatorStateService sut = CreateScopedState(time, accessor);
        int viewOneNotifications = 0;
        int viewTwoNotifications = 0;
        using IDisposable viewOneSubscription = sut.Subscribe("view-1", () => viewOneNotifications++);
        using IDisposable viewTwoSubscription = sut.Subscribe("view-2", () => viewTwoNotifications++);

        sut.Add(new NewItemIndicatorEntry("view-1", "old-counter-1", "message-1", time.GetUtcNow()));
        sut.Add(new NewItemIndicatorEntry("view-2", "old-counter-2", "message-2", time.GetUtcNow()));
        viewOneNotifications = 0;
        viewTwoNotifications = 0;
        accessor.TenantId = "tenant-2";

        sut.Add(new NewItemIndicatorEntry("view-1", "new-counter-1", "message-3", time.GetUtcNow()));

        sut.Snapshot("view-1").Single().EntityKey.ShouldBe("new-counter-1");
        sut.Snapshot("view-2").ShouldBeEmpty();
        viewOneNotifications.ShouldBe(1);
        viewTwoNotifications.ShouldBe(1);
    }

    [Fact]
    public void State_InvalidOrThrowingRegisteredScope_FailsClosedBeforeReadOrAdd() {
        FakeTimeProvider time = new(new DateTimeOffset(2026, 4, 26, 12, 0, 0, TimeSpan.Zero));
        MutableUserContextAccessor accessor = new("tenant-1", "user-1");
        using NewItemIndicatorStateService sut = CreateScopedState(time, accessor);
        int notifications = 0;
        using IDisposable subscription = sut.Subscribe("view-1", () => notifications++);

        sut.Add(new NewItemIndicatorEntry("view-1", "counter-1", "message-1", time.GetUtcNow()));
        notifications = 0;
        accessor.UserId = "   ";

        sut.Snapshot("view-1").ShouldBeEmpty();
        notifications.ShouldBe(1);
        sut.Add(new NewItemIndicatorEntry("view-1", "counter-2", "message-2", time.GetUtcNow()));
        sut.Snapshot("view-1").ShouldBeEmpty();
        notifications.ShouldBe(1);

        accessor.UserId = "user-1";
        sut.Add(new NewItemIndicatorEntry("view-1", "counter-3", "message-3", time.GetUtcNow()));
        notifications = 0;
        accessor.ThrowOnRead = true;

        Should.NotThrow(() => sut.Snapshot("view-1")).ShouldBeEmpty();
        notifications.ShouldBe(1);
    }

    [Fact]
    public async Task State_TimerClearAndUnsubscribeRace_BoundsDeliveryAndLeavesNoEntry() {
        FakeTimeProvider time = new(new DateTimeOffset(2026, 4, 26, 12, 0, 0, TimeSpan.Zero));
        using NewItemIndicatorStateService sut = new(time);
        CancellationToken cancellationToken = Xunit.TestContext.Current.CancellationToken;
        int notifications = 0;
        IDisposable subscription = sut.Subscribe("view-1", () => Interlocked.Increment(ref notifications));
        sut.Add(new NewItemIndicatorEntry("view-1", "counter-1", "message-1", time.GetUtcNow()));
        notifications = 0;

        await Task.WhenAll(
            Task.Run(() => time.Advance(TimeSpan.FromSeconds(10)), cancellationToken),
            Task.Run(() => sut.Clear("race"), cancellationToken),
            Task.Run(subscription.Dispose, cancellationToken));

        Volatile.Read(ref notifications).ShouldBeInRange(0, 1);
        sut.Snapshot("view-1").ShouldBeEmpty();
    }

    [Fact]
    public void State_DisposedService_NoOpsForValidOperations() {
        FakeTimeProvider time = new(new DateTimeOffset(2026, 4, 26, 12, 0, 0, TimeSpan.Zero));
        NewItemIndicatorStateService sut = new(time);
        sut.Dispose();

        Should.NotThrow(() => sut.Add(
            new NewItemIndicatorEntry("view-1", "counter-1", "message-1", time.GetUtcNow())));
        Should.NotThrow(() => sut.DismissMaterialized("view-1", "counter-1"));
        Should.NotThrow(() => sut.DismissForFilterChange("view-1"));
        Should.NotThrow(() => sut.Clear("disposed"));
        sut.Snapshot("view-1").ShouldBeEmpty();
    }

    [Fact]
    public void State_InvalidKeys_RetainBoundaryValidation() {
        using NewItemIndicatorStateService sut = new();
        DateTimeOffset now = new(2026, 4, 26, 12, 0, 0, TimeSpan.Zero);

        Should.Throw<ArgumentException>(() => sut.Add(
            new NewItemIndicatorEntry(" ", "counter-1", "message-1", now)));
        Should.Throw<ArgumentException>(() => sut.Add(
            new NewItemIndicatorEntry("view-1", " ", "message-1", now)));
        Should.Throw<ArgumentException>(() => sut.Snapshot(" "));
        Should.Throw<ArgumentException>(() => sut.DismissForFilterChange(" "));
        Should.Throw<ArgumentException>(() => sut.DismissMaterialized("view-1", " "));
        Should.Throw<ArgumentException>(() => sut.Clear(" "));
    }

    private static NewItemIndicatorStateService CreateScopedState(
        TimeProvider time,
        IUserContextAccessor accessor) =>
        new(time, accessor, NullLogger<NewItemIndicatorStateService>.Instance);

    private sealed class RecordingTimerTimeProvider(params bool[] throwOnDispose) : TimeProvider {
        private readonly Queue<bool> _throwOnDispose = new(throwOnDispose);

        public List<RecordingTimer> Timers { get; } = [];

        /// <summary>The controllable clock; entries stamped from this provider stay deterministic.</summary>
        public DateTimeOffset UtcNow { get; set; } = new(2026, 4, 26, 12, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => UtcNow;

        public override ITimer CreateTimer(
            TimerCallback callback,
            object? state,
            TimeSpan dueTime,
            TimeSpan period) {
            _ = dueTime;
            _ = period;
            RecordingTimer timer = new(callback, state, _throwOnDispose.Dequeue());
            Timers.Add(timer);
            return timer;
        }
    }

    private sealed class RecordingTimer(TimerCallback callback, object? state, bool throwOnDispose) : ITimer {
        public int DisposeCount { get; private set; }

        public bool Change(TimeSpan dueTime, TimeSpan period) => true;

        /// <summary>Invokes the captured callback, standing in for the runtime firing this timer.</summary>
        public void Fire() => callback(state);

        public void Dispose() {
            DisposeCount++;
            if (throwOnDispose) {
                throw new InvalidOperationException("timer disposal failed");
            }
        }

        public ValueTask DisposeAsync() {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }

    private sealed class MutableUserContextAccessor(string? tenantId, string? userId) : IUserContextAccessor {
        private string? _tenantId = tenantId;
        private string? _userId = userId;

        public string? TenantId {
            get {
                ThrowIfRequested();
                return _tenantId;
            }
            set => _tenantId = value;
        }

        public string? UserId {
            get {
                ThrowIfRequested();
                return _userId;
            }
            set => _userId = value;
        }

        public bool ThrowOnRead { get; set; }

        private void ThrowIfRequested() {
            if (ThrowOnRead) {
                throw new InvalidOperationException("scope accessor fault");
            }
        }
    }
}
