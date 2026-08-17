using System.Globalization;

using Bunit;

using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Components.DataGrid;
using Hexalith.FrontComposer.Shell.State.PendingCommands;
using Hexalith.FrontComposer.Shell.Tests.Components.Layout;

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
    public void State_ReplacedEntry_KeepsTheNewGenerationAndLifetime() {
        FakeTimeProvider time = new(new DateTimeOffset(2026, 4, 26, 12, 0, 0, TimeSpan.Zero));
        using NewItemIndicatorStateService sut = new(time);
        int notifications = 0;
        using IDisposable subscription = sut.Subscribe("view-1", () => notifications++);

        sut.Add(new NewItemIndicatorEntry("view-1", "counter-1", "message-1", time.GetUtcNow()));
        time.Advance(TimeSpan.FromSeconds(5));
        sut.Add(new NewItemIndicatorEntry("view-1", "counter-1", "message-2", time.GetUtcNow()));
        notifications = 0;

        time.Advance(TimeSpan.FromSeconds(5));

        sut.Snapshot("view-1").Single().MessageId.ShouldBe("message-2");
        notifications.ShouldBe(0);

        time.Advance(TimeSpan.FromSeconds(5));

        sut.Snapshot("view-1").ShouldBeEmpty();
        notifications.ShouldBe(1);
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

        public override ITimer CreateTimer(
            TimerCallback callback,
            object? state,
            TimeSpan dueTime,
            TimeSpan period) {
            _ = callback;
            _ = state;
            _ = dueTime;
            _ = period;
            RecordingTimer timer = new(_throwOnDispose.Dequeue());
            Timers.Add(timer);
            return timer;
        }
    }

    private sealed class RecordingTimer(bool throwOnDispose) : ITimer {
        public int DisposeCount { get; private set; }

        public bool Change(TimeSpan dueTime, TimeSpan period) => true;

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
