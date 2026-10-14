using System.Collections.Concurrent;
using System.Reflection;

using Hexalith.FrontComposer.Shell.Services.Announcements;

using Microsoft.Extensions.Time.Testing;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Services.Announcements;

public sealed class SurfaceAnnouncementCoordinatorTests
{
    [Theory]
    [InlineData("scope:0:page:2", "failure:page")]
    [InlineData("blocked:attempt", "blocked")]
    public void FreshOverlayRetainsUnderlyingFailureAndBlockedPrecedence(string terminalGroup, string identity)
    {
        FakeTimeProvider time = new();
        using SurfaceAnnouncementCoordinator sut = new(time);
        const string surface = "projection:one";
        sut.Announce(surface, terminalGroup, identity, "Terminal barrier.", terminal: true);
        sut.Announce(surface, "fresh-row:A", "message", "Updated row: row.", immediate: true);
        List<string> messages = [];
        using var subscription = sut.Subscribe(surface, messages.Add);
        sut.Announce(surface, "limit:later", "cap", "Later cap.", immediate: true);
        sut.Announce(surface, "stale:later", "stale", "Later stale.");
        sut.Announce(surface, "slow-query:old", "slow", "Old slow.", immediate: true);
        if (identity == "failure:page")
            sut.Announce(surface, "page:1", "ready", "Old page ready.");
        time.Advance(TimeSpan.FromMilliseconds(250));
        messages.ShouldBeEmpty();
        sut.Current(surface).ShouldBe("Updated row: row.");
        sut.Cancel(surface, "fresh-row:A");
        sut.Current(surface).ShouldBe("Terminal barrier.");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void InvalidatedOldCapIsNeverRestoredThroughFreshPreviousOrFallback(bool olderFirst)
    {
        using SurfaceAnnouncementCoordinator sut = new(new FakeTimeProvider());
        const string surface = "projection:one";
        sut.Announce(surface, "limit:old", "old-cap", "Old cap.", immediate: true);
        sut.Announce(surface, "fresh-row:A", "a", "Updated row: A.", immediate: true);
        sut.Announce(surface, "limit:new", "new-cap", "New cap.", immediate: true);
        sut.Current(surface).ShouldBe("New cap.");
        sut.Announce(surface, "fresh-row:B", "b", "Updated row: B.", immediate: true);
        sut.Cancel(surface, "limit:new");
        if (olderFirst)
        {
            sut.Cancel(surface, "fresh-row:A");
            sut.Current(surface).ShouldBe("Updated row: B.");
            sut.Cancel(surface, "fresh-row:B");
        }
        else
        {
            sut.Cancel(surface, "fresh-row:B");
            sut.Current(surface).ShouldBe("Updated row: A.");
            sut.Cancel(surface, "fresh-row:A");
        }
        sut.Current(surface).ShouldBeEmpty();
    }

    [Fact]
    public void FreshOverlayKeepsARepeatedDeliveredUnderlyingIdentitySilent()
    {
        FakeTimeProvider time = new();
        using SurfaceAnnouncementCoordinator sut = new(time);
        const string surface = "projection:one";
        sut.Announce(surface, "load:page", "loading", "Loading.");
        time.Advance(TimeSpan.FromMilliseconds(250));
        sut.Announce(surface, "fresh-row:A", "a", "Updated row: A.", immediate: true);
        List<string> delivered = [];
        using IDisposable subscription = sut.Subscribe(surface, delivered.Add);
        sut.Announce(surface, "load:page", "loading", "Loading.");
        time.Advance(TimeSpan.FromMilliseconds(250));
        delivered.ShouldBeEmpty();
        sut.Current(surface).ShouldBe("Updated row: A.");
    }

    [Fact]
    public void FreshWithdrawalAfterItsUnderlyingOwnerIsCancelledRestoresOnlyWhatCancelWould()
    {
        using SurfaceAnnouncementCoordinator sut = new(new FakeTimeProvider());
        const string surface = "projection:one";
        sut.Announce(surface, "load:first", "data:first", "Loaded first page.", terminal: true);
        sut.Announce(surface, "load:second", "data:second", "Loaded second page.", terminal: true);
        sut.Announce(surface, "fresh-row:A", "a", "Updated row: A.", immediate: true);
        sut.Cancel(surface, "load:second");
        sut.Cancel(surface, "fresh-row:A");
        sut.Current(surface).ShouldBeEmpty();
    }

    /// <summary>Cancellation under a fresh overlay preserves ordinary delivery of a new result-limit outcome.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FreshOverlayAfterCancellingUnderlyingLoadRetainsOrdinaryCapDelivery(bool freshOverlay)
    {
        using SurfaceAnnouncementCoordinator sut = new(new FakeTimeProvider());
        const string surface = "projection:one";
        sut.Announce(surface, "load:first", "data:first", "Loaded first page.", terminal: true);
        sut.Announce(surface, "load:second", "data:second", "Loaded second page.", terminal: true);
        if (freshOverlay)
        {
            sut.Announce(surface, "fresh-row:A", "a", "Updated row: A.", immediate: true);
        }
        sut.Cancel(surface, "load:second");
        List<string> delivered = [];
        using IDisposable subscription = sut.Subscribe(surface, delivered.Add);

        sut.Announce(surface, "limit:new", "cap:new", "New cap.", immediate: true);

        sut.Current(surface).ShouldBe("New cap.");
        delivered.ShouldBe(["New cap."]);
        if (freshOverlay)
        {
            sut.Cancel(surface, "fresh-row:A");
            sut.Current(surface).ShouldBe("New cap.");
            delivered.ShouldBe(["New cap."]);
        }
    }

    [Fact]
    public void OldestFirstFreshWithdrawalRestoresBlockedOwnerAndKeepsLaterCapSilent()
    {
        using SurfaceAnnouncementCoordinator sut = new(new FakeTimeProvider());
        const string surface = "projection:one";
        List<(string Message, bool Silent)> delivered = [];
        using IDisposable subscription = sut.SubscribeWithReplay(surface, (message, revision) =>
            delivered.Add((message, sut.IsSilentRevision(surface, revision))));
        sut.Announce(surface, "blocked:attempt", "blocked", "Second attempt did not run.", terminal: true);
        sut.Announce(surface, "fresh-row:A", "a", "Updated row: A.", immediate: true);
        sut.Announce(surface, "fresh-row:B", "b", "Updated row: B.", immediate: true);

        sut.Cancel(surface, "fresh-row:A");
        sut.Current(surface).ShouldBe("Updated row: B.");
        sut.Cancel(surface, "fresh-row:B");
        sut.Current(surface).ShouldBe("Second attempt did not run.");
        sut.IsCurrent(surface, "blocked:attempt", "blocked").ShouldBeTrue();
        delivered[^1].ShouldBe(("Second attempt did not run.", true));
        int before = delivered.Count;
        sut.Announce(surface, "limit:later", "cap", "Later cap.", immediate: true);
        sut.Current(surface).ShouldBe("Second attempt did not run.");
        delivered.Count.ShouldBe(before);
    }

    [Fact]
    public void MiddleThenNewestFreshWithdrawalRestoresRemainingOverlayBeforeUnderlyingLoad()
    {
        using SurfaceAnnouncementCoordinator sut = new(new FakeTimeProvider());
        const string surface = "projection:one";
        List<(string Message, bool Silent)> delivered = [];
        using IDisposable subscription = sut.SubscribeWithReplay(surface, (message, revision) =>
            delivered.Add((message, sut.IsSilentRevision(surface, revision))));
        sut.Announce(surface, "load:page", "data", "Loaded page.", terminal: true);
        sut.Announce(surface, "fresh-row:A", "a", "Updated row: A.", immediate: true);
        sut.Announce(surface, "fresh-row:B", "b", "Updated row: B.", immediate: true);
        sut.Announce(surface, "fresh-row:C", "c", "Updated row: C.", immediate: true);

        sut.Cancel(surface, "fresh-row:B");
        sut.Current(surface).ShouldBe("Updated row: C.");
        sut.Cancel(surface, "fresh-row:C");
        sut.Current(surface).ShouldBe("Updated row: A.");
        sut.IsCurrent(surface, "fresh-row:A", "a").ShouldBeTrue();
        delivered[^1].ShouldBe(("Updated row: A.", true));
    }

    [Fact]
    public void StackedFreshOverlaysKeepTheirCancelledUnderlyingOwnerWithoutRestoringOlderLoad()
    {
        using SurfaceAnnouncementCoordinator sut = new(new FakeTimeProvider());
        const string surface = "projection:one";
        List<(string Message, bool Silent)> delivered = [];
        using IDisposable subscription = sut.SubscribeWithReplay(surface, (message, revision) =>
            delivered.Add((message, sut.IsSilentRevision(surface, revision))));
        sut.Announce(surface, "load:first", "data:first", "Loaded first page.", terminal: true);
        sut.Announce(surface, "load:second", "data:second", "Loaded second page.", terminal: true);
        sut.Announce(surface, "fresh-row:A", "a", "Updated row: A.", immediate: true);
        sut.Cancel(surface, "load:second");
        sut.Announce(surface, "fresh-row:B", "b", "Updated row: B.", immediate: true);

        sut.Cancel(surface, "fresh-row:A");
        sut.Current(surface).ShouldBe("Updated row: B.");
        sut.Cancel(surface, "fresh-row:B");
        sut.Current(surface).ShouldBeEmpty();
        delivered[^1].ShouldBe((string.Empty, true));
    }

    [Fact]
    public void FreshWithdrawalFallbackDeliversCapFirstObservedBehindCancelledFailure()
    {
        FakeTimeProvider time = new();
        using SurfaceAnnouncementCoordinator sut = new(time);
        const string surface = "projection:one";
        List<(string Message, bool Silent)> delivered = [];
        using IDisposable subscription = sut.SubscribeWithReplay(surface, (message, revision) =>
            delivered.Add((message, sut.IsSilentRevision(surface, revision))));
        sut.Announce(surface, "scope:0:page:1", "failure:page", "Page failed.", terminal: true);
        sut.Announce(surface, "limit:cap", "cap", "Results limited.", immediate: true);
        sut.Current(surface).ShouldBe("Page failed.");
        sut.Announce(surface, "fresh-row:A", "a", "Updated row: A.", immediate: true);
        sut.Cancel(surface, "scope:0:page:1");

        sut.Cancel(surface, "fresh-row:A");
        sut.Current(surface).ShouldBe("Results limited.");
        sut.IsCurrent(surface, "limit:cap", "cap").ShouldBeTrue();
        delivered[^1].ShouldBe(("Results limited.", false));
        delivered.Where(static update => !update.Silent && update.Message.Length > 0)
            .Select(static update => update.Message).ShouldBe(["Page failed.", "Updated row: A.", "Results limited."]);
        time.Advance(TimeSpan.FromMilliseconds(250));
        sut.Announce(surface, "limit:cap", "cap", "Results limited.", immediate: true);
        delivered.Count(static update => !update.Silent && update.Message == "Results limited.").ShouldBe(1);
        sut.Cancel(surface, "limit:cap");
        sut.Current(surface).ShouldBeEmpty();
        delivered[^1].ShouldBe((string.Empty, false));
    }

    [Fact]
    public void FreshWithdrawalFallbackCompletesPendingStaleIdentityAndCancelsItsOriginalTimer()
    {
        FakeTimeProvider time = new();
        using SurfaceAnnouncementCoordinator sut = new(time);
        const string surface = "projection:one";
        List<(string Message, bool Silent)> delivered = [];
        using IDisposable subscription = sut.SubscribeWithReplay(surface, (message, revision) =>
            delivered.Add((message, sut.IsSilentRevision(surface, revision))));
        sut.Announce(surface, "load:page", "data", "Loaded page.", terminal: true);
        sut.Announce(surface, "fresh-row:A", "a", "Updated row: A.", immediate: true);
        sut.Announce(surface, "stale:pending", "pending-stale", "Data may be out of date.");
        sut.Cancel(surface, "load:page");

        sut.Cancel(surface, "fresh-row:A");
        sut.Current(surface).ShouldBe("Data may be out of date.");
        sut.IsCurrent(surface, "stale:pending", "stale").ShouldBeTrue();
        delivered[^1].ShouldBe(("Data may be out of date.", false));
        int before = delivered.Count;
        time.Advance(TimeSpan.FromMilliseconds(250));
        delivered.Count.ShouldBe(before);
        sut.IsCurrent(surface, "stale:pending", "stale").ShouldBeTrue();
        sut.Announce(surface, "stale:pending", "stale", "Data may be out of date.");
        time.Advance(TimeSpan.FromMilliseconds(250));
        delivered.Count.ShouldBe(before);
    }

    [Fact]
    public void ClearPreservesInFlightFreshWithdrawalSilence()
    {
        using SurfaceAnnouncementCoordinator sut = new(new FakeTimeProvider());
        const string surface = "projection:one";
        sut.Announce(surface, "limit:prior", "cap", "Prior cap.", immediate: true);
        sut.Announce(surface, "fresh-row:A", "a", "Updated row: A.", immediate: true);
        using IDisposable clearOnRestore = sut.Subscribe(surface, message =>
        {
            if (message == "Prior cap.") sut.Clear(surface);
        });
        List<(string Message, bool Silent)> delivered = [];
        using IDisposable observer = sut.SubscribeWithReplay(surface, (message, revision) =>
        {
            if (message == "Prior cap.") delivered.Add((message, sut.IsSilentRevision(surface, revision)));
        });

        sut.Cancel(surface, "fresh-row:A");

        delivered.ShouldBe([("Prior cap.", true)]);
        sut.Current(surface).ShouldBeEmpty();
    }

    [Fact]
    public void FreshOverlayAllowsDeliveredLoadingToReturnAfterDifferentPendingUnderlyingIdentity()
    {
        FakeTimeProvider time = new();
        using SurfaceAnnouncementCoordinator sut = new(time);
        const string surface = "projection:one";
        List<string> delivered = [];
        using IDisposable subscription = sut.Subscribe(surface, delivered.Add);
        sut.Announce(surface, "load:page", "loading", "Loading.");
        time.Advance(TimeSpan.FromMilliseconds(250));
        sut.Announce(surface, "fresh-row:A", "a", "Updated row: A.", immediate: true);
        sut.Announce(surface, "load:page", "refreshing", "Refreshing.");
        sut.Announce(surface, "load:page", "loading", "Loading.");

        time.Advance(TimeSpan.FromMilliseconds(250));
        delivered.ShouldBe(["Loading.", "Updated row: A.", "Loading."]);
        sut.Current(surface).ShouldBe("Loading.");
        sut.IsCurrent(surface, "load:page", "loading").ShouldBeTrue();
    }

    [Fact]
    public void FreshOverlayAllowsDeliveredLoadingToReturnAfterDifferentDeliveredUnderlyingIdentity()
    {
        FakeTimeProvider time = new();
        using SurfaceAnnouncementCoordinator sut = new(time);
        const string surface = "projection:one";
        List<string> delivered = [];
        using IDisposable subscription = sut.Subscribe(surface, delivered.Add);
        sut.Announce(surface, "load:page", "loading", "Loading.");
        time.Advance(TimeSpan.FromMilliseconds(250));
        sut.Announce(surface, "load:page", "refreshing", "Refreshing.");
        time.Advance(TimeSpan.FromMilliseconds(250));
        sut.Announce(surface, "fresh-row:A", "a", "Updated row: A.", immediate: true);
        sut.Announce(surface, "load:page", "loading", "Loading.");

        time.Advance(TimeSpan.FromMilliseconds(250));
        delivered.ShouldBe(["Loading.", "Refreshing.", "Updated row: A.", "Loading."]);
        sut.Current(surface).ShouldBe("Loading.");
        sut.IsCurrent(surface, "load:page", "loading").ShouldBeTrue();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task InFlightWithdrawalRevisionsRemainSilentAcrossConcurrentAndReentrantDelivery(bool concurrent)
    {
        using SurfaceAnnouncementCoordinator sut = new(new FakeTimeProvider());
        const string surface = "projection:one";
        sut.Announce(surface, "limit:prior", "cap", "Prior cap.", immediate: true);
        sut.Announce(surface, "fresh-row:A", "a", "Updated row: A.", immediate: true);
        sut.Announce(surface, "fresh-row:B", "b", "Updated row: B.", immediate: true);
        using ManualResetEventSlim entered = new();
        using ManualResetEventSlim resume = new();
        using var blocker = sut.Subscribe(surface, message => {
            if (message != "Updated row: A.") return;
            if (concurrent)
            {
                entered.Set();
                resume.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken).ShouldBeTrue();
            }
            else sut.Cancel(surface, "fresh-row:A");
        });
        ConcurrentBag<(string Message, bool Silent, bool ForwardedSilent)> delivered = [];
        using var subscription = sut.SubscribeWithReplay(surface, (message, revision) => {
            if (message is "Updated row: A." or "Prior cap.")
                delivered.Add((message, sut.IsSilentRevision(surface, revision), SurfaceAnnouncementDelivery.IsSilent(surface, revision)));
        });
        if (concurrent)
        {
            Task first = Task.Run(() => sut.Cancel(surface, "fresh-row:B"), TestContext.Current.CancellationToken);
            try
            {
                entered.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken).ShouldBeTrue();
                sut.Cancel(surface, "fresh-row:A");
            }
            finally { resume.Set(); }
            await first.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken).ConfigureAwait(true);
        }
        else sut.Cancel(surface, "fresh-row:B");
        delivered.Count.ShouldBe(2);
        delivered.All(static delivery => delivery.Silent && delivery.ForwardedSilent).ShouldBeTrue();
        var surfaces = (System.Collections.IDictionary)typeof(SurfaceAnnouncementCoordinator)
            .GetField("_surfaces", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(sut)!;
        object owner = surfaces[surface]!;
        ((HashSet<long>)owner.GetType().GetProperty("PendingFreshSilentRevisions")!.GetValue(owner)!).ShouldBeEmpty();
    }

