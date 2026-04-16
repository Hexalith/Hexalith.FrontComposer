using Bunit;

using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Shell.Components.Lifecycle;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Lifecycle;

/// <summary>
/// TDD RED-phase accessibility markup tests for Story 2-4. Covers AC1/AC4/AC5 aria-live
/// politeness escalation, AC8 prefers-reduced-motion CSS opt-out, and focus-ring preservation
/// (UX-DR49). Real axe-core DOM walking lands at the E2E browser layer per Story 10-2.
/// </summary>
public sealed class FcLifecycleWrapperA11yTests : LifecycleWrapperTestBase {
    [Fact(Skip = "TDD RED — Story 2-4 Task 2.1 / AC1+AC4: Submitting and Still-syncing live regions must have role=status (polite).")]
    public void Live_region_role_is_status_when_Submitting_or_Syncing() {
        (IRenderedComponent<FcLifecycleWrapper> cut, Action<CommandLifecycleTransition> push, FakeTimeProvider time) = RenderWrapperWithFakeTime();

        push(TransitionAt(CommandLifecycleState.Idle, CommandLifecycleState.Submitting, time.GetUtcNow()));
        cut.Find("[role='status']").GetAttribute("aria-live").ShouldBe("polite");

        push(TransitionAt(CommandLifecycleState.Submitting, CommandLifecycleState.Acknowledged, time.GetUtcNow()));
        push(TransitionAt(CommandLifecycleState.Acknowledged, CommandLifecycleState.Syncing, time.GetUtcNow()));
        time.Advance(TimeSpan.FromMilliseconds(2_500));
        cut.WaitForAssertion(() => cut.Find("[role='status']").GetAttribute("aria-live").ShouldBe("polite"));
    }

    [Fact(Skip = "TDD RED — Story 2-4 Task 2.1 / AC5+AC7: action-prompt and Rejected live regions must have role=alert (assertive).")]
    public void Live_region_role_is_alert_when_Rejected_or_ActionPrompt() {
        (IRenderedComponent<FcLifecycleWrapper> cut, Action<CommandLifecycleTransition> push, FakeTimeProvider time) = RenderWrapperWithFakeTime();

        push(TransitionAt(CommandLifecycleState.Syncing, CommandLifecycleState.Rejected, time.GetUtcNow()));
        cut.Find("[role='alert'][data-fc-phase='rejected']").GetAttribute("aria-live").ShouldBe("assertive");

        push(TransitionAt(CommandLifecycleState.Rejected, CommandLifecycleState.Idle, time.GetUtcNow()));
        push(TransitionAt(CommandLifecycleState.Idle, CommandLifecycleState.Submitting, time.GetUtcNow()));
        push(TransitionAt(CommandLifecycleState.Submitting, CommandLifecycleState.Acknowledged, time.GetUtcNow()));
        push(TransitionAt(CommandLifecycleState.Acknowledged, CommandLifecycleState.Syncing, time.GetUtcNow()));
        time.Advance(TimeSpan.FromMilliseconds(10_500));
        cut.WaitForAssertion(() => cut.Find("[role='alert'][data-fc-phase='action-prompt']").GetAttribute("aria-live").ShouldBe("assertive"));
    }

    [Fact(Skip = "TDD RED — Story 2-4 Task 2.2 / UX-DR49: pulse class applies ONLY to outer wrapper — focus ring on descendant button must not be dimmed.")]
    public async Task Focus_ring_preserved_on_descendant_focusable_during_pulse_phase() {
        ILifecycleStateService service = Substitute.For<ILifecycleStateService>();
        Action<CommandLifecycleTransition>? captured = null;
        _ = service.Subscribe(Arg.Any<string>(), Arg.Do<Action<CommandLifecycleTransition>>(cb => captured = cb))
            .Returns(Substitute.For<IDisposable>());
        RegisterLifecycleService(service);

        IRenderedComponent<FcLifecycleWrapper> cut = Render<FcLifecycleWrapper>(p => p
            .Add(c => c.CorrelationId, "corr-focus")
            .AddChildContent("<button data-testid='inner-focusable'>Inner</button>"));

        DateTimeOffset anchor = FakeTime.GetUtcNow();
        await cut.InvokeAsync(() =>
        {
            captured!(TransitionAt(CommandLifecycleState.Acknowledged, CommandLifecycleState.Syncing, anchor));
            return Task.CompletedTask;
        });
        FakeTime.Advance(TimeSpan.FromMilliseconds(500));

        cut.WaitForAssertion(() => {
            cut.Find(".fc-lifecycle-wrapper").GetAttribute("class")!.ShouldContain("fc-lifecycle-pulse");
            // Pulse class must NOT be applied to descendants — focus-ring suppression would be a UX-DR49 violation.
            cut.Find("button[data-testid='inner-focusable']").GetAttribute("class")!.ShouldNotContain("fc-lifecycle-pulse");
        });
    }

    [Fact(Skip = "TDD RED — Story 2-4 Task 2.2 / AC8: scoped .razor.css must expose a prefers-reduced-motion media query that neutralises the pulse animation.")]
    public void Reduced_motion_media_query_present_in_scoped_css() {
        // Task 2.2 creates FcLifecycleWrapper.razor.css alongside the component. This test parses the
        // raw source file (not runtime DOM, since bUnit does not materialize scoped-CSS output) and
        // asserts the reduced-motion opt-out landed per UX-DR49.
        string cssPath = LocateScopedCssFile();
        string content = File.ReadAllText(cssPath);

        content.ShouldContain("@media (prefers-reduced-motion: reduce)", Case.Insensitive);
        content.ShouldContain("animation: none", Case.Insensitive);
    }

    private static string LocateScopedCssFile() {
        string here = AppContext.BaseDirectory;
        DirectoryInfo? cursor = new(here);
        while (cursor is not null) {
            string candidate = Path.Combine(cursor.FullName, "src", "Hexalith.FrontComposer.Shell", "Components", "Lifecycle", "FcLifecycleWrapper.razor.css");
            if (File.Exists(candidate)) {
                return candidate;
            }
            cursor = cursor.Parent;
        }

        throw new FileNotFoundException("FcLifecycleWrapper.razor.css not found — Task 2.2 must create it.");
    }
}
