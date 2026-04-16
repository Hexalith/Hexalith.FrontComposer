using Bunit;

using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Shell.Components.Lifecycle;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Lifecycle;

/// <summary>
/// TDD RED-phase threshold-boundary tests for Story 2-4 AC2–AC5. All tests use
/// <see cref="LifecycleWrapperTestBase.FakeTimeProvider"/> to advance past the default
/// <c>FcShellOptions</c> thresholds (300 ms / 2 000 ms / 10 000 ms) and assert the
/// corresponding UI surface.
/// </summary>
public sealed class FcLifecycleWrapperThresholdTests : LifecycleWrapperTestBase {
    [Fact(Skip = "TDD RED — Story 2-4 Task 2.4 / AC2 / UX-DR48: Confirmed within SyncPulseThresholdMs never applies pulse class (brand-signal fusion).")]
    public void Confirmed_within_SyncPulseThresholdMs_never_applies_pulse_class_brand_signal_fusion() {
        (IRenderedComponent<FcLifecycleWrapper> cut, Action<CommandLifecycleTransition> push, FakeTimeProvider time) = RenderWrapperWithFakeTime();
        DateTimeOffset ackAt = time.GetUtcNow();
        push(TransitionAt(CommandLifecycleState.Submitting, CommandLifecycleState.Acknowledged, ackAt));
        time.Advance(TimeSpan.FromMilliseconds(250));
        push(TransitionAt(CommandLifecycleState.Acknowledged, CommandLifecycleState.Confirmed, time.GetUtcNow()));

        cut.WaitForAssertion(() => {
            cut.Find(".fc-lifecycle-wrapper").GetAttribute("class")!.ShouldNotContain("fc-lifecycle-pulse");
            cut.Markup.ShouldContain("Submission confirmed", Case.Insensitive);
            cut.FindAll("fluent-badge").ShouldBeEmpty();
        });
    }

    [Fact(Skip = "TDD RED — Story 2-4 Task 2.4 / AC3 boundary: exactly at SyncPulseThresholdMs the pulse CSS class applies.")]
    public void Exactly_at_SyncPulseThresholdMs_applies_pulse_class() {
        (IRenderedComponent<FcLifecycleWrapper> cut, Action<CommandLifecycleTransition> push, FakeTimeProvider time) = RenderWrapperWithFakeTime();
        DateTimeOffset syncAt = time.GetUtcNow();
        push(TransitionAt(CommandLifecycleState.Acknowledged, CommandLifecycleState.Syncing, syncAt));
        time.Advance(TimeSpan.FromMilliseconds(300));

        cut.WaitForAssertion(() => cut.Find(".fc-lifecycle-wrapper").GetAttribute("class")!.ShouldContain("fc-lifecycle-pulse"));
    }

    [Fact(Skip = "TDD RED — Story 2-4 Task 2.4 / AC4 boundary: exactly at StillSyncingThresholdMs the Still-syncing badge renders.")]
    public void Exactly_at_StillSyncingThresholdMs_renders_still_syncing_badge() {
        (IRenderedComponent<FcLifecycleWrapper> cut, Action<CommandLifecycleTransition> push, FakeTimeProvider time) = RenderWrapperWithFakeTime();
        DateTimeOffset syncAt = time.GetUtcNow();
        push(TransitionAt(CommandLifecycleState.Acknowledged, CommandLifecycleState.Syncing, syncAt));
        time.Advance(TimeSpan.FromMilliseconds(2_000));

        cut.WaitForAssertion(() => {
            cut.Find("fluent-badge").TextContent.ShouldContain("Still syncing", Case.Insensitive);
        });
    }

    [Fact(Skip = "TDD RED — Story 2-4 Task 2.4 / AC5 boundary: exactly at TimeoutActionThresholdMs the action-prompt MessageBar renders.")]
    public void Exactly_at_TimeoutActionThresholdMs_renders_action_prompt_message_bar() {
        (IRenderedComponent<FcLifecycleWrapper> cut, Action<CommandLifecycleTransition> push, FakeTimeProvider time) = RenderWrapperWithFakeTime();
        DateTimeOffset syncAt = time.GetUtcNow();
        push(TransitionAt(CommandLifecycleState.Acknowledged, CommandLifecycleState.Syncing, syncAt));
        time.Advance(TimeSpan.FromMilliseconds(10_000));

        cut.WaitForAssertion(() => {
            cut.Markup.ShouldContain("taking longer than expected", Case.Insensitive);
            cut.FindAll("fluent-badge").ShouldBeEmpty("badge is replaced by the message bar when the prompt phase is reached");
        });
    }

    [Fact(Skip = "TDD RED — Story 2-4 Task 2.4 / D15 / Sally Story C: reconnect-replay anchors the timer on original LastTransitionAt, NOT on subscribe time.")]
    public void Reconnect_replay_anchors_timer_on_original_LastTransitionAt_not_on_subscribe_time() {
        (IRenderedComponent<FcLifecycleWrapper> cut, Action<CommandLifecycleTransition> push, FakeTimeProvider time) = RenderWrapperWithFakeTime();
        // Original transition anchor = T-3s relative to subscribe.
        DateTimeOffset originalAnchor = time.GetUtcNow().AddSeconds(-3);
        push(TransitionAt(CommandLifecycleState.Acknowledged, CommandLifecycleState.Syncing, originalAnchor));

        // No time advance — wrapper must already be past StillSyncingThresholdMs because elapsed = 3 s.
        cut.WaitForAssertion(() => {
            cut.Find("fluent-badge").TextContent.ShouldContain("Still syncing", Case.Insensitive);
            cut.FindAll("[data-testid='fc-action-prompt']").ShouldBeEmpty("3 s elapsed is under TimeoutActionThresholdMs (10 s)");
        });
    }

    [Fact(Skip = "TDD RED — Story 2-4 Task 2.4 / D19 single-writer invariant: Confirmed while in ActionPrompt immediately resolves to success, no dangling pulse.")]
    public void Confirmed_while_in_ActionPrompt_phase_immediately_resolves_to_success_message_bar_no_dangling_pulse() {
        (IRenderedComponent<FcLifecycleWrapper> cut, Action<CommandLifecycleTransition> push, FakeTimeProvider time) = RenderWrapperWithFakeTime();
        DateTimeOffset syncAt = time.GetUtcNow();
        push(TransitionAt(CommandLifecycleState.Acknowledged, CommandLifecycleState.Syncing, syncAt));
        time.Advance(TimeSpan.FromMilliseconds(10_500));
        cut.WaitForState(() => cut.Markup.Contains("taking longer", StringComparison.OrdinalIgnoreCase));

        push(TransitionAt(CommandLifecycleState.Syncing, CommandLifecycleState.Confirmed, time.GetUtcNow()));

        cut.WaitForAssertion(() => {
            cut.Markup.ShouldContain("Submission confirmed", Case.Insensitive);
            cut.Find(".fc-lifecycle-wrapper").GetAttribute("class")!.ShouldNotContain("fc-lifecycle-pulse");
            cut.FindAll("[data-testid='fc-action-prompt']").ShouldBeEmpty();
        });
    }
}
