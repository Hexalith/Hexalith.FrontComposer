using Bunit;

using Hexalith.FrontComposer.Shell.Components.Forms;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Forms;

/// <summary>
/// Story 2-5 Task 8.1 — component-level bUnit coverage for <see cref="FcDestructiveConfirmationDialog"/>
/// (AC3 / D11 / D12 / D22). IDialogService-portal integration is covered by Story 10-2 (G11).
/// </summary>
public sealed class FcDestructiveConfirmationDialogTests : BunitContext {
    public FcDestructiveConfirmationDialogTests() {
        JSInterop.Mode = JSRuntimeMode.Loose;
        _ = Services.AddFluentUIComponents();
        _ = Services.AddLogging();
    }

    [Fact]
    public void Renders_title_and_body_text() {
        IRenderedComponent<FcDestructiveConfirmationDialog> cut = Render<FcDestructiveConfirmationDialog>(p => p
            .Add(c => c.Title, "Delete Order?")
            .Add(c => c.Body, "This action cannot be undone.")
            .Add(c => c.DestructiveLabel, "Delete Order"));

        cut.Markup.ShouldContain("Delete Order?");
        cut.Markup.ShouldContain("This action cannot be undone.");
        cut.Markup.ShouldContain("Delete Order");
    }

    [Fact]
    public void Cancel_button_carries_autofocus_affordance() {
        IRenderedComponent<FcDestructiveConfirmationDialog> cut = Render<FcDestructiveConfirmationDialog>(p => p
            .Add(c => c.Title, "Delete?")
            .Add(c => c.Body, "Gone forever.")
            .Add(c => c.DestructiveLabel, "Delete"));

        // AutoFocus="true" emits the autofocus attribute on the rendered button element.
        AngleSharp.Dom.IElement cancel = cut.Find("[data-testid='fc-destructive-cancel']");
        cancel.HasAttribute("autofocus").ShouldBeTrue("Cancel button must auto-focus so Enter does the safe thing (D11).");
    }

    [Fact]
    public async Task Cancel_click_invokes_OnCancel_delegate() {
        bool cancelled = false;
        IRenderedComponent<FcDestructiveConfirmationDialog> cut = Render<FcDestructiveConfirmationDialog>(p => p
            .Add(c => c.Title, "Delete?")
            .Add(c => c.Body, "Gone forever.")
            .Add(c => c.DestructiveLabel, "Delete")
            .Add(c => c.OnCancel, EventCallback.Factory.Create(this, () => cancelled = true)));

        await cut.InvokeAsync(() => cut.Find("[data-testid='fc-destructive-cancel']").Click());

        cancelled.ShouldBeTrue();
    }

    [Fact]
    public async Task Confirm_click_invokes_OnConfirm_delegate() {
        bool confirmed = false;
        IRenderedComponent<FcDestructiveConfirmationDialog> cut = Render<FcDestructiveConfirmationDialog>(p => p
            .Add(c => c.Title, "Delete?")
            .Add(c => c.Body, "Gone forever.")
            .Add(c => c.DestructiveLabel, "Delete")
            .Add(c => c.OnConfirm, EventCallback.Factory.Create(this, () => confirmed = true)));

        await cut.InvokeAsync(() => cut.Find("[data-testid='fc-destructive-confirm']").Click());

        confirmed.ShouldBeTrue();
    }

    [Fact]
    public async Task Escape_key_dispatches_OnCancel_not_OnConfirm() {
        bool cancelled = false;
        bool confirmed = false;
        IRenderedComponent<FcDestructiveConfirmationDialog> cut = Render<FcDestructiveConfirmationDialog>(p => p
            .Add(c => c.Title, "Delete?")
            .Add(c => c.Body, "Gone forever.")
            .Add(c => c.DestructiveLabel, "Delete")
            .Add(c => c.OnCancel, EventCallback.Factory.Create(this, () => cancelled = true))
            .Add(c => c.OnConfirm, EventCallback.Factory.Create(this, () => confirmed = true)));

        await cut.InvokeAsync(() => cut.Find("[data-testid='fc-destructive-dialog']").KeyDown(new KeyboardEventArgs { Key = "Escape" }));

        cancelled.ShouldBeTrue("D22 defense-in-depth: Escape must invoke OnCancel.");
        confirmed.ShouldBeFalse();
    }

    [Fact]
    public void Destructive_button_has_danger_class() {
        IRenderedComponent<FcDestructiveConfirmationDialog> cut = Render<FcDestructiveConfirmationDialog>(p => p
            .Add(c => c.Title, "Delete?")
            .Add(c => c.Body, "Gone forever.")
            .Add(c => c.DestructiveLabel, "Delete"));

        AngleSharp.Dom.IElement confirm = cut.Find("[data-testid='fc-destructive-confirm']");
        confirm.ClassList.Contains("fc-destructive-confirm").ShouldBeTrue("Destructive button needs the red-palette CSS hook (D11).");
    }
}
