using AngleSharp.Dom;

using Bunit;

using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Shell.Components.Lifecycle;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Lifecycle;

/// <summary>
/// TDD RED-phase bUnit tests authored via <c>bmad-testarch-atdd</c> for Story 2-4.
/// Covers AC1 (Submitting live-region), AC6 (Confirmed auto-dismiss), AC7 (Rejected no-dismiss)
/// plus CorrelationId re-subscription (Task 2.5 D15 / OnParametersSet).
/// </summary>
/// <remarks>
/// Every <see cref="FactAttribute"/> carries <c>Skip = "TDD RED — Story 2-4 Task N.N"</c>.
/// When the matching task lands, the dev agent removes the Skip argument to activate the test.
/// See <c>_bmad-output/test-artifacts/atdd-checklist-2-4.md</c>.
/// </remarks>
public sealed class FcLifecycleWrapperTests : LifecycleWrapperTestBase {
    private const string CorrelationId = "corr-test-001";

    [Fact(Skip = "TDD RED — Story 2-4 Task 2.1: wrapper must render only ChildContent in Idle, no aria-live region.")]
    public void Idle_state_renders_only_child_content_no_aria_live() {
        IRenderedComponent<FcLifecycleWrapper> cut = RenderWrapperWithStubService();

        cut.Markup.ShouldContain("fc-lifecycle-wrapper");
        cut.Markup.ShouldContain("child-content-marker");
        cut.FindAll("[role='status']").ShouldBeEmpty();
        cut.FindAll("[role='alert']").ShouldBeEmpty();
    }

    [Fact(Skip = "TDD RED — Story 2-4 Task 2.1 / AC1: Submitting renders polite aria-live with localized Submitting announcement.")]
    public void Submitting_state_renders_polite_aria_live_with_submitting_announcement() {
        (IRenderedComponent<FcLifecycleWrapper> cut, Action<CommandLifecycleTransition> push) = RenderWrapperWithLiveService();
        push(Transition(CommandLifecycleState.Idle, CommandLifecycleState.Submitting));

        IElement region = cut.Find("[role='status']");
        region.GetAttribute("aria-live").ShouldBe("polite");
        region.TextContent.ShouldContain("Submitting", Case.Insensitive);
    }

    [Fact(Skip = "TDD RED — Story 2-4 Task 2.1 / AC1: Submitting must NOT apply the pulse CSS class yet.")]
    public void Submitting_state_does_not_apply_pulse_class() {
        (IRenderedComponent<FcLifecycleWrapper> cut, Action<CommandLifecycleTransition> push) = RenderWrapperWithLiveService();
        push(Transition(CommandLifecycleState.Idle, CommandLifecycleState.Submitting));

        cut.Find(".fc-lifecycle-wrapper").GetAttribute("class")!.ShouldNotContain("fc-lifecycle-pulse");
    }

    [Fact(Skip = "TDD RED — Story 2-4 Task 2.1 / D19: Acknowledged arrival starts the threshold timer in NoPulse phase.")]
    public void Acknowledged_state_transitions_timer_phase_to_NoPulse() {
        (IRenderedComponent<FcLifecycleWrapper> cut, Action<CommandLifecycleTransition> push) = RenderWrapperWithLiveService();
        push(Transition(CommandLifecycleState.Submitting, CommandLifecycleState.Acknowledged));

        cut.Find(".fc-lifecycle-wrapper").GetAttribute("class")!.ShouldNotContain("fc-lifecycle-pulse");
        cut.FindAll("fluent-badge").ShouldBeEmpty();
        cut.FindAll("[data-testid='fc-action-prompt']").ShouldBeEmpty();
    }

