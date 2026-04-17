using AngleSharp.Dom;

using Bunit;

using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Shell.Components.Lifecycle;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Lifecycle;

/// <summary>
/// Story 2-4 bUnit tests covering AC1 (Submitting live-region), AC6 (Confirmed auto-dismiss),
/// AC7 (Rejected no-dismiss), AC5 (action-prompt Start-over navigation), and CorrelationId
/// re-subscription (Task 2.5 D15 / OnParametersSet).
/// </summary>
public sealed class FcLifecycleWrapperTests : LifecycleWrapperTestBase {
    [Fact]
    public void Idle_state_renders_only_child_content_no_aria_live() {
        IRenderedComponent<FcLifecycleWrapper> cut = RenderWrapperWithStubService();

        cut.Markup.ShouldContain("fc-lifecycle-wrapper");
        cut.Markup.ShouldContain("child-content-marker");
        cut.FindAll("[role='status']").ShouldBeEmpty();
        cut.FindAll("[role='alert']").ShouldBeEmpty();
    }

    [Fact]
    public void Submitting_state_renders_polite_aria_live_with_submitting_announcement() {
        (IRenderedComponent<FcLifecycleWrapper> cut, Action<CommandLifecycleTransition> push) = RenderWrapperWithLiveService();
        push(Transition(CommandLifecycleState.Idle, CommandLifecycleState.Submitting));

        IElement region = cut.Find("[role='status']");
        region.GetAttribute("aria-live").ShouldBe("polite");
        region.TextContent.ShouldContain("Submitting", Case.Insensitive);
    }

    [Fact]
    public void Submitting_state_does_not_apply_pulse_class() {
        (IRenderedComponent<FcLifecycleWrapper> cut, Action<CommandLifecycleTransition> push) = RenderWrapperWithLiveService();
        push(Transition(CommandLifecycleState.Idle, CommandLifecycleState.Submitting));

        cut.Find(".fc-lifecycle-wrapper").GetAttribute("class")!.ShouldNotContain("fc-lifecycle-pulse");
    }

    [Fact]
    public void Acknowledged_state_transitions_timer_phase_to_NoPulse() {
        (IRenderedComponent<FcLifecycleWrapper> cut, Action<CommandLifecycleTransition> push) = RenderWrapperWithLiveService();
        push(Transition(CommandLifecycleState.Submitting, CommandLifecycleState.Acknowledged));

        cut.Find(".fc-lifecycle-wrapper").GetAttribute("class")!.ShouldNotContain("fc-lifecycle-pulse");
        cut.Markup.ShouldNotContain("Still syncing", Case.Insensitive);
        cut.FindAll("[data-testid='fc-action-prompt']").ShouldBeEmpty();
    }

    [Fact]
    public void Syncing_state_applies_pulse_class_once_phase_reaches_Pulse() {
        (IRenderedComponent<FcLifecycleWrapper> cut, Action<CommandLifecycleTransition> push, FakeTimeProvider time) = RenderWrapperWithFakeTime();
        DateTimeOffset anchor = time.GetUtcNow();
        push(TransitionAt(CommandLifecycleState.Acknowledged, CommandLifecycleState.Syncing, anchor));
        time.Advance(TimeSpan.FromMilliseconds(400));

        cut.WaitForAssertion(() => cut.Find(".fc-lifecycle-wrapper").GetAttribute("class")!.ShouldContain("fc-lifecycle-pulse"));
    }

    [Fact]
    public void Syncing_state_renders_still_syncing_badge_at_StillSyncing_phase() {
        (IRenderedComponent<FcLifecycleWrapper> cut, Action<CommandLifecycleTransition> push, FakeTimeProvider time) = RenderWrapperWithFakeTime();
        DateTimeOffset anchor = time.GetUtcNow();
        push(TransitionAt(CommandLifecycleState.Acknowledged, CommandLifecycleState.Syncing, anchor));
        time.Advance(TimeSpan.FromMilliseconds(2_100));

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Still syncing", Case.Insensitive));
    }

    [Fact]
    public void Syncing_state_renders_action_prompt_message_bar_at_ActionPrompt_phase() {
        (IRenderedComponent<FcLifecycleWrapper> cut, Action<CommandLifecycleTransition> push, FakeTimeProvider time) = RenderWrapperWithFakeTime();
        DateTimeOffset anchor = time.GetUtcNow();
        push(TransitionAt(CommandLifecycleState.Acknowledged, CommandLifecycleState.Syncing, anchor));
        time.Advance(TimeSpan.FromMilliseconds(10_050));

        cut.WaitForAssertion(() => {
            cut.Markup.ShouldContain("Action needed", Case.Insensitive);
            cut.Markup.ShouldContain("Start over", Case.Insensitive);
        });
    }

