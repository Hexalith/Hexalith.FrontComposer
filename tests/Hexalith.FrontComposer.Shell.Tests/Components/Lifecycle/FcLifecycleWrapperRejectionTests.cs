using Bunit;

using Hexalith.FrontComposer.Contracts.Communication;
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
        (IRenderedComponent<FcLifecycleWrapper>? cut, Action<CommandLifecycleTransition>? push) = RenderWrapperWithLiveService(
            rejectionMessage: "Inventory insufficient. Order returned to Pending.",
            rejectionTitle: "Approval failed");

        push(RejectedNow());

        cut.Markup.ShouldContain("Approval failed");
        cut.Markup.ShouldContain("Inventory insufficient. Order returned to Pending.");
    }

    [Fact]
    public void Rejection_falls_back_to_generic_title_when_no_title_provided() {
        (IRenderedComponent<FcLifecycleWrapper>? cut, Action<CommandLifecycleTransition>? push) = RenderWrapperWithLiveService(rejectionMessage: "Domain rule X violated.");

        push(RejectedNow());

        cut.Markup.ShouldContain("Submission rejected");
    }

    [Fact]
    public void Rejection_body_is_plain_text_not_markup() {
        (IRenderedComponent<FcLifecycleWrapper>? cut, Action<CommandLifecycleTransition>? push) = RenderWrapperWithLiveService(rejectionMessage: "<script>alert('xss')</script>");

        push(RejectedNow());

        cut.Markup.ShouldContain("&lt;script&gt;");
        cut.Markup.ShouldNotContain("<script>alert", Case.Sensitive);
    }

    [Fact]
    public void Rejection_typed_fields_render_as_plain_text() {
        CommandRejectionDetails details = new(
            ErrorCode: "<E409>",
            ReasonCategory: "Inventory",
            SuggestedAction: "Lower quantity",
            DocsCode: "FC-CMD-409");
        (IRenderedComponent<FcLifecycleWrapper>? cut, Action<CommandLifecycleTransition>? push) = RenderWrapperWithLiveService(
            rejectionMessage: "Rejected.",
            rejectionDetails: details);

        push(RejectedNow());

        cut.Markup.ShouldContain("Error code");
        cut.Markup.ShouldContain("&lt;E409&gt;");
        cut.Markup.ShouldContain("Reason category");
        cut.Markup.ShouldContain("Inventory");
        cut.Markup.ShouldContain("Suggested action");
        cut.Markup.ShouldContain("Lower quantity");
        cut.Markup.ShouldContain("Docs code");
        cut.Markup.ShouldContain("FC-CMD-409");
        cut.Markup.ShouldNotContain("<E409>", Case.Sensitive);
    }

    [Fact]
    public void Rejection_typed_fields_use_fallback_text_when_values_missing() {
        var details = CommandRejectionDetails.FromOptional(
            errorCode: null,
            reasonCategory: null,
            suggestedAction: null,
            docsCode: null,
            fallbackSuggestedAction: null);
        (IRenderedComponent<FcLifecycleWrapper>? cut, Action<CommandLifecycleTransition>? push) = RenderWrapperWithLiveService(
            rejectionMessage: "Rejected.",
            rejectionDetails: details);

        push(RejectedNow());

        cut.Markup.ShouldContain(CommandRejectionDetails.UnknownErrorCode);
        cut.Markup.ShouldContain(CommandRejectionDetails.UnknownReasonCategory);
        cut.Markup.ShouldContain(CommandRejectionDetails.UnknownSuggestedAction);
        cut.Markup.ShouldContain(CommandRejectionDetails.UnknownDocsCode);
    }

    [Fact]
    public async Task Rejection_bar_has_no_auto_dismiss_regression() {
        (IRenderedComponent<FcLifecycleWrapper>? cut, Action<CommandLifecycleTransition>? push) = RenderWrapperWithLiveService(rejectionMessage: "Rejected.");

        push(RejectedNow());
        cut.Markup.ShouldContain("fc-rejected");

        FakeTime.Advance(TimeSpan.FromMinutes(10));
        await cut.InvokeAsync(() => { });

        cut.Markup.ShouldContain("fc-rejected", customMessage: "D17 — rejected bar persists without auto-dismiss.");
    }

    [Fact]
    public void Rejection_fallback_copy_when_message_null() {
        (IRenderedComponent<FcLifecycleWrapper>? cut, Action<CommandLifecycleTransition>? push) = RenderWrapperWithLiveService();

        push(RejectedNow());

        cut.Markup.ShouldContain("The command was rejected");
    }

    private CommandLifecycleTransition RejectedNow()
        => TransitionAt(CommandLifecycleState.Syncing, CommandLifecycleState.Rejected, FakeTime.GetUtcNow());
}