    [Fact(Skip = "TDD RED — Story 2-4 Task 2.2 / AC3: pulse CSS class applies once phase reaches Pulse (>= SyncPulseThresholdMs).")]
    public void Syncing_state_applies_pulse_class_once_phase_reaches_Pulse() {
        (IRenderedComponent<FcLifecycleWrapper> cut, Action<CommandLifecycleTransition> push, FakeTimeProvider time) = RenderWrapperWithFakeTime();
        DateTimeOffset anchor = time.GetUtcNow();
        push(TransitionAt(CommandLifecycleState.Acknowledged, CommandLifecycleState.Syncing, anchor));
        time.Advance(TimeSpan.FromMilliseconds(400));

        cut.WaitForAssertion(() => cut.Find(".fc-lifecycle-wrapper").GetAttribute("class")!.ShouldContain("fc-lifecycle-pulse"));
    }

    [Fact(Skip = "TDD RED — Story 2-4 Task 2.2 / AC4: still-syncing badge renders at StillSyncingThresholdMs (default 2s).")]
    public void Syncing_state_renders_still_syncing_badge_at_StillSyncing_phase() {
        (IRenderedComponent<FcLifecycleWrapper> cut, Action<CommandLifecycleTransition> push, FakeTimeProvider time) = RenderWrapperWithFakeTime();
        DateTimeOffset anchor = time.GetUtcNow();
        push(TransitionAt(CommandLifecycleState.Acknowledged, CommandLifecycleState.Syncing, anchor));
        time.Advance(TimeSpan.FromMilliseconds(2_100));

        cut.WaitForAssertion(() => {
            cut.Markup.ShouldContain("still syncing", Case.Insensitive);
            cut.Find("fluent-badge").TextContent.ShouldContain("Still syncing", Case.Insensitive);
        });
    }

    [Fact(Skip = "TDD RED — Story 2-4 Task 2.2 / AC5: action-prompt FluentMessageBar (Warning) renders at TimeoutActionThresholdMs (default 10s).")]
    public void Syncing_state_renders_action_prompt_message_bar_at_ActionPrompt_phase() {
        (IRenderedComponent<FcLifecycleWrapper> cut, Action<CommandLifecycleTransition> push, FakeTimeProvider time) = RenderWrapperWithFakeTime();
        DateTimeOffset anchor = time.GetUtcNow();
        push(TransitionAt(CommandLifecycleState.Acknowledged, CommandLifecycleState.Syncing, anchor));
        time.Advance(TimeSpan.FromMilliseconds(10_050));

        cut.WaitForAssertion(() => {
            cut.Markup.ShouldContain("taking longer than expected", Case.Insensitive);
            cut.Markup.ShouldContain("Refresh page", Case.Insensitive);
        });
    }

    [Fact(Skip = "TDD RED — Story 2-4 Task 2.5 / AC5 / ADR-022: Refresh button calls NavigationManager.NavigateTo(Uri, forceLoad: true).")]
    public void ActionPrompt_refresh_button_calls_NavigateTo_forceLoad_true() {
        (IRenderedComponent<FcLifecycleWrapper> cut, Action<CommandLifecycleTransition> push, FakeTimeProvider time) = RenderWrapperWithFakeTime();
        TestNavigationManager nav = (TestNavigationManager)Services.GetRequiredService<NavigationManager>();
        DateTimeOffset anchor = time.GetUtcNow();
        push(TransitionAt(CommandLifecycleState.Acknowledged, CommandLifecycleState.Syncing, anchor));
        time.Advance(TimeSpan.FromMilliseconds(10_050));

        cut.WaitForState(() => cut.FindAll("fluent-button[data-testid='fc-refresh-action']").Count > 0);
        cut.Find("fluent-button[data-testid='fc-refresh-action']").Click();

        nav.LastNavigateCall.ShouldNotBeNull();
        nav.LastNavigateCall!.Value.ForceLoad.ShouldBeTrue();
    }