    [Fact]
    public void LongPendingRequestRetainsItsOriginalOrderBeyondSettledObservationLimit()
    {
        using SurfaceAnnouncementCoordinator sut = new(new FakeTimeProvider());
        SurfaceAnnouncementGroupOwnership ownership = SurfaceAnnouncementGroupOwnership.For(sut);
        TaskCompletionSource<object> oldPending = new();
        ownership.ObserveRequest("projection:orders", oldPending).ShouldBe(1);
        for (int index = 0; index < 600; index++)
        {
            TaskCompletionSource<object> settled = new();
            settled.SetResult(new object());
            _ = ownership.ObserveRequest("projection:orders", settled);
        }
        ownership.ObserveRequest("projection:orders", oldPending).ShouldBe(1);
        System.Collections.IDictionary entries = (System.Collections.IDictionary)typeof(SurfaceAnnouncementGroupOwnership)
            .GetField("_requestOrders", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(ownership)!;
        entries.Count.ShouldBeLessThanOrEqualTo(512);

        using IDisposable cap = ownership.ClaimTransitionEpisode("projection:orders", "limit:orders", "cap", out string capGroup);
        sut.Announce("projection:orders", capGroup, "cap", "Results limited", terminal: true);
        sut.Announce("projection:orders", "slow-query:orders:scope:0:started:1:old:observed:1", "old", "Still loading", immediate: true);
        sut.Current("projection:orders").ShouldBe("Results limited");
    }
    [Fact]
    public void CapFirstObservedBehindFailureSpeaksOnceAfterWithdrawal()
    {
        using SurfaceAnnouncementCoordinator sut = new(new FakeTimeProvider());
        List<string> spoken = [];
        using IDisposable subscription = sut.Subscribe("projection", spoken.Add);
        sut.Announce("projection", "page:1", "failure", "Page failed", terminal: true);
        sut.Announce("projection", "limit:result-a", "cap-a", "First limit", terminal: true);
        sut.Announce("projection", "limit:result-b", "cap-b", "Newest limit", terminal: true);
        sut.Current("projection").ShouldBe("Page failed");

        sut.Cancel("projection", "page:1");
        sut.Current("projection").ShouldBe("Newest limit");
        spoken.ShouldBe(["Page failed", "Newest limit"]);
        sut.Cancel("projection", "limit:result-b");
        sut.Current("projection").ShouldBeEmpty();
    }

    [Fact]
    public void DeliveredCapRestoresSilentlyAfterLaterSlowNoticeSettles()
    {
        using SurfaceAnnouncementCoordinator sut = new(new FakeTimeProvider());
        List<(string Message, long Revision)> updates = [];
        using IDisposable subscription = sut.SubscribeWithReplay("projection", (message, revision) => updates.Add((message, revision)));
        sut.Announce("projection", "limit:orders:scope:1:1:episode:1:observed:1", "cap-a", "Results limited", terminal: true);
        sut.Announce("projection", "slow-query:orders:scope:1:started:123:1:request:observed:2", "slow", "Still loading", immediate: true);
        sut.Cancel("projection", "slow-query:orders:scope:1:started:123:1:request:observed:2");

        sut.Current("projection").ShouldBe("Results limited");
        updates.Select(static update => update.Message).ShouldBe([string.Empty, "Results limited", "Still loading", "Results limited"]);
        sut.IsSilentRevision("projection", updates[^1].Revision).ShouldBeTrue();
        sut.Cancel("projection", "limit:orders:scope:1:1:episode:1:observed:1");
        sut.Current("projection").ShouldBeEmpty();
    }

    [Fact]
    public void StillMountedFailureRegainsPriorityAfterLaterSlowNoticeSettles()
    {
        using SurfaceAnnouncementCoordinator sut = new(new FakeTimeProvider());
        List<(string Message, long Revision)> updates = [];
        using IDisposable subscription = sut.SubscribeWithReplay("projection", (message, revision) => updates.Add((message, revision)));
        sut.Announce("projection", "limit:orders:scope:1:1:episode:1:observed:1", "cap", "Results limited", terminal: true);
        sut.Announce("projection", "scope:1:page:2", "failure", "Page failed", terminal: true);
        sut.Announce("projection", "slow-query:orders:scope:1:started:123:2:request:observed:3", "slow", "Still loading", immediate: true);
        sut.Current("projection").ShouldBe("Still loading");

        sut.Cancel("projection", "slow-query:orders:scope:1:started:123:2:request:observed:3");
        sut.Current("projection").ShouldBe("Page failed");
        sut.IsSilentRevision("projection", updates[^1].Revision).ShouldBeTrue();
        sut.Cancel("projection", "scope:1:page:2");
        sut.Current("projection").ShouldBe("Results limited");
        sut.IsSilentRevision("projection", updates[^1].Revision).ShouldBeTrue();
    }

    [Fact]
    public void WithdrawnPageFailureRestoresStillVisibleCapSilently()
    {
        FakeTimeProvider time = new();
        using SurfaceAnnouncementCoordinator sut = new(time);
        List<(string Message, long Revision)> updates = [];
        using IDisposable subscription = sut.SubscribeWithReplay("projection:orders", (message, revision) => updates.Add((message, revision)));

        sut.Announce("projection:orders", "limit:orders:scope:1:result-a:episode:1", "cap-a", "Results limited", terminal: true);
        sut.Announce("projection:orders", "scope:1:page:2", "failure", "Page failed", terminal: true);
        sut.Cancel("projection:orders", "scope:1:page:2");

        sut.Current("projection:orders").ShouldBe("Results limited");
        sut.IsCurrent("projection:orders", "limit:orders:scope:1:result-a:episode:1", "cap-a").ShouldBeTrue();
        sut.IsSilentRevision("projection:orders", updates[^1].Revision).ShouldBeTrue();
        time.Advance(TimeSpan.FromMilliseconds(250));
        updates.Select(static update => update.Message).ShouldBe([string.Empty, "Results limited", "Page failed", "Results limited"]);

        sut.Cancel("projection:orders", "limit:orders:scope:1:result-a:episode:1");
        sut.Current("projection:orders").ShouldBeEmpty();
        sut.Announce("projection:orders", "limit:orders:scope:1:result-b:episode:2", "cap-b", "New limit", terminal: true);
        sut.Current("projection:orders").ShouldBe("New limit");
        sut.IsSilentRevision("projection:orders", updates[^1].Revision).ShouldBeFalse();
    }

    [Fact]
    public void SupersededCapCannotReturnWhenLaterFailureWithdraws()
    {
        using SurfaceAnnouncementCoordinator sut = new(new FakeTimeProvider());
        sut.Announce("projection:orders", "limit:orders:scope:1:result-a", "cap-a", "Old limit", terminal: true);
        sut.Cancel("projection:orders", "limit:orders:scope:1:result-a");
        sut.Announce("projection:orders", "limit:orders:scope:1:result-b", "cap-b", "Current limit", terminal: true);
        sut.Announce("projection:orders", "scope:1:page:3", "failure", "Page failed", terminal: true);
        sut.Cancel("projection:orders", "limit:orders:scope:1:result-b");
        sut.Cancel("projection:orders", "scope:1:page:3");
        sut.Current("projection:orders").ShouldBeEmpty();
    }

    [Fact]
    public void WithdrawnBlockedAttemptRestoresCurrentLifecycleWithoutRepeatingSpeech()
    {
        FakeTimeProvider time = new();
        using SurfaceAnnouncementCoordinator sut = new(time);
        List<(string Message, long Revision)> updates = [];
        using IDisposable subscription = sut.SubscribeWithReplay("command", (message, revision) => updates.Add((message, revision)));

        sut.Announce("command", "lifecycle:original", "Acknowledged", "Command accepted");
        time.Advance(TimeSpan.FromMilliseconds(250));
        sut.Announce("command", "blocked:attempt:1", "blocked", "Second attempt did not run", terminal: true);
        sut.Announce("command", "lifecycle:original", "Acknowledged", "Command accepted");
        sut.Current("command").ShouldBe("Second attempt did not run");

        sut.Cancel("command", "blocked:attempt:1");
        sut.Current("command").ShouldBe("Command accepted");
        sut.IsCurrent("command", "lifecycle:original", "Acknowledged").ShouldBeTrue();
        sut.IsSilentRevision("command", updates[^1].Revision).ShouldBeTrue();
        time.Advance(TimeSpan.FromMilliseconds(250));
        updates.Select(static update => update.Message).ShouldBe([string.Empty, "Command accepted", "Second attempt did not run", "Command accepted"]);

        sut.Announce("command", "lifecycle:original", "Confirmed", "Command confirmed", terminal: true);
        sut.Current("command").ShouldBe("Command confirmed");
        sut.IsSilentRevision("command", updates[^1].Revision).ShouldBeFalse();
        sut.Cancel("command", "blocked:attempt:1");
        sut.Current("command").ShouldBe("Command confirmed");
    }

    [Fact]
    public void WithdrawnBlockedAttemptDoesNotRestoreSupersededLifecycleWork()
    {
        FakeTimeProvider time = new();
        using SurfaceAnnouncementCoordinator sut = new(time);
        sut.Announce("command", "lifecycle:original", "Acknowledged", "Command accepted");
        time.Advance(TimeSpan.FromMilliseconds(250));
        sut.Announce("command", "lifecycle:original", "Syncing", "Command syncing");
        sut.Announce("command", "blocked:attempt:1", "blocked", "Second attempt did not run", terminal: true);
        sut.Cancel("command", "blocked:attempt:1");
        sut.Current("command").ShouldBeEmpty();
        time.Advance(TimeSpan.FromMilliseconds(250));
        sut.Current("command").ShouldBeEmpty();
    }

    [Fact]
    public void NewerStaleCandidateKeepsItsExistingRestorationPrecedence()
    {
        FakeTimeProvider time = new();
        using SurfaceAnnouncementCoordinator sut = new(time);
        sut.Announce("shared", "lifecycle:original", "Acknowledged", "Command accepted");
        time.Advance(TimeSpan.FromMilliseconds(250));
        sut.Announce("shared", "blocked:attempt:1", "blocked", "Second attempt did not run", terminal: true);
        sut.Announce("shared", "stale:episode:1", "stale", "Cached data may be old");

        sut.Cancel("shared", "blocked:attempt:1");
        sut.Current("shared").ShouldBe("Cached data may be old");
        sut.IsCurrent("shared", "stale:episode:1", "stale").ShouldBeTrue();
        time.Advance(TimeSpan.FromMilliseconds(250));
        sut.Current("shared").ShouldBe("Cached data may be old");
    }

    [Fact]
    public void NonnumericCapUsesObservedOrderForSameTickSlowRequests()
    {
        FakeTimeProvider time = new();
        using SurfaceAnnouncementCoordinator sut = new(time);
        long tick = time.GetUtcNow().UtcTicks;
        sut.Announce("projection:orders", "limit:orders:scope:1:result-x:10000:episode:1:observed:2",
            "result-x", "Results limited", terminal: true);

        sut.Announce("projection:orders", $"slow-query:orders:scope:1:started:{tick}:0:old:observed:1",
            "old", "Old slow", immediate: true);
        sut.Current("projection:orders").ShouldBe("Results limited");
        sut.Announce("projection:orders", $"slow-query:orders:scope:0:started:{tick}:0:stale-scope:observed:99",
            "stale-scope", "Wrong scope", immediate: true);
        sut.Current("projection:orders").ShouldBe("Results limited");
        sut.Announce("projection:orders", $"slow-query:orders:scope:1:started:{tick}:0:new:observed:3",
            "new", "New slow", immediate: true);
        sut.Current("projection:orders").ShouldBe("New slow");
    }

    [Fact]
    public void OlderImmediateSlowRequestCannotReplaceClosedPageButNewRequestCan()
    {
        FakeTimeProvider time = new();
        using SurfaceAnnouncementCoordinator sut = new(time);
        List<string> spoken = [];
        using IDisposable subscription = sut.Subscribe("projection:orders", spoken.Add);
        long oldStart = time.GetUtcNow().UtcTicks;
        time.Advance(TimeSpan.FromSeconds(1));
        sut.Announce("projection:orders", "scope:1:page:1", "failure", "Page failed", terminal: true);

        sut.Announce("projection:orders", $"slow-query:orders:started:{oldStart}:pending-b", "pending-b", "Still loading", immediate: true);
        sut.Current("projection:orders").ShouldBe("Page failed");
        spoken.ShouldBe(["Page failed"]);

        time.Advance(TimeSpan.FromTicks(1));
        long newStart = time.GetUtcNow().UtcTicks;
        sut.Announce("projection:orders", $"slow-query:orders:started:{newStart}:request-c", "request-c", "Still loading", immediate: true);
        sut.Current("projection:orders").ShouldBe("Still loading");
        spoken.ShouldBe(["Page failed", "Still loading"]);
    }

    [Fact]
    public void ResultSequenceDistinguishesNewRequestAtTheSameClockTick()
    {
        FakeTimeProvider time = new();
        using SurfaceAnnouncementCoordinator sut = new(time);
        long tick = time.GetUtcNow().UtcTicks;
        sut.Announce("projection:orders", "scope:0:page:1", "failure", "Page failed", terminal: true);
        sut.Announce("projection:orders", $"slow-query:orders:scope:0:started:{tick}:0:old", "0:old", "Old slow", immediate: true);
        sut.Current("projection:orders").ShouldBe("Page failed");
        sut.Announce("projection:orders", $"slow-query:orders:scope:0:started:{tick}:1:new", "1:new", "New slow", immediate: true);
        sut.Current("projection:orders").ShouldBe("New slow");
    }

    [Fact]
    public void CapResultAllowsOnlyANewSlowRequestAtTheSameClockTick()
    {
        FakeTimeProvider time = new();
        using SurfaceAnnouncementCoordinator sut = new(time);
        long tick = time.GetUtcNow().UtcTicks;
        sut.Announce("projection:orders", "limit:orders:scope:0:5:10000", "cap", "Results limited", terminal: true);

        sut.Announce("projection:orders", $"slow-query:orders:scope:0:started:{tick}:4:old", "4:old", "Old slow", immediate: true);
        sut.Current("projection:orders").ShouldBe("Results limited");
        sut.Announce("projection:orders", $"slow-query:orders:scope:0:started:{tick}:5:new", "5:new", "New slow", immediate: true);
        sut.Current("projection:orders").ShouldBe("New slow");
    }

    [Theory]
    [InlineData("blocked:attempt:1", "blocked")]
    [InlineData("load:request:1", "failure")]
    public void NewStaleCandidateBehindTerminalAppearsWhenTerminalWithdraws(string terminalGroup, string terminalIdentity)
    {
        FakeTimeProvider time = new();
        using SurfaceAnnouncementCoordinator sut = new(time);
        List<string> spoken = [];
        using IDisposable subscription = sut.Subscribe("shared", spoken.Add);
        sut.Announce("shared", terminalGroup, terminalIdentity, "Terminal", terminal: true);
        sut.Announce("shared", "stale:episode:2", "stale", "Cached data may be old");
        time.Advance(TimeSpan.FromMilliseconds(250));
        sut.Current("shared").ShouldBe("Terminal");
        spoken.ShouldBe(["Terminal"]);

        sut.Cancel("shared", terminalGroup);
        sut.Current("shared").ShouldBe("Cached data may be old");
        spoken.ShouldBe(["Terminal", "Cached data may be old"]);
        sut.Announce("shared", "stale:episode:2", "stale", "Cached data may be old");
        time.Advance(TimeSpan.FromMilliseconds(250));
        spoken.ShouldBe(["Terminal", "Cached data may be old"]);
    }

    [Fact]
    public void RestoredPendingStaleCandidateSpeaksOnceAcrossItsOriginalDeadline()
    {
        FakeTimeProvider time = new();
        using SurfaceAnnouncementCoordinator sut = new(time);
        List<string> spoken = [];
        using IDisposable subscription = sut.Subscribe("shared", spoken.Add);
        sut.Announce("shared", "limit:result:1", "cap", "Results limited", terminal: true);
        sut.Announce("shared", "stale:episode:1", "stale", "Cached data may be old");
        time.Advance(TimeSpan.FromMilliseconds(249));
        spoken.ShouldBe(["Results limited"]);

        sut.Cancel("shared", "limit:result:1");
        sut.Current("shared").ShouldBe("Cached data may be old");
        spoken.ShouldBe(["Results limited", "Cached data may be old"]);
        time.Advance(TimeSpan.FromMilliseconds(1));
        time.Advance(TimeSpan.FromMilliseconds(250));
        sut.Current("shared").ShouldBe("Cached data may be old");
        spoken.ShouldBe(["Results limited", "Cached data may be old"]);

        sut.Cancel("shared", "stale:episode:1");
        sut.Announce("shared", "stale:episode:2", "stale", "Cached data may be old");
        time.Advance(TimeSpan.FromMilliseconds(250));
        spoken.ShouldBe(["Results limited", "Cached data may be old", "", "Cached data may be old"]);
    }

    [Fact]
    public void QueuedStaleTimerCannotReplaceMountedLimitOrLaterTerminal()
    {
        var time = new Hexalith.FrontComposer.Shell.Tests.Infrastructure.QueuedTimerTimeProvider();
        using SurfaceAnnouncementCoordinator sut = new(time);
        List<string> spoken = [];
        using IDisposable subscription = sut.Subscribe("shared", spoken.Add);
        sut.Announce("shared", "limit:result:1", "cap", "Results limited", terminal: true);
        sut.Announce("shared", "stale:episode:1", "stale", "Cached data may be old");
        time.Callbacks.Count.ShouldBe(1);
        time.Callbacks[0]();
        sut.Current("shared").ShouldBe("Results limited");
        spoken.ShouldBe(["Results limited"]);

        sut.Cancel("shared", "limit:result:1");
        spoken.ShouldBe(["Results limited", "Cached data may be old"]);
        sut.Announce("shared", "limit:result:2", "cap", "New limit", terminal: true);
        time.Callbacks[0]();
        sut.Current("shared").ShouldBe("New limit");
        spoken.ShouldBe(["Results limited", "Cached data may be old", "New limit"]);
    }

    [Fact]
    public void DeliveredStaleEpisodeCannotReclaimSharedBlockedTerminal()
    {
        FakeTimeProvider time = new();
        using SurfaceAnnouncementCoordinator sut = new(time);
        List<string> spoken = [];
        using IDisposable subscription = sut.Subscribe("shared", spoken.Add);
        sut.Announce("shared", "stale:episode:1", "stale", "Cached data may be old");
        time.Advance(TimeSpan.FromMilliseconds(250));
        sut.Announce("shared", "blocked:attempt:1", "blocked", "Another command is active", terminal: true);

        sut.Announce("shared", "stale:episode:1", "stale", "Cached data may be old");
        time.Advance(TimeSpan.FromMilliseconds(250));
        spoken.ShouldBe(["Cached data may be old", "Another command is active"]);
        sut.Current("shared").ShouldBe("Another command is active");

        sut.Cancel("shared", "blocked:attempt:1");
        sut.Current("shared").ShouldBe("Cached data may be old");
        sut.IsCurrent("shared", "stale:episode:1", "stale").ShouldBeTrue();
        sut.Announce("shared", "stale:episode:2", "stale", "Cached data may be old");
        time.Advance(TimeSpan.FromMilliseconds(250));
        spoken.ShouldBe(["Cached data may be old", "Another command is active", "Cached data may be old", "Cached data may be old"]);
    }

    [Fact]
    public void ScopedPageResultAndOriginalCommandProgressCanReplaceOlderTerminalSpeech()
    {
        FakeTimeProvider time = new();
        using SurfaceAnnouncementCoordinator sut = new(time);
        sut.Announce("grid", "scope:1:page:1", "empty", "No rows", terminal: true);
        sut.Announce("grid", "scope:1:page:2", "loaded", "Rows loaded");
        time.Advance(TimeSpan.FromMilliseconds(250));
        sut.Current("grid").ShouldBe("Rows loaded");
        sut.Announce("grid", "scope:2:page:1", "empty", "New scope empty", terminal: true);
        sut.Current("grid").ShouldBe("New scope empty");

        sut.Announce("command", "blocked:attempt:1", "blocked", "Another command is active", terminal: true);
        sut.Announce("command", "original-correlation", "syncing", "Original command syncing");
        time.Advance(TimeSpan.FromMilliseconds(250));
        sut.Current("command").ShouldBe("Original command syncing");
    }

    [Fact]
    public void CancellingOrdinaryAndVersionedObserversCannotSuppressHealthyListeners()
    {
        using SurfaceAnnouncementCoordinator sut = new(new FakeTimeProvider());
        List<string> ordinary = [];
        List<string> versioned = [];
        using IDisposable first = sut.Subscribe("surface", _ => throw new OperationCanceledException());
        using IDisposable second = sut.Subscribe("surface", ordinary.Add);
        using IDisposable third = sut.SubscribeWithReplay("surface", (message, _) =>
        {
            if (message.Length > 0) { throw new TaskCanceledException(); }
        });
        using IDisposable fourth = sut.SubscribeWithReplay("surface", (message, _) =>
        {
            if (message.Length > 0) { versioned.Add(message); }
        });
        sut.Announce("surface", "load", "loaded", "Data loaded.", immediate: true);
        ordinary.ShouldBe(["Data loaded."]);
        versioned.ShouldBe(["Data loaded."]);
    }

    [Fact]
    public void LifecycleBurstAnnouncesOnlyAcknowledgedThenImmediateTerminal()
    {
        FakeTimeProvider time = new();
        using SurfaceAnnouncementCoordinator sut = new(time);
        List<string> spoken = [];
        using IDisposable subscription = sut.Subscribe("command", spoken.Add);

        sut.Announce("command", "operation-1", "submitting", "Submitting command.");
        time.Advance(TimeSpan.FromMilliseconds(100));
        sut.Announce("command", "operation-1", "acknowledged", "Command accepted. Waiting for confirmation.");
        time.Advance(TimeSpan.FromMilliseconds(249));
        spoken.ShouldBeEmpty();
        time.Advance(TimeSpan.FromMilliseconds(1));
        spoken.ShouldBe(["Command accepted. Waiting for confirmation."]);

        sut.Announce("command", "operation-1", "syncing", "Command accepted. Updating the view.");
        time.Advance(TimeSpan.FromMilliseconds(100));
        sut.Announce("command", "operation-1", "confirmed", "Command confirmed.", terminal: true);
        spoken.ShouldBe(["Command accepted. Waiting for confirmation.", "Command confirmed."]);
        time.Advance(TimeSpan.FromSeconds(1));
        sut.Announce("command", "operation-1", "late", "Late result");
        spoken.ShouldBe(["Command accepted. Waiting for confirmation.", "Command confirmed."]);
    }

    [Theory]
    [InlineData("connection:7", "reconnecting", "fallback")]
    [InlineData("load:42", "loading", "loaded")]
    public void ConnectionAndLoadGroupsUseTrailing250Milliseconds(string group, string first, string last)
    {
        FakeTimeProvider time = new();
        using SurfaceAnnouncementCoordinator sut = new(time);
        List<string> spoken = [];
        using IDisposable subscription = sut.Subscribe("surface", spoken.Add);

        sut.Announce("surface", group, first, "first");
        time.Advance(TimeSpan.FromMilliseconds(75));
        sut.Announce("surface", group, last, "last");
        time.Advance(TimeSpan.FromMilliseconds(249));
        spoken.ShouldBeEmpty();
        time.Advance(TimeSpan.FromMilliseconds(1));
        spoken.ShouldBe(["last"]);
        sut.Announce("surface", group, last, "last");
        time.Advance(TimeSpan.FromSeconds(1));
        spoken.ShouldBe(["last"]);
    }

    [Fact]
    public void LastEligibleStateWinsWhenBurstReturnsToItsFirstIdentity()
    {
        FakeTimeProvider time = new();
        using SurfaceAnnouncementCoordinator sut = new(time);
        List<string> spoken = [];
        using IDisposable subscription = sut.Subscribe("projection:one", spoken.Add);

        sut.Announce("projection:one", "load:1", "A", "First A");
        time.Advance(TimeSpan.FromMilliseconds(50));
        sut.Announce("projection:one", "load:1", "B", "B");
        time.Advance(TimeSpan.FromMilliseconds(50));
        sut.Announce("projection:one", "load:1", "A", "Final A");
        time.Advance(TimeSpan.FromMilliseconds(249));
        spoken.ShouldBeEmpty();
        time.Advance(TimeSpan.FromMilliseconds(1));
        spoken.ShouldBe(["Final A"]);
    }

    [Fact]
    public void DeliveredIdentityCancelsAnotherPendingStateWithoutRepeatingSpeech()
    {
        FakeTimeProvider time = new();
        using SurfaceAnnouncementCoordinator sut = new(time);
        List<string> spoken = [];
        using IDisposable subscription = sut.Subscribe("surface", spoken.Add);
        sut.Announce("surface", "operation", "A", "Delivered A");
        time.Advance(TimeSpan.FromMilliseconds(250));
        sut.Announce("surface", "operation", "B", "Pending B");
        time.Advance(TimeSpan.FromMilliseconds(100));
        sut.Announce("surface", "operation", "A", "Delivered A");
        time.Advance(TimeSpan.FromMilliseconds(250));
        spoken.ShouldBe(["Delivered A"]);

        sut.Announce("surface", "operation", "B", "Pending B");
        sut.Announce("surface", "unrelated", "other", "Unrelated");
        sut.Announce("surface", "operation", "A", "Delivered A");
        time.Advance(TimeSpan.FromMilliseconds(250));
        spoken.ShouldBe(["Delivered A", "Unrelated"]);

        // B was cancelled before delivery and remains eligible if it returns later.
        sut.Announce("surface", "operation", "B", "Current B");
        time.Advance(TimeSpan.FromMilliseconds(250));
        spoken.ShouldBe(["Delivered A", "Unrelated", "Current B"]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DeliveredPageResultCannotReclaimNewerSharedSlowOrBlockedStatus(bool blocked)
    {
        FakeTimeProvider time = new();
        using SurfaceAnnouncementCoordinator sut = new(time);
        List<string> spoken = [];
        using IDisposable subscription = sut.Subscribe("projection:orders", spoken.Add);
        sut.Announce("projection:orders", "scope:1:page:1", "loaded", "Rows loaded");
        time.Advance(TimeSpan.FromMilliseconds(250));
        string group = blocked ? "blocked:attempt:2" : "slow-query:orders:scope:1:started:1:1:request-b";
        sut.Announce("projection:orders", group, "B", blocked ? "Command blocked" : "Still loading",
            terminal: blocked, immediate: !blocked);

        sut.Announce("projection:orders", "scope:1:page:1", "loaded", "Rows loaded");
        time.Advance(TimeSpan.FromMilliseconds(250));
        sut.Current("projection:orders").ShouldBe(blocked ? "Command blocked" : "Still loading");
        spoken.ShouldBe(blocked ? ["Rows loaded", "Command blocked"] : ["Rows loaded", "Still loading"]);

        sut.Announce("projection:orders", "scope:1:page:2", "loaded", "New rows loaded");
        time.Advance(TimeSpan.FromMilliseconds(250));
        sut.Current("projection:orders").ShouldBe("New rows loaded");
        spoken.Last().ShouldBe("New rows loaded");
    }

    [Fact]
    public void DeliveredConnectionStateCanReturnAfterAnotherStateWasDelivered()
    {
        FakeTimeProvider time = new();
        using SurfaceAnnouncementCoordinator sut = new(time);
        List<string> spoken = [];
        using IDisposable subscription = sut.Subscribe("connection", spoken.Add);

        sut.Announce("connection", "connection:7", "A", "Reconnecting");
        time.Advance(TimeSpan.FromMilliseconds(250));
        sut.Announce("connection", "connection:7", "B", "Disconnected");
        time.Advance(TimeSpan.FromMilliseconds(250));
        sut.Announce("connection", "connection:7", "A", "Reconnecting");
        time.Advance(TimeSpan.FromMilliseconds(250));
        sut.Announce("connection", "connection:7", "A", "Reconnecting");
        time.Advance(TimeSpan.FromMilliseconds(250));

        spoken.ShouldBe(["Reconnecting", "Disconnected", "Reconnecting"]);
        sut.Current("connection").ShouldBe("Reconnecting");
    }

    [Fact]
    public void TerminalResultBlocksRearmedStaleAndCapUntilWithdrawn()
    {
        FakeTimeProvider time = new();
        using SurfaceAnnouncementCoordinator sut = new(time);
        sut.Announce("projection", "stale:1", "stale", "Old data");
        sut.Announce("projection", "page:2", "failure", "Query failed", terminal: true);
        sut.Announce("projection", "stale:1", "stale", "Old data");
        sut.Announce("projection", "limit:1", "cap", "Limited data", terminal: true);
        time.Advance(TimeSpan.FromSeconds(1));
        sut.Current("projection").ShouldBe("Query failed");

        sut.Cancel("projection", "page:2");
        sut.Current("projection").ShouldBe("Old data");
        sut.Cancel("projection", "stale:1");
        sut.Current("projection").ShouldBe("Limited data");
    }

    [Fact]
    public void DistinctProjectionSurfacesDoNotCancelEachOther()
    {
        FakeTimeProvider time = new();
        using SurfaceAnnouncementCoordinator sut = new(time);
        sut.Announce("projection:First.Orders", "load:1", "loading", "First loading");
        sut.Announce("projection:Second.Orders", "load:1", "failed", "Second failed", terminal: true);
        time.Advance(TimeSpan.FromMilliseconds(250));

        sut.Current("projection:First.Orders").ShouldBe("First loading");
        sut.Current("projection:Second.Orders").ShouldBe("Second failed");
    }

    [Fact]
    public void ClearCancelsPendingSpeechAndPreviousScopeMessage()
    {
        FakeTimeProvider time = new();
        using SurfaceAnnouncementCoordinator sut = new(time);
        sut.Announce("projection", "load:old", "loading", "Loading old scope.");
        sut.Clear("projection");
        time.Advance(TimeSpan.FromMilliseconds(250));
        sut.Current("projection").ShouldBeEmpty();
        sut.Announce("projection", "load:new", "loaded", "New scope loaded.");
        time.Advance(TimeSpan.FromMilliseconds(250));
        sut.Current("projection").ShouldBe("New scope loaded.");
    }

    [Fact]
    public void CancelWithdrawsOnlyTheMessageOwnedByItsGroup()
    {
        FakeTimeProvider time = new();
        using SurfaceAnnouncementCoordinator sut = new(time);
        sut.Announce("projection", "stale:1", "stale", "Data may be out of date.", immediate: true);
        sut.Announce("projection", "load:2", "loaded", "Data loaded.", immediate: true);

        sut.Cancel("projection", "stale:1");
        sut.Current("projection").ShouldBe("Data loaded.");
        sut.IsCurrent("projection", "load:2", "loaded").ShouldBeTrue();
        sut.IsCurrent("projection", "load:2", "failure").ShouldBeFalse();
        sut.IsCurrent("projection", "stale:1", "stale").ShouldBeFalse();

        sut.Cancel("projection", "load:2");
        sut.Current("projection").ShouldBeEmpty();
        sut.IsCurrent("projection", "load:2", "loaded").ShouldBeFalse();
    }

    [Fact]
    public void TerminalCancelsOlderPendingGroupsOnTheSameSurface()
    {
        FakeTimeProvider time = new();
        using SurfaceAnnouncementCoordinator sut = new(time);
        List<string> spoken = [];
        using IDisposable subscription = sut.Subscribe("projection", spoken.Add);

        sut.Announce("projection", "load:old", "loading", "Loading");
        time.Advance(TimeSpan.FromMilliseconds(50));
        sut.Announce("projection", "connection:old", "reconnecting", "Reconnecting");
        time.Advance(TimeSpan.FromMilliseconds(50));
        sut.Announce("projection", "result:new", "failed", "Data could not be loaded.", terminal: true);
        time.Advance(TimeSpan.FromSeconds(1));

        spoken.ShouldBe(["Data could not be loaded."]);
        sut.Current("projection").ShouldBe("Data could not be loaded.");
    }

    [Fact]
    public void ReplayAndDeliveriesHaveMonotonicRevisions()
    {
        FakeTimeProvider time = new();
        using SurfaceAnnouncementCoordinator sut = new(time);
        sut.Announce("projection", "first", "a", "First", immediate: true);
        List<(string Message, long Revision)> delivered = [];
        using IDisposable subscription = sut.SubscribeWithReplay("projection", (message, revision) => delivered.Add((message, revision)));
        sut.Announce("projection", "second", "b", "Second", terminal: true);
        sut.Clear("projection");

        delivered.Select(static entry => entry.Message).ShouldBe(["First", "Second", string.Empty]);
        delivered.Select(static entry => entry.Revision).ShouldBe([1L, 2L, 3L]);
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CancelledUndeliveredIdentityCanSpeakWhenItBecomesRelevantAgain(bool decisiveCancellation)
    {
        FakeTimeProvider time = new();
        using SurfaceAnnouncementCoordinator sut = new(time);
        List<string> spoken = [];
        using IDisposable subscription = sut.Subscribe("surface", spoken.Add);
        sut.Announce("surface", "connection", "reconnecting", "Reconnecting");
        if (decisiveCancellation)
        {
            sut.Announce("surface", "result", "failed", "Failed", terminal: true);
            sut.Cancel("surface", "result");
        }
        else
        {
            sut.Cancel("surface", "connection");
        }
        sut.Announce("surface", "connection", "reconnecting", "Reconnecting");
        time.Advance(TimeSpan.FromMilliseconds(250));
        spoken.Count(message => message == "Reconnecting").ShouldBe(1);
        sut.Cancel("surface", "connection");
        sut.Announce("surface", "connection", "reconnecting", "Reconnecting");
        time.Advance(TimeSpan.FromMilliseconds(250));
        spoken.Count(message => message == "Reconnecting").ShouldBe(1);
    }

    [Fact]
    public void ClearedGroupCallbackCannotFlushTheReplacementGroupWithTheSameVersion()
    {
        var time = new Hexalith.FrontComposer.Shell.Tests.Infrastructure.QueuedTimerTimeProvider();
        using SurfaceAnnouncementCoordinator sut = new(time);
        sut.Announce("surface", "same-group", "old", "Old scope");
        sut.Clear("surface");
        sut.Announce("surface", "same-group", "new", "New scope");
        time.Callbacks[0]();
        sut.Current("surface").ShouldBeEmpty();
        time.Callbacks[1]();
        sut.Current("surface").ShouldBe("New scope");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FatalSubscriberExceptionsPropagate(bool versioned)
    {
        using SurfaceAnnouncementCoordinator sut = new(new FakeTimeProvider());
        using IDisposable subscription = versioned
            ? sut.SubscribeWithReplay("surface", (message, _) => { if (message.Length > 0) { throw (OutOfMemoryException)Activator.CreateInstance(typeof(OutOfMemoryException), "fatal")!; } })
            : sut.Subscribe("surface", _ => throw (OutOfMemoryException)Activator.CreateInstance(typeof(OutOfMemoryException), "fatal")!);
        Should.Throw<OutOfMemoryException>(() => sut.Announce("surface", "operation", "terminal", "Done", terminal: true));
    }

}
