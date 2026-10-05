using Bunit;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Shell.Components.Lifecycle;
using Hexalith.FrontComposer.Shell.Options;
using Hexalith.FrontComposer.Shell.Services.Lifecycle;
using Hexalith.FrontComposer.Shell.State.ProjectionConnection;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Lifecycle;

/// <summary>
/// Story 2-4 threshold-boundary bUnit tests for AC2–AC5. All tests use
/// <see cref="FakeTimeProvider"/> to advance past the default <c>FcShellOptions</c> thresholds
/// (300 ms / 2 000 ms / 10 000 ms) and assert the corresponding UI surface.
/// </summary>
public sealed class FcLifecycleWrapperThresholdTests : LifecycleWrapperTestBase {
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
}
