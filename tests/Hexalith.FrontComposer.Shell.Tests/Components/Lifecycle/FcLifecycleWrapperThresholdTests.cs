using Bunit;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Components.Lifecycle;
using Hexalith.FrontComposer.Shell.Options;
using Hexalith.FrontComposer.Shell.Services.Lifecycle;
using Hexalith.FrontComposer.Shell.Services.Announcements;
using Hexalith.FrontComposer.Shell.State.ProjectionConnection;
using Hexalith.FrontComposer.Shell.State.PendingCommands;
using Hexalith.FrontComposer.Shell.Tests.State.PendingCommands;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using Shouldly;
using NSubstitute;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Lifecycle;

/// <summary>
/// Story 2-4 threshold-boundary bUnit tests for AC2–AC5. All tests use
/// <see cref="FakeTimeProvider"/> to advance past the default <c>FcShellOptions</c> thresholds
/// (300 ms / 2 000 ms / 10 000 ms) and assert the corresponding UI surface.
/// </summary>
public sealed class FcLifecycleWrapperThresholdTests : LifecycleWrapperTestBase {
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CoMountedWrappersKeepSharedLifecycleStatusUntilFinalOwnerLeaves(bool firstLeaves)
    {
        using LifecycleStateService lifecycle = new(Microsoft.Extensions.Options.Options.Create(new LifecycleOptions()), FakeTime);
        RegisterLifecycleService(lifecycle);
        ISurfaceAnnouncementCoordinator announcements = Services.GetRequiredService<ISurfaceAnnouncementCoordinator>();
        List<string> spoken = [];
        using IDisposable subscription = announcements.Subscribe("shared-command", spoken.Add);
        IRenderedComponent<FcLifecycleWrapper> first = Render<FcLifecycleWrapper>(p => p
            .Add(c => c.CorrelationId, DefaultCorrelationId).Add(c => c.StatusSurface, "shared-command"));
        IRenderedComponent<FcLifecycleWrapper> second = Render<FcLifecycleWrapper>(p => p
            .Add(c => c.CorrelationId, DefaultCorrelationId).Add(c => c.StatusSurface, "shared-command"));
        lifecycle.Transition(DefaultCorrelationId, CommandLifecycleState.Submitting, "message");
        lifecycle.Transition(DefaultCorrelationId, CommandLifecycleState.Acknowledged, "message");
        FakeTime.Advance(TimeSpan.FromMilliseconds(249));
        spoken.ShouldBeEmpty();
        (firstLeaves ? first : second).Instance.Dispose();
        FakeTime.Advance(TimeSpan.FromMilliseconds(1));
        (firstLeaves ? second : first).WaitForAssertion(() => {
            announcements.Current("shared-command").ShouldBe("Command accepted. Waiting for confirmation.");
            spoken.ShouldBe(["Command accepted. Waiting for confirmation."]);
        });
        (firstLeaves ? second : first).Instance.Dispose();
        announcements.Current("shared-command").ShouldBeEmpty();
    }

