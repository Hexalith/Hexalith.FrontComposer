using Bunit;

using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Shell.Components.Lifecycle;

using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Lifecycle;

/// <summary>
/// Story 2-4 accessibility tests. Covers AC1/AC4/AC5 aria-live politeness escalation, AC8
/// prefers-reduced-motion CSS opt-out, and focus-ring preservation (UX-DR49).
/// </summary>
public sealed class FcLifecycleWrapperA11yTests : LifecycleWrapperTestBase {
    [Fact]
    public void Live_region_role_is_status_when_Submitting_or_StillSyncing() {
        (IRenderedComponent<FcLifecycleWrapper> cut, Action<CommandLifecycleTransition> push, FakeTimeProvider time) = RenderWrapperWithFakeTime();

        push(TransitionAt(CommandLifecycleState.Idle, CommandLifecycleState.Submitting, time.GetUtcNow()));
        cut.Find("[role='status']").GetAttribute("aria-live").ShouldBe("polite");

        push(TransitionAt(CommandLifecycleState.Submitting, CommandLifecycleState.Acknowledged, time.GetUtcNow()));
        push(TransitionAt(CommandLifecycleState.Acknowledged, CommandLifecycleState.Syncing, time.GetUtcNow()));
        time.Advance(TimeSpan.FromMilliseconds(2_500));
        cut.WaitForAssertion(() => cut.Find("[role='status']").GetAttribute("aria-live").ShouldBe("polite"));
    }

    [Fact]
    public void Live_region_role_is_alert_when_Rejected_or_ActionPrompt() {
        (IRenderedComponent<FcLifecycleWrapper> cut, Action<CommandLifecycleTransition> push, FakeTimeProvider time) = RenderWrapperWithFakeTime();

        push(TransitionAt(CommandLifecycleState.Syncing, CommandLifecycleState.Rejected, time.GetUtcNow()));
        cut.Find("[data-fc-phase='rejected']").GetAttribute("role").ShouldBe("alert");
        cut.Find("[data-fc-phase='rejected']").GetAttribute("aria-live").ShouldBe("assertive");

        // Re-arm for action prompt path.
        push(TransitionAt(CommandLifecycleState.Rejected, CommandLifecycleState.Idle, time.GetUtcNow()));
        push(TransitionAt(CommandLifecycleState.Idle, CommandLifecycleState.Submitting, time.GetUtcNow()));
        push(TransitionAt(CommandLifecycleState.Submitting, CommandLifecycleState.Acknowledged, time.GetUtcNow()));
        push(TransitionAt(CommandLifecycleState.Acknowledged, CommandLifecycleState.Syncing, time.GetUtcNow()));
        time.Advance(TimeSpan.FromMilliseconds(10_500));
        cut.WaitForAssertion(() => {
            cut.Find("[data-fc-phase='action-prompt']").GetAttribute("role").ShouldBe("alert");
            cut.Find("[data-fc-phase='action-prompt']").GetAttribute("aria-live").ShouldBe("assertive");
        });
    }

    [Fact]
    public async Task Focus_ring_preserved_on_descendant_focusable_during_pulse_phase() {
        ILifecycleStateService service = Substitute.For<ILifecycleStateService>();
        Action<CommandLifecycleTransition>? captured = null;
        _ = service.Subscribe(Arg.Any<string>(), Arg.Do<Action<CommandLifecycleTransition>>(cb => captured = cb))
            .Returns(Substitute.For<IDisposable>());
        RegisterLifecycleService(service);

        IRenderedComponent<FcLifecycleWrapper> cut = Render<FcLifecycleWrapper>(p => p
            .Add(c => c.CorrelationId, DefaultCorrelationId)
            .AddChildContent("<button data-testid='inner-focusable'>Inner</button>"));

        DateTimeOffset anchor = FakeTime.GetUtcNow();
        await cut.InvokeAsync(() => captured!(TransitionAt(CommandLifecycleState.Acknowledged, CommandLifecycleState.Syncing, anchor)));
        FakeTime.Advance(TimeSpan.FromMilliseconds(400));

        cut.WaitForAssertion(() => {
            cut.Find(".fc-lifecycle-wrapper").GetAttribute("class")!.ShouldContain("fc-lifecycle-pulse");
            // Pulse class must NOT be applied to descendants — focus-ring suppression would be a UX-DR49 violation.
            string? innerClass = cut.Find("button[data-testid='inner-focusable']").GetAttribute("class");
            (innerClass is null || !innerClass.Contains("fc-lifecycle-pulse")).ShouldBeTrue(
                "inner focusable button must not carry the pulse class; pulse lives on outer wrapper only (UX-DR49)");
        });
    }

    [Fact]
    public void Reduced_motion_media_query_present_in_scoped_css() {
        string cssPath = LocateScopedCssFile();
        string content = File.ReadAllText(cssPath);

        content.ShouldContain("@media (prefers-reduced-motion: reduce)", Case.Insensitive);
        content.ShouldContain("animation: none", Case.Insensitive);
        content.ShouldContain("outline: 2px solid", Case.Insensitive);
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