    [Fact]
    public void ActionPrompt_Start_over_button_calls_NavigateTo_forceLoad_true() {
        (IRenderedComponent<FcLifecycleWrapper> cut, Action<CommandLifecycleTransition> push, FakeTimeProvider time) = RenderWrapperWithFakeTime();
        TestNavigationManager nav = (TestNavigationManager)Services.GetRequiredService<NavigationManager>();
        DateTimeOffset anchor = time.GetUtcNow();
        push(TransitionAt(CommandLifecycleState.Acknowledged, CommandLifecycleState.Syncing, anchor));
        time.Advance(TimeSpan.FromMilliseconds(10_050));

        cut.WaitForState(() => cut.FindAll("[data-testid='fc-refresh-action']").Count > 0);
        cut.Find("[data-testid='fc-refresh-action']").Click();

        nav.LastNavigateCall.ShouldNotBeNull();
        nav.LastNavigateCall!.Value.ForceLoad.ShouldBeTrue();
    }

    [Fact]
    public void Confirmed_state_renders_success_message_bar_with_polite_aria_live() {
        (IRenderedComponent<FcLifecycleWrapper> cut, Action<CommandLifecycleTransition> push) = RenderWrapperWithLiveService();
        push(Transition(CommandLifecycleState.Syncing, CommandLifecycleState.Confirmed));

        IElement region = cut.Find("[data-fc-phase='confirmed']");
        region.GetAttribute("aria-live")!.ShouldBe("polite");
        region.GetAttribute("role")!.ShouldBe("status");
        cut.Markup.ShouldContain("Submission confirmed", Case.Insensitive);
    }

    [Fact]
    public void Confirmed_state_auto_dismisses_after_ConfirmedToastDurationMs() {
        (IRenderedComponent<FcLifecycleWrapper> cut, Action<CommandLifecycleTransition> push, FakeTimeProvider time) = RenderWrapperWithFakeTime();
        push(TransitionAt(CommandLifecycleState.Syncing, CommandLifecycleState.Confirmed, time.GetUtcNow()));
        cut.WaitForState(() => cut.Markup.Contains("Submission confirmed", StringComparison.OrdinalIgnoreCase));

        time.Advance(TimeSpan.FromMilliseconds(5_100));

        cut.WaitForAssertion(() => cut.Markup.ShouldNotContain("Submission confirmed", Case.Insensitive));
    }

    [Fact]
    public void Rejected_state_renders_danger_message_bar_with_assertive_aria_live_and_no_auto_dismiss() {
        (IRenderedComponent<FcLifecycleWrapper> cut, Action<CommandLifecycleTransition> push, FakeTimeProvider time) = RenderWrapperWithFakeTime();
        push(TransitionAt(CommandLifecycleState.Syncing, CommandLifecycleState.Rejected, time.GetUtcNow()));

        IElement region = cut.Find("[data-fc-phase='rejected']");
        region.GetAttribute("aria-live")!.ShouldBe("assertive");
        region.GetAttribute("role")!.ShouldBe("alert");

        time.Advance(TimeSpan.FromMinutes(10));

        cut.Markup.ShouldContain("Submission rejected", Case.Insensitive);
    }

    [Fact]
    public void Rejected_state_uses_default_fallback_message_when_RejectionMessage_parameter_null() {
        (IRenderedComponent<FcLifecycleWrapper> cut, Action<CommandLifecycleTransition> push) = RenderWrapperWithLiveService(rejectionMessage: null);
        push(Transition(CommandLifecycleState.Syncing, CommandLifecycleState.Rejected));

        cut.Markup.ShouldContain("The command was rejected", Case.Insensitive);
    }

    [Fact]
    public void Rejected_state_uses_parameter_message_when_RejectionMessage_populated() {
        const string domainCopy = "Approval failed: insufficient inventory.";
        (IRenderedComponent<FcLifecycleWrapper> cut, Action<CommandLifecycleTransition> push) = RenderWrapperWithLiveService(rejectionMessage: domainCopy);
        push(Transition(CommandLifecycleState.Syncing, CommandLifecycleState.Rejected));

        cut.Markup.ShouldContain(domainCopy);
    }

    [Fact]
    public void CorrelationId_change_disposes_old_subscription_and_resubscribes_to_new_id() {
        ILifecycleStateService service = Substitute.For<ILifecycleStateService>();
        IDisposable firstHandle = Substitute.For<IDisposable>();
        IDisposable secondHandle = Substitute.For<IDisposable>();
        service.Subscribe("corr-a", Arg.Any<Action<CommandLifecycleTransition>>()).Returns(firstHandle);
        service.Subscribe("corr-b", Arg.Any<Action<CommandLifecycleTransition>>()).Returns(secondHandle);
        RegisterLifecycleService(service);

        IRenderedComponent<FcLifecycleWrapper> cut = Render<FcLifecycleWrapper>(p => p
            .Add(c => c.CorrelationId, "corr-a")
            .AddChildContent("<span class='child-content-marker'/>"));
        cut.Render(p => p.Add(c => c.CorrelationId, "corr-b"));

        firstHandle.Received(1).Dispose();
        service.Received(1).Subscribe("corr-b", Arg.Any<Action<CommandLifecycleTransition>>());
    }
}