    [Fact(Skip = "TDD RED — Story 2-4 Task 2.1 / AC6: Confirmed renders success FluentMessageBar with polite aria-live.")]
    public void Confirmed_state_renders_success_message_bar_with_polite_aria_live() {
        (IRenderedComponent<FcLifecycleWrapper> cut, Action<CommandLifecycleTransition> push) = RenderWrapperWithLiveService();
        push(Transition(CommandLifecycleState.Syncing, CommandLifecycleState.Confirmed));

        IElement region = cut.Find("[role='status'][data-fc-phase='confirmed']");
        region.GetAttribute("aria-live")!.ShouldBe("polite");
        cut.Markup.ShouldContain("Submission confirmed", Case.Insensitive);
        string intent = cut.Find("fluent-message-bar").GetAttribute("intent")!;
        intent.Equals("success", StringComparison.OrdinalIgnoreCase).ShouldBeTrue();
    }

    [Fact(Skip = "TDD RED — Story 2-4 Task 2.2 / AC6: Confirmed auto-dismiss after ConfirmedToastDurationMs (default 5s).")]
    public void Confirmed_state_auto_dismisses_after_ConfirmedToastDurationMs() {
        (IRenderedComponent<FcLifecycleWrapper> cut, Action<CommandLifecycleTransition> push, FakeTimeProvider time) = RenderWrapperWithFakeTime();
        push(TransitionAt(CommandLifecycleState.Syncing, CommandLifecycleState.Confirmed, time.GetUtcNow()));
        cut.WaitForState(() => cut.FindAll("fluent-message-bar").Count > 0);

        time.Advance(TimeSpan.FromMilliseconds(5_100));

        cut.WaitForAssertion(() => cut.FindAll("fluent-message-bar").ShouldBeEmpty());
    }

    [Fact(Skip = "TDD RED — Story 2-4 Task 2.1 / AC7 / NFR46: Rejected renders danger message bar with assertive aria-live and NO auto-dismiss.")]
    public void Rejected_state_renders_danger_message_bar_with_assertive_aria_live_and_no_auto_dismiss() {
        (IRenderedComponent<FcLifecycleWrapper> cut, Action<CommandLifecycleTransition> push, FakeTimeProvider time) = RenderWrapperWithFakeTime();
        push(TransitionAt(CommandLifecycleState.Syncing, CommandLifecycleState.Rejected, time.GetUtcNow()));

        IElement region = cut.Find("[role='alert'][data-fc-phase='rejected']");
        region.GetAttribute("aria-live")!.ShouldBe("assertive");
        string rejectedIntent = cut.Find("fluent-message-bar").GetAttribute("intent")!;
        rejectedIntent.Equals("error", StringComparison.OrdinalIgnoreCase).ShouldBeTrue();

        time.Advance(TimeSpan.FromMinutes(10));

        cut.FindAll("fluent-message-bar").Count.ShouldBe(1, "danger bar must not auto-dismiss per D17/NFR46");
    }

    [Fact(Skip = "TDD RED — Story 2-4 Task 2.1 / AC7: RejectionMessage=null → localized generic fallback copy is rendered.")]
    public void Rejected_state_uses_default_fallback_message_when_RejectionMessage_parameter_null() {
        (IRenderedComponent<FcLifecycleWrapper> cut, Action<CommandLifecycleTransition> push) = RenderWrapperWithLiveService(rejectionMessage: null);
        push(Transition(CommandLifecycleState.Syncing, CommandLifecycleState.Rejected));

        cut.Markup.ShouldContain("The command was rejected", Case.Insensitive);
    }

    [Fact(Skip = "TDD RED — Story 2-4 Task 2.1 / AC7: populated RejectionMessage parameter flows into message bar body.")]
    public void Rejected_state_uses_parameter_message_when_RejectionMessage_populated() {
        const string domainCopy = "Approval failed: insufficient inventory.";
        (IRenderedComponent<FcLifecycleWrapper> cut, Action<CommandLifecycleTransition> push) = RenderWrapperWithLiveService(rejectionMessage: domainCopy);
        push(Transition(CommandLifecycleState.Syncing, CommandLifecycleState.Rejected));

        cut.Markup.ShouldContain(domainCopy);
    }

    [Fact(Skip = "TDD RED — Story 2-4 Task 2.5 / D15: CorrelationId param change disposes old Subscribe handle and re-subscribes to new id.")]
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
