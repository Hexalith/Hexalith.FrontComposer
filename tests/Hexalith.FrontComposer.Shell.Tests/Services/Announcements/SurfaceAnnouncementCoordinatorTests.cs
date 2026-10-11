using Hexalith.FrontComposer.Shell.Services.Announcements;

using Microsoft.Extensions.Time.Testing;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Services.Announcements;

public sealed class SurfaceAnnouncementCoordinatorTests
{
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