    [Fact]
    public void OldMountedDeadlineCannotRecreateLifecycleAfterScopeReset()
    {
        LifecycleStateService lifecycle = new(Microsoft.Extensions.Options.Options.Create(new LifecycleOptions()), FakeTime);
        RegisterLifecycleService(lifecycle);
        ISurfaceAnnouncementCoordinator announcements = Services.GetRequiredService<ISurfaceAnnouncementCoordinator>();
        IRenderedComponent<FcLifecycleWrapper> cut = Render<FcLifecycleWrapper>(p => p.Add(c => c.CorrelationId, DefaultCorrelationId));
        lifecycle.Transition(DefaultCorrelationId, CommandLifecycleState.Submitting, "old-message");
        lifecycle.Transition(DefaultCorrelationId, CommandLifecycleState.Acknowledged, "old-message");
        FakeTime.Advance(TimeSpan.FromMilliseconds(250));
        cut.WaitForAssertion(() => announcements.Current("lifecycle:" + DefaultCorrelationId).ShouldNotBeEmpty());

        lifecycle.ResetScope();
        cut.WaitForAssertion(() =>
        {
            cut.Find(".fc-lifecycle-wrapper").GetAttribute("data-lifecycle-state").ShouldBe("idle");
            announcements.Current("lifecycle:" + DefaultCorrelationId).ShouldBeEmpty();
        });
        FakeTime.Advance(TimeSpan.FromSeconds(120));
        lifecycle.GetState(DefaultCorrelationId).ShouldBe(CommandLifecycleState.Idle);
        announcements.Current("lifecycle:" + DefaultCorrelationId).ShouldBeEmpty();

        const string currentCorrelation = "01DPZ3NDEKTSV4RRFFQ69G5FAV";
        cut.Render(p => p.Add(c => c.CorrelationId, currentCorrelation));
        lifecycle.Transition(currentCorrelation, CommandLifecycleState.Submitting, "current-message");
        lifecycle.Transition(currentCorrelation, CommandLifecycleState.Acknowledged, "current-message");
        FakeTime.Advance(TimeSpan.FromSeconds(120));
        cut.WaitForAssertion(() => lifecycle.GetState(currentCorrelation).ShouldBe(CommandLifecycleState.DegradedExhausted));
    }
    [Fact]
    public void StoredTerminalWithDelayedLifecycleDispatchReplacesActiveWrapperAtAcceptanceDeadline()
    {
        const string correlation = "01CPZ3NDEKTSV4RRFFQ69G5FAV";
        const string message = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
        LifecycleStateService actual = new(Microsoft.Extensions.Options.Options.Create(new LifecycleOptions()), FakeTime);
        ILifecycleStateService delayed = Substitute.For<ILifecycleStateService>();
        delayed.Subscribe(Arg.Any<string>(), Arg.Any<Action<CommandLifecycleTransition>>())
            .Returns(call => actual.Subscribe(call.ArgAt<string>(0), call.ArgAt<Action<CommandLifecycleTransition>>(1)));
        delayed.GetState(Arg.Any<string>()).Returns(call => actual.GetState(call.ArgAt<string>(0)));
        delayed.GetMessageId(Arg.Any<string>()).Returns(call => actual.GetMessageId(call.ArgAt<string>(0)));
        delayed.When(service => service.Transition(Arg.Any<string>(), Arg.Any<CommandLifecycleState>(), Arg.Any<string?>()))
            .Do(call => actual.Transition(call.ArgAt<string>(0), call.ArgAt<CommandLifecycleState>(1), call.ArgAt<string?>(2)));
        bool releaseTerminal = false;
        delayed.When(service => service.Transition(Arg.Any<string>(), Arg.Any<CommandLifecycleState>(), Arg.Any<string?>(), Arg.Any<bool>()))
            .Do(call =>
            {
                if (!releaseTerminal)
                {
                    throw new InvalidOperationException("held lifecycle dispatch");
                }
                actual.Transition(call.ArgAt<string>(0), call.ArgAt<CommandLifecycleState>(1),
                    call.ArgAt<string?>(2), call.ArgAt<bool>(3));
            });
        RegisterLifecycleService(delayed);
        IUserContextAccessor user = Substitute.For<IUserContextAccessor>();
        user.TenantId.Returns("tenant-a");
        user.UserId.Returns("user-a");
        PendingCommandStateService pending = new(Microsoft.Extensions.Options.Options.Create(new FcShellOptions()),
            delayed, user, time: FakeTime);
        Services.AddSingleton<IPendingCommandStateService>(pending);
        ISurfaceAnnouncementCoordinator announcements = Services.GetRequiredService<ISurfaceAnnouncementCoordinator>();
        List<string> spoken = [];
        using IDisposable subscription = announcements.Subscribe("lifecycle:" + correlation, spoken.Add);
        IRenderedComponent<FcLifecycleWrapper> cut = Render<FcLifecycleWrapper>(p => p.Add(c => c.CorrelationId, correlation));
        DateTimeOffset acceptedAt = FakeTime.GetUtcNow();
        actual.Transition(correlation, CommandLifecycleState.Submitting, message);
        actual.Transition(correlation, CommandLifecycleState.Acknowledged, message);
        pending.Register(new(correlation, message, "Counter.Increment", "Counter.Count", "counter-counts",
            "counter-1", null, null, acceptedAt)).Status.ShouldBe(PendingCommandRegistrationStatus.Registered);
        FakeTime.Advance(TimeSpan.FromSeconds(30));
        pending.ResolveTerminal(PendingCommandTerminalObservation.Confirmed(message)).Status
            .ShouldBe(PendingCommandResolutionStatus.LifecycleDispatchFailed);
        actual.GetState(correlation).ShouldBe(CommandLifecycleState.Degraded);

        FakeTime.Advance(TimeSpan.FromMilliseconds(89_999));
        cut.Find(".fc-lifecycle-wrapper").GetAttribute("data-lifecycle-state").ShouldBe("degraded");
        FakeTime.Advance(TimeSpan.FromMilliseconds(1));
        cut.WaitForAssertion(() => cut.Find(".fc-lifecycle-wrapper")
            .GetAttribute("data-lifecycle-state").ShouldBe("confirmed"));
        pending.GetByMessageId(message).ShouldNotBeNull().Status.ShouldBe(PendingCommandStatus.Confirmed);
        actual.GetState(correlation).ShouldBe(CommandLifecycleState.Degraded);
        releaseTerminal = true;
        pending.ConvergeLifecycle(1).Converged.ShouldBe(1);
        actual.GetState(correlation).ShouldBe(CommandLifecycleState.Confirmed);
        spoken.Count(text => text == "Command confirmed.").ShouldBe(1);
    }

