using Bunit;

using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Shell.Components.Lifecycle;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Lifecycle;

/// <summary>
/// Story 2-5 Task 8.4 — rejection-branch coverage (AC1 / D4 / D5 / D14 / D17). Domain-language title,
/// plain-text body rendering, no auto-dismiss (Story 2-4 D17 regression).
/// </summary>
public sealed class FcLifecycleWrapperRejectionTests : LifecycleWrapperTestBase {
    [Fact]
    public void Rejection_renders_domain_language_title_when_provided() {
        var (cut, push) = RenderWrapperWithLiveService(
            rejectionMessage: "Inventory insufficient. Order returned to Pending.",
            rejectionTitle: "Approval failed");

        push(RejectedNow());

        cut.Markup.ShouldContain("Approval failed");
        cut.Markup.ShouldContain("Inventory insufficient. Order returned to Pending.");
    }

    [Fact]
    public void Rejection_falls_back_to_generic_title_when_no_title_provided() {
        var (cut, push) = RenderWrapperWithLiveService(rejectionMessage: "Domain rule X violated.");

        push(RejectedNow());

        cut.Markup.ShouldContain("Submission rejected");
    }

    [Fact]
    public void Rejection_body_is_plain_text_not_markup() {
        var (cut, push) = RenderWrapperWithLiveService(rejectionMessage: "<script>alert('xss')</script>");

        push(RejectedNow());

        cut.Markup.ShouldContain("&lt;script&gt;");
        cut.Markup.ShouldNotContain("<script>alert", Case.Sensitive);
    }

    [Fact]
    public async Task Rejection_bar_has_no_auto_dismiss_regression() {
        var (cut, push) = RenderWrapperWithLiveService(rejectionMessage: "Rejected.");

        push(RejectedNow());
        cut.Markup.ShouldContain("fc-rejected");

        FakeTime.Advance(TimeSpan.FromMinutes(10));
        await cut.InvokeAsync(() => { });

        cut.Markup.ShouldContain("fc-rejected", customMessage: "D17 — rejected bar persists without auto-dismiss.");
    }

    [Fact]
    public void Rejection_fallback_copy_when_message_null() {
        var (cut, push) = RenderWrapperWithLiveService();

        push(RejectedNow());

        cut.Markup.ShouldContain("The command was rejected");
    }

    private CommandLifecycleTransition RejectedNow()
        => TransitionAt(CommandLifecycleState.Syncing, CommandLifecycleState.Rejected, FakeTime.GetUtcNow());
}
