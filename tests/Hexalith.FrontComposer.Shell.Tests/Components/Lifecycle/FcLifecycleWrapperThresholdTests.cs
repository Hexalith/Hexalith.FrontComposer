using Bunit;

using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Shell.Components.Lifecycle;

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
}