    [Theory]
    [InlineData(120000, false)]
    [InlineData(30000, true)]
    public async Task UnmountedAcceptedCommandClosesAtItsHeaderDeadlineWhileTheQueryAndCleanupAreHeld(int duration, bool blockingCleanup)
    {
        const string correlation = "01CPZ3NDEKTSV4RRFFQ69G5FAV";
        const string message = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
        Services.Configure<FcShellOptions>(options => options.MaxPendingCommandPollingDurationMs = duration);
        LifecycleStateService lifecycle = new(Microsoft.Extensions.Options.Options.Create(new LifecycleOptions()), FakeTime);
        RegisterLifecycleService(lifecycle);
        IRenderedComponent<FcLifecycleWrapper> cut = Render<FcLifecycleWrapper>(p => p.Add(c => c.CorrelationId, correlation));
        DateTimeOffset headers = FakeTime.GetUtcNow();
        lifecycle.Transition(correlation, CommandLifecycleState.Submitting);
        lifecycle.TransitionObserved(correlation, CommandLifecycleState.Syncing, message, headers);
        cut.WaitForAssertion(() => lifecycle.GetState(correlation).ShouldBe(CommandLifecycleState.Syncing));
        cut.Dispose();

        IUserContextAccessor user = Substitute.For<IUserContextAccessor>();
        user.TenantId.Returns("tenant-a");
        user.UserId.Returns("user-a");
        FcShellOptions options = new() { MaxPendingCommandPollingDurationMs = duration };
        PendingCommandStateService state = new(Microsoft.Extensions.Options.Options.Create(options), lifecycle, user, time: FakeTime);
        // Optional response work has finished late, but registration retains the proven header anchor.
        FakeTime.Advance(TimeSpan.FromSeconds(2));
        state.Register(new(correlation, message, "Counter.Increment", "Counter.Count", "counter-counts", "counter-1", null, null, headers));
        HeldPendingCommandStatusQuery query = new();
        using ManualResetEventSlim release = new(!blockingCleanup);
        query.CancellationRelease = release;
        PendingCommandPollingCoordinator polling = new(state, new PendingCommandOutcomeResolver(state), query,
            Microsoft.Extensions.Options.Options.Create(options), timeProvider: FakeTime);
        int exhausted = 0;
        using IDisposable listener = lifecycle.Subscribe(correlation, change =>
        {
            if (change.NewState == CommandLifecycleState.DegradedExhausted) { exhausted++; }
        });
        FakeTime.Advance(TimeSpan.FromMilliseconds(duration - 2001));
        Task<int> poll = polling.PollOnceAsync(Xunit.TestContext.Current.CancellationToken);
        query.Calls.ShouldBe(1);
        poll.IsCompleted.ShouldBeFalse();
        state.GetByMessageId(message).ShouldNotBeNull().Status.ShouldBe(PendingCommandStatus.Pending);
        try
        {
            FakeTime.Advance(TimeSpan.FromMilliseconds(1));
            (await poll.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken)).ShouldBe(0);
            state.GetByMessageId(message).ShouldNotBeNull().Status.ShouldBe(PendingCommandStatus.DegradedExhausted);
            lifecycle.GetState(correlation).ShouldBe(CommandLifecycleState.DegradedExhausted);
            exhausted.ShouldBe(1);
            await query.CancellationObserved.Task.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);
            (await polling.PollOnceAsync(Xunit.TestContext.Current.CancellationToken)).ShouldBe(0);
            query.Calls.ShouldBe(1);
            PendingCommandOutcomeObservation late = new(PendingCommandOutcomeSource.FallbackPolling, PendingCommandTerminalOutcome.Confirmed, MessageId: message);
            query.Completion.TrySetResult(late);
            new PendingCommandOutcomeResolver(state).Resolve(late).Status.ShouldBe(PendingCommandOutcomeResolutionStatus.DuplicateIgnored);
            lifecycle.GetState(correlation).ShouldBe(CommandLifecycleState.DegradedExhausted);
            exhausted.ShouldBe(1);
        }
        finally
        {
            release.Set();
            query.Completion.TrySetResult(null);
            await query.CancellationFinished.Task.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public void AcceptedCommandBecomesDegradedAtTenSecondsAndClosesAtTwoMinutes() {
        LifecycleStateService lifecycle = new(
            Microsoft.Extensions.Options.Options.Create(new LifecycleOptions()), FakeTime,
            NullLogger<LifecycleStateService>.Instance);
        RegisterLifecycleService(lifecycle);
        IRenderedComponent<FcLifecycleWrapper> cut = Render<FcLifecycleWrapper>(p => p
            .Add(c => c.CorrelationId, DefaultCorrelationId));

        lifecycle.Transition(DefaultCorrelationId, CommandLifecycleState.Submitting, "01HVTESTULID");
        lifecycle.Transition(DefaultCorrelationId, CommandLifecycleState.Acknowledged, "01HVTESTULID");
        cut.WaitForAssertion(() => cut.Find(".fc-lifecycle-wrapper")
            .GetAttribute("data-lifecycle-state").ShouldBe("acknowledged"));

        FakeTime.Advance(TimeSpan.FromMilliseconds(9_999));
        lifecycle.GetState(DefaultCorrelationId).ShouldBe(CommandLifecycleState.Acknowledged);
        FakeTime.Advance(TimeSpan.FromMilliseconds(1));
        cut.WaitForAssertion(() => lifecycle.GetState(DefaultCorrelationId)
            .ShouldBe(CommandLifecycleState.Degraded));
        cut.Find("[data-testid='fc-surface-status']").TextContent
            .ShouldBe("Confirmation is taking longer than expected. Review status or continue working.");

        FakeTime.Advance(TimeSpan.FromMilliseconds(109_999));
        lifecycle.GetState(DefaultCorrelationId).ShouldBe(CommandLifecycleState.Degraded);
        FakeTime.Advance(TimeSpan.FromMilliseconds(1));
        cut.WaitForAssertion(() => lifecycle.GetState(DefaultCorrelationId)
            .ShouldBe(CommandLifecycleState.DegradedExhausted));
        cut.Find("[data-testid='fc-surface-status']").TextContent
            .ShouldBe("Confirmation was not received. Review status later or continue working.");

        lifecycle.Transition(DefaultCorrelationId, CommandLifecycleState.Confirmed, "01HVTESTULID");
        lifecycle.GetState(DefaultCorrelationId).ShouldBe(CommandLifecycleState.DegradedExhausted);
    }

    [Fact]
    public void DegradedStartsAtTenSecondsWhenActionPromptBeginsEarlier() {
        Services.Configure<FcShellOptions>(options => options.TimeoutActionThresholdMs = 5_000);
        LifecycleStateService lifecycle = new(
            Microsoft.Extensions.Options.Options.Create(new LifecycleOptions()), FakeTime,
            NullLogger<LifecycleStateService>.Instance);
        RegisterLifecycleService(lifecycle);
        IRenderedComponent<FcLifecycleWrapper> cut = Render<FcLifecycleWrapper>(p => p
            .Add(c => c.CorrelationId, DefaultCorrelationId));

        lifecycle.Transition(DefaultCorrelationId, CommandLifecycleState.Submitting, "01HVTESTULID");
        lifecycle.Transition(DefaultCorrelationId, CommandLifecycleState.Acknowledged, "01HVTESTULID");
        cut.WaitForAssertion(() => cut.Find(".fc-lifecycle-wrapper")
            .GetAttribute("data-lifecycle-state").ShouldBe("acknowledged"));

        Services.GetRequiredService<IProjectionConnectionState>()
            .Apply(new ProjectionConnectionTransition(ProjectionConnectionStatus.Disconnected));
        FakeTime.Advance(TimeSpan.FromMilliseconds(5_000));
        cut.WaitForAssertion(() => cut.Find(".fc-lifecycle-wrapper")
            .GetAttribute("data-lifecycle-state").ShouldBe("acknowledged"));
        lifecycle.GetState(DefaultCorrelationId).ShouldBe(CommandLifecycleState.Acknowledged);

        FakeTime.Advance(TimeSpan.FromMilliseconds(4_999));
        lifecycle.GetState(DefaultCorrelationId).ShouldBe(CommandLifecycleState.Acknowledged);
        FakeTime.Advance(TimeSpan.FromMilliseconds(1));
        cut.WaitForAssertion(() => lifecycle.GetState(DefaultCorrelationId)
            .ShouldBe(CommandLifecycleState.Degraded));
    }

    [Fact]
    public void RebindIgnoresATimerCallbackQueuedForThePreviousCommand() {
        LifecycleStateService lifecycle = new(
            Microsoft.Extensions.Options.Options.Create(new LifecycleOptions()), FakeTime,
            NullLogger<LifecycleStateService>.Instance);
        RegisterLifecycleService(lifecycle);
        IRenderedComponent<FcLifecycleWrapper> cut = Render<FcLifecycleWrapper>(p => p
            .Add(c => c.CorrelationId, DefaultCorrelationId));
        lifecycle.Transition(DefaultCorrelationId, CommandLifecycleState.Submitting, "01HVTESTULID");
        lifecycle.Transition(DefaultCorrelationId, CommandLifecycleState.Acknowledged, "01HVTESTULID");
        cut.WaitForAssertion(() => cut.Find(".fc-lifecycle-wrapper")
            .GetAttribute("data-lifecycle-state").ShouldBe("acknowledged"));

        FakeTime.Advance(TimeSpan.FromMilliseconds(2_000));
        cut.Render(p => p.Add(c => c.CorrelationId, "corr-replacement"));
        cut.WaitForAssertion(() => {
            cut.Find(".fc-lifecycle-wrapper").GetAttribute("data-lifecycle-state").ShouldBe("idle");
            cut.Markup.ShouldNotContain("Still syncing");
        });
        lifecycle.GetState("corr-replacement").ShouldBe(CommandLifecycleState.Idle);

        FakeTime.Advance(TimeSpan.FromSeconds(10));
        lifecycle.GetState("corr-replacement").ShouldBe(CommandLifecycleState.Idle);
        lifecycle.GetState(DefaultCorrelationId).ShouldBe(CommandLifecycleState.Acknowledged);
    }

    [Fact]
    public void DegradedReplayRetainsTheOriginalAcceptanceDeadline() {
        LifecycleStateService lifecycle = new(
            Microsoft.Extensions.Options.Options.Create(new LifecycleOptions()), FakeTime,
            NullLogger<LifecycleStateService>.Instance);
        lifecycle.Transition(DefaultCorrelationId, CommandLifecycleState.Submitting, "01HVTESTULID");
        lifecycle.Transition(DefaultCorrelationId, CommandLifecycleState.Acknowledged, "01HVTESTULID");
        FakeTime.Advance(TimeSpan.FromSeconds(10));
        lifecycle.Transition(DefaultCorrelationId, CommandLifecycleState.Degraded, "01HVTESTULID");
        RegisterLifecycleService(lifecycle);

        _ = Render<FcLifecycleWrapper>(p => p.Add(c => c.CorrelationId, DefaultCorrelationId));
        FakeTime.Advance(TimeSpan.FromMilliseconds(109_999));
        lifecycle.GetState(DefaultCorrelationId).ShouldBe(CommandLifecycleState.Degraded);
        FakeTime.Advance(TimeSpan.FromMilliseconds(1));
        lifecycle.GetState(DefaultCorrelationId).ShouldBe(CommandLifecycleState.DegradedExhausted);
    }

    [Fact]
    public void Confirmed_within_SyncPulseThresholdMs_never_applies_pulse_class_brand_signal_fusion() {
        (IRenderedComponent<FcLifecycleWrapper> cut, Action<CommandLifecycleTransition> push, FakeTimeProvider time) = RenderWrapperWithFakeTime();
        DateTimeOffset ackAt = time.GetUtcNow();
        push(TransitionAt(CommandLifecycleState.Submitting, CommandLifecycleState.Acknowledged, ackAt));
        time.Advance(TimeSpan.FromMilliseconds(250));
        push(TransitionAt(CommandLifecycleState.Acknowledged, CommandLifecycleState.Confirmed, time.GetUtcNow()));

        cut.WaitForAssertion(() => {
            cut.Find(".fc-lifecycle-wrapper").GetAttribute("class")!.ShouldNotContain("fc-lifecycle-pulse");
            cut.Markup.ShouldContain("Submission confirmed", Case.Insensitive);
            cut.Markup.ShouldNotContain("Still syncing", Case.Insensitive);
        });
    }

    [Fact]
    public void Exactly_at_SyncPulseThresholdMs_applies_pulse_class() {
        (IRenderedComponent<FcLifecycleWrapper> cut, Action<CommandLifecycleTransition> push, FakeTimeProvider time) = RenderWrapperWithFakeTime();
        DateTimeOffset syncAt = time.GetUtcNow();
        push(TransitionAt(CommandLifecycleState.Acknowledged, CommandLifecycleState.Syncing, syncAt));
        time.Advance(TimeSpan.FromMilliseconds(300));

        cut.WaitForAssertion(() => cut.Find(".fc-lifecycle-wrapper").GetAttribute("class")!.ShouldContain("fc-lifecycle-pulse"));
    }

    [Fact]
    public void Exactly_at_StillSyncingThresholdMs_renders_still_syncing_badge() {
        (IRenderedComponent<FcLifecycleWrapper> cut, Action<CommandLifecycleTransition> push, FakeTimeProvider time) = RenderWrapperWithFakeTime();
        DateTimeOffset syncAt = time.GetUtcNow();
        push(TransitionAt(CommandLifecycleState.Acknowledged, CommandLifecycleState.Syncing, syncAt));
        time.Advance(TimeSpan.FromMilliseconds(2_000));

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Still syncing", Case.Insensitive));
    }

    [Fact]
    public void StillSyncing_phase_does_not_apply_pulse_class_AC3_band_exclusive_of_AC4() {
        (IRenderedComponent<FcLifecycleWrapper> cut, Action<CommandLifecycleTransition> push, FakeTimeProvider time) = RenderWrapperWithFakeTime();
        DateTimeOffset syncAt = time.GetUtcNow();
        push(TransitionAt(CommandLifecycleState.Acknowledged, CommandLifecycleState.Syncing, syncAt));
        time.Advance(TimeSpan.FromMilliseconds(2_500));

        cut.WaitForAssertion(() => {
            cut.Markup.ShouldContain("Still syncing", Case.Insensitive);
            cut.Find(".fc-lifecycle-wrapper").GetAttribute("class")!.ShouldNotContain("fc-lifecycle-pulse");
        });
    }

    [Fact]
    public void Exactly_at_TimeoutActionThresholdMs_renders_action_prompt_message_bar() {
        (IRenderedComponent<FcLifecycleWrapper> cut, Action<CommandLifecycleTransition> push, FakeTimeProvider time) = RenderWrapperWithFakeTime();
        DateTimeOffset syncAt = time.GetUtcNow();
        push(TransitionAt(CommandLifecycleState.Acknowledged, CommandLifecycleState.Syncing, syncAt));
        time.Advance(TimeSpan.FromMilliseconds(10_000));

        cut.WaitForAssertion(() => {
            cut.Markup.ShouldContain("Action needed", Case.Insensitive);
            cut.Markup.ShouldNotContain("Still syncing", Case.Insensitive);
        });
    }

    [Fact]
    public void Timer_anchors_on_LastTransitionAt_not_subscribe_time() {
        (IRenderedComponent<FcLifecycleWrapper> cut, Action<CommandLifecycleTransition> push, FakeTimeProvider time) = RenderWrapperWithFakeTime();
        // Transition anchor already 3 s in the past — the wrapper must treat elapsed-from-anchor
        // (not elapsed-from-subscribe) so "Still syncing…" surfaces without any further advance.
        DateTimeOffset originalAnchor = time.GetUtcNow().AddSeconds(-3);
        push(TransitionAt(CommandLifecycleState.Acknowledged, CommandLifecycleState.Syncing, originalAnchor));

        cut.WaitForAssertion(() => {
            cut.Markup.ShouldContain("Still syncing", Case.Insensitive);
            cut.Markup.ShouldNotContain("Action needed", Case.Insensitive);
        });
    }

    [Fact]
    public void Confirmed_while_in_ActionPrompt_phase_immediately_resolves_to_success_message_bar_no_dangling_pulse() {
        (IRenderedComponent<FcLifecycleWrapper> cut, Action<CommandLifecycleTransition> push, FakeTimeProvider time) = RenderWrapperWithFakeTime();
        DateTimeOffset syncAt = time.GetUtcNow();
        push(TransitionAt(CommandLifecycleState.Acknowledged, CommandLifecycleState.Syncing, syncAt));
        time.Advance(TimeSpan.FromMilliseconds(10_500));
        cut.WaitForState(() => cut.Markup.Contains("Action needed", StringComparison.OrdinalIgnoreCase));

        push(TransitionAt(CommandLifecycleState.Syncing, CommandLifecycleState.Confirmed, time.GetUtcNow()));

        cut.WaitForAssertion(() => {
            cut.Markup.ShouldContain("Submission confirmed", Case.Insensitive);
            cut.Find(".fc-lifecycle-wrapper").GetAttribute("class")!.ShouldNotContain("fc-lifecycle-pulse");
            cut.Markup.ShouldNotContain("Action needed", Case.Insensitive);
        });
    }

    [Fact]
    public void Rejected_while_in_ActionPrompt_phase_replaces_degraded_warning_with_rejection() {
        (IRenderedComponent<FcLifecycleWrapper> cut, Action<CommandLifecycleTransition> push, FakeTimeProvider time) = RenderWrapperWithFakeTime();
        DateTimeOffset syncAt = time.GetUtcNow();
        push(TransitionAt(CommandLifecycleState.Acknowledged, CommandLifecycleState.Syncing, syncAt));
        time.Advance(TimeSpan.FromMilliseconds(10_500));
        cut.WaitForState(() => cut.Markup.Contains("Action needed", StringComparison.OrdinalIgnoreCase));

        push(TransitionAt(CommandLifecycleState.Syncing, CommandLifecycleState.Rejected, time.GetUtcNow()));

        cut.WaitForAssertion(() => {
            cut.Markup.ShouldContain("Submission rejected", Case.Insensitive);
            cut.Markup.ShouldNotContain("Action needed", Case.Insensitive);
            cut.Markup.ShouldNotContain("Still syncing", Case.Insensitive);
        });
    }

    [Fact]
    public void IdempotentConfirmed_while_in_ActionPrompt_phase_replaces_degraded_warning_with_info_bar() {
        (IRenderedComponent<FcLifecycleWrapper> cut, Action<CommandLifecycleTransition> push, FakeTimeProvider time) = RenderWrapperWithFakeTime();
        DateTimeOffset syncAt = time.GetUtcNow();
        push(TransitionAt(CommandLifecycleState.Acknowledged, CommandLifecycleState.Syncing, syncAt));
        time.Advance(TimeSpan.FromMilliseconds(10_500));
        cut.WaitForState(() => cut.Markup.Contains("Action needed", StringComparison.OrdinalIgnoreCase));

        push(TransitionAt(CommandLifecycleState.Syncing, CommandLifecycleState.Confirmed, time.GetUtcNow(), idempotencyResolved: true));

        cut.WaitForAssertion(() => {
            cut.Markup.ShouldContain("Already confirmed", Case.Insensitive);
            cut.FindAll("[data-testid='fc-action-prompt']").ShouldBeEmpty();
            cut.Markup.ShouldNotContain("Still syncing", Case.Insensitive);
        });
    }
    [Fact]
    public void OldConfirmationDismissCannotDismissOrClearTheReplacementOperation()
    {
        var time = new Hexalith.FrontComposer.Shell.Tests.Infrastructure.QueuedTimerTimeProvider();
        Services.AddSingleton<TimeProvider>(time);
        var (cut, push) = RenderWrapperWithLiveService();
        push(TransitionAt(CommandLifecycleState.Syncing, CommandLifecycleState.Confirmed, time.GetUtcNow()));
        Action oldDismiss = time.Callbacks.Single();
        cut.Render(p => p.Add(c => c.CorrelationId, "replacement"));
        push(TransitionAt(CommandLifecycleState.Syncing, CommandLifecycleState.Confirmed, time.GetUtcNow()) with { CorrelationId = "replacement" });
        oldDismiss();
        cut.Find(".fc-lifecycle-wrapper").GetAttribute("data-lifecycle-state").ShouldBe("confirmed");
        cut.Find("[data-testid='fc-surface-status']").TextContent.ShouldBe("Command confirmed.");
    }

    [Fact]
    public void CapturedDeadlineAndDegradedTicksAfterDisposalCannotTransition()
    {
        var time = new Hexalith.FrontComposer.Shell.Tests.Infrastructure.QueuedTimerTimeProvider();
        Services.AddSingleton<TimeProvider>(time);
        LifecycleStateService lifecycle = new(Microsoft.Extensions.Options.Options.Create(new LifecycleOptions()), time,
            NullLogger<LifecycleStateService>.Instance);
        RegisterLifecycleService(lifecycle);
        IRenderedComponent<FcLifecycleWrapper> cut = Render<FcLifecycleWrapper>(p => p.Add(c => c.CorrelationId, DefaultCorrelationId));
        lifecycle.Transition(DefaultCorrelationId, CommandLifecycleState.Submitting, "message");
        lifecycle.Transition(DefaultCorrelationId, CommandLifecycleState.Acknowledged, "message");
        cut.WaitForAssertion(() => cut.Find(".fc-lifecycle-wrapper").GetAttribute("data-lifecycle-state").ShouldBe("acknowledged"));
        Action[] queued = [.. time.Callbacks];
        cut.Instance.Dispose();
        foreach (Action callback in queued)
        {
            Should.NotThrow(callback);
        }
        lifecycle.GetState(DefaultCorrelationId).ShouldBe(CommandLifecycleState.Acknowledged);
    }

    [Fact]
    public async Task QueuedDeadlineAndDegradedCallbacksCannotMutateReboundSameCorrelation()
    {
        var time = new Hexalith.FrontComposer.Shell.Tests.Infrastructure.QueuedTimerTimeProvider();
        Services.AddSingleton<TimeProvider>(time);
        ILifecycleStateService service = Substitute.For<ILifecycleStateService>();
        Action<CommandLifecycleTransition>? current = null;
        _ = service.Subscribe(Arg.Any<string>(), Arg.Do<Action<CommandLifecycleTransition>>(callback => current = callback))
            .Returns(Substitute.For<IDisposable>());
        RegisterLifecycleService(service);
        IRenderedComponent<FcLifecycleWrapper> cut = Render<FcLifecycleWrapper>(p => p.Add(c => c.CorrelationId, DefaultCorrelationId));
        await cut.InvokeAsync(() => current!(TransitionAt(CommandLifecycleState.Submitting,
            CommandLifecycleState.Acknowledged, time.GetUtcNow())));
        Action[] oldCallbacks = [.. time.Callbacks];

        cut.Render(p => p.Add(c => c.CorrelationId, "other"));
        cut.Render(p => p.Add(c => c.CorrelationId, DefaultCorrelationId));
        await cut.InvokeAsync(() => current!(TransitionAt(CommandLifecycleState.Submitting,
            CommandLifecycleState.Acknowledged, time.GetUtcNow())));
        foreach (Action callback in oldCallbacks)
        {
            callback();
        }

        cut.Find(".fc-lifecycle-wrapper").GetAttribute("data-lifecycle-state").ShouldBe("acknowledged");
        service.DidNotReceive().Transition(DefaultCorrelationId, CommandLifecycleState.Degraded, Arg.Any<string?>());
        service.DidNotReceive().Transition(DefaultCorrelationId, CommandLifecycleState.DegradedExhausted, Arg.Any<string?>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CapturedOldTimerDelegateCannotApplyToReplacementOrReusedCorrelation(bool reuseCorrelation)
    {
        var (cut, push) = RenderWrapperWithLiveService();
        push(TransitionAt(CommandLifecycleState.Submitting, CommandLifecycleState.Acknowledged, FakeTime.GetUtcNow()));
        LifecycleThresholdTimer original = (LifecycleThresholdTimer)typeof(FcLifecycleWrapper)
            .GetField("_timer", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(cut.Instance)!;
        Action<LifecycleTimerPhase> captured = (Action<LifecycleTimerPhase>)typeof(LifecycleThresholdTimer)
            .GetField("OnPhaseChanged", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(original)!;
        cut.Render(p => p.Add(c => c.CorrelationId, "replacement"));
        string current = reuseCorrelation ? DefaultCorrelationId : "replacement";
        if (reuseCorrelation)
        {
            cut.Render(p => p.Add(c => c.CorrelationId, DefaultCorrelationId));
        }
        push(TransitionAt(CommandLifecycleState.Submitting, CommandLifecycleState.Acknowledged, FakeTime.GetUtcNow()) with { CorrelationId = current });
        push(TransitionAt(CommandLifecycleState.Acknowledged, CommandLifecycleState.Syncing, FakeTime.GetUtcNow()) with { CorrelationId = current });
        captured(LifecycleTimerPhase.ActionPrompt);
        cut.FindAll("[data-testid='fc-action-prompt']").ShouldBeEmpty();
        FakeTime.Advance(TimeSpan.FromMilliseconds(2000));
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Still syncing", Case.Insensitive));
        cut.FindAll("[data-testid='fc-action-prompt']").ShouldBeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EqualCorrelationSurfaceRebindWithdrawsOwnedSpeechAndKeepsOriginalBudget(bool terminal)
    {
        LifecycleStateService lifecycle = new(Microsoft.Extensions.Options.Options.Create(new LifecycleOptions()), FakeTime,
            NullLogger<LifecycleStateService>.Instance);
        RegisterLifecycleService(lifecycle);
        ISurfaceAnnouncementCoordinator announcements = Services.GetRequiredService<ISurfaceAnnouncementCoordinator>();
        List<string> originalSpeech = [];
        List<string> replacementSpeech = [];
        List<long> originalRevisions = [];
        using IDisposable originalSubscription = announcements.Subscribe("original", originalSpeech.Add);
        using IDisposable replacementSubscription = announcements.Subscribe("replacement", replacementSpeech.Add);
        using IDisposable revisionSubscription = announcements.SubscribeWithReplay("original", (_, revision) => originalRevisions.Add(revision));
        IRenderedComponent<FcLifecycleWrapper> cut = Render<FcLifecycleWrapper>(p => p
            .Add(c => c.CorrelationId, DefaultCorrelationId).Add(c => c.StatusSurface, "original"));
        lifecycle.Transition(DefaultCorrelationId, CommandLifecycleState.Submitting, "message");
        lifecycle.Transition(DefaultCorrelationId, CommandLifecycleState.Acknowledged, "message");
        if (terminal) { lifecycle.Transition(DefaultCorrelationId, CommandLifecycleState.Confirmed, "message"); }
        FakeTime.Advance(TimeSpan.FromMilliseconds(250));
        string expected = terminal ? "Command confirmed." : "Command accepted. Waiting for confirmation.";
        cut.WaitForAssertion(() => announcements.Current("original").ShouldBe(expected));
        object timer = typeof(FcLifecycleWrapper).GetField("_timer", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(cut.Instance)!;
        cut.Render(p => p.Add(c => c.StatusSurface, "replacement"));
        announcements.Current("original").ShouldBeEmpty();
        FakeTime.Advance(TimeSpan.FromMilliseconds(250));
        cut.WaitForAssertion(() => announcements.Current("replacement").ShouldBe(expected));
        typeof(FcLifecycleWrapper).GetField("_timer", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(cut.Instance).ShouldBeSameAs(timer);
        cut.Render(p => p.Add(c => c.StatusSurface, "original"));
        FakeTime.Advance(TimeSpan.FromMilliseconds(250));
        cut.WaitForAssertion(() => announcements.Current("original").ShouldBe(expected));
        announcements.Current("replacement").ShouldBeEmpty();
        originalSpeech.Count(message => message == expected).ShouldBe(2);
        ((SurfaceAnnouncementCoordinator)announcements).IsSilentRevision("original", originalRevisions[^1]).ShouldBeTrue();
        replacementSpeech.Count(message => message == expected).ShouldBe(1);
        cut.Find(".fc-lifecycle-wrapper").GetAttribute("data-lifecycle-state").ShouldBe(terminal ? "confirmed" : "acknowledged");
        if (terminal)
        {
            FakeTime.Advance(TimeSpan.FromMilliseconds(Services.GetRequiredService<IOptionsMonitor<FcShellOptions>>().CurrentValue.ConfirmedToastDurationMs - 750));
            cut.WaitForAssertion(() => announcements.Current("original").ShouldBeEmpty());
            cut.Find(".fc-lifecycle-wrapper").GetAttribute("data-lifecycle-state").ShouldBe("idle");
        }
        else
        {
            FakeTime.Advance(TimeSpan.FromMilliseconds(9_249));
            lifecycle.GetState(DefaultCorrelationId).ShouldBe(CommandLifecycleState.Acknowledged);
            FakeTime.Advance(TimeSpan.FromMilliseconds(1));
            cut.WaitForAssertion(() => lifecycle.GetState(DefaultCorrelationId).ShouldBe(CommandLifecycleState.Degraded));
            originalSpeech.Count(message => message == "Confirmation is taking longer than expected. Review status or continue working.").ShouldBe(1);
            FakeTime.Advance(TimeSpan.FromMilliseconds(110_000));
            cut.WaitForAssertion(() => lifecycle.GetState(DefaultCorrelationId).ShouldBe(CommandLifecycleState.DegradedExhausted));
        }
        announcements.Announce("original", "unrelated", "current", "Other owner remains", immediate: true);
        cut.Instance.Dispose();
        announcements.Current("original").ShouldBe("Other owner remains");
        announcements.Current("replacement").ShouldBeEmpty();
    }

    [Fact]
    public void ActiveLifecycleReturnsAfterAnotherSurfaceOwnerWithdrawsWithoutRepeatingSpeech()
    {
        using LifecycleStateService lifecycle = new(Microsoft.Extensions.Options.Options.Create(new LifecycleOptions()), FakeTime);
        RegisterLifecycleService(lifecycle);
        ISurfaceAnnouncementCoordinator announcements = Services.GetRequiredService<ISurfaceAnnouncementCoordinator>();
        List<string> spoken = [];
        List<long> revisions = [];
        using IDisposable speech = announcements.Subscribe("original", spoken.Add);
        using IDisposable version = announcements.SubscribeWithReplay("original", (_, revision) => revisions.Add(revision));
        IRenderedComponent<FcLifecycleWrapper> cut = Render<FcLifecycleWrapper>(p => p
            .Add(c => c.CorrelationId, DefaultCorrelationId).Add(c => c.StatusSurface, "original"));
        lifecycle.Transition(DefaultCorrelationId, CommandLifecycleState.Submitting, "message");
        lifecycle.Transition(DefaultCorrelationId, CommandLifecycleState.Acknowledged, "message");
        FakeTime.Advance(TimeSpan.FromMilliseconds(250));
        cut.WaitForAssertion(() => announcements.Current("original").ShouldBe("Command accepted. Waiting for confirmation."));
        cut.Render(p => p.Add(c => c.StatusSurface, "replacement"));
        announcements.Announce("original", "other:operation", "current", "Other current status", immediate: true);
        cut.Render(p => p.Add(c => c.StatusSurface, "original"));
        announcements.Current("original").ShouldBe("Other current status");
        announcements.Cancel("original", "other:operation");
        cut.WaitForAssertion(() => announcements.Current("original").ShouldBe("Command accepted. Waiting for confirmation."));
        ((SurfaceAnnouncementCoordinator)announcements).IsSilentRevision("original", revisions[^1]).ShouldBeTrue();
        spoken.Count(message => message == "Command accepted. Waiting for confirmation.").ShouldBe(2);
        lifecycle.Transition(DefaultCorrelationId, CommandLifecycleState.Degraded, "message");
        cut.WaitForAssertion(() => announcements.Current("original")
            .ShouldBe("Confirmation is taking longer than expected. Review status or continue working."));
    }

}
