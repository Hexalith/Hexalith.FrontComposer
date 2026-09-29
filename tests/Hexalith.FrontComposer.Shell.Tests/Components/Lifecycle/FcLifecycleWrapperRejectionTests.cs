using Bunit;

using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Shell.Components.Lifecycle;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

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
        cut.Markup.ShouldContain("Documentation code");
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

    [Fact]
    public void MappedRejection_SuppressesGenericLiveAnnouncementAndRendersNonLiveRecovery() {
        (IRenderedComponent<FcLifecycleWrapper> cut, Action<CommandLifecycleTransition> push) = RenderWrapperWithLiveService(
            rejectionMessage: "Correct Quantity and retry.",
            mappedRejection: true);

        push(RejectedNow());

        cut.FindAll("[data-fc-phase='rejected']").ShouldBeEmpty();
        // VG5-01 — the mapped outcome suppresses the generic AM-14 rejection bar and its recovery actions.
        cut.FindAll("[data-testid='fc-rejected']").ShouldBeEmpty();
        cut.FindAll("[data-testid='fc-rejection-edit-retry']").ShouldBeEmpty();
        cut.FindAll("[data-testid='fc-rejection-return']").ShouldBeEmpty();
        cut.FindAll("[data-testid='fc-rejection-copy-reference']").ShouldBeEmpty();
        AngleSharp.Dom.IElement mapped = cut.Find("[data-testid='fc-rejected-mapped']");
        mapped.GetAttribute("role").ShouldNotBe("alert");
        mapped.GetAttribute("role").ShouldNotBe("status");
        mapped.HasAttribute("aria-live").ShouldBeFalse();
        mapped.TextContent.ShouldContain("Correct Quantity and retry.");
    }

    [Fact]
    public void UnmappedRejectionOffersEveryKeyboardRecoveryActionAfterTheMessage() {
        (IRenderedComponent<FcLifecycleWrapper> cut, Action<CommandLifecycleTransition> push) = RenderWrapperWithLiveService(
            rejectionMessage: "Order locked.",
            rejectionDetails: SupportDetails);

        push(RejectedNow());

        // VR-03 / AA5-02 — edit and retry, return, and copy the support reference follow the message in
        // tab order as native buttons; the bar's own dismiss control is the cancel path.
        AngleSharp.Dom.IElement bar = cut.Find("[data-testid='fc-rejected']");
        string[] actions = [.. bar.QuerySelectorAll("fluent-button[data-testid]").Select(button => button.GetAttribute("data-testid")!)];
        actions.ShouldBe(["fc-rejection-edit-retry", "fc-rejection-return", "fc-rejection-copy-reference"]);
        bar.QuerySelector("[data-testid='fc-rejection-return']")!.TextContent.Trim().ShouldBe("Return");
        bar.QuerySelector("[data-testid='fc-rejection-copy-reference']")!.TextContent.Trim().ShouldBe("Copy support reference");
        bar.QuerySelectorAll("fluent-button[disabled]").ShouldBeEmpty();
    }

    [Fact]
    public void UnmappedRejectionWithoutDetailsOffersNoCopyAction() {
        (IRenderedComponent<FcLifecycleWrapper> cut, Action<CommandLifecycleTransition> push) = RenderWrapperWithLiveService(rejectionMessage: "Rejected.");

        push(RejectedNow());

        _ = cut.Find("[data-testid='fc-rejection-return']");
        cut.FindAll("[data-testid='fc-rejection-copy-reference']").ShouldBeEmpty();
    }

    [Fact]
    public void ReturnGoesBackInTheBrowserHistoryWithoutNavigatingThroughTheWrapper() {
        (IRenderedComponent<FcLifecycleWrapper> cut, Action<CommandLifecycleTransition> push) = RenderWrapperWithLiveService(rejectionMessage: "Rejected.");
        push(RejectedNow());

        cut.Find("[data-testid='fc-rejection-return']").Click();

        // VR-03 — Return behaves like the browser Back button: the abandonment guard applies only to
        // in-app history navigation on a guarded form, and a back to another document is not guarded.
        cut.WaitForAssertion(() => JSInterop.VerifyInvoke("history.back", 1));
        ((TestNavigationManager)Services.GetRequiredService<NavigationManager>()).LastNavigateCall.ShouldBeNull();
    }

    [Fact]
    public void CopySupportReferenceCopiesOnlyTheSupportSafeCodes() {
        BunitJSModuleInterop clipboard = JSInterop.SetupModule(ClipboardModulePath);
        _ = clipboard.Setup<string>("copyToClipboard", _ => true).SetResult("Success");
        (IRenderedComponent<FcLifecycleWrapper> cut, Action<CommandLifecycleTransition> push) = RenderWrapperWithLiveService(
            rejectionMessage: "Order locked. Payload detail must never be copied.",
            rejectionDetails: SupportDetails);
        push(RejectedNow());

        cut.Find("[data-testid='fc-rejection-copy-reference']").Click();

        // VR-03 — the copied reference is exactly the error and documentation codes shown in the details.
        cut.WaitForAssertion(() => {
            JSRuntimeInvocation copy = clipboard.VerifyInvoke("copyToClipboard", 1).Single();
            copy.Arguments.ShouldBe(["Error code: ORDER_LOCKED, documentation code: FC-CMD-409"]);
            AngleSharp.Dom.IElement action = cut.Find("[data-testid='fc-rejection-copy-reference']");
            action.TextContent.Trim().ShouldBe("Support reference copied");
            action.GetAttribute("data-fc-copy-outcome").ShouldBe("copied");
        });
    }

    [Fact]
    public void CopySupportReferenceReportsARefusedCopyAndResetsOnTheNextRejection() {
        BunitJSModuleInterop clipboard = JSInterop.SetupModule(ClipboardModulePath);
        _ = clipboard.Setup<string>("copyToClipboard", _ => true).SetResult("Denied");
        (IRenderedComponent<FcLifecycleWrapper> cut, Action<CommandLifecycleTransition> push) = RenderWrapperWithLiveService(
            rejectionMessage: "Rejected.",
            rejectionDetails: SupportDetails);
        push(RejectedNow());

        cut.Find("[data-testid='fc-rejection-copy-reference']").Click();

        cut.WaitForAssertion(() => {
            AngleSharp.Dom.IElement action = cut.Find("[data-testid='fc-rejection-copy-reference']");
            action.TextContent.Trim().ShouldBe("Copy failed. Use the codes shown.");
            action.GetAttribute("data-fc-copy-outcome").ShouldBe("failed");
        });

        push(TransitionAt(CommandLifecycleState.Idle, CommandLifecycleState.Submitting, FakeTime.GetUtcNow()));
        push(RejectedNow());

        AngleSharp.Dom.IElement reset = cut.Find("[data-testid='fc-rejection-copy-reference']");
        reset.TextContent.Trim().ShouldBe("Copy support reference");
        reset.GetAttribute("data-fc-copy-outcome").ShouldBe("none");
    }

    private const string ClipboardModulePath = "./_content/Hexalith.FrontComposer.Shell/js/fc-devmode-clipboard.js";

    private static readonly CommandRejectionDetails SupportDetails = new(
        ErrorCode: "ORDER_LOCKED",
        ReasonCategory: "Concurrency",
        SuggestedAction: "Reload before retrying",
        DocsCode: "FC-CMD-409");

    private CommandLifecycleTransition RejectedNow()
        => TransitionAt(CommandLifecycleState.Syncing, CommandLifecycleState.Rejected, FakeTime.GetUtcNow());
}
